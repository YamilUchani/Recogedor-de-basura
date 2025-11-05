using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Modo de generación del terreno: manual (baches definidos por el usuario) o aleatorio.
/// </summary>
public enum ModoGeneracion
{
    Manual,
    Aleatorio
}

/// <summary>
/// Representa un bache individual con parámetros detallados para su forma, profundidad y deformación.
/// </summary>
[System.Serializable]
public class Bache
{
    [Tooltip("Posición en porcentaje: X=0(izq) a 100(der), Y=0(atrás) a 100(frente)")]
    public Vector2 posicionPorcentaje = new Vector2(50f, 50f);

    [Tooltip("Radio como % del tamaño promedio del terreno")]
    [Range(1f, 50f)]
    public float radioPorcentaje = 10f;

    [Tooltip("Profundidad base en metros (absoluta)")]
    [Range(0.01f, 1f)]
    public float profundidad = 0.1f;

    [Tooltip("Variación de profundidad interna (0 = uniforme, 1 = muy caótico)")]
    [Range(0f, 1f)]
    public float variacionProfundidad = 0.5f;

    [Tooltip("Intensidad de deformación de forma (ruido local)")]
    [Range(0f, 1f)]
    public float deformacion = 0.2f;

    [Tooltip("Irregularidad de la forma base del borde (0 = circular, 1 = muy orgánico)")]
    [Range(0f, 1f)]
    public float irregularidadBorde = 0.6f;

    [Tooltip("Fracción del radio que es fondo plano (0 = puntiagudo, 0.8 = muy plano)")]
    [Range(0f, 0.9f)]
    public float fondoPlano = 0.3f;

    [Tooltip("Controla la suavidad de la transición desde el fondo plano al borde")]
    [Range(0.1f, 5f)]
    public float suavidad = 1.5f;

    [Tooltip("Semilla única para este bache (determina el ruido interno)")]
    public int semilla = 12345;
}

/// <summary>
/// Utilidades para generar ruido procedural 2D (Perlin + FBM).
/// </summary>
public static class NoiseUtils
{
    /// <summary>
    /// Genera ruido fractal (FBM) en 2D usando PerlinNoise de Unity.
    /// </summary>
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

    /// <summary>
    /// Genera un vector de desplazamiento 2D basado en ruido FBM (útil para deformar formas).
    /// </summary>
    public static Vector2 VectorNoise2D(float x, float y, int seedOffset = 0)
    {
        float angle = Fbm2D(x + seedOffset, y + seedOffset, 2, 2f, 0.6f) * Mathf.PI * 2f;
        float mag = Fbm2D(x + seedOffset + 100, y + seedOffset + 200, 2, 2f, 0.5f);
        return new Vector2(Mathf.Cos(angle) * mag, Mathf.Sin(angle) * mag);
    }
}

/// <summary>
/// Generador de mallas de terreno con baches personalizables o aleatorios.
/// Crea un GameObject hijo con MeshFilter y MeshRenderer.
/// Funciona en tiempo de edición (gracias a [ExecuteInEditMode]).
/// </summary>
[ExecuteInEditMode]
public class TerrainBacheGenerator : MonoBehaviour
{
    // ─── Parámetros públicos expuestos en el Inspector ───────────────────────

    public ModoGeneracion modo = ModoGeneracion.Manual;

    // Configuración para modo aleatorio
    public int cantidadBachesAleatorios = 10;
    public float minRadioPorcentaje = 3f, maxRadioPorcentaje = 15f;
    public float minProfundidad = 0.05f, maxProfundidad = 0.2f;
    public float minDeformacion = 0.2f, maxDeformacion = 0.6f;
    public float minIrregularidadBorde = 0.4f, maxIrregularidadBorde = 0.9f;
    public float minFondoPlano = 0.2f, maxFondoPlano = 0.6f;
    public float minVariacionProf = 0.3f, maxVariacionProf = 0.7f;

    // Dimensiones del terreno
    public float width = 2f, length = 2f;

    // Resolución de la malla
    [Range(20, 200)] public int polygonsX = 100;
    [Range(20, 200)] public int polygonsZ = 100;

    // Borde exterior suave
    public float bordeMin = 0.1f, bordeMax = 0.3f;
    public float noiseScale = 2f;
    [Range(0.1f, 5f)] public float bordeSuavidad = 1.5f;

    // Límite global de profundidad (evita agujeros demasiado profundos)
    [Range(0.01f, 1f)] public float profundidadMaximaGlobal = 0.2f;

    // Lista de baches en modo manual
    public List<Bache> baches = new List<Bache>
    {
        new Bache { /* valores por defecto */ }
    };

