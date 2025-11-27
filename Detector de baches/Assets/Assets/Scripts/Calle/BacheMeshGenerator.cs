using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utilidades para generar ruido procedural 2D (Perlin + FBM).
/// </summary>
public static class NoiseUtils
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

/// <summary>
/// Generador de mallas de terreno con baches aleatorios.
/// Todos los baches usan los mismos parámetros globales.
/// Solo varían en posición y semilla (para variación procedural).
/// </summary>
[ExecuteInEditMode]
public class BacheMeshGenerator : MonoBehaviour
{
    [Range(10, 20)] public int cantidadBachesAleatorios = 10;
    [Range(12f, 20f)] public float radioPorcentaje = 10f;
    [Range(0.1f, 0.14f)] public float profundidad = 0.5f; // porcentaje de profundidadMaximaGlobal
    [Range(0f, 0.1f)] public float variacionProfundidad = 0.5f;
    [Range(0.02f, 0.1f)] public float deformacion = 0.2f;
    [Range(0.2f, 1f)] public float irregularidadBorde = 0.6f;
    [Range(0.5f, 2f)] public float fondoPlano = 0.3f;
    [Range(0.5f, 5f)] public float suavidad = 1.5f;

    public float width = 2f, length = 2f;
    [Range(20, 200)] public int polygonsX = 100;
    [Range(20, 200)] public int polygonsZ = 100;

    [Header("Bordes del terreno (porcentaje del tamaño total)")]
    [Range(0f, 0.1f)] public float bordeMinPorcentaje = 0.1f;
    [Range(0f, 0.3f)] public float bordeMaxPorcentaje = 0.3f;
    public float noiseScale = 2f;
    [Range(0f, 5f)] public float bordeSuavidad = 1.5f;

    [Range(0f, 0.1f)] public float profundidadMaximaGlobal = 0.02f;
    public Material material;
    public string nombreObjeto = "TerrenoConBaches";

    private GameObject terreno;

    [ContextMenu("Generar")]
    public void GenerarMeshEnNuevoObjeto()
    {
        if (terreno != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(terreno);
#else
            Destroy(terreno);
#endif
        }

        terreno = new GameObject(nombreObjeto);
        terreno.transform.SetParent(transform, false);
        var mf = terreno.AddComponent<MeshFilter>();
        var mr = terreno.AddComponent<MeshRenderer>();
        if (material != null) mr.sharedMaterial = material;

        Mesh mesh = new Mesh { name = "Mesh_" + nombreObjeto };

        float stepX = width / polygonsX;
        float stepZ = length / polygonsZ;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        float tamañoPromedio = (width + length) * 0.5f;

        // Precomputar bordes
        float bordeMinW = width * bordeMinPorcentaje;
        float bordeMaxW = width * bordeMaxPorcentaje;
        float bordeMinL = length * bordeMinPorcentaje;
        float bordeMaxL = length * bordeMaxPorcentaje;

        float[] bordeIzq = new float[polygonsZ + 1];
        float[] bordeDer = new float[polygonsZ + 1];
        float[] bordeDetras = new float[polygonsX + 1];
        float[] bordeFrente = new float[polygonsX + 1];

        for (int z = 0; z <= polygonsZ; z++)
        {
            float t = (float)z / polygonsZ;
            bordeIzq[z] = Mathf.Lerp(bordeMinW, bordeMaxW, Mathf.PerlinNoise(0.1f, t * noiseScale));
            bordeDer[z] = Mathf.Lerp(bordeMinW, bordeMaxW, Mathf.PerlinNoise(10.73f, t * noiseScale));
        }
        for (int x = 0; x <= polygonsX; x++)
        {
            float t = (float)x / polygonsX;
            bordeDetras[x] = Mathf.Lerp(bordeMinL, bordeMaxL, Mathf.PerlinNoise(t * noiseScale, 0.3f));
            bordeFrente[x] = Mathf.Lerp(bordeMinL, bordeMaxL, Mathf.PerlinNoise(t * noiseScale, 15.41f));
        }

        // Generar posiciones y semillas de baches
        List<(Vector2 posPorcentaje, int semilla)> baches = new List<(Vector2, int)>();
        float margenPorcentaje = Mathf.Max(bordeMaxPorcentaje, bordeMinPorcentaje) * 100f;
        margenPorcentaje = Mathf.Clamp(margenPorcentaje * 10f, 5f, 20f);
        float minPerc = margenPorcentaje;
        float maxPerc = 100f - margenPorcentaje;

        for (int i = 0; i < cantidadBachesAleatorios; i++)
        {
            baches.Add((
                new Vector2(Random.Range(minPerc, maxPerc), Random.Range(minPerc, maxPerc)),
                Random.Range(1000, 100000)
            ));
        }

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

                // Calcular factor de borde
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

                float sumaBaches = 0f;
                if (bordeFactor > 0f)
                {
                    foreach (var (posPorcentaje, semilla) in baches)
                    {
                        float profundidadUsable = profundidadMaximaGlobal * profundidad;
                        float bacheWorldX = (posPorcentaje.x * 0.01f) * width - halfW;
                        float bacheWorldZ = (posPorcentaje.y * 0.01f) * length - halfL;
                        float radioMetros = tamañoPromedio * (radioPorcentaje * 0.01f);

                        float dx = worldX - bacheWorldX;
                        float dz = worldZ - bacheWorldZ;
                        float dist = Mathf.Sqrt(dx * dx + dz * dz);

                        // Saltear si está fuera del radio (y no hay irregularidad)
                        if (dist >= radioMetros && irregularidadBorde <= 0f) continue;

                        // Aplicar irregularidad en el borde
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

                        // Aplicar deformación
                        Vector2 desplazamiento = Vector2.zero;
                        if (deformacion > 0f)
                        {
                            float scale = 5f / Mathf.Max(radioMetros, 0.01f);
                            Vector2 noiseVec = NoiseUtils.VectorNoise2D(dx * scale, dz * scale, semilla);
                            desplazamiento = noiseVec * deformacion * radioMetros;
                        }

                        Vector2 puntoRelativo = new Vector2(dx, dz) - desplazamiento;
                        float distDeformada = puntoRelativo.magnitude;
                        float t = distDeformada / radioEfectivo;
                        if (t >= 1f) continue;

                        // Variación de profundidad
                        float factorProfundidad = 1f;
                        if (variacionProfundidad > 0f)
                        {
                            float scaleProf = 8f / Mathf.Max(radioMetros, 0.01f);
                            float noiseProf = NoiseUtils.Fbm2D(dx * scaleProf, dz * scaleProf, 4, 2f, 0.5f);
                            factorProfundidad = 1f + (noiseProf - 0.5f) * 2f * variacionProfundidad;
                            factorProfundidad = Mathf.Clamp(factorProfundidad, 0f, 1.5f);
                        }

                        // Perfil del bache
                        float factorPerfil = (t <= fondoPlano)
                            ? 1f
                            : 1f - Mathf.Pow((t - fondoPlano) / (1f - fondoPlano), suavidad);

                        sumaBaches += profundidadUsable * factorPerfil * factorProfundidad;
                    }

                    sumaBaches = Mathf.Min(sumaBaches, profundidadMaximaGlobal);
                }

                vertices[index++] = new Vector3(worldX, -sumaBaches * bordeFactor, worldZ);
            }
        }

        // Generar triángulos
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

        // Asignar mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }
}