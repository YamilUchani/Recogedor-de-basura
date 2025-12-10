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

    [System.Serializable]
    public class InternalBache
    {
        [Tooltip("Posición en porcentaje: X=0a100, Y=0a100")]
        public Vector2 posicionPorcentaje = new Vector2(50f, 50f);
        [Range(1f, 50f)] public float radioPorcentaje = 10f;
        [Range(0.01f, 1f)] public float profundidad = 0.1f;
        [Range(0f, 1f)] public float variacionProfundidad = 0.5f;
        [Range(0f, 1f)] public float deformacion = 0.2f;
        [Range(0f, 1f)] public float irregularidadBorde = 0.6f;
        [Range(0f, 0.9f)] public float fondoPlano = 0.3f;
        [Range(0.1f, 5f)] public float suavidad = 1.5f;
        public int semilla = 12345;
    }

    public static class InternalNoiseUtils
    {
        public static float Fbm2D(float x, float y, int octaves = 3, float lacunarity = 2f, float gain = 0.5f)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }
            return value;
        }

        public static Vector2 VectorNoise2D(float x, float y, int seedOffset = 0)
        {
            float angle = Fbm2D(x + seedOffset, y + seedOffset, 2, 2f, 0.6f) * Mathf.PI * 2f;
            float mag = Fbm2D(x + seedOffset + 100, y + seedOffset + 200, 2, 2f, 0.5f);
            return new Vector2(Mathf.Cos(angle) * mag, Mathf.Sin(angle) * mag);
        }
    }

    // ─── SETTINGS ───────────────────────────────────────────────────────────

    [Header("Main Configuration")]
    public UnifiedMode mode = UnifiedMode.Mixed;
    [Range(0f, 1f)] public float crocodileMixRatio = 0.5f; // 0 = All Bache, 1 = All Croc
    public int Seed = 12345;
    public bool autoUpdate = true;
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
        [Header("Dimension Ranges")]
        public float minWidth = 1.5f; public float maxWidth = 2.5f;
        public float minLength = 1.5f; public float maxLength = 2.5f;

        [Header("Internal Details")]
        public int cantidadBachesAleatorios = 10;
        public float minRadioPorcentaje = 3f, maxRadioPorcentaje = 15f;
        public float minProfundidad = 0.05f, maxProfundidad = 0.2f;
        public float minDeformacion = 0.2f, maxDeformacion = 0.6f;
        public float minIrregularidadBorde = 0.4f, maxIrregularidadBorde = 0.9f;
        public float minFondoPlano = 0.2f, maxFondoPlano = 0.6f;
        public float minVariacionProf = 0.3f, maxVariacionProf = 0.7f;
        
        [Range(20, 200)] public int polygonsX = 50;
        [Range(20, 200)] public int polygonsZ = 50;
        public float bordeMin = 0.1f; public float bordeMax = 0.3f;
        public float noiseScale = 2f;
        [Range(0.1f, 5f)] public float bordeSuavidad = 1.5f;
        [Range(0.01f, 1f)] public float profundidadMaximaGlobal = 0.2f;
    }

    [System.Serializable]
    public class CrocodileConfig
    {
        [Header("Dimension Ranges")]
        public float minWidth = 1.5f; public float maxWidth = 2.5f;
        public float minLength = 2.5f; public float maxLength = 3.5f;

        [Header("Pattern Details")]
        [Range(1, 50)] public int minCrackCount = 10;
        [Range(1, 50)] public int maxCrackCount = 20;
        [Range(0.1f, 5f)] public float minCrackGapPercent = 0.3f;
        [Range(0.1f, 5f)] public float maxCrackGapPercent = 0.8f;
        public float crackDepth = 0.05f;
        [Range(0f, 45f)] public float borderLimitMin = 5f;
        [Range(0f, 45f)] public float borderLimitMax = 15f;
    }

    // ─── UNITY EVENTS ───────────────────────────────────────────────────────

    private void OnValidate()
    {
        if (autoUpdate)
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () => { if (this != null) Generate(); };
#endif
        }
    }

    [ContextMenu("Generate Spawner")]
    public void Generate()
    {
        // 1. Cleanup children
        if (transform.childCount > 0)
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            foreach (var child in children)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        Random.InitState(Seed);

        // 2. Loop to spawn
        int generated = 0;
        int maxAttempts = quantidadeBachesSeguridad(); // Simple safety limit
        int attempts = 0;

        float halfSide = ladoArea * 0.5f;
        float xmin = -halfSide + margenBorde;
        float xmax = halfSide - margenBorde;
        float zmin = xmin;
        float zmax = xmax;

        // Keep track of internal bounds to avoid overlapping *within this generation batch*
        List<Bounds> spawnedBounds = new List<Bounds>();

        while (generated < cantidadBaches && attempts < maxAttempts)
        {
            attempts++;

            bool useCroc = false;
            
            // DECIDE TYPE
            if (mode == UnifiedMode.OnlyCrocodile) useCroc = true;
            else if (mode == UnifiedMode.OnlyBache) useCroc = false;
            else useCroc = (Random.value < crocodileMixRatio);

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
            Vector3 pos = transform.position + new Vector3(rx, 0, rz);

            // Check Space (Internal + Physics)
            if (HasSpace(pos, w, l, spawnedBounds))
            {
                // SPAWN
                GameObject obj = new GameObject($"{(useCroc ? "Croc" : "Bache")}_{generated}");
                obj.transform.SetParent(transform);
                obj.transform.position = pos;
                obj.layer = 7; // Obstacles

                if (!useCroc)
                {
                    GenerateBacheMesh(obj, w, l);
                }
                else
                {
                    GenerateCrocMesh(obj, w, l);
                }

                // Register Bounds
                Bounds b = new Bounds(pos, new Vector3(w, 1f, l)); // 1f height buffer
                spawnedBounds.Add(b);
                generated++;
            }
        }
        
        if (generated < cantidadBaches)
        {
            Debug.LogWarning($"[TerrainPotholeGenerator] Could only generate {generated}/{cantidadBaches} potholes due to space constraints.");
        }
        generacionTerminada = true;
    }

    private int quantidadeBachesSeguridad() => cantidadBaches * 20;

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
        return hits.Length == 0;
    }

    // ─── PROCEDURAL GENERATION CALLS ──────────────────────────────────────────

    private void GenerateBacheMesh(GameObject obj, float width, float length)
    {
        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();
        var mc = obj.AddComponent<MeshCollider>();
        if (sharedMaterial) mr.sharedMaterial = sharedMaterial;

        Mesh mesh = new Mesh { name = "Proc_Bache" };

        int px = bacheSettings.polygonsX;
        int pz = bacheSettings.polygonsZ;
        float stepX = width / px;
        float stepZ = length / pz;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        float avgSize = (width + length) * 0.5f;

        float[] bordeIzq = new float[pz + 1];
        float[] bordeDer = new float[pz + 1];
        float[] bordeDet = new float[px + 1];
        float[] bordeFre = new float[px + 1];
        for (int z = 0; z <= pz; z++) {
            float t = (float)z / pz;
            bordeIzq[z] = Mathf.Lerp(bacheSettings.bordeMin, bacheSettings.bordeMax, Mathf.PerlinNoise(0.1f, t * bacheSettings.noiseScale));
            bordeDer[z] = Mathf.Lerp(bacheSettings.bordeMin, bacheSettings.bordeMax, Mathf.PerlinNoise(10.73f, t * bacheSettings.noiseScale));
        }
        for (int x = 0; x <= px; x++) {
            float t = (float)x / px;
            bordeDet[x] = Mathf.Lerp(bacheSettings.bordeMin, bacheSettings.bordeMax, Mathf.PerlinNoise(t * bacheSettings.noiseScale, 0.3f));
            bordeFre[x] = Mathf.Lerp(bacheSettings.bordeMin, bacheSettings.bordeMax, Mathf.PerlinNoise(t * bacheSettings.noiseScale, 15.41f));
        }

        List<InternalBache> bachesList = GenerarBachesAleatorios(width, length);

        Vector3[] verts = new Vector3[(px + 1) * (pz + 1)];
        Vector2[] uvs = new Vector2[verts.Length];
        int idx = 0;

        for (int z = 0; z <= pz; z++) {
            float worldZ = (z * stepZ) - halfL;
            for (int x = 0; x <= px; x++) {
                float localX = x * stepX;
                float worldX = localX - halfW;
                
                float dLeft = localX - bordeIzq[z];
                float dRight = (width - bordeDer[z]) - localX;
                float dBack = (z * stepZ) - bordeDet[x];
                float dFront = (length - bordeFre[x]) - (z * stepZ);
                float minD = Mathf.Min(dLeft, dRight, dBack, dFront);
                float borderFactor = 0f;
                if (minD > 0f) {
                    float t = Mathf.Clamp01(minD / (Mathf.Max(width, length) * 0.5f) * bacheSettings.bordeSuavidad);
                    borderFactor = 1f - (1f - t) * (1f - t);
                }

                float suma = 0f;
                if (borderFactor > 0f) {
                    foreach (var bache in bachesList) {
                        float profUsable = Mathf.Min(bache.profundidad, bacheSettings.profundidadMaximaGlobal);
                        float bx = (bache.posicionPorcentaje.x * 0.01f) * width - halfW;
                        float bz = (bache.posicionPorcentaje.y * 0.01f) * length - halfL;
                        float rad = avgSize * (bache.radioPorcentaje * 0.01f);
                        float dx = worldX - bx;
                        float dz = worldZ - bz;
                        float dist = Mathf.Sqrt(dx * dx + dz * dz);
                        if (dist >= rad && bache.irregularidadBorde <= 0f) continue;
                        float radEff = rad;
                        if (bache.irregularidadBorde > 0f && dist > 0.001f) {
                            float ang = Mathf.Atan2(dz, dx);
                            float n1 = Mathf.PerlinNoise(ang * 1.3f + bache.semilla * 0.1f, 0f);
                            float fac = 1f + (n1 - 0.5f) * 2f * bache.irregularidadBorde;
                            radEff = rad * Mathf.Max(0.3f, fac);
                        }
                        if (dist < radEff) {
                            Vector2 disp = Vector2.zero;
                            if (bache.deformacion > 0f) {
                                float s = 5f/Mathf.Max(rad,0.01f);
                                Vector2 nv = InternalNoiseUtils.VectorNoise2D(dx*s, dz*s, bache.semilla);
                                disp = nv * bache.deformacion * rad;
                            }
                            Vector2 pr = new Vector2(dx, dz) - disp;
                            float dd = pr.magnitude;
                            float t = dd / radEff;
                            if (t < 1f) {
                                float fS = (t <= bache.fondoPlano) ? 1f : 1f - Mathf.Pow((t - bache.fondoPlano) / (1f - bache.fondoPlano), bache.suavidad);
                                suma += profUsable * fS;
                            }
                        }
                    }
                    suma = Mathf.Min(suma, bacheSettings.profundidadMaximaGlobal);
                }

                verts[idx] = new Vector3(worldX, -suma * borderFactor, worldZ);
                uvs[idx] = new Vector2((float)x / px, (float)z / pz);
                idx++;
            }
        }

        int[] tris = new int[px * pz * 6];
        int ti = 0;
        for (int z = 0; z < pz; z++) {
            for (int x = 0; x < px; x++) {
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

    private void GenerateCrocMesh(GameObject obj, float w, float l)
    {
        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();
        var mc = obj.AddComponent<MeshCollider>();
        if (sharedMaterial) mr.sharedMaterial = sharedMaterial;

        int crackCount = Random.Range(crocSettings.minCrackCount, crocSettings.maxCrackCount); 
        float crackGap = Random.Range(crocSettings.minCrackGapPercent, crocSettings.maxCrackGapPercent);

        Mesh mesh = new Mesh { name = "Proc_Croc" };
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        int segC = Mathf.Max(4, Mathf.CeilToInt(Mathf.Max(w, l) * 4));
        List<Vector3> outer = new List<Vector3>();
        float hw = w / 2f, hl = l / 2f;
        void AL(Vector3 s, Vector3 e, int seg) { for (int i = 0; i < seg; i++) outer.Add(Vector3.Lerp(s, e, (float)i / seg)); }
        AL(new Vector3(-hw, 0, -hl), new Vector3(-hw, 0, hl), segC);
        AL(new Vector3(-hw, 0, hl), new Vector3(hw, 0, hl), segC);
        AL(new Vector3(hw, 0, hl), new Vector3(hw, 0, -hl), segC);
        AL(new Vector3(hw, 0, -hl), new Vector3(-hw, 0, -hl), segC);

        float yB = -crocSettings.crackDepth;
        int idxS = verts.Count;
        foreach (var p in outer) { verts.Add(new Vector3(p.x, yB, p.z)); uvs.Add(new Vector2((p.x+w/2)/w, (p.z+l/2)/l)); }
        int idxC = verts.Count; verts.Add(new Vector3(0, yB, 0)); uvs.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i < outer.Count; i++) { tris.Add(idxC); tris.Add(idxS + i); tris.Add(idxS + ((i + 1) % outer.Count)); }

        if (crackCount > 0) {
            List<Vector2> seeds = new List<Vector2>();
            int saf = 0;
            while (seeds.Count < crackCount && saf < 500) {
                saf++;
                seeds.Add(new Vector2(Random.Range(-hw, hw), Random.Range(-hl, hl)));
            }
            // Simple voronoi logic
            float bL = Mathf.Lerp(Mathf.Min(w, l) * crocSettings.borderLimitMin / 100f, Mathf.Min(w, l) * crocSettings.borderLimitMax / 100f, Random.value);
            List<Vector2> bP = new List<Vector2>(); foreach (var v in outer) bP.Add(new Vector2(v.x, v.z));
            List<bool> bF = new List<bool>(); foreach (var x in bP) bF.Add(true);

            for(int i=0; i<seeds.Count; i++) {
                List<Vector2> cell = new List<Vector2>(bP);
                List<bool> fl = new List<bool>(bF);
                Vector2 s = seeds[i];
                for(int j=0; j<seeds.Count; j++) {
                    if(i==j) continue;
                    Vector2 mid = (s+seeds[j])*0.5f;
                    Vector2 n = (seeds[j]-s).normalized;
                    ClipC(ref cell, ref fl, mid, n);
                    if(cell.Count<3) break;
                }
                if(cell.Count<3) continue;
                Vector2 cen=Vector2.zero; foreach(var p in cell) cen+=p; cen/=cell.Count;
                float minD = Mathf.Min(Mathf.Abs(cen.x - -hw), Mathf.Abs(cen.x - hw), Mathf.Abs(cen.y - -hl), Mathf.Abs(cen.y - hl));
                bool isB = minD < bL;
                float gap = Mathf.Min(w, l) * crackGap / 100f;
                if(!isB && gap > 0.0001f) {
                    cell = InsetC(cell, fl, gap * 0.5f);
                    if(cell.Count<3) continue;
                }
                AddC(verts, uvs, tris, cell, -crocSettings.crackDepth, crocSettings.crackDepth, w, l);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private List<InternalBache> GenerarBachesAleatorios(float w, float l)
    {
        List<InternalBache> r = new List<InternalBache>();
        float mP = Mathf.Max(bacheSettings.bordeMax / w, bacheSettings.bordeMax / l) * 100f;
        mP = Mathf.Clamp(mP, 5f, 20f);
        float minP = mP, maxP = 100f - mP;
        for (int i = 0; i < bacheSettings.cantidadBachesAleatorios; i++) {
            r.Add(new InternalBache {
                posicionPorcentaje = new Vector2(Random.Range(minP, maxP), Random.Range(minP, maxP)),
                radioPorcentaje = Random.Range(bacheSettings.minRadioPorcentaje, bacheSettings.maxRadioPorcentaje),
                profundidad = Random.Range(bacheSettings.minProfundidad, bacheSettings.maxProfundidad),
                deformacion = Random.Range(bacheSettings.minDeformacion, bacheSettings.maxDeformacion),
                irregularidadBorde = Random.Range(bacheSettings.minIrregularidadBorde, bacheSettings.maxIrregularidadBorde),
                fondoPlano = Random.Range(bacheSettings.minFondoPlano, bacheSettings.maxFondoPlano),
                variacionProfundidad = Random.Range(bacheSettings.minVariacionProf, bacheSettings.maxVariacionProf),
                suavidad = Random.Range(1f, 3f),
                semilla = Random.Range(1000, 100000)
            });
        }
        return r;
    }

    // --- Helpers Copied ---
    private void ClipC(ref List<Vector2> p, ref List<bool> f, Vector2 pp, Vector2 pn) {
        List<Vector2> np = new List<Vector2>(); List<bool> nf = new List<bool>();
        if(p.Count==0) return;
        for(int i=0; i<p.Count; i++) {
            Vector2 S=p[i]; Vector2 E=p[(i+1)%p.Count];
            bool sf=f[i]; bool ef=f[(i+1)%p.Count];
            bool sin = Vector2.Dot(pn, S-pp)<0; bool ein = Vector2.Dot(pn, E-pp)<0;
            if(sin) { if(ein) { np.Add(E); nf.Add(ef); } else { np.Add(Ints(S, E, pp, pn)); nf.Add(false); } }
            else { if(ein) { np.Add(Ints(S, E, pp, pn)); nf.Add(false); np.Add(E); nf.Add(ef); } }
        }
        p=np; f=nf;
    }
    private Vector2 Ints(Vector2 A, Vector2 B, Vector2 P, Vector2 N) {
        Vector2 d = B-A; float t = Vector2.Dot(N, P-A)/Vector2.Dot(N, d); return A+d*t;
    }
    private List<Vector2> InsetC(List<Vector2> p, List<bool> f, float a) {
        Vector2 c=Vector2.zero; foreach(var pp in p) c+=pp; c/=p.Count;
        List<Vector2> r = new List<Vector2>();
        for(int i=0; i<p.Count; i++) {
            if(f[i]) r.Add(p[i]);
            else {
                float d=Vector2.Distance(c, p[i]);
                if(d>a) r.Add(p[i]+(c-p[i]).normalized*a); else r.Add(c);
            }
        }
        return r;
    }
    private void AddC(List<Vector3> v, List<Vector2> u, List<int> t, List<Vector2> p, float yb, float qt, float w, float l) {
        float yt=yb+qt; int idx=v.Count;
        Vector2 c=Vector2.zero; foreach(var pp in p) c+=pp; c/=p.Count;
        v.Add(new Vector3(c.x, yt, c.y)); u.Add(new Vector2((v[v.Count-1].x+w/2)/w, (v[v.Count-1].z+l/2)/l));
        for(int i=0; i<p.Count; i++) { v.Add(new Vector3(p[i].x, yt, p[i].y)); u.Add(new Vector2((v[v.Count-1].x+w/2)/w, (v[v.Count-1].z+l/2)/l)); }
        for(int i=0; i<p.Count; i++) { t.Add(idx); t.Add(idx+1+i); t.Add(idx+1+((i+1)%p.Count)); }
        int wa=v.Count;
        for(int i=0; i<p.Count; i++) {
            v.Add(new Vector3(p[i].x, yt, p[i].y)); v.Add(new Vector3(p[i].x, yb, p[i].y));
            u.Add(new Vector2((v[v.Count-2].x+w/2)/w, (v[v.Count-2].z+l/2)/l)); u.Add(new Vector2((v[v.Count-1].x+w/2)/w, (v[v.Count-1].z+l/2)/l));
        }
        for(int i=0; i<p.Count; i++) {
            int c2=i*2; int n2=((i+1)%p.Count)*2;
            int tl=wa+c2, bl=wa+c2+1, tr=wa+n2, br=wa+n2+1;
            t.Add(tl); t.Add(br); t.Add(tr); t.Add(tl); t.Add(bl); t.Add(br);
        }
    }
}