    // Material y nombre del objeto generado
    public Material material;
    public string nombreObjeto = "TerrenoConBaches";

    // Referencia al objeto generado (para destruirlo al regenerar)
    private GameObject terreno;

    // ─── Métodos públicos ───────────────────────────────────────────────────

    /// <summary>
    /// Genera una nueva malla de terreno con baches y la asigna a un nuevo GameObject hijo.
    /// Se puede llamar desde el botón "Generar" en el inspector ([ContextMenu]).
    /// </summary>
    [ContextMenu("Generar")]
    public void GenerarMeshEnNuevoObjeto()
    {
        // Destruir instancia anterior
        if (terreno != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(terreno);
#else
            Destroy(terreno);
#endif
        }

        // Crear nuevo objeto
        terreno = new GameObject(nombreObjeto);
        terreno.transform.SetParent(transform, false);
        terreno.transform.localPosition = Vector3.zero;
        terreno.transform.localRotation = Quaternion.identity;

        var mf = terreno.AddComponent<MeshFilter>();
        var mr = terreno.AddComponent<MeshRenderer>();
        if (material != null) mr.sharedMaterial = material;

        var mesh = new Mesh { name = "Mesh_" + nombreObjeto };

        // Precálculos geométricos
        float stepX = width / polygonsX;
        float stepZ = length / polygonsZ;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        float tamañoPromedio = (width + length) * 0.5f;

        // Precomputar el perfil del borde exterior (usando ruido para hacerlo orgánico)
        float[] bordeIzq = new float[polygonsZ + 1];
        float[] bordeDer = new float[polygonsZ + 1];
        float[] bordeDetras = new float[polygonsX + 1];
        float[] bordeFrente = new float[polygonsX + 1];

        for (int z = 0; z <= polygonsZ; z++)
        {
            float t = (float)z / polygonsZ;
            bordeIzq[z] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(0.1f, t * noiseScale));
            bordeDer[z] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(10.73f, t * noiseScale));
        }
        for (int x = 0; x <= polygonsX; x++)
        {
            float t = (float)x / polygonsX;
            bordeDetras[x] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(t * noiseScale, 0.3f));
            bordeFrente[x] = Mathf.Lerp(bordeMin, bordeMax, Mathf.PerlinNoise(t * noiseScale, 15.41f));
        }

        // Obtener lista de baches a aplicar
        List<Bache> bachesUsar = (modo == ModoGeneracion.Manual) ? baches : GenerarBachesAleatorios();

        // Generar vértices
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

                // Calcular cuánto afecta el borde exterior en este punto
                float dLeft   = localX - bordeIzq[z];
                float dRight  = (width - bordeDer[z]) - localX;
                float dBack   = localZ - bordeDetras[x];
                float dFront  = (length - bordeFrente[x]) - localZ;
                float minDistToBorder = Mathf.Min(dLeft, dRight, dBack, dFront);

                float bordeFactor = 0f;
                if (minDistToBorder > 0f)
                {
                    float t = Mathf.Clamp01(minDistToBorder / (Mathf.Max(width, length) * 0.5f) * bordeSuavidad);
                    bordeFactor = 1f - (1f - t) * (1f - t); // Curva suave (ease-in-out)
                }

                float sumaBaches = 0f;
                if (bordeFactor > 0f)
                {
                    foreach (var bache in bachesUsar)
                    {
                        float profundidadUsable = Mathf.Min(bache.profundidad, profundidadMaximaGlobal);
                        float bacheWorldX = (bache.posicionPorcentaje.x * 0.01f) * width - halfW;
                        float bacheWorldZ = (bache.posicionPorcentaje.y * 0.01f) * length - halfL;
                        float radioMetros = tamañoPromedio * (bache.radioPorcentaje * 0.01f);

                        float dx = worldX - bacheWorldX;
                        float dz = worldZ - bacheWorldZ;
                        float dist = Mathf.Sqrt(dx * dx + dz * dz);

                        // Si está fuera del radio base y no hay irregularidad, saltar
                        if (dist >= radioMetros && bache.irregularidadBorde <= 0f) continue;

                        // Ajustar radio según irregularidad del borde
                        float radioEfectivo = radioMetros;
                        if (bache.irregularidadBorde > 0f && dist > 0.001f)
                        {
                            float angle = Mathf.Atan2(dz, dx);
                            float n1 = Mathf.PerlinNoise(angle * 1.3f + bache.semilla * 0.1f, 0f);
                            float n2 = Mathf.PerlinNoise(angle * 2.7f + bache.semilla * 0.1f + 10f, 0f);
                            float n3 = Mathf.PerlinNoise(angle * 5.1f + bache.semilla * 0.1f + 20f, 0f);
                            float angular = n1 * 0.6f + n2 * 0.3f + n3 * 0.1f;
                            float factor = 1f + (angular - 0.5f) * 2f * bache.irregularidadBorde;
                            radioEfectivo = radioMetros * Mathf.Max(0.3f, factor);
                        }

                        if (dist >= radioEfectivo) continue;

                        // Aplicar deformación local (ruido vectorial)
                        Vector2 desplazamiento = Vector2.zero;
                        if (bache.deformacion > 0f)
                        {
                            float scale = 5f / Mathf.Max(radioMetros, 0.01f);
                            Vector2 noiseVec = NoiseUtils.VectorNoise2D(dx * scale, dz * scale, bache.semilla);
                            desplazamiento = noiseVec * bache.deformacion * radioMetros;
                        }

                        Vector2 puntoRelativo = new Vector2(dx, dz) - desplazamiento;
                        float distDeformada = puntoRelativo.magnitude;
                        float t = distDeformada / radioEfectivo;
                        if (t >= 1f) continue;

                        // Variación de profundidad interna (ruido escalar)
                        float factorProfundidad = 1f;
                        if (bache.variacionProfundidad > 0f)
                        {
                            float scaleProf = 8f / Mathf.Max(radioMetros, 0.01f);
                            float noiseProf = NoiseUtils.Fbm2D(dx * scaleProf, dz * scaleProf, 4, 2f, 0.5f);
                            factorProfundidad = 1f + (noiseProf - 0.5f) * 2f * bache.variacionProfundidad;
                            factorProfundidad = Mathf.Clamp(factorProfundidad, 0f, 1.5f);
                        }

                        // Perfil de profundidad: fondo plano + transición suave
                        float factorPerfil = (t <= bache.fondoPlano)
                            ? 1f
                            : 1f - Mathf.Pow((t - bache.fondoPlano) / (1f - bache.fondoPlano), bache.suavidad);

                        sumaBaches += profundidadUsable * factorPerfil * factorProfundidad;
                    }

                    sumaBaches = Mathf.Min(sumaBaches, profundidadMaximaGlobal);
                }

                vertices[index++] = new Vector3(worldX, -sumaBaches * bordeFactor, worldZ);
            }
        }

        // Generar triángulos en orden de rejilla
        int[] triangles = new int[polygonsX * polygonsZ * 6];
        int tIndex = 0;
        for (int z = 0; z < polygonsZ; z++)
        {
            for (int x = 0; x < polygonsX; x++)
            {
                int v = z * (polygonsX + 1) + x;
                triangles[tIndex++] = v;
                triangles[tIndex++] = v + polygonsX + 1;
                triangles[tIndex++] = v + 1;
                triangles[tIndex++] = v + 1;
                triangles[tIndex++] = v + polygonsX + 1;
                triangles[tIndex++] = v + polygonsX + 2;
            }
        }

        // Asignar datos a la malla
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }

    // ─── Métodos privados ───────────────────────────────────────────────────

    /// <summary>
    /// Genera una lista de baches con parámetros aleatorios dentro de los rangos definidos.
    /// Evita colocar baches demasiado cerca del borde usando un margen dinámico.
    /// </summary>
    private List<Bache> GenerarBachesAleatorios()
    {
        List<Bache> resultado = new List<Bache>();

        // Calcular margen seguro en porcentaje (basado en bordeMax)
        float margenPorcentaje = Mathf.Max(bordeMax / width, bordeMax / length) * 100f;
        margenPorcentaje = Mathf.Clamp(margenPorcentaje, 5f, 20f);

        float minPerc = margenPorcentaje;
        float maxPerc = 100f - margenPorcentaje;

        for (int i = 0; i < cantidadBachesAleatorios; i++)
        {
            resultado.Add(new Bache
            {
                posicionPorcentaje = new Vector2(
                    Random.Range(minPerc, maxPerc),
                    Random.Range(minPerc, maxPerc)
                ),
                radioPorcentaje = Random.Range(minRadioPorcentaje, maxRadioPorcentaje),
                profundidad = Random.Range(minProfundidad, maxProfundidad),
                deformacion = Random.Range(minDeformacion, maxDeformacion),
                irregularidadBorde = Random.Range(minIrregularidadBorde, maxIrregularidadBorde),
                fondoPlano = Random.Range(minFondoPlano, maxFondoPlano),
                variacionProfundidad = Random.Range(minVariacionProf, maxVariacionProf),
                suavidad = Random.Range(1f, 3f),
                semilla = Random.Range(1000, 100000)
            });
        }

        return resultado;
    }
}