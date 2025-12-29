using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TerrainPotholeGenerator : MonoBehaviour
{
    // ─── INTERNAL TYPES ─────────────────────────────────────────────────────

    public enum UnifiedMode
    {
        Mixed,          // Generates both types based on mixRatio
        OnlyBache,
        OnlyCrocodile
    }



    // ─── SETTINGS ───────────────────────────────────────────────────────────

    [Header("Main Configuration")]
    public UnifiedMode mode = UnifiedMode.Mixed;
    [Range(0f, 1f)] public float crocodileMixRatio = 0.5f; // 0 = All Bache, 1 = All Croc
    public int Seed = 12345;
    public bool autoUpdate = false;
    public bool randomizeSeedOnStart = true;
    public bool modoCaptura = false;
    public Material sharedMaterial;

    public bool generacionTerminada = false;

    [Header("Spawner Settings")]
    public int cantidadBaches = 30;
    public float ladoArea = 10f;
    public float margenBorde = 0.5f;
    public LayerMask capasObstaculos = ~0; 

    [Header("Bache Configuration")]
    public BacheConfig bacheSettings;

    [Header("Crocodile Configuration")]
    public CrocodileConfig crocSettings;

    [System.Serializable]
    public class BacheConfig
    {
        [Header("Configuración Genética (Seed)")]
        [Tooltip("Semilla única. Cada número genera una variante diferente respetando los rangos.")]
        public int seed = 235;

        [Header("Dimensiones Generales")]
        [Tooltip("Ancho del área cuadrada del bache")]
        [Range(0.25f, 5f)] public float minWidth = 0.25f;
        [Range(0.25f, 5f)] public float maxWidth = 1f;
        [Range(0.25f, 5f)] public float minLength = 0.25f;
        [Range(0.25f, 5f)] public float maxLength = 1f;

        [Header("Resolución")]
        [Range(10, 254)] public int polygonsX = 147;
        [Range(10, 254)] public int polygonsZ = 144;

        [Header("Topología (Multi-Spots)")]
        [Tooltip("Cantidad de núcleos o sub-baches que conforman el bache principal")]
        [Range(1, 15)] public int minSpots = 6;
        [Range(1, 15)] public int maxSpots = 10;

        [Tooltip("Radio de cada núcleo como % del tamaño total del bache")]
        [Range(5f, 50f)] public float minSpotRadiusPercent = 16.4f;
        [Range(5f, 50f)] public float maxSpotRadiusPercent = 31.4f;

        [Tooltip("Dispersión de los núcleos (0.1 = muy juntos, 0.9 = muy separados)")]
        [Range(0.1f, 1.0f)] public float minSpread = 0.568f;
        [Range(0.1f, 1.0f)] public float maxSpread = 0.764f;

        [Header("Erosión (Aleatoriedad Relativa)")]
        [Tooltip("Escala del ruido (Ciclos por Objeto). Mismo valor = Mismo aspecto en 0.25m y 1m")]
        [Range(1f, 20f)] public float minErosionScale = 1f;
        [Range(1f, 20f)] public float maxErosionScale = 19.92f;

        [Tooltip("Fuerza de la erosión (Amplitud)")]
        [Range(0.1f, 1.0f)] public float minErosionAmount = 0.496f;
        [Range(0.1f, 1.0f)] public float maxErosionAmount = 0.936f;

        [Header("Detalle de Borde (Fractura)")]
        [Tooltip("Cantidad de fractura en los bordes (0 = suave, 1 = muy roto)")]
        [Range(0.0f, 1.0f)] public float minEdgeFracture = 0f;
        [Range(0.0f, 1.0f)] public float maxEdgeFracture = 0.084f;

        [Header("Perfil y Detalle")]
        [Tooltip("Límite Global de Profundidad (% del Tamaño promedio)")]
        [Range(1f, 20f)] public float globalDepthLimitPercent = 6.83f;

        [Tooltip("Profundidad Base por Spot (% del Tamaño promedio)")]
        [Range(0.1f, 10f)] public float minDepthPercent = 0.76f;
        [Range(0.1f, 10f)] public float maxDepthPercent = 2.48f;

        [Tooltip("Pendiente de las paredes (Acantilado)")]
        [Range(5f, 50f)] public float minWallSteepness = 5.8f;
        [Range(5f, 50f)] public float maxWallSteepness = 9.7f;

        [Tooltip("Rugosidad del fondo (Piedras)")]
        [Range(0.0f, 0.05f)] public float minBottomRoughness = 0f;
        [Range(0.0f, 0.05f)] public float maxBottomRoughness = 0.0229f;

        [Tooltip("Escala de rugosidad (Ciclos por Objeto)")]
        [Range(10f, 100f)] public float bottomRoughnessRelativeScale = 24f;

        [Header("Márgenes (Contenedor Irregular)")]
        [Tooltip("Mínimo margen (%)")]
        [Range(5, 30)] public int bordeMinPercent = 5;
        [Tooltip("Máximo margen (%)")]
        [Range(10, 45)] public int bordeMaxPercent = 37;
    }

    [System.Serializable]
    public class CrocodileConfig
    {
        [Header("Dimensiones Aleatorias")]
        public float minWidth = 1.5f;
        public float maxWidth = 2.5f;
        public float minLength = 2.5f;
        public float maxLength = 3.5f;

        [Header("Calidad")]
        [Tooltip("Polígonos por eje. A mayor número, menos 'dientes de sierra', pero más costo.")]
        [Range(20, 200)] public int polygonsX = 200;
        [Range(20, 200)] public int polygonsZ = 200;

        [Header("Patrón Cocodrilo")]
        [Tooltip("Cantidad de bloques poligonales.")]
        [Range(10, 100)] public int cellCount = 51;
        
        [Tooltip("Ancho mínimo de las grietas (% del tamaño).")]
        [Range(0.1f, 2f)] public float minCrackWidthPercent = 0.332f;

        [Tooltip("Ancho máximo de las grietas (% del tamaño).")]
        [Range(0.5f, 5f)] public float maxCrackWidthPercent = 2.79f;
        
        [Tooltip("Profundidad de las grietas.")]
        [Range(0.01f, 0.08f)] public float crackDepth = 0.0279f;
        
        [Tooltip("Suavidad del borde de la grieta.")]
        [Range(1f, 4f)] public float crackSmoothness = 2.99f;

        [Header("Variación Orgánica")]
        [Tooltip("Escala del ruido para variar el ancho (mayor = cambios más rápidos).")]
        [Range(0.1f, 10f)] public float widthNoiseScale = 1.43f;

        [Tooltip("Irregularidad de los bordes (dientes naturales).")]
        [Range(0f, 0.5f)] public float edgeIrregularity = 0.05f;

        [Tooltip("Escala del ruido de irregularidad (mayor = más detalle fino).")]
        [Range(5f, 50f)] public float irregularityScale = 18.8f;

        [Tooltip("Distorsión leve para que no sean líneas perfectas (0 = rectas).")]
        [Range(0f, 1f)] public float distortion = 0.668f;

        [Header("Bordes Orgánicos")]
        [Tooltip("Distancia mínima al borde sin grietas (% del tamaño).")]
        [Range(5f, 25f)] public float bordeMinPercent = 11.3f;
        [Tooltip("Distancia máxima al borde (% del tamaño).")]
        [Range(10f, 35f)] public float bordeMaxPercent = 24f;
    }

    // ─── UNITY EVENTS ───────────────────────────────────────────────────────
    
    private void Start()
    {
        if (Application.isPlaying && randomizeSeedOnStart)
        {
            // Use Ticks for much better entropy than Random.Range on start
            Seed = (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF);
            Generate();
        }
    }

    private void OnDisable()
    {
        // Automatically cleanup when stopping execution or disabling the object
        ClearChildren();
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
        
        try { Generate(); }
        catch (System.Exception e) { Debug.LogWarning($"TerrainPotholeGenerator: {e.Message}"); }
    }
#endif

    [ContextMenu("Generate Spawner")]
    public void Generate()
    {
        // 1. Cleanup children
        ClearChildren();

        Random.InitState(Seed);

        // 2. Prepare Types (Strict Ratio)
        List<bool> typesToGenerate = new List<bool>();
        int crocCountRequested = 0;
        if (mode == UnifiedMode.OnlyCrocodile) crocCountRequested = cantidadBaches;
        else if (mode == UnifiedMode.OnlyBache) crocCountRequested = 0;
        else crocCountRequested = Mathf.RoundToInt(cantidadBaches * crocodileMixRatio);

        for (int i = 0; i < cantidadBaches; i++) typesToGenerate.Add(i < crocCountRequested);
        
        // Shuffle using the Spawner Seed
        for (int i = 0; i < typesToGenerate.Count; i++)
        {
            int rnd = Random.Range(i, typesToGenerate.Count);
            bool temp = typesToGenerate[i];
            typesToGenerate[i] = typesToGenerate[rnd];
            typesToGenerate[rnd] = temp;
        }

        int generated = 0;
        int maxTotalAttempts = cantidadBaches * (modoCaptura ? 1000 : 100); 
        int totalAttempts = 0;
        int attemptsThisObject = 0;

        float halfSide = ladoArea * 0.5f;
        float xmin = -halfSide + margenBorde;
        float xmax = halfSide - margenBorde;
        float zmin = xmin;
        float zmax = xmax;

        List<Bounds> spawnedBounds = new List<Bounds>();

        while (generated < cantidadBaches && totalAttempts < maxTotalAttempts)
        {
            totalAttempts++;
            attemptsThisObject++;
            bool useCroc = typesToGenerate[generated];

            // Random Dimensions for this instance
            float w = 0f, l = 0f;
            if (!useCroc)
            {
                w = Random.Range(bacheSettings.minWidth, bacheSettings.maxWidth);
                l = Random.Range(bacheSettings.minLength, bacheSettings.maxLength);
            }
            else
            {
                w = Random.Range(crocSettings.minWidth, crocSettings.maxWidth);
                l = Random.Range(crocSettings.minLength, crocSettings.maxLength);
            }

            // Random Position
            float rx = Random.Range(xmin, xmax);
            float rz = Random.Range(zmin, zmax);

            if (modoCaptura)
            {
                // Expanding search PER OBJECT: start at 10% and expand to 100% by attempt 400.
                float progress = Mathf.Clamp01((float)attemptsThisObject / 400f);
                float bias = Mathf.Lerp(0.1f, 1.0f, progress); 
                rx *= bias;
                rz *= bias;
            }

            Vector3 pos = transform.position + new Vector3(rx, 0, rz);

            if (HasSpace(pos, w, l, spawnedBounds))
            {
                // Unique child seed derived from spawner seed + position in sequence
                int instanceSeed = Seed + generated + 1000;

                GameObject obj = new GameObject($"{(useCroc ? "Croc" : "Bache")}_{generated}");
                obj.transform.SetParent(transform);
                obj.transform.position = pos;
                obj.layer = 7; 
                obj.hideFlags = HideFlags.DontSave; 

                if (!useCroc) GenerateBacheMesh(obj, w, l, instanceSeed);
                else GenerateCrocMesh(obj, w, l, instanceSeed);

                Bounds b = new Bounds(pos, new Vector3(w, 1f, l)); 
                spawnedBounds.Add(b);
                generated++;
                attemptsThisObject = 0; // Reset for next bache
            }
        }
        
        if (generated < cantidadBaches)
        {
            Debug.LogWarning($"[TerrainPotholeGenerator] Could only generate {generated}/{cantidadBaches} potholes due to space constraints.");
        }
        generacionTerminada = true;
    }

    private int CantidadBachesSeguridad() => cantidadBaches * (modoCaptura ? 200 : 50);

    private void ClearChildren()
    {
        if (this == null) return;
        
        // Use a list to avoid modifying the collection while iterating
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child != null) toDestroy.Add(child.gameObject);
        }

        foreach (var child in toDestroy)
        {
            if (child == null) continue;
            
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private bool HasSpace(Vector3 center, float w, float l, List<Bounds> existing)
    {
        Vector3 size = new Vector3(w, 2f, l); // Height 2f for safety
        Bounds newB = new Bounds(center, size);

        // 1 Check self-overlap with already spawned in this batch
        foreach (var b in existing)
        {
            if (b.Intersects(newB)) return false;
        }

        // 2 Check physics overlap with world
        Collider[] hits = Physics.OverlapBox(center, size * 0.5f, Quaternion.identity, capasObstaculos, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            // Ignore if the hit is a child of this spawner (already placed bache)
            // because we already check them via 'spawnedBounds'
            if (hit.transform.IsChildOf(transform)) continue;
            return false;
        }
        return true;
    }

    // ─── PROCEDURAL GENERATION CALLS ──────────────────────────────────────────

    private void GenerateBacheMesh(GameObject obj, float width, float length, int seed)
    {
        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();
        var mc = obj.AddComponent<MeshCollider>();
        if (sharedMaterial) mr.sharedMaterial = sharedMaterial;
        obj.tag = "Pothole";

        Random.InitState(seed);

        Mesh mesh = new Mesh { name = "Proc_Bache_Erosion" };

        int px = bacheSettings.polygonsX;
        int pz = bacheSettings.polygonsZ;
        float avgSize = (width + length) * 0.5f;

        // --- PARAMETROS RELATIVOS AL TAMAÑO ---
        float currentErosionCycles = Random.Range(bacheSettings.minErosionScale, bacheSettings.maxErosionScale);
        float realErosionScale = currentErosionCycles / avgSize;

        float realBottomRoughnessScale = bacheSettings.bottomRoughnessRelativeScale / avgSize;
        float realBorderNoiseScale = 8.0f / avgSize;

        // Depths (Percentage-based)
        float currentDepthPercent = Random.Range(bacheSettings.minDepthPercent, bacheSettings.maxDepthPercent);
        float currentMaxDepth = avgSize * (currentDepthPercent / 100f);

        float currentErosionAmount = Random.Range(bacheSettings.minErosionAmount, bacheSettings.maxErosionAmount);
        float currentWallSteepness = Random.Range(bacheSettings.minWallSteepness, bacheSettings.maxWallSteepness);
        float currentBottomRoughness = Random.Range(bacheSettings.minBottomRoughness, bacheSettings.maxBottomRoughness);
        float currentSpread = Random.Range(bacheSettings.minSpread, bacheSettings.maxSpread);
        float currentEdgeFracture = Random.Range(bacheSettings.minEdgeFracture, bacheSettings.maxEdgeFracture);

        // Generar Spots (Núcleos)
        int spotsCount = Random.Range(bacheSettings.minSpots, bacheSettings.maxSpots + 1);
        List<Vector4> spots = new List<Vector4>();

        float spreadX = width * 0.5f * currentSpread;
        float spreadZ = length * 0.5f * currentSpread;

        for (int i = 0; i < spotsCount; i++)
        {
            Vector2 pos = Random.insideUnitCircle;
            float sx = pos.x * spreadX;
            float sz = pos.y * spreadZ;

            float srPercent = Random.Range(bacheSettings.minSpotRadiusPercent, bacheSettings.maxSpotRadiusPercent);
            float sr = avgSize * (srPercent / 100f);

            float ss = Random.Range(0f, 100f);
            spots.Add(new Vector4(sx, sz, sr, ss));
        }

        // Generar Mesh
        Vector3[] verts = new Vector3[(px + 1) * (pz + 1)];
        Vector2[] uvs = new Vector2[verts.Length];

        float stepX = width / px;
        float stepZ = length / pz;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        // --- ALGORITMO DE EROSIÓN ---
        float seedOffset = Random.Range(0f, 100f);
        float fractureScale = 30f / avgSize;

        int vIndex = 0;
        for (int z = 0; z <= pz; z++)
        {
            float localZ = z * stepZ;
            float worldZ = localZ - halfL;

            for (int x = 0; x <= px; x++)
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

                float marginMinInMeters = Mathf.Min(width, length) * (bacheSettings.bordeMinPercent / 100f);
                float marginMaxInMeters = Mathf.Min(width, length) * (bacheSettings.bordeMaxPercent / 100f);

                float dynamicMargin = Mathf.Lerp(marginMinInMeters, marginMaxInMeters, borderNoise);

                // --- CONTAINER FRACTURE ---
                float containerFrac = Mathf.PerlinNoise(worldX * fractureScale + seedOffset + 50f, worldZ * fractureScale + seedOffset + 50f);
                dynamicMargin += containerFrac * currentEdgeFracture * 0.5f;

                float containerMask = 0f;
                float fadeSize = 0.002f; // 2mm transition
                float delta = distToEdge - dynamicMargin;
                containerMask = Mathf.Clamp01(delta / fadeSize);

                // B. Cálculo de Profundidad
                float accumulatedFactor = 0f;

                if (containerMask > 0.001f)
                {
                    // 1. Sumar Factores de Erosion
                    for (int k = 0; k < spotsCount; k++)
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
                    float frac = Mathf.PerlinNoise(worldX * fractureScale + seedOffset, worldZ * fractureScale + seedOffset);

                    float depthProtection = Mathf.Clamp01(accumulatedFactor);
                    float fractureInfluence = (1.0f - depthProtection) * currentEdgeFracture;

                    if (accumulatedFactor < 1.0f)
                    {
                        accumulatedFactor -= frac * fractureInfluence;
                    }

                    accumulatedFactor = Mathf.Max(0f, accumulatedFactor);

                    // 3. Convertir a Metros
                    float accumulatedDepthVal = accumulatedFactor * currentMaxDepth;

                    // 4. Tope Global
                    float globalLimit = avgSize * (bacheSettings.globalDepthLimitPercent / 100f);
                    accumulatedDepthVal = Mathf.Min(accumulatedDepthVal, globalLimit);

                    // 5. Rubble
                    if (accumulatedDepthVal > 0.0001f)
                    {
                        float rubble = Mathf.PerlinNoise(worldX * realBottomRoughnessScale, worldZ * realBottomRoughnessScale);
                        rubble = Mathf.Abs(rubble - 0.5f) * 2f;
                        accumulatedDepthVal += (rubble - 0.5f) * currentBottomRoughness;
                    }

                    float finalY = -Mathf.Max(0f, accumulatedDepthVal * containerMask);
                    verts[vIndex] = new Vector3(worldX, finalY, worldZ);
                }
                else
                {
                    verts[vIndex] = new Vector3(worldX, 0f, worldZ);
                }

                uvs[vIndex] = new Vector2((float)x / px, (float)z / pz);
                vIndex++;
            }
        }

        int[] tris = new int[px * pz * 6];
        int ti = 0;
        for (int z = 0; z < pz; z++)
        {
            for (int x = 0; x < px; x++)
            {
                int v = z * (px + 1) + x;
                tris[ti++] = v; tris[ti++] = v + px + 1; tris[ti++] = v + 1;
                tris[ti++] = v + 1; tris[ti++] = v + px + 1; tris[ti++] = v + px + 2;
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private float CalculateSingleSpotErosion(float dx, float dz, float radius, float seed, float scale, float amount, float steepness)
    {
        float dist = Mathf.Sqrt(dx * dx + dz * dz);
        float angle = Mathf.Atan2(dz, dx);

        // Deformacion del radio
        float angleNoise = Mathf.PerlinNoise(angle * 2.5f, seed) * 0.3f;
        float posNoise = Mathf.PerlinNoise(dx * scale + seed, dz * scale + seed);

        float effectiveRadius = radius * (1.0f - amount * posNoise + angleNoise * 0.2f);

        if (dist >= effectiveRadius) return 0f;

        float t = dist / effectiveRadius;
        return 1f - Mathf.Pow(t, steepness);
    }

    private void GenerateCrocMesh(GameObject obj, float width, float length, int seed)
    {
        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();
        var mc = obj.AddComponent<MeshCollider>();
        if (sharedMaterial) mr.sharedMaterial = sharedMaterial;
        obj.tag = "Crocodile";

        Random.InitState(seed);

        float avgSize = (width + length) * 0.5f;
        float minCrackWidth = avgSize * (crocSettings.minCrackWidthPercent / 100f);
        float maxCrackWidth = avgSize * (crocSettings.maxCrackWidthPercent / 100f);
        float bordeMin = avgSize * (crocSettings.bordeMinPercent / 100f);
        float bordeMax = avgSize * (crocSettings.bordeMaxPercent / 100f);

        // Generar semillas Voronoi
        List<Vector2> seeds = new List<Vector2>();
        for (int i = 0; i < crocSettings.cellCount; i++)
        {
            seeds.Add(new Vector2(
                Random.Range(-width * 0.6f, width * 0.6f),
                Random.Range(-length * 0.6f, length * 0.6f)
            ));
        }

        Mesh mesh = new Mesh { name = "Proc_Croc_Voronoi" };

        int px = crocSettings.polygonsX;
        int pz = crocSettings.polygonsZ;
        float stepX = width / px;
        float stepZ = length / pz;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        // Precalcular bordes irregulares
        float noiseScale = 2f;
        float noiseOffX = Random.Range(0f, 100f);

        float[] bordeIzq = new float[pz + 1];
        float[] bordeDer = new float[pz + 1];
        float[] bordeDet = new float[px + 1];
        float[] bordeFre = new float[px + 1];

        for (int z = 0; z <= pz; z++)
        {
            float t = (float)z / pz;
            bordeIzq[z] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(0.1f + noiseOffX, t * noiseScale));
            bordeDer[z] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(10.73f + noiseOffX, t * noiseScale));
        }

        for (int x = 0; x <= px; x++)
        {
            float t = (float)x / px;
            bordeDet[x] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(t * noiseScale + noiseOffX, 0.3f));
            bordeFre[x] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(t * noiseScale + noiseOffX, 15.41f));
        }

        Vector3[] vertices = new Vector3[(px + 1) * (pz + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int index = 0;

        for (int z = 0; z <= pz; z++)
        {
            float localZ = z * stepZ;
            float worldZ = localZ - halfL;

            for (int x = 0; x <= px; x++)
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
                    depth = CalculateVoronoiDepth(worldX, worldZ, seeds, minCrackWidth, maxCrackWidth, seed);
                }

                // 3. Aplicar altura (solo Y)
                vertices[index] = new Vector3(worldX, -depth * borderFactor, worldZ);
                uvs[index] = new Vector2((float)x / px, (float)z / pz);
                index++;
            }
        }

        // Generar triángulos
        int[] triangles = new int[px * pz * 6];
        int ti = 0;

        for (int z = 0; z < pz; z++)
        {
            for (int x = 0; x < px; x++)
            {
                int v = z * (px + 1) + x;
                triangles[ti++] = v;
                triangles[ti++] = v + px + 1;
                triangles[ti++] = v + 1;
                triangles[ti++] = v + 1;
                triangles[ti++] = v + px + 1;
                triangles[ti++] = v + px + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private float CalculateVoronoiDepth(float x, float z, List<Vector2> seeds, float minCrackWidth, float maxCrackWidth, int instanceSeed)
    {
        // 1. Distorsión de dominio para bordes irregulares ("dientes naturales")
        float warpX = 0f;
        float warpZ = 0f;

        if (crocSettings.edgeIrregularity > 0f)
        {
            warpX = (Mathf.PerlinNoise(x * crocSettings.irregularityScale, z * crocSettings.irregularityScale) - 0.5f) * crocSettings.edgeIrregularity;
            warpZ = (Mathf.PerlinNoise(x * crocSettings.irregularityScale + 15.3f, z * crocSettings.irregularityScale + 15.3f) - 0.5f) * crocSettings.edgeIrregularity;
        }

        x += warpX;
        z += warpZ;

        // 2. Distorsión global leve
        if (crocSettings.distortion > 0f)
        {
            float noiseX = Mathf.PerlinNoise(x * 0.5f + instanceSeed + 123f, z * 0.5f);
            float noiseZ = Mathf.PerlinNoise(x * 0.5f, z * 0.5f + instanceSeed + 456f);
            x += (noiseX - 0.5f) * crocSettings.distortion * 0.5f;
            z += (noiseZ - 0.5f) * crocSettings.distortion * 0.5f;
        }

        // 3. Encontrar las 2 semillas más cercanas
        float d1 = float.MaxValue;
        float d2 = float.MaxValue;

        foreach (var s in seeds)
        {
            float dx = x - s.x;
            float dz = z - s.y;
            float d = Mathf.Sqrt(dx * dx + dz * dz);

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

        // 4. Ancho variable de grieta
        float widthNoise = Mathf.PerlinNoise(x * crocSettings.widthNoiseScale + instanceSeed, z * crocSettings.widthNoiseScale + instanceSeed);
        float currentCrackWidth = Mathf.Lerp(minCrackWidth, maxCrackWidth, widthNoise);

        if (distToEdge > currentCrackWidth)
        {
            return 0f;
        }

        float t = distToEdge / currentCrackWidth;

        float roughness = 0f;
        if (distToEdge < currentCrackWidth * 0.8f)
        {
            roughness = (Mathf.PerlinNoise(x * 30f, z * 30f) - 0.5f) * 0.2f * crocSettings.crackDepth;
        }

        float profile = 1f - Mathf.Pow(t, crocSettings.crackSmoothness);

        return (crocSettings.crackDepth * profile) + roughness;
    }
}
