using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TerrainCocodriloGenerator : MonoBehaviour
{
    [Header("Terreno")]
    public float width = 1f;
    public float length = 1f;

    [Header("Cuadrícula de Grietas")]
    public int gridSizeX = 10;
    public int gridSizeZ = 10;

    [Tooltip("Ancho de la grieta como porcentaje del tamaño del terreno (0.001 = 0.1%).")]
    [Range(0.0001f, 0.1f)]
    public float anchoGrietaPorcentaje = 0.001f; // 0.1%

    [Header("Longitud Aleatoria de Grietas")]
    [Range(0f, 1f)] public float minCrackLength = 0.3f;
    [Range(0f, 1f)] public float maxCrackLength = 1f;
    public int randomSeed = 42;

    [Header("Profundidad")]
    public float profundidadGrieta = -0.025f;

    [Header("Bordes")]
    [Tooltip("Borde sin grietas como porcentaje del tamaño del terreno (0 = sin borde, 0.5 = 50%).")]
    [Range(0f, 0.5f)]
    public float bordePorcentaje = 0.05f; // 5%

    [Header("Rendimiento")]
    public int maxVertexCount = 60000;
    [Range(0, 100)] public float polygonBudget = 100f;

    public Material material;
    private GameObject meshObject;

    [System.Serializable]
    private class CrackSegment
    {
        public float fixedCoord;
        public float start;
        public float end;
    }

    [ContextMenu("Generar")]
    public void GenerarMeshEnNuevoObjeto()
    {
        if (meshObject != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(meshObject);
#else
            Destroy(meshObject);
#endif
        }

        // === Resolución ===
        float effectiveMaxVerts = Mathf.Max(100, maxVertexCount * (polygonBudget / 100f));
        float maxDim = Mathf.Sqrt(effectiveMaxVerts) - 1;
        int maxRes = Mathf.Max(10, (int)maxDim);
        int resX = Mathf.Clamp(Mathf.RoundToInt(200 * (polygonBudget / 100f)), 10, maxRes);
        int resZ = Mathf.Clamp(Mathf.RoundToInt(200 * (polygonBudget / 100f)), 10, maxRes);
        if ((resX + 1) * (resZ + 1) > effectiveMaxVerts)
        {
            float ratio = width / length;
            float total = Mathf.Sqrt(effectiveMaxVerts);
            resX = Mathf.Max(10, (int)(total * Mathf.Sqrt(ratio)));
            resZ = Mathf.Max(10, (int)(total / Mathf.Sqrt(ratio)));
        }

#if UNITY_EDITOR
        int finalVertCount = (resX + 1) * (resZ + 1);
        if (finalVertCount > maxVertexCount)
        {
            Debug.LogWarning($"[TerrainCocodrilo] Resolución ajustada a {resX}x{resZ} ({finalVertCount} vértices).", this);
        }
#endif

        // === Calcular parámetros en metros ===
        float bordeAnchoX = width * bordePorcentaje;
        float bordeAnchoZ = length * bordePorcentaje;
        float sizeRef = Mathf.Min(width, length); // Para escalar el ancho de grieta
        float anchoGrietaReal = sizeRef * anchoGrietaPorcentaje;

        // === Generar grietas aleatorias ===
        System.Random rand = new System.Random(randomSeed);
        List<CrackSegment> verticalCracks = new List<CrackSegment>();
        List<CrackSegment> horizontalCracks = new List<CrackSegment>();

        float interiorW = width - 2f * bordeAnchoX;
        float interiorL = length - 2f * bordeAnchoZ;
        float startX = bordeAnchoX;
        float startZ = bordeAnchoZ;

        if (gridSizeX > 1)
        {
            for (int i = 1; i < gridSizeX; i++)
            {
                float x = startX + (float)i / gridSizeX * interiorW;
                float lenFrac = Mathf.Lerp(minCrackLength, maxCrackLength, (float)rand.NextDouble());
                float usableLen = interiorL * lenFrac;
                float offset = (interiorL - usableLen) * (float)rand.NextDouble();
                verticalCracks.Add(new CrackSegment { fixedCoord = x, start = startZ + offset, end = startZ + offset + usableLen });
            }
        }

        if (gridSizeZ > 1)
        {
            for (int j = 1; j < gridSizeZ; j++)
            {
                float z = startZ + (float)j / gridSizeZ * interiorL;
                float lenFrac = Mathf.Lerp(minCrackLength, maxCrackLength, (float)rand.NextDouble());
                float usableLen = interiorW * lenFrac;
                float offset = (interiorW - usableLen) * (float)rand.NextDouble();
                horizontalCracks.Add(new CrackSegment { fixedCoord = z, start = startX + offset, end = startX + offset + usableLen });
            }
        }

        // === Crear objeto ===
        meshObject = new GameObject("TerrenoConGrietas");
        meshObject.transform.SetParent(transform, false);
        var mf = meshObject.AddComponent<MeshFilter>();
        var mr = meshObject.AddComponent<MeshRenderer>();
        if (material != null) mr.sharedMaterial = material;

        // === Vértices con remapeo para ancho métrico ===
        Vector3[] vertices = new Vector3[(resX + 1) * (resZ + 1)];
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        for (int z = 0; z <= resZ; z++)
        {
            float tZ = (float)z / resZ;
            float worldZ = RemapWithCrackDensity(tZ, 0f, length, horizontalCracks, anchoGrietaReal, bordeAnchoZ, length - bordeAnchoZ, false);

            for (int x = 0; x <= resX; x++)
            {
                float tX = (float)x / resX;
                float worldX = RemapWithCrackDensity(tX, 0f, width, verticalCracks, anchoGrietaReal, bordeAnchoX, width - bordeAnchoX, true);

                float y = 0f;

                if (worldX >= bordeAnchoX && worldX <= (width - bordeAnchoX) &&
                    worldZ >= bordeAnchoZ && worldZ <= (length - bordeAnchoZ))
                {
                    bool inCrack = false;

                    foreach (var crack in verticalCracks)
                    {
                        if (Mathf.Abs(worldX - crack.fixedCoord) <= anchoGrietaReal * 0.5f)
                        {
                            if (worldZ >= crack.start && worldZ <= crack.end)
                            {
                                inCrack = true;
                                break;
                            }
                        }
                    }

                    if (!inCrack)
                    {
                        foreach (var crack in horizontalCracks)
                        {
                            if (Mathf.Abs(worldZ - crack.fixedCoord) <= anchoGrietaReal * 0.5f)
                            {
                                if (worldX >= crack.start && worldX <= crack.end)
                                {
                                    inCrack = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (inCrack)
                    {
                        y = profundidadGrieta;
                    }
                }

                vertices[z * (resX + 1) + x] = new Vector3(worldX - halfW, y, worldZ - halfL);
            }
        }

        // === Triangulación ===
        int[] triangles = new int[resX * resZ * 6];
        int idx = 0;
        for (int row = 0; row < resZ; row++)
        {
            for (int col = 0; col < resX; col++)
            {
                int v0 = row * (resX + 1) + col;
                int v1 = v0 + 1;
                int v2 = (row + 1) * (resX + 1) + col;
                int v3 = v2 + 1;
                triangles[idx++] = v0; triangles[idx++] = v2; triangles[idx++] = v1;
                triangles[idx++] = v1; triangles[idx++] = v2; triangles[idx++] = v3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;
    }

    private float RemapWithCrackDensity(float t, float min, float max, List<CrackSegment> cracks, float crackWidth, float zoneMin, float zoneMax, bool isVertical)
    {
        float linear = Mathf.Lerp(min, max, t);
        if (cracks.Count == 0 || linear < zoneMin || linear > zoneMax)
            return linear;

        float totalPull = 0f;
        float totalWeight = 0f;
        float influenceRange = Mathf.Max(crackWidth * 3f, (max - min) * 0.05f);

        foreach (var crack in cracks)
        {
            float dist = Mathf.Abs(linear - crack.fixedCoord);
            if (dist < influenceRange)
            {
                float relDist = dist / influenceRange;
                float weight = 1f - relDist * relDist;
                totalPull += (crack.fixedCoord - linear) * weight;
                totalWeight += weight;
            }
        }

        if (totalWeight > 0f)
        {
            float pull = totalPull / totalWeight;
            pull = Mathf.Clamp(pull, -influenceRange * 0.5f, influenceRange * 0.5f);
            return linear + pull;
        }

        return linear;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TerrainCocodriloGenerator))]
    public class Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Generar Terreno"))
            {
                ((TerrainCocodriloGenerator)target).GenerarMeshEnNuevoObjeto();
            }
        }
    }
#endif
}