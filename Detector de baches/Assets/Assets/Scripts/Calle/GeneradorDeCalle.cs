using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(GeneradorDeBaches))]
[DisallowMultipleComponent]
public class GeneradorDeCalle : MonoBehaviour
{
    [Header("Configuración")]
    public Material calleMaterial;
    public float alturaCalle = 0.0f;
    public float tamanoSegmento = 10.0f; // Tamaño de cada segmento de calle

    private GeneradorDeBaches _genBaches;
    private bool _generacionIniciada = false;
    private List<GameObject> _segmentosCalle = new List<GameObject>();

    private void Awake()
    {
        _genBaches = GetComponent<GeneradorDeBaches>();
    }

    private void Update()
    {
        if (!_generacionIniciada && _genBaches != null && _genBaches.generacionTerminada)
        {
            IniciarGeneracion();
            _generacionIniciada = true;
        }
    }

    private void IniciarGeneracion()
    {
        // Limpiar segmentos anteriores si existen
        foreach (var segmento in _segmentosCalle)
        {
            if (segmento != null) Destroy(segmento);
        }
        _segmentosCalle.Clear();

        // Obtener todos los baches
        List<Renderer> renderersBaches = new List<Renderer>();
        if (_genBaches.objetoPadre != null)
        {
            renderersBaches.AddRange(_genBaches.objetoPadre.GetComponentsInChildren<Renderer>());
        }
        else
        {
            renderersBaches.AddRange(GetComponentsInChildren<Renderer>());
        }

        // Configurar área total
        float ladoTotal = _genBaches.ladoArea;
        Vector3 centro = transform.position;
        float mitad = ladoTotal * 0.5f;
        Vector3 inicio = centro - new Vector3(mitad, 0, mitad);

        // Dividir en segmentos
        int numSegmentos = Mathf.CeilToInt(ladoTotal / tamanoSegmento);

        for (int zSeg = 0; zSeg < numSegmentos; zSeg++)
        {
            for (int xSeg = 0; xSeg < numSegmentos; xSeg++)
            {
                // Calcular límites del segmento actual
                float xMin = inicio.x + xSeg * tamanoSegmento;
                float zMin = inicio.z + zSeg * tamanoSegmento;
                float xMax = xMin + tamanoSegmento;
                float zMax = zMin + tamanoSegmento;

                // Crear segmento
                CrearSegmentoCalle(xMin, zMin, xMax, zMax, renderersBaches);
            }
        }
    }

    private void CrearSegmentoCalle(float xMin, float zMin, float xMax, float zMax, List<Renderer> baches)
    {
        // Crear listas para la malla
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangulos = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // 1. Empezar con el rectángulo completo del segmento
        List<Bounds> areasLibres = new List<Bounds>();
        areasLibres.Add(new Bounds(
            new Vector3((xMin + xMax) * 0.5f, alturaCalle, (zMin + zMax) * 0.5f),
            new Vector3(xMax - xMin, 0.1f, zMax - zMin)
        ));

        // 2. Procesar baches que intersectan con este segmento
        foreach (Renderer rend in baches)
        {
            if (rend.transform == transform) continue;

            Bounds bache = rend.bounds;
            Bounds segmentoBounds = new Bounds(
                new Vector3((xMin + xMax) * 0.5f, alturaCalle, (zMin + zMax) * 0.5f),
                new Vector3(xMax - xMin, 0.1f, zMax - zMin)
            );

            if (!segmentoBounds.Intersects(bache)) continue;

            List<Bounds> nuevasAreas = new List<Bounds>();
            foreach (Bounds area in areasLibres)
            {
                if (!area.Intersects(bache))
                {
                    nuevasAreas.Add(area);
                    continue;
                }

                // Dividir el área alrededor del bache
                // Izquierda
                if (bache.min.x > area.min.x)
                {
                    nuevasAreas.Add(new Bounds(
                        new Vector3((area.min.x + bache.min.x) * 0.5f, area.center.y, area.center.z),
                        new Vector3(bache.min.x - area.min.x, area.size.y, area.size.z)
                    ));
                }

                // Derecha
                if (bache.max.x < area.max.x)
                {
                    nuevasAreas.Add(new Bounds(
                        new Vector3((bache.max.x + area.max.x) * 0.5f, area.center.y, area.center.z),
                        new Vector3(area.max.x - bache.max.x, area.size.y, area.size.z)
                    ));
                }

                // Frente (Z positivo)
                if (bache.max.z < area.max.z)
                {
                    nuevasAreas.Add(new Bounds(
                        new Vector3(area.center.x, area.center.y, (bache.max.z + area.max.z) * 0.5f),
                        new Vector3(area.size.x, area.size.y, area.max.z - bache.max.z)
                    ));
                }

                // Atrás (Z negativo)
                if (bache.min.z > area.min.z)
                {
                    nuevasAreas.Add(new Bounds(
                        new Vector3(area.center.x, area.center.y, (area.min.z + bache.min.z) * 0.5f),
                        new Vector3(area.size.x, area.size.y, bache.min.z - area.min.z)
                    ));
                }
            }
            areasLibres = nuevasAreas;
        }

        // 3. Generar geometría para las áreas libres
        foreach (Bounds area in areasLibres)
        {
            AddRectangleToMesh(area.min.x, area.min.z, area.max.x, area.max.z, 
                              ref vertices, ref triangulos, ref uvs);
        }

        // 4. Crear el GameObject del segmento si tiene geometría
        if (vertices.Count > 0)
        {
            GameObject segmentoGO = new GameObject($"SegmentoCalle_{xMin}_{zMin}");
            segmentoGO.transform.SetParent(transform);
            segmentoGO.transform.localPosition = Vector3.zero;
            segmentoGO.layer = 7;
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangulos, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter mf = segmentoGO.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = segmentoGO.AddComponent<MeshRenderer>();
            mr.material = calleMaterial;

            MeshCollider mc = segmentoGO.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            _segmentosCalle.Add(segmentoGO);
        }
    }

    private void AddRectangleToMesh(float minX, float minZ, float maxX, float maxZ,
                                  ref List<Vector3> vertices, ref List<int> triangulos, ref List<Vector2> uvs)
    {
        int start = vertices.Count;

        // Vértices
        vertices.Add(new Vector3(minX, alturaCalle, minZ)); // BL
        vertices.Add(new Vector3(minX, alturaCalle, maxZ)); // TL
        vertices.Add(new Vector3(maxX, alturaCalle, maxZ)); // TR
        vertices.Add(new Vector3(maxX, alturaCalle, minZ)); // BR

        // UVs
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));

        // Triángulos
        triangulos.Add(start + 0); triangulos.Add(start + 1); triangulos.Add(start + 2);
        triangulos.Add(start + 0); triangulos.Add(start + 2); triangulos.Add(start + 3);
    }
}