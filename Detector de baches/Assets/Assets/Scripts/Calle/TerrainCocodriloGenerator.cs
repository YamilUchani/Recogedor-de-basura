using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generador de Bache Cocodrilo (Standalone).
/// </summary>
[ExecuteInEditMode]
public class TerrainCocodriloGenerator : MonoBehaviour
{
    // -------------------- Parámetros --------------------
    [Header("Seed Control")]
    public int Seed = 42;

    [Header("Dimension Ranges")]
    public float minWidth = 1.5f; public float maxWidth = 2.5f;
    public float minLength = 2.5f; public float maxLength = 3.5f;

    [Header("Detalle del Cocodrilo")]
    [Range(1, 50)] public int minCrackCount = 10;
    [Range(1, 50)] public int maxCrackCount = 20;

    [Tooltip("Ancho de las grietas (%)")]
    [Range(0.1f, 5f)] public float minCrackGapPercent = 0.3f;
    [Range(0.1f, 5f)] public float maxCrackGapPercent = 0.8f;

    [Tooltip("Profundidad de las grietas (grosor del asfalto).")]
    public float crackDepth = 0.05f;
    
    [Tooltip("Límite del borde (%)")]
    [Range(0f, 45f)] public float borderLimitMin = 5f;
    [Range(0f, 45f)] public float borderLimitMax = 15f;

    public Material material;
    public bool autoUpdate = true;

    private void OnValidate()
    {
        if (autoUpdate)
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () => 
            { 
                if (this != null) 
                    GenerateIntegratedMesh();
            };
#endif
        }
    }

    [ContextMenu("Generar Malla")]
    public void GenerateIntegratedMesh()
    {
        // Init Seed
        Random.InitState(Seed);

        // Remove existing child mesh object if present.
        Transform existing = transform.Find("CocodriloMesh");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        // Create a new child GameObject for the SINGLE mesh
        GameObject meshObj = new GameObject("CocodriloMesh");
        meshObj.transform.SetParent(this.transform, false);
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.layer = 7; 

        MeshFilter childMF = meshObj.AddComponent<MeshFilter>();
        MeshRenderer childMR = meshObj.AddComponent<MeshRenderer>();
        MeshCollider childMC = meshObj.AddComponent<MeshCollider>();
        
        if (material != null) childMR.sharedMaterial = material;
        
        // --- SINGLE PATCH GENERATION ---
        float pW = Random.Range(minWidth, maxWidth);
        float pL = Random.Range(minLength, maxLength);
        int crackC = Random.Range(minCrackCount, maxCrackCount + 1);
        float crackGap = Random.Range(minCrackGapPercent, maxCrackGapPercent);

        Mesh finalMesh = GenerateSinglePatch(pW, pL, crackC, crackGap);
        finalMesh.name = "Alligator_Single_v1";
        
        finalMesh.RecalculateNormals();
        finalMesh.RecalculateBounds();
        finalMesh.RecalculateTangents();

        childMF.sharedMesh = finalMesh;
        childMC.sharedMesh = finalMesh;
    }

    private Mesh GenerateSinglePatch(float width, float length, int crackCount, float crackGapPercent)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        int segCount = Mathf.Max(4, Mathf.CeilToInt(Mathf.Max(width, length) * 4));
        List<Vector3> outerRing = GenerateOuterRingPath(width, length, segCount);

        AddFloorGeometry(verts, uvs, tris, outerRing, width, length);

        if (crackCount > 0)
        {
            GenerateVoronoiChunks(verts, uvs, tris, outerRing, width, length, crackCount, crackGapPercent);
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }

    private void AddFloorGeometry(List<Vector3> verts, List<Vector2> uvs, List<int> tris, List<Vector3> outer, float w, float l)
    {
        float yBot = -crackDepth;
        int idxStart = verts.Count;
        foreach (var p in outer)
        {
            verts.Add(new Vector3(p.x, yBot, p.z));
            uvs.Add(GetUV(p, w, l));
        }

        int idxCenter = verts.Count;
        Vector3 centerPos = Vector3.zero;
        foreach (var p in outer) centerPos += p;
        centerPos /= outer.Count;
        centerPos.y = yBot;
        verts.Add(centerPos);
        uvs.Add(GetUV(centerPos, w, l));

        for (int i = 0; i < outer.Count; i++)
        {
            int next = (i + 1) % outer.Count;
            tris.Add(idxCenter);
            tris.Add(idxStart + i);
            tris.Add(idxStart + next);
        }
    }

    private void GenerateVoronoiChunks(List<Vector3> verts, List<Vector2> uvs, List<int> tris, List<Vector3> boundary, float w, float l, int count, float gapPercent)
    {
        List<Vector2> seeds = new List<Vector2>();
        Rect bounds = GetBounds(boundary);
        int safety = 0;
        
        while (seeds.Count < count && safety < 2000)
        {
            safety++;
            Vector2 p = new Vector2(
                Random.Range(bounds.xMin, bounds.xMax),
                Random.Range(bounds.yMin, bounds.yMax)
            );
            if (IsPointInPolygon(p, boundary)) seeds.Add(p);
        }

        float borderLimit = Mathf.Lerp(
            Mathf.Min(w, l) * borderLimitMin / 100f,
            Mathf.Min(w, l) * borderLimitMax / 100f,
            Random.value
        );

        List<Vector2> basePoly = ConvertTo2D(boundary);
        List<bool> baseFlags = new List<bool>(basePoly.Count);
        for (int i = 0; i < basePoly.Count; i++) baseFlags.Add(true);

        for (int i = 0; i < seeds.Count; i++)
        {
            List<Vector2> cellPoly = new List<Vector2>(basePoly);
            List<bool> cellFlags = new List<bool>(baseFlags);
            Vector2 site = seeds[i];

            for (int j = 0; j < seeds.Count; j++)
            {
                if (i == j) continue;
                Vector2 neighbor = seeds[j];
                Vector2 mid = (site + neighbor) * 0.5f;
                Vector2 normal = (neighbor - site).normalized;
                ClipPolygon(ref cellPoly, ref cellFlags, mid, normal);
                if (cellPoly.Count < 3) break;
            }

            if (cellPoly.Count < 3) continue;

            Vector2 center = Vector2.zero;
            foreach (var p in cellPoly) center += p;
            center /= cellPoly.Count;

            float distToLeft = Mathf.Abs(center.x - (-w / 2f));
            float distToRight = Mathf.Abs(center.x - (w / 2f));
            float distToBottom = Mathf.Abs(center.y - (-l / 2f));
            float distToTop = Mathf.Abs(center.y - (l / 2f));
            float minDistToBorder = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);

            bool isBorder = minDistToBorder < borderLimit;

            float gapSize = Mathf.Min(w, l) * gapPercent / 100f;
            if (!isBorder && gapSize > 0.0001f)
            {
                cellPoly = InsetPolygonSelective(cellPoly, cellFlags, gapSize * 0.5f);
                if (cellPoly.Count < 3) continue;
            }

            AddChunkGeometry(verts, uvs, tris, cellPoly, -crackDepth, crackDepth, w, l);
        }
    }

    private void AddChunkGeometry(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
        List<Vector2> poly, float substrateY, float height, float w, float l)
    {
        float yBot = substrateY;
        float yTop = substrateY + height;
        int idxTop = verts.Count;

        Vector2 center = Vector2.zero;
        foreach (var p in poly) center += p;
        center /= poly.Count;
        verts.Add(new Vector3(center.x, yTop, center.y));
        uvs.Add(GetUV(verts[verts.Count - 1], w, l));

        for (int i = 0; i < poly.Count; i++)
        {
            verts.Add(new Vector3(poly[i].x, yTop, poly[i].y));
            uvs.Add(GetUV(verts[verts.Count - 1], w, l));
        }

        for (int i = 0; i < poly.Count; i++)
        {
            tris.Add(idxTop);
            tris.Add(idxTop + 1 + i);
            tris.Add(idxTop + 1 + ((i + 1) % poly.Count));
        }

        int idxWallStart = verts.Count;
        for (int i = 0; i < poly.Count; i++)
        {
            verts.Add(new Vector3(poly[i].x, yTop, poly[i].y));
            verts.Add(new Vector3(poly[i].x, yBot, poly[i].y));
            uvs.Add(GetUV(verts[verts.Count - 2], w, l));
            uvs.Add(GetUV(verts[verts.Count - 1], w, l));
        }

        for (int i = 0; i < poly.Count; i++)
        {
            int curr = i * 2;
            int next = ((i + 1) % poly.Count) * 2;
            int tl = idxWallStart + curr;
            int bl = idxWallStart + curr + 1;
            int tr = idxWallStart + next;
            int br = idxWallStart + next + 1;

            tris.Add(tl); tris.Add(br); tris.Add(tr);
            tris.Add(tl); tris.Add(bl); tris.Add(br);
        }
    }

    // -------------------- Utilidades --------------------
    private List<Vector3> GenerateOuterRingPath(float w, float l, int segPerSide)
    {
        List<Vector3> path = new List<Vector3>();
        float hw = w / 2f;
        float hl = l / 2f;
        AddLine(path, new Vector3(-hw, 0, -hl), new Vector3(-hw, 0, hl), segPerSide);
        AddLine(path, new Vector3(-hw, 0, hl), new Vector3(hw, 0, hl), segPerSide);
        AddLine(path, new Vector3(hw, 0, hl), new Vector3(hw, 0, -hl), segPerSide);
        AddLine(path, new Vector3(hw, 0, -hl), new Vector3(-hw, 0, -hl), segPerSide);
        return path;
    }

    private void AddLine(List<Vector3> list, Vector3 start, Vector3 end, int segments)
    {
        for (int i = 0; i < segments; i++)
            list.Add(Vector3.Lerp(start, end, (float)i / segments));
    }

    private List<Vector2> ConvertTo2D(List<Vector3> v3)
    {
        List<Vector2> v2 = new List<Vector2>(v3.Count);
        foreach (var v in v3) v2.Add(new Vector2(v.x, v.z));
        return v2;
    }

    private Rect GetBounds(List<Vector3> poly)
    {
        if (poly.Count == 0) return new Rect();
        float xMin = poly[0].x, xMax = poly[0].x, zMin = poly[0].z, zMax = poly[0].z;
        foreach (var p in poly)
        {
            if (p.x < xMin) xMin = p.x;
            if (p.x > xMax) xMax = p.x;
            if (p.z < zMin) zMin = p.z;
            if (p.z > zMax) zMax = p.z;
        }
        return new Rect(xMin, zMin, xMax - xMin, zMax - zMin);
    }

    private bool IsPointInPolygon(Vector2 p, List<Vector3> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if (((poly[i].z > p.y) != (poly[j].z > p.y)) &&
                 (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }

    private void ClipPolygon(ref List<Vector2> poly, ref List<bool> flags, Vector2 planeP, Vector2 planeN)
    {
        List<Vector2> newPoly = new List<Vector2>();
        List<bool> newFlags = new List<bool>();
        if (poly.Count == 0) return;
        int count = poly.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2 S = poly[i];
            Vector2 E = poly[(i + 1) % count];
            bool SFlag = flags[i];
            bool EFlag = flags[(i + 1) % count];
            bool SIn = Vector2.Dot(planeN, S - planeP) < 0;
            bool EIn = Vector2.Dot(planeN, E - planeP) < 0;
            if (SIn)
            {
                if (EIn) { newPoly.Add(E); newFlags.Add(EFlag); }
                else { newPoly.Add(Intersect(S, E, planeP, planeN)); newFlags.Add(false); }
            }
            else
            {
                if (EIn) { newPoly.Add(Intersect(S, E, planeP, planeN)); newFlags.Add(false); newPoly.Add(E); newFlags.Add(EFlag); }
            }
        }
        poly = newPoly;
        flags = newFlags;
    }

    private Vector2 Intersect(Vector2 A, Vector2 B, Vector2 planeP, Vector2 planeN)
    {
        Vector2 dir = B - A;
        float t = Vector2.Dot(planeN, planeP - A) / Vector2.Dot(planeN, dir);
        return A + dir * t;
    }

    private List<Vector2> InsetPolygonSelective(List<Vector2> poly, List<bool> flags, float amount)
    {
        Vector2 centroid = Vector2.zero;
        foreach (var p in poly) centroid += p;
        centroid /= poly.Count;
        List<Vector2> result = new List<Vector2>();
        for (int i = 0; i < poly.Count; i++)
        {
            if (flags[i]) result.Add(poly[i]);
            else
            {
                float dist = Vector2.Distance(centroid, poly[i]);
                if (dist > amount) result.Add(poly[i] + (centroid - poly[i]).normalized * amount);
                else result.Add(centroid);
            }
        }
        return result;
    }

    private Vector2 GetUV(Vector3 p, float w, float l) => new Vector2((p.x + w / 2f) / w, (p.z + l / 2f) / l);
}