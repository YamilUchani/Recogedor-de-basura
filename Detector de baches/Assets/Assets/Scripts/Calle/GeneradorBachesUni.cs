using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TipoBache
{
    Voronoi,
    Complejo,
    Hibrido
}

public class GeneradorDeBachesUnificado : MonoBehaviour
{
    #region 🎛️ Configuración General
    [Header("📦 Configuración General")]
    public TipoBache tipoBache = TipoBache.Complejo;
    [Min(1)] public int cantidadBaches = 30;
    public GameObject objetoPadre;
    public Material materialBache;
    public Material waterMaterial;
    [Range(0f, 1f)] public float probabilidadAgua = 0.5f;
    [Min(0.1f)] public float ladoArea = 4f;
    [Min(0f)] public float margenBorde = 0.5f;
    [Min(0.01f)] public float pasoCuadricula = 0.25f;
    public float alturaGeneracion = 0.01f;
    public LayerMask capasObstaculos = ~0;
    #endregion

    #region 📏 Tamaño y Resolución (compartido)
    [Header("📏 Tamaño y Resolución (compartido)")]
    [Range(0.25f, 1f)] public float minSize = 0.25f;
    [Range(0.25f, 1f)] public float maxSize = 1f;
    [Range(20, 200)] public int polygonsX = 100;
    [Range(20, 200)] public int polygonsZ = 100;
    [Range(0.01f, 0.04f)] public float profundidadMaximaGlobal = 0.02f;
    [Header("InputBorder (compartido)")]
    [Range(0.01f, 0.1f)] public float bordeMinPorcentaje = 0.1f;
    [Range(0.1f, 0.3f)] public float bordeMaxPorcentaje = 0.3f;
    public float noiseScale = 2f;
    [Range(0.1f, 5f)] public float bordeSuavidad = 1.5f;
    public float uvScale = 1f;
    #endregion

    #region 🐊 Parámetros Voronoi
    [Header("🐊 Parámetros Voronoi (solo si Tipo = Voronoi o Hibrido)")]
    [Range(8, 20)] public int numSites = 10;
    [Range(0.03f, 0.04f)] public float escalaCelda = 0.035f;
    public float borderDepth = -0.5f;
    [Range(0.3f, 0.5f)] public float roundness = 0.4f;
    [Range(10f, 20f)] public float bordeMinPerc = 12f;
    [Range(25f, 30f)] public float bordeMaxPerc = 27f;
    #endregion

    #region 🧱 Parámetros Bache Complejo
    [Header("🧱 Parámetros Bache Complejo (solo si Tipo = Complejo o Hibrido)")]
    [Range(1, 20)] public int cantidadDeformacionesPorBache = 3;
    [Range(12f, 20f)] public float radioPorcentaje = 10f;
    [Range(0.1f, 0.14f)] public float profundidad = 0.5f;
    [Range(0f, 0.1f)] public float variacionProfundidad = 0.5f;
    [Range(0.02f, 0.1f)] public float deformacion = 0.2f;
    [Range(0.2f, 1f)] public float irregularidadBorde = 0.6f;
    [Range(0.3f, 0.7f)] public float fondoPlano = 0.3f;
    [Range(4f, 5f)] public float suavidad = 1.5f;
    #endregion

    [Header("👁️ Debug Visual")]
    public bool mostrarZonaDeteccion = true;
    public float duracionDebug = 5f;

    public bool generacionTerminada = false;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(GenerarBachesCoroutine());
    }

    private IEnumerator GenerarBachesCoroutine()
    {
        float medioLado = ladoArea * 0.5f;
        float xmin = -medioLado + margenBorde;
        float xmax = medioLado - margenBorde;
        float zmin = xmin;
        float zmax = xmax;
        Vector3 centro = transform.position;
        int generados = 0;
        int intentos = 0;
        int maxIntentos = cantidadBaches * 10;
        int attemptsThisFrame = 0;
        float elevacionDeteccion = 0.02f;

        float minX = centro.x - medioLado;
        float maxX = centro.x + medioLado;
        float minZ = centro.z - medioLado;
        float maxZ = centro.z + medioLado;

        while (generados < cantidadBaches && intentos < maxIntentos)
        {
            intentos++;
            attemptsThisFrame++;
            if (attemptsThisFrame > 100)
            {
                yield return null;
                attemptsThisFrame = 0;
            }

            float xL = Random.Range(xmin, xmax);
            float zL = Random.Range(zmin, zmax);
            xL = Mathf.Round(xL / pasoCuadricula) * pasoCuadricula;
            zL = Mathf.Round(zL / pasoCuadricula) * pasoCuadricula;
            Vector3 posicion = centro + new Vector3(xL, alturaGeneracion, zL);
            float width = Random.Range(minSize, maxSize);
            float length = Random.Range(minSize, maxSize);

            TipoBache tipoActual = tipoBache;
            if (tipoBache == TipoBache.Hibrido)
            {
                tipoActual = (generados % 2 == 0) ? TipoBache.Voronoi : TipoBache.Complejo;
            }

            // Verificar que esté dentro del área global (aproximación exacta en horizontal)
            float minBX = posicion.x - width / 2;
            float maxBX = posicion.x + width / 2;
            float minBZ = posicion.z - length / 2;
            float maxBZ = posicion.z + length / 2;
            if (minBX < minX || maxBX > maxX || minBZ < minZ || maxBZ > maxZ)
            {
                continue;
            }

            // Aproximar bounds para overlap check
            float maxDepth = (tipoActual == TipoBache.Voronoi) ? Mathf.Abs(borderDepth) : profundidadMaximaGlobal;
            Vector3 approxCenter = posicion + Vector3.down * maxDepth / 2;
            Vector3 approxHalfExtents = new Vector3(width / 2, maxDepth / 2, length / 2);
            Vector3 centroElevado = approxCenter + Vector3.up * elevacionDeteccion;

            Collider[] hits = Physics.OverlapBox(
                centroElevado,
                approxHalfExtents,
                Quaternion.identity,
                capasObstaculos,
                QueryTriggerInteraction.Collide
            );

            if (hits.Length > 0)
            {
                continue;
            }

            // ✅ Todo bien, crear el bache
            GameObject bache;
            if (tipoActual == TipoBache.Voronoi)
                bache = CrearBacheVoronoi(width, length, posicion);
            else
                bache = CrearBacheComplejo(posicion, width, length);

            bache.name = $"bache_{generados + 1} ({tipoActual})";

            Renderer rend = bache.GetComponent<Renderer>();
            if (rend == null)
            {
                Destroy(bache);
                continue;
            }

            CrearPlanoDeAgua(bache);
            if (objetoPadre != null)
                bache.transform.SetParent(objetoPadre.transform);

            if (mostrarZonaDeteccion)
            {
                CrearVisualizadorOverlap(bache, centroElevado, approxHalfExtents * 2, duracionDebug);
            }

            generados++;
            yield return null; // Espaciar la creación pesada
        }

        if (generados < cantidadBaches)
            Debug.LogWarning($"Solo se generaron {generados} de {cantidadBaches} baches.");

        generacionTerminada = true;
    }

    // 🎨 Crea un cubo rojo intenso como hijo del bache para visualizar el OverlapBox
    private void CrearVisualizadorOverlap(GameObject bachePadre, Vector3 worldCenter, Vector3 size, float duracion)
    {
        GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugCube.name = "OverlapBox_Debug";
        debugCube.transform.parent = bachePadre.transform;
        debugCube.transform.position = worldCenter;
        debugCube.transform.localScale = size;

        var rend = debugCube.GetComponent<Renderer>();
        rend.material.color = new Color(1f, 0f, 0f, 0.4f);
        Destroy(debugCube.GetComponent<Collider>());
        Destroy(debugCube, duracion);
    }

    #region 🧱 Creación de Baches (Voronoi)
    private GameObject CrearBacheVoronoi(float w, float l, Vector3 position)
    {
        GameObject go = new GameObject("BacheVoronoi");
        go.transform.position = position;
        go.tag = "Crocodile"; // 👈 TAG
        go.layer = 7; // Set layer to Obstacles (7)
        go.isStatic = true; // Set as static

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mc = go.AddComponent<MeshCollider>(); // 👈 COLLIDER

        if (materialBache != null) mr.material = materialBache;

        Mesh mesh = GenerarMeshBacheVoronoi(w, l);
        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        mc.convex = false;
        mc.enabled = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.hideFlags = HideFlags.HideInInspector;

        return go;
    }

    private Mesh GenerarMeshBacheVoronoi(float width, float length)
    {
        int nx = polygonsX + 1;
        int nz = polygonsZ + 1;
        Vector3[] vertices = new Vector3[nx * nz];
        Vector2[] uvs = new Vector2[nx * nz];
        List<Vector2> sites = new List<Vector2>();
        for (int i = 0; i < numSites; i++)
            sites.Add(new Vector2(Random.value * width, Random.value * length));
        float bordeMinMetros = Mathf.Min(width, length) * (bordeMinPerc * 0.01f);
        float bordeMaxMetros = Mathf.Min(width, length) * (bordeMaxPerc * 0.01f);
        float[] bordeIzq = new float[nz];
        float[] bordeDer = new float[nz];
        float[] bordeDetras = new float[nx];
        float[] bordeFrente = new float[nx];
        for (int z = 0; z < nz; z++)
        {
            float r = (float)z / (nz - 1);
            bordeIzq[z] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(0.1f, r * noiseScale));
            bordeDer[z] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(10.73f, r * noiseScale));
        }
        for (int x = 0; x < nx; x++)
        {
            float r = (float)x / (nx - 1);
            bordeDetras[x] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(r * noiseScale, 0.3f));
            bordeFrente[x] = Mathf.Lerp(bordeMinMetros, bordeMaxMetros, Mathf.PerlinNoise(r * noiseScale, 15.41f));
        }
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        for (int z = 0; z < nz; z++)
        {
            float localZ = (float)z / polygonsZ * length;
            float worldZ = localZ - halfL;
            for (int x = 0; x < nx; x++)
            {
                float localX = (float)x / polygonsX * width;
                float worldX = localX - halfW;
                float dLeft = localX - bordeIzq[z];
                float dRight = (width - bordeDer[z]) - localX;
                float dBack = localZ - bordeDetras[x];
                float dFront = (length - bordeFrente[x]) - localZ;
                float minDistToBorder = Mathf.Min(dLeft, dRight, dBack, dFront);
                float bordeFactor = 0f;
                if (minDistToBorder > 0f)
                {
                    float r = Mathf.Clamp01(minDistToBorder / (Mathf.Max(width, length) * 0.5f) * bordeSuavidad);
                    bordeFactor = 1f - (1f - r) * (1f - r);
                }
                float height = CalculateVoronoiSteppedHeight(new Vector2(localX, localZ), sites);
                float finalHeight = height * bordeFactor;
                int idx = z * nx + x;
                vertices[idx] = new Vector3(worldX, finalHeight, worldZ);
                uvs[idx] = new Vector2(worldX * uvScale, worldZ * uvScale);
            }
        }
        int[] triangles = new int[polygonsX * polygonsZ * 6];
        int t = 0;
        for (int row = 0; row < polygonsZ; row++)
        {
            for (int col = 0; col < polygonsX; col++)
            {
                int v0 = row * nx + col;
                int v1 = v0 + 1;
                int v2 = (row + 1) * nx + col;
                int v3 = v2 + 1;
                triangles[t++] = v0; triangles[t++] = v2; triangles[t++] = v1;
                triangles[t++] = v1; triangles[t++] = v2; triangles[t++] = v3;
            }
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (materialBache != null && materialBache.HasProperty("_BumpMap"))
            mesh.tangents = CalculateTangents(vertices, uvs, triangles, mesh.normals);
        return mesh;
    }

    private float CalculateVoronoiSteppedHeight(Vector2 point, List<Vector2> sites)
    {
        float f1 = float.MaxValue;
        float f2 = float.MaxValue;
        Vector2 scaledPoint = point / escalaCelda;
        foreach (var site in sites)
        {
            float dist = Vector2.Distance(scaledPoint, site / escalaCelda);
            if (dist < f1) { f2 = f1; f1 = dist; }
            else if (dist < f2) { f2 = dist; }
        }
        float ridgeDistance = f2 - f1;
        float t = Mathf.Clamp01(ridgeDistance / roundness);
        float blend = Mathf.SmoothStep(0f, 1f, t);
        return Mathf.Lerp(0f, borderDepth, 1f - blend);
    }
    #endregion

    #region 🧱 Creación de Baches (Complejo)
    private GameObject CrearBacheComplejo(Vector3 position, float width, float length)
    {
        GameObject go = new GameObject("BacheComplejo");
        go.transform.position = position;
        go.tag = "Pothole"; // 👈 TAG
        go.layer = 7; // Set layer to Obstacles (7)
        go.isStatic = true; // Set as static

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mc = go.AddComponent<MeshCollider>(); // 👈 COLLIDER

        if (materialBache != null) mr.material = materialBache;

        Mesh mesh = GenerarMeshBacheComplejo(width, length);
        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        mc.convex = false;
        mc.enabled = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.hideFlags = HideFlags.HideInInspector;

        return go;
    }

    private Mesh GenerarMeshBacheComplejo(float width, float length)
    {
        int nx = polygonsX + 1;
        int nz = polygonsZ + 1;
        Vector3[] vertices = new Vector3[nx * nz];
        Vector2[] uvs = new Vector2[nx * nz];
        float stepX = width / polygonsX;
        float stepZ = length / polygonsZ;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        float tamañoPromedio = (width + length) * 0.5f;
        float bordeMinW = width * bordeMinPorcentaje;
        float bordeMaxW = width * bordeMaxPorcentaje;
        float bordeMinL = length * bordeMinPorcentaje;
        float bordeMaxL = length * bordeMaxPorcentaje;
        float[] bordeIzq = new float[nz];
        float[] bordeDer = new float[nz];
        float[] bordeDetras = new float[nx];
        float[] bordeFrente = new float[nx];
        for (int z = 0; z < nz; z++)
        {
            float r = (float)z / (nz - 1);
            bordeIzq[z] = Mathf.Lerp(bordeMinW, bordeMaxW, Mathf.PerlinNoise(0.1f, r * noiseScale));
            bordeDer[z] = Mathf.Lerp(bordeMinW, bordeMaxW, Mathf.PerlinNoise(10.73f, r * noiseScale));
        }
        for (int x = 0; x < nx; x++)
        {
            float r = (float)x / (nx - 1);
            bordeDetras[x] = Mathf.Lerp(bordeMinL, bordeMaxL, Mathf.PerlinNoise(r * noiseScale, 0.3f));
            bordeFrente[x] = Mathf.Lerp(bordeMinL, bordeMaxL, Mathf.PerlinNoise(r * noiseScale, 15.41f));
        }
        List<(Vector2 posPorcentaje, int semilla)> deformaciones = new List<(Vector2, int)>();
        float margenPorcentaje = Mathf.Max(bordeMaxPorcentaje, bordeMinPorcentaje) * 100f;
        margenPorcentaje = Mathf.Clamp(margenPorcentaje * 10f, 5f, 20f);
        float minPerc = margenPorcentaje;
        float maxPerc = 100f - margenPorcentaje;
        for (int i = 0; i < cantidadDeformacionesPorBache; i++)
        {
            deformaciones.Add((
                new Vector2(Random.Range(minPerc, maxPerc), Random.Range(minPerc, maxPerc)),
                Random.Range(1000, 100000)
            ));
        }
        int idx = 0;
        for (int z = 0; z < nz; z++)
        {
            float localZ = z * stepZ;
            float worldZ = localZ - halfL;
            for (int x = 0; x < nx; x++)
            {
                float localX = x * stepX;
                float worldX = localX - halfW;
                float dLeft = localX - bordeIzq[z];
                float dRight = (width - bordeDer[z]) - localX;
                float dBack = localZ - bordeDetras[x];
                float dFront = (length - bordeFrente[x]) - localZ;
                float minDistToBorder = Mathf.Min(dLeft, dRight, dBack, dFront);
                float bordeFactor = 0f;
                if (minDistToBorder > 0f)
                {
                    float r = Mathf.Clamp01(minDistToBorder / (Mathf.Max(width, length) * 0.5f) * bordeSuavidad);
                    bordeFactor = 1f - (1f - r) * (1f - r);
                }
                float sumaDepresiones = 0f;
                if (bordeFactor > 0f)
                {
                    foreach (var (posPorcentaje, semilla) in deformaciones)
                    {
                        float profundidadUsable = profundidadMaximaGlobal * profundidad;
                        float deformWorldX = (posPorcentaje.x * 0.01f) * width - halfW;
                        float deformWorldZ = (posPorcentaje.y * 0.01f) * length - halfL;
                        float radioMetros = tamañoPromedio * (radioPorcentaje * 0.01f);
                        float dx = worldX - deformWorldX;
                        float dz = worldZ - deformWorldZ;
                        float dist = Mathf.Sqrt(dx * dx + dz * dz);
                        if (dist >= radioMetros && irregularidadBorde <= 0f) continue;
                        float radioEfectivo = radioMetros;
                        if (irregularidadBorde > 0f && dist > 0.001f)
                        {
                            float angle = Mathf.Atan2(dz, dx);
                            float n1 = Mathf.PerlinNoise(angle * 1.3f + semilla * 0.1f, 0f);
                            float n2 = Mathf.PerlinNoise(angle * 2.7f + semilla * 0.1f + 10f, 0f);
                            float n3 = Mathf.PerlinNoise(angle * 5.1f + semilla * 0.1f + 20f, 0f);
                            float angular = n1 * 0.6f + n2 * 0.3f + n3 * 0.1f;
                            float factor = 1f + (angular - 0.5f) * 2f * irregularidadBorde;
                            radioEfectivo = radioMetros * Mathf.Max(0.3f, factor);
                        }
                        if (dist >= radioEfectivo) continue;
                        Vector2 desplazamiento = Vector2.zero;
                        if (deformacion > 0f)
                        {
                            float scale = 5f / Mathf.Max(radioMetros, 0.01f);
                            Vector2 noiseVec = NoiseUtils.VectorNoise2D(dx * scale, dz * scale, semilla);
                            desplazamiento = noiseVec * deformacion * radioMetros;
                        }
                        Vector2 puntoRelativo = new Vector2(dx, dz) - desplazamiento;
                        float distDeformada = puntoRelativo.magnitude;
                        float r = distDeformada / radioEfectivo;
                        if (r >= 1f) continue;
                        float factorProfundidad = 1f;
                        if (variacionProfundidad > 0f)
                        {
                            float scaleProf = 8f / Mathf.Max(radioMetros, 0.01f);
                            float noiseProf = NoiseUtils.Fbm2D(dx * scaleProf, dz * scaleProf, 4, 2f, 0.5f);
                            factorProfundidad = 1f + (noiseProf - 0.5f) * 2f * variacionProfundidad;
                            factorProfundidad = Mathf.Clamp(factorProfundidad, 0f, 1.5f);
                        }
                        float factorPerfil = (r <= fondoPlano)
                            ? 1f
                            : 1f - Mathf.Pow((r - fondoPlano) / (1f - fondoPlano), suavidad);
                        sumaDepresiones += profundidadUsable * factorPerfil * factorProfundidad;
                    }
                    sumaDepresiones = Mathf.Min(sumaDepresiones, profundidadMaximaGlobal);
                }
                vertices[idx] = new Vector3(worldX, -sumaDepresiones * bordeFactor, worldZ);
                uvs[idx] = new Vector2(worldX * uvScale, worldZ * uvScale);
                idx++;
            }
        }
        int[] triangles = new int[polygonsX * polygonsZ * 6];
        int t = 0;
        for (int row = 0; row < polygonsZ; row++)
        {
            for (int col = 0; col < polygonsX; col++)
            {
                int v0 = row * nx + col;
                int v1 = v0 + 1;
                int v2 = (row + 1) * nx + col;
                int v3 = v2 + 1;
                triangles[t++] = v0; triangles[t++] = v2; triangles[t++] = v1;
                triangles[t++] = v1; triangles[t++] = v2; triangles[t++] = v3;
            }
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (materialBache != null && materialBache.HasProperty("_BumpMap"))
            mesh.tangents = CalculateTangents(vertices, uvs, triangles, mesh.normals);
        return mesh;
    }
    #endregion

    #region 🧠 Utilidades Compartidas
    private void CrearPlanoDeAgua(GameObject bache)
    {
        if (Random.value > probabilidadAgua) return;
        Renderer rend = bache.GetComponent<Renderer>();
        if (rend == null) return;
        Bounds bounds = rend.bounds;
        float aguaY = bounds.min.y + 0.01f;
        Vector3 center = bounds.center;
        GameObject plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Destroy(plano.GetComponent<MeshCollider>());
        plano.transform.SetParent(bache.transform);
        plano.transform.position = new Vector3(center.x, aguaY, center.z);
        plano.transform.localRotation = Quaternion.identity;
        plano.transform.localScale = new Vector3(bounds.size.x / 10f, 1, bounds.size.z / 10f);
        if (waterMaterial != null)
            plano.GetComponent<Renderer>().material = waterMaterial;
    }

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
            Vector3 n = normals[i];
            Vector3 t = tan1[i];
            Vector3.OrthoNormalize(ref n, ref t);
            tangents[i] = new Vector4(t.x, t.y, t.z, (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f);
        }
        return tangents;
    }
    #endregion
}