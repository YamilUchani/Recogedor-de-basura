using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainBacheGenerator : MonoBehaviour
{
    [Header("Configuración Genética (Seed)")]
    [Tooltip("Semilla única. Cada número genera una variante diferente respetando los rangos.")]
    public int seed = 0;

    [Header("Dimensiones Generales")]
    [Tooltip("Ancho del área cuadrada del bache")]
    [Range(0.25f, 5f)] public float minWidth = 0.5f;
    [Range(0.25f, 5f)] public float maxWidth = 1.0f;
    [Range(0.25f, 5f)] public float minLength = 0.5f;
    [Range(0.25f, 5f)] public float maxLength = 1.0f;
    
    [Header("Resolución")]
    [Range(10, 254)] public int polygonsX = 120;
    [Range(10, 254)] public int polygonsZ = 120;
    
    [Header("Topología (Multi-Spots)")]
    [Tooltip("Cantidad de núcleos o sub-baches que conforman el bache principal")]
    [Range(1, 15)] public int minSpots = 5;
    [Range(1, 15)] public int maxSpots = 10;
    
    [Tooltip("Radio de cada núcleo como % del tamaño total del bache")]
    [Range(5f, 50f)] public float minSpotRadiusPercent = 15f;
    [Range(5f, 50f)] public float maxSpotRadiusPercent = 35f;

    [Tooltip("Dispersión de los núcleos (0.1 = muy juntos, 0.9 = muy separados)")]
    [Range(0.1f, 1.0f)] public float minSpread = 0.15f;
    [Range(0.1f, 1.0f)] public float maxSpread = 0.5f;

    [Header("Erosión (Aleatoriedad Relativa)")]
    [Tooltip("Escala del ruido (Ciclos por Objeto). Mismo valor = Mismo aspecto en 0.25m y 1m")]
    [Range(1f, 20f)] public float minErosionScale = 3.0f;
    [Range(1f, 20f)] public float maxErosionScale = 8.0f;
    
    [Tooltip("Fuerza de la erosión (Amplitud)")]
    [Range(0.1f, 1.0f)] public float minErosionAmount = 0.5f;
    [Range(0.1f, 1.0f)] public float maxErosionAmount = 0.9f;
    
    [Header("Detalle de Borde (Fractura)")]
    [Tooltip("Cantidad de fractura en los bordes (0 = suave, 1 = muy roto)")]
    [Range(0.0f, 1.0f)] public float minEdgeFracture = 0.3f;
    [Range(0.0f, 1.0f)] public float maxEdgeFracture = 0.8f;
    
    [Header("Perfil y Detalle")]
    [Tooltip("Límite Global de Profundidad (% del Tamaño promedio)")]
    [Range(1f, 20f)] public float globalDepthLimitPercent = 10f;

    [Tooltip("Profundidad Base por Spot (% del Tamaño promedio)")]
    [Range(0.1f, 10f)] public float minDepthPercent = 1.0f;
    [Range(0.1f, 10f)] public float maxDepthPercent = 4.0f;

    [Tooltip("Pendiente de las paredes (Acantilado)")]
    [Range(2f, 50f)] public float minWallSteepness = 5f;
    [Range(2f, 50f)] public float maxWallSteepness = 25f;

    [Tooltip("Rugosidad del fondo (Piedras)")]
    [Range(0.0f, 0.05f)] public float minBottomRoughness = 0.002f;
    [Range(0.0f, 0.05f)] public float maxBottomRoughness = 0.01f;
    
    [Tooltip("Escala de rugosidad (Ciclos por Objeto)")]
    [Range(10f, 100f)] public float bottomRoughnessRelativeScale = 20f;

    [Header("Márgenes (Contenedor Irregular)")]
    [Tooltip("Mínimo margen (%)")]
    [Range(5, 30)] public int bordeMinPercent = 15;
    [Tooltip("Máximo margen (%)")]
    [Range(10, 45)] public int bordeMaxPercent = 35;

    // --- Variables Internas ---
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    public void GenerarMeshEnNuevoObjeto(int seed, Material material)
    {
        // 1. Setup Básico
        Random.InitState(seed);
        
        // Limpiamos hijos previos si existen (por seguridad)
        foreach (Transform child in transform) {
            #if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
            #else
                Destroy(child.gameObject);
            #endif
        }

        GameObject nuevoBache = new GameObject("Bache_Procedural_V2");
        nuevoBache.transform.SetParent(this.transform, false);
        nuevoBache.transform.localPosition = Vector3.zero;
        nuevoBache.layer = gameObject.layer;

        MeshFilter mf = nuevoBache.AddComponent<MeshFilter>();
        MeshRenderer mr = nuevoBache.AddComponent<MeshRenderer>();
        MeshCollider mc = nuevoBache.AddComponent<MeshCollider>();
        
        if (material != null) mr.sharedMaterial = material;
        
        // 2. Dimensiones Aleatorias
        float width = Random.Range(minWidth, maxWidth);
        float length = Random.Range(minLength, maxLength);
        float avgSize = (width + length) * 0.5f; // Tamaño promedio de referencia
        
        // --- CALCULO DE PARAMETROS RELATIVOS AL TAMAÑO ---
        // 1. Escalas de Ruido normalizadas (Ciclos por Objeto)
        // Si el objeto mide 0.5m y queremos 5 ciclos, la frecuencia mundial debe ser 10.
        // Frecuencia = Ciclos / Tamaño
        float currentErosionCycles = Random.Range(minErosionScale, maxErosionScale);
        float realErosionScale = currentErosionCycles / avgSize;

        float realBottomRoughnessScale = bottomRoughnessRelativeScale / avgSize;
        float realBorderNoiseScale = 8.0f / avgSize; // 8 ciclos por objeto para el borde
        
        // Depths (AHORA RELATIVAS %)
        float currentDepthPercent = Random.Range(minDepthPercent, maxDepthPercent);
        float currentMaxDepth = avgSize * (currentDepthPercent / 100f);

        float currentErosionAmount = Random.Range(minErosionAmount, maxErosionAmount);
        float currentWallSteepness = Random.Range(minWallSteepness, maxWallSteepness);
        float currentBottomRoughness = Random.Range(minBottomRoughness, maxBottomRoughness);
        float currentSpread = Random.Range(minSpread, maxSpread);
        
        // 3. Generar Spots (Núcleos)
        int spotsCount = Random.Range(minSpots, maxSpots + 1);
        List<Vector4> spots = new List<Vector4>(); // x, z, radius, seed
        
        // Usamos el parametro Spread para decidir cuanto se alejan del centro
        float spreadX = width * 0.5f * currentSpread;
        float spreadZ = length * 0.5f * currentSpread;
        
        for(int i=0; i<spotsCount; i++)
        {
            Vector2 pos = Random.insideUnitCircle;
            // Potenciar para concentrar mas en el centro si spread es bajo
            float sx = pos.x * spreadX;
            float sz = pos.y * spreadZ;
            
            // Radio relativo (%)
            float srPercent = Random.Range(minSpotRadiusPercent, maxSpotRadiusPercent);
            float sr = avgSize * (srPercent / 100f);

            float ss = Random.Range(0f, 100f);
            spots.Add(new Vector4(sx, sz, sr, ss));
        }
        
        // 4. Generar Mesh
        mesh = new Mesh { name = "Bache_Erosionado" };
        
        int vCount = (polygonsX + 1) * (polygonsZ + 1);
        vertices = new Vector3[vCount];
        triangles = new int[polygonsX * polygonsZ * 6];

        float stepX = width / polygonsX;
        float stepZ = length / polygonsZ;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        // Pre-calculo: YA NO ES NECESARIO (Usamos 2D Noise en caliente)

        // --- ALGORITMO DE EROSIÓN ---
        float seedOffset = Random.Range(0f, 100f);
        // Parametros Locales para fractura
        float currentEdgeFracture = Random.Range(minEdgeFracture, maxEdgeFracture);
        float fractureScale = 30f / avgSize; // Alta frecuencia relativa

        int vIndex = 0;
        for (int z = 0; z <= polygonsZ; z++)
        {
            float localZ = z * stepZ;
            float worldZ = localZ - halfL;

            for (int x = 0; x <= polygonsX; x++)
            {
                float localX = x * stepX;
                float worldX = localX - halfW;
                
                // A. Máscara del Contenedor (ORGANIC 2D NOISE)
                float dL = localX;
                float dR = width - localX;
                float dB = localZ;
                float dT = length - localZ;
                float distToEdge = Mathf.Min(dL, dR, dB, dT); 
                
                float borderNoise = Mathf.PerlinNoise(worldX * realBorderNoiseScale + seedOffset, worldZ * realBorderNoiseScale + seedOffset);
                
                float marginMinInMeters = Mathf.Min(width, length) * (bordeMinPercent / 100f);
                float marginMaxInMeters = Mathf.Min(width, length) * (bordeMaxPercent / 100f);
                
                float dynamicMargin = Mathf.Lerp(marginMinInMeters, marginMaxInMeters, borderNoise);

                // --- CONTAINER FRACTURE ---
                // Aplicamos la misma logica de "roto" al borde exterior
                float containerFrac = Mathf.PerlinNoise(worldX * fractureScale + seedOffset + 50f, worldZ * fractureScale + seedOffset + 50f);
                // Si el parametro de fractura es alto, comemos mas borde
                dynamicMargin += containerFrac * currentEdgeFracture * 0.5f; // 0.5f factor arb para no comer demasiado
                
                float containerMask = 0f;
                // Fade muy sharp (Corte duro de pavimento)
                float fadeSize = 0.002f; // 2mm transition
                float delta = distToEdge - dynamicMargin;
                containerMask = Mathf.Clamp01(delta / fadeSize);

                // B. Cálculo de Profundidad 
                float accumulatedFactor = 0f;
                
                if (containerMask > 0.001f)
                {
                    // 1. Sumar Factores de Erosion (0..N)
                    for(int k=0; k<spotsCount; k++)
                    {
                        Vector4 s = spots[k];
                        float d = CalculateSingleSpotErosion(
                            worldX - s.x, 
                            worldZ - s.y, 
                            s.z, 
                            s.w, 
                            realErosionScale,
                            currentErosionAmount,
                            currentWallSteepness
                        );
                        accumulatedFactor += d; 
                    }
                    
                    // 2. Aplicar FRACTURA DE BORDE MODULADA
                    // Queremos bordes rotos pero fondo suave.
                    // La fractura afecta más donde "profundidad" es baja (bordes) y menos donde es alta (fondo).
                    
                    float frac = Mathf.PerlinNoise(worldX * fractureScale + seedOffset, worldZ * fractureScale + seedOffset);
                    
                    // Modulador: Intenso en superficie (val ~0), Nulo en fondo (val > 1)
                    // Usamos la inversa de la profundidad acumulada.
                    float depthProtection = Mathf.Clamp01(accumulatedFactor); 
                    float fractureInfluence = (1.0f - depthProtection) * currentEdgeFracture;
                    
                    // Solo restamos si estamos cerca de la superficie para "romper" el borde
                    if (accumulatedFactor < 1.0f) 
                    {
                        accumulatedFactor -= frac * fractureInfluence;
                    }
                    
                    // Cortar negativos (piso plano)
                    accumulatedFactor = Mathf.Max(0f, accumulatedFactor);

                    // 3. Convertir a Metros
                    float accumulatedDepthVal = accumulatedFactor * currentMaxDepth;
                    
                    // 4. Tope Global (Ahora relativo)
                    float globalLimit = avgSize * (globalDepthLimitPercent / 100f);
                    accumulatedDepthVal = Mathf.Min(accumulatedDepthVal, globalLimit);

                    // 5. Rubble (Detalle fino en el fondo)
                    if (accumulatedDepthVal > 0.0001f)
                    {
                        float rubble = Mathf.PerlinNoise(worldX * realBottomRoughnessScale, worldZ * realBottomRoughnessScale);
                        rubble = Mathf.Abs(rubble - 0.5f) * 2f; 
                        accumulatedDepthVal += (rubble - 0.5f) * currentBottomRoughness;
                    }
                    
                    // Final Assignment
                     float finalY = -Mathf.Max(0f, accumulatedDepthVal * containerMask);
                    vertices[vIndex] = new Vector3(worldX, finalY, worldZ);
                }
                else
                {
                     vertices[vIndex] = new Vector3(worldX, 0f, worldZ);
                }
                
                vIndex++;
            }
        }

        // Triángulos
        int tIndex = 0;
        for (int z = 0; z < polygonsZ; z++) {
            for (int x = 0; x < polygonsX; x++) {
                int start = z * (polygonsX + 1) + x;
                triangles[tIndex++] = start;
                triangles[tIndex++] = start + polygonsX + 1;
                triangles[tIndex++] = start + 1;
                triangles[tIndex++] = start + 1;
                triangles[tIndex++] = start + polygonsX + 1;
                triangles[tIndex++] = start + polygonsX + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    // --- ALGORITMO EROSIÓN UNITARIA ---
    // Retorna un valor 0..1 indicando "factor de profundidad"
    private float CalculateSingleSpotErosion(float dx, float dz, float radius, float seed, float scale, float amount, float steepness)
    {
        float dist = Mathf.Sqrt(dx*dx + dz*dz);
        float angle = Mathf.Atan2(dz, dx);

        // Deformacion del radio
        float angleNoise = Mathf.PerlinNoise(angle * 2.5f, seed) * 0.3f; 
        float posNoise = Mathf.PerlinNoise(dx * scale + seed, dz * scale + seed);
        
        float effectiveRadius = radius * (1.0f - amount * posNoise + angleNoise * 0.2f);
        
        if (dist >= effectiveRadius) return 0f;

        float t = dist / effectiveRadius;
        // 0 en centro, 1 en borde.
        // Queremos 1 en centro, 0 en borde.
        // Wall Profile: 
        return 1f - Mathf.Pow(t, steepness);
    }

#if UNITY_EDITOR
    [Header("Debug Editor")]
    [SerializeField] private Material debugMaterial;

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        // Auto-update en editor
        // Usamos delayCall para evitar errores de SendMessage/Destroy durante el ciclo de inspeccion
        UnityEditor.EditorApplication.delayCall += () => { 
            if(this != null) GenerarMeshEnNuevoObjeto(seed, debugMaterial); 
        };
    }
#endif
}
