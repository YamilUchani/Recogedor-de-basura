using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generador de Bache Cocodrilo - Enfoque Puro Voronoi (Geométrico).
/// </summary>
[ExecuteInEditMode]
public class TerrainCocodriloGenerator : MonoBehaviour
{
    [Header("General")]
    public int Seed = 42;
    public Material material;
    public bool autoUpdate = false;

    [Header("Dimensiones Aleatorias")]
    public float minWidth = 1.5f;
    public float maxWidth = 2.5f;
    public float minLength = 2.5f;
    public float maxLength = 3.5f;
    
    // Variables internas para generación (no editables)
    private float width;
    private float length;

    [Header("Calidad")]
    [Tooltip("Polígonos por eje. A mayor número, menos 'dientes de sierra', pero más costo.")]
    [Range(20, 200)] public int polygonsX = 120;
    [Range(20, 200)] public int polygonsZ = 120;

    [Header("Patrón Cocodrilo")]
    [Tooltip("Cantidad de bloques poligonales.")]
    [Range(10, 100)] public int cellCount = 40;
    
    [Tooltip("Ancho mínimo de las grietas (% del tamaño).")]
    [Range(0.1f, 2f)] public float minCrackWidthPercent = 0.5f;

    [Tooltip("Ancho máximo de las grietas (% del tamaño).")]
    [Range(0.5f, 5f)] public float maxCrackWidthPercent = 2.0f;
    
    [Tooltip("Profundidad de las grietas.")]
    [Range(0.01f, 0.08f)] public float crackDepth = 0.03f;
    
    [Tooltip("Suavidad del borde de la grieta.")]
    [Range(1f, 4f)] public float crackSmoothness = 2f;

    [Header("Variación Orgánica")]
    [Tooltip("Escala del ruido para variar el ancho (mayor = cambios más rápidos).")]
    [Range(0.1f, 10f)] public float widthNoiseScale = 2.0f;

    [Tooltip("Irregularidad de los bordes (dientes naturales).")]
    [Range(0f, 0.5f)] public float edgeIrregularity = 0.2f;

    [Tooltip("Escala del ruido de irregularidad (mayor = más detalle fino).")]
    [Range(5f, 50f)] public float irregularityScale = 20f;

    [Tooltip("Distorsión leve para que no sean líneas perfectas (0 = rectas).")]
    [Range(0f, 1f)] public float distortion = 0.1f;

    [Header("Bordes Orgánicos")]
    [Tooltip("Distancia mínima al borde sin grietas (% del tamaño).")]
    [Range(5f, 25f)] public float bordeMinPercent = 10f;
    [Tooltip("Distancia máxima al borde (% del tamaño).")]
    [Range(10f, 35f)] public float bordeMaxPercent = 18f;
    
    [Header("Estadísticas y Debug")]
    public bool showSeeds = false;
    [SerializeField] private int vertexCount;
    [SerializeField] private int triangleCount;
    
    private float minCrackWidth;
    private float maxCrackWidth;
    private float bordeMin;
    private float bordeMax;
    private List<Vector2> currentSeeds; // Para visualizar con Gizmos

    private void OnDrawGizmos()
    {
        if (!showSeeds || currentSeeds == null) return;
        
        Gizmos.color = Color.red;
        foreach (Vector2 seed in currentSeeds)
        {
            Vector3 worldPos = transform.TransformPoint(new Vector3(seed.x, 0.05f, seed.y));
            Gizmos.DrawSphere(worldPos, 0.05f); // "Cuentas" (Beads)
        }
    }

    private float CalculateVoronoiDepth(float x, float z, List<Vector2> seeds)
    {
        // 1. Distorsión de dominio para bordes irregulares ("dientes naturales")
        float warpX = 0f;
        float warpZ = 0f;
        
        if (edgeIrregularity > 0f)
        {
            warpX = (Mathf.PerlinNoise(x * irregularityScale, z * irregularityScale) - 0.5f) * edgeIrregularity;
            warpZ = (Mathf.PerlinNoise(x * irregularityScale + 15.3f, z * irregularityScale + 15.3f) - 0.5f) * edgeIrregularity;
            
            x += warpX;
            z += warpZ;
        }

        if (distortion > 0f)
        {
            float noiseX = Mathf.PerlinNoise(x * 0.5f + Seed, z * 0.5f);
            float noiseZ = Mathf.PerlinNoise(x * 0.5f, z * 0.5f + Seed);
            x += (noiseX - 0.5f) * distortion * 0.5f;
            z += (noiseZ - 0.5f) * distortion * 0.5f;
        }

        float d1 = float.MaxValue;
        float d2 = float.MaxValue;
        
        foreach (var seed in seeds)
        {
            float dx = x - seed.x;
            float dz = z - seed.y;
            float d = Mathf.Sqrt(dx*dx + dz*dz);

            if (d < d1)
            {
                d2 = d1;
                d1 = d;
            }
            else if (d < d2)
            {
                d2 = d;
            }
        }

        float distToEdge = d2 - d1;

        // 2. Variación del ancho de grieta usando Min/Max
        // Ruido de baja frecuencia para interpolar entre min y max
        float widthNoise = Mathf.PerlinNoise(x * widthNoiseScale + Seed, z * widthNoiseScale + Seed); // 0 a 1
        float currentCrackWidth = Mathf.Lerp(minCrackWidth, maxCrackWidth, widthNoise);

        if (distToEdge > currentCrackWidth)
        {
            return 0f;
        }

        float t = distToEdge / currentCrackWidth;
        
        float roughness = 0f;
        if (distToEdge < currentCrackWidth * 0.8f)
        {
             roughness = (Mathf.PerlinNoise(x * 30f, z * 30f) - 0.5f) * 0.2f * crackDepth;
        }

        float profile = 1f - Mathf.Pow(t, crackSmoothness);
        
        return (crackDepth * profile) + roughness;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!autoUpdate) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (this == null || this.gameObject == null) return;
        
        EditorApplication.delayCall -= DelayedGenerate;
        EditorApplication.delayCall += DelayedGenerate;
#endif
    }

#if UNITY_EDITOR
    private void DelayedGenerate()
    {
        if (this == null || this.gameObject == null) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        
        try { GenerateIntegratedMesh(); }
        catch (System.Exception e) { Debug.LogWarning($"TerrainCocodriloGenerator: {e.Message}"); }
    }
#endif

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Seed = Random.Range(0, 1000000);
            GenerateIntegratedMesh();
        }
    }

    [ContextMenu("Generar Malla")]
    public void GenerateIntegratedMesh()
    {
        Random.InitState(Seed);
        
        // Determinar dimensiones basadas en la semilla
        width = Random.Range(minWidth, maxWidth);
        length = Random.Range(minLength, maxLength);

        float avgSize = (width + length) * 0.5f;
        minCrackWidth = avgSize * (minCrackWidthPercent / 100f);
        maxCrackWidth = avgSize * (maxCrackWidthPercent / 100f);
        bordeMin = avgSize * (bordeMinPercent / 100f);
        bordeMax = avgSize * (bordeMaxPercent / 100f);

        Transform existing = transform.Find("CocodriloMesh");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        GameObject meshObj = new GameObject("CocodriloMesh");
        meshObj.transform.SetParent(this.transform, false);
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.layer = 7;

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
        MeshCollider mc = meshObj.AddComponent<MeshCollider>();
        
        if (material != null) mr.sharedMaterial = material;
        
        // Generar semillas Voronoi
        currentSeeds = new List<Vector2>();
        for (int i = 0; i < cellCount; i++)
        {
            currentSeeds.Add(new Vector2(
                Random.Range(-width * 0.6f, width * 0.6f),
                Random.Range(-length * 0.6f, length * 0.6f)
            ));
        }

        Mesh mesh = GenerateMesh(currentSeeds);
        mesh.name = "Proc_Croc_Voronoi";
        
        vertexCount = mesh.vertexCount;
        triangleCount = mesh.triangles.Length / 3;
        
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private Mesh GenerateMesh(List<Vector2> seeds)
    {
        Mesh mesh = new Mesh();
        
        float stepX = width / polygonsX;
        float stepZ = length / polygonsZ;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        // Precalcular bordes irregulares
        float noiseScale = 2f;
        float noiseOffX = Random.Range(0f, 100f);
        
        float[] bordeIzq = new float[polygonsZ + 1];
        float[] bordeDer = new float[polygonsZ + 1];
        float[] bordeDet = new float[polygonsX + 1];
        float[] bordeFre = new float[polygonsX + 1];

        for (int z = 0; z <= polygonsZ; z++)
        {
            float t = (float)z / polygonsZ;
            bordeIzq[z] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(0.1f + noiseOffX, t * noiseScale));
            bordeDer[z] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(10.73f + noiseOffX, t * noiseScale));
        }
        
        for (int x = 0; x <= polygonsX; x++)
        {
            float t = (float)x / polygonsX;
            bordeDet[x] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(t * noiseScale + noiseOffX, 0.3f));
            bordeFre[x] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(t * noiseScale + noiseOffX, 15.41f));
        }

        Vector3[] vertices = new Vector3[(polygonsX + 1) * (polygonsZ + 1)];
        int index = 0;

        for (int z = 0; z <= polygonsZ; z++)
        {
            float localZ = z * stepZ;
            float worldZ = localZ - halfL;
            
            for (int x = 0; x <= polygonsX; x++)
            {
                float localX = x * stepX;
                float worldX = localX - halfW;

                // 1. Calcular máscara de borde del bache completo
                float dLeft = localX - bordeIzq[z];
                float dRight = (width - bordeDer[z]) - localX;
                float dBack = localZ - bordeDet[x];
                float dFront = (length - bordeFre[x]) - localZ;
                float minD = Mathf.Min(dLeft, dRight, dBack, dFront);
                
                float borderFactor = 0f;
                if (minD > 0f)
                {
                    float t = Mathf.Clamp01(minD / (Mathf.Max(width, length) * 0.1f));
                    borderFactor = Mathf.SmoothStep(0f, 1f, t);
                }

                // 2. Calcular profundidad de grieta Voronoi
                float depth = 0f;
                if (borderFactor > 0.01f)
                {
                    depth = CalculateVoronoiDepth(worldX, worldZ, seeds);
                }

                // 3. Aplicar altura (solo Y)
                vertices[index++] = new Vector3(worldX, -depth * borderFactor, worldZ);
            }
        }

        // Generar triángulos
        int[] triangles = new int[polygonsX * polygonsZ * 6];
        int ti = 0;
        
        for (int z = 0; z < polygonsZ; z++)
        {
            for (int x = 0; x < polygonsX; x++)
            {
                int v = z * (polygonsX + 1) + x;
                triangles[ti++] = v;
                triangles[ti++] = v + polygonsX + 1;
                triangles[ti++] = v + 1;
                triangles[ti++] = v + 1;
                triangles[ti++] = v + polygonsX + 1;
                triangles[ti++] = v + polygonsX + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

 
}