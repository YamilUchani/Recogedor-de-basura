using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class VoronoiBacheGenerator : MonoBehaviour
{
    [Header("Opción")]
    public bool randomizeOnGenerate = true;

    [Header("Terreno")]
    [Range(0.25f, 1f)] public float width = 0.5f;
    [Range(0.25f, 1f)] public float length = 0.5f;

    [Header("Configuración Voronoi (Escala y Bordes Hundidos)")]
    [Range(8, 20)] public int numSites = 10;

    [Tooltip("Controla simultáneamente el tamaño de las celdas y el ancho relativo de los bordes.")]
    [Range(0f,1f)] public float escalaCelda = 0.035f;

    [Tooltip("La profundidad final de los bordes. Usa un valor negativo para hundirlos.")]
    public float borderDepth = -0.5f; // ❗ Nunca se aleatoriza

    [Tooltip("Controla cuánto se redondean las esquinas de las celdas. Valores bajos = más redondeo, altos = más angulosos.")]
    [Range(0f, 2f)] public float roundness = 0.4f;

    public int randomSeed = 42;

    [Header("Borde Exterior")]
    [Tooltip("Mínimo margen de borde como % del tamaño del terreno")]
    [Range(10f, 20f)] public float bordeMinPerc = 12f;

    [Tooltip("Máximo margen de borde como % del tamaño del terreno")]
    [Range(25f, 30f)] public float bordeMaxPerc = 27f;

    public float noiseScale = 2f;
    [Range(0f, 50f)] public float bordeSuavidad = 3f;

    [Header("Rendimiento")]
    [Range(50, 200)] public int resolution = 100;

    [Header("Textura")]
    [Tooltip("Escala de las coordenadas UV para controlar la repetición de la textura")]
    public float uvScale = 1.0f;

    public Material material;

    private GameObject meshObject;

    [ContextMenu("Generar Terreno Voronoi Plano (Bordes Hundidos)")]
    public void GenerarMeshVoronoi()
    {
        if (randomizeOnGenerate)
        {
            RandomizeParameters();
        }

        if (meshObject != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(meshObject);
#else
            Destroy(meshObject);
#endif
        }

        meshObject = new GameObject("TerrenoVoronoiPlanoHundido");
        meshObject.transform.SetParent(transform, false);
        var mf = meshObject.AddComponent<MeshFilter>();
        var mr = meshObject.AddComponent<MeshRenderer>();
        if (material != null) mr.sharedMaterial = material;

        Random.InitState(randomSeed);
        List<Vector2> voronoiSites = new List<Vector2>();
        for (int i = 0; i < numSites; i++)
        {
            voronoiSites.Add(new Vector2(Random.value * width, Random.value * length));
        }

        float bordeMinMetros = Mathf.Min(width, length) * (bordeMinPerc * 0.01f);
        float bordeMaxMetros = Mathf.Min(width, length) * (bordeMaxPerc * 0.01f);

        int numVertsX = resolution + 1;
        int numVertsZ = resolution + 1;
        Vector3[] vertices = new Vector3[numVertsX * numVertsZ];
        Vector2[] uvs = new Vector2[numVertsX * numVertsZ]; // ✅ NUEVO: Array de UVs
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        float[] bordeIzq = new float[numVertsZ];
        float[] bordeDer = new float[numVertsZ];
        float[] bordeDetras = new float[numVertsX];
        float[] bordeFrente = new float[numVertsX];

        for (int z = 0; z < numVertsZ; z++)
        {
            float t = (float)z / (numVertsZ - 1);
            bordeIzq[z] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(0.1f, t * noiseScale));
            bordeDer[z] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(10.73f, t * noiseScale));
        }
        for (int x = 0; x < numVertsX; x++)
        {
            float t = (float)x / (numVertsX - 1);
            bordeDetras[x] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(t * noiseScale, 0.3f));
            bordeFrente[x] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(t * noiseScale, 15.41f));
        }

        for (int z = 0; z < numVertsZ; z++)
        {
            float localZ = (float)z / resolution * length;
            float worldZ = localZ - halfL;
            for (int x = 0; x < numVertsX; x++)
            {
                float localX = (float)x / resolution * width;
                float worldX = localX - halfW;

                float dLeft = localX - bordeIzq[z];
                float dRight = (width - bordeDer[z]) - localX;
                float dBack = localZ - bordeDetras[x];
                float dFront = (length - bordeFrente[x]) - localZ;
                float minDistToBorder = Mathf.Min(dLeft, dRight, dBack, dFront);

                float bordeFactor = 0f;
                if (minDistToBorder > 0f)
                {
                    float t = Mathf.Clamp01(minDistToBorder / (Mathf.Max(width, length) * 0.5f) * bordeSuavidad);
                    bordeFactor = 1f - (1f - t) * (1f - t);
                }

                float height = CalculateVoronoiSteppedHeight(new Vector2(localX, localZ), voronoiSites);
                float finalHeight = height * bordeFactor;

                int vertexIndex = z * numVertsX + x;
                vertices[vertexIndex] = new Vector3(worldX, finalHeight, worldZ);
                
                // ✅ NUEVO: Asignar coordenadas UV basadas en la posición mundial
                uvs[vertexIndex] = new Vector2(worldX * uvScale, worldZ * uvScale);
            }
        }

        int[] triangles = new int[resolution * resolution * 6];
        int idx = 0;
        for (int row = 0; row < resolution; row++)
        {
            for (int col = 0; col < resolution; col++)
            {
                int v0 = row * numVertsX + col;
                int v1 = v0 + 1;
                int v2 = (row + 1) * numVertsX + col;
                int v3 = v2 + 1;

                triangles[idx++] = v0; triangles[idx++] = v2; triangles[idx++] = v1;
                triangles[idx++] = v1; triangles[idx++] = v2; triangles[idx++] = v3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs; // ✅ NUEVO: Asignar UVs al mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // ✅ OPCIONAL: Calcular tangentes para normal maps
        if (material != null && material.HasProperty("_BumpMap"))
        {
            mesh.tangents = CalculateTangents(vertices, uvs, triangles, mesh.normals);
        }
        
        mf.mesh = mesh;
    }

    private void RandomizeParameters()
    {
        System.Random sysRand = new System.Random();

        // ✅ Nuevos: width y length entre 0.25 y 1
        width = Mathf.Lerp(0.25f, 1f, (float)sysRand.NextDouble());
        length = Mathf.Lerp(0.25f, 1f, (float)sysRand.NextDouble());

        numSites = sysRand.Next(16, 40); // inclusive 8–20

        escalaCelda = Mathf.Lerp(0.03f, 0.04f, (float)sysRand.NextDouble());
        // ❗ borderDepth: no se toca

        roundness = Mathf.Lerp(0.3f, 0.5f, (float)sysRand.NextDouble());

        bordeMinPerc = Mathf.Lerp(10f, 20f, (float)sysRand.NextDouble());
        bordeMaxPerc = Mathf.Lerp(25f, 30f, (float)sysRand.NextDouble());

        noiseScale = 0.5f + (float)sysRand.NextDouble() * 4.5f; // 0.5 → 5.0
        bordeSuavidad = Mathf.Lerp(2f, 5f, (float)sysRand.NextDouble());

        resolution = Mathf.RoundToInt(Mathf.Lerp(50f, 200f, (float)sysRand.NextDouble()));

        // ✅ Nuevo: Aleatorizar escala UV
        uvScale = Mathf.Lerp(0.5f, 3.0f, (float)sysRand.NextDouble());

        randomSeed = sysRand.Next();
    }

    private float CalculateVoronoiSteppedHeight(Vector2 point, List<Vector2> sites)
    {
        float f1 = float.MaxValue;
        float f2 = float.MaxValue;
        Vector2 scaledPoint = point / escalaCelda;

        foreach (var site in sites)
        {
            float dist = Vector2.Distance(scaledPoint, site / escalaCelda);
            if (dist < f1)
            {
                f2 = f1;
                f1 = dist;
            }
            else if (dist < f2)
            {
                f2 = dist;
            }
        }

        float borderThickness = roundness;
        float ridgeDistance = f2 - f1;
        float t = Mathf.Clamp01(ridgeDistance / borderThickness);
        float blend = Mathf.SmoothStep(0f, 1f, t);
        return Mathf.Lerp(0f, borderDepth, 1f - blend);
    }

    // ✅ CORREGIDO: Método para calcular tangentes (necesario para normal maps)
    private Vector4[] CalculateTangents(Vector3[] vertices, Vector2[] uvs, int[] triangles, Vector3[] normals)
    {
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector3[] tan1 = new Vector3[vertices.Length];
        Vector3[] tan2 = new Vector3[vertices.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            Vector2 w1 = uvs[i1];
            Vector2 w2 = uvs[i2];
            Vector2 w3 = uvs[i3];

            float x1 = v2.x - v1.x;
            float x2 = v3.x - v1.x;
            float y1 = v2.y - v1.y;
            float y2 = v3.y - v1.y;
            float z1 = v2.z - v1.z;
            float z2 = v3.z - v1.z;

            float s1 = w2.x - w1.x;
            float s2 = w3.x - w1.x;
            float t1 = w2.y - w1.y;
            float t2 = w3.y - w1.y;

            float div = s1 * t2 - s2 * t1;
            float r = div == 0.0f ? 0.0f : 1.0f / div;

            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;

            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 n = normals[i]; // ✅ CORREGIDO: Usar el array de normals pasado como parámetro
            Vector3 t = tan1[i];

            Vector3.OrthoNormalize(ref n, ref t);
            tangents[i].x = t.x;
            tangents[i].y = t.y;
            tangents[i].z = t.z;
            tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
        }

        return tangents;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VoronoiBacheGenerator))]
    public class Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Generar Terreno Voronoi Plano (Bordes Hundidos)"))
            {
                ((VoronoiBacheGenerator)target).GenerarMeshVoronoi();
            }
        }
    }
#endif
}