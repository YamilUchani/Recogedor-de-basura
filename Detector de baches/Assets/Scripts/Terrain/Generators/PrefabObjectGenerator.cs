using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generador robusto de prefabs evitando intersecciones reales
/// usando BoxColliders.
/// </summary>
[ExecuteInEditMode]
public class PrefabObjectGenerator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // PREFABS
    // ─────────────────────────────────────────────

    [Header("Prefabs")]
    public List<GameObject> prefabs = new();

    // ─────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────

    [Header("Configuración")]
    public int cantidad = 20;

    public int seed = 42;

    public bool randomizeSeedOnStart = false;

    public bool autoUpdate = false;

    // ─────────────────────────────────────────────
    // ÁREA
    // ─────────────────────────────────────────────

    [Header("Área")]
    public float ladoArea = 20f;

    public float margenBorde = 1f;

    [Range(0f, 1f)]
    [Tooltip("0 = uniforme | 1 = muy centrado")]
    public float concentracionCentro = 0.65f;

    // ─────────────────────────────────────────────
    // RESTRICCIONES
    // ─────────────────────────────────────────────

    [Header("Restricciones")]
    public float margenExtra = 0.15f;

    public float alturaY = 0f;

    public bool randomRotacionY = true;

    // ─────────────────────────────────────────────
    // INTERNO
    // ─────────────────────────────────────────────

    private class PlacedObject
    {
        public Bounds bounds;
    }

    private readonly List<PlacedObject> placedObjects = new();

    public bool IsGenerating { get; private set; } = false;

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (!Application.isPlaying) return;

        if (randomizeSeedOnStart)
            seed = (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF);

        StartCoroutine(GenerateRoutine());
    }

    private void OnDisable()
    {
        ClearChildren();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!autoUpdate) return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorApplication.isCompiling)
            return;

        EditorApplication.delayCall -= DelayedGenerate;
        EditorApplication.delayCall += DelayedGenerate;
#endif
    }

#if UNITY_EDITOR
    private void DelayedGenerate()
    {
        if (this == null) return;

        Generate();
    }
#endif

    // ─────────────────────────────────────────────
    // GENERACIÓN
    // ─────────────────────────────────────────────

    [ContextMenu("Generar")]
    public void Generate()
    {
        if (Application.isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(GenerateRoutine());
        }
        else
        {
            // En modo editor generamos instantáneamente como antes
            GenerateSync();
        }
    }

    private void GenerateSync()
    {
        IsGenerating = true;
        ClearChildren();
        placedObjects.Clear();

        if (prefabs == null || prefabs.Count == 0)
        {
            IsGenerating = false;
            return;
        }

        Random.InitState(seed);

        int generated = 0;
        int attempts = 0;
        int maxAttempts = cantidad * 400;
        float usableHalf = Mathf.Max(0f, ladoArea * 0.5f - margenBorde);

        while (generated < cantidad && attempts < maxAttempts)
        {
            attempts++;
            GameObject prefab = prefabs[generated % prefabs.Count];
            if (prefab == null) continue;

            Vector2 posXZ = GetCenteredRandomPosition(usableHalf);
            Vector3 spawnPos = new Vector3(transform.position.x + posXZ.x, transform.position.y + alturaY, transform.position.z + posXZ.y);
            Quaternion rotation = randomRotacionY ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;

            Bounds candidateBounds = CalculatePrefabBounds(prefab, spawnPos, rotation);
            if (IntersectsAny(candidateBounds)) continue;

            GameObject instance;
#if UNITY_EDITOR
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            instance.transform.SetPositionAndRotation(spawnPos, rotation);
#else
            instance = Instantiate(prefab, spawnPos, rotation, transform);
#endif
            instance.name = $"{prefab.name}_{generated}";
            placedObjects.Add(new PlacedObject { bounds = candidateBounds });
            generated++;
        }
        IsGenerating = false;
    }

    public IEnumerator GenerateRoutine()
    {
        IsGenerating = true;
        try
        {
            ClearChildren();
            placedObjects.Clear();

            if (prefabs == null || prefabs.Count == 0)
            {
                Debug.LogWarning("[PrefabObjectGenerator] No hay prefabs.");
                yield break;
            }

            Random.InitState(seed);

            int generated = 0;
            int attempts = 0;
            int maxAttempts = cantidad * 400;
            float usableHalf = Mathf.Max(0f, ladoArea * 0.5f - margenBorde);

            while (generated < cantidad && attempts < maxAttempts)
            {
                attempts++;

                GameObject prefab = prefabs[generated % prefabs.Count];
                if (prefab == null) continue;

                Vector2 posXZ = GetCenteredRandomPosition(usableHalf);
                Vector3 spawnPos = new Vector3(transform.position.x + posXZ.x, transform.position.y + alturaY, transform.position.z + posXZ.y);
                Quaternion rotation = randomRotacionY ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;

                Bounds candidateBounds = CalculatePrefabBounds(prefab, spawnPos, rotation);
                if (IntersectsAny(candidateBounds)) continue;

                // ─────────────────────────────
                // INSTANTIAR
                // ─────────────────────────────
                GameObject instance = Instantiate(prefab, spawnPos, rotation, transform);
                instance.name = $"{prefab.name}_{generated}";

                // ─────────────────────────────
                // GESTIÓN DE OVERLAP SCRIPT
                // ─────────────────────────────
                var overlapScript = instance.GetComponent<DestroyIfOverlap>();
                if (overlapScript != null)
                {
                    overlapScript.enabled = true;
                }

                // Esperar un breve momento para que las colisiones se procesen
                yield return new WaitForSeconds(0.1f);

                // Si el objeto sobrevivió (no fue destruido por el script de overlap)
                if (instance != null)
                {
                    if (overlapScript != null)
                    {
                        overlapScript.enabled = false;
                    }

                    placedObjects.Add(new PlacedObject
                    {
                        bounds = candidateBounds
                    });

                    generated++;
                }
            }

            Debug.Log($"[PrefabObjectGenerator] {generated}/{cantidad} generados ({attempts} intentos)");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    // ─────────────────────────────────────────────
    // POSICIÓN MÁS CENTRADA
    // ─────────────────────────────────────────────

    private Vector2 GetCenteredRandomPosition(float usableHalf)
    {
        float power =
            Mathf.Lerp(1f, 3.5f, concentracionCentro);

        float tx = Mathf.Pow(Random.value, power);
        float tz = Mathf.Pow(Random.value, power);

        float sx = Random.value > 0.5f ? 1f : -1f;
        float sz = Random.value > 0.5f ? 1f : -1f;

        float x = tx * usableHalf * sx;
        float z = tz * usableHalf * sz;

        return new Vector2(x, z);
    }

    // ─────────────────────────────────────────────
    // INTERSECCIONES
    // ─────────────────────────────────────────────

    private bool IntersectsAny(Bounds candidate)
    {
        foreach (var p in placedObjects)
        {
            if (candidate.Intersects(p.bounds))
                return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────
    // CALCULAR BOUNDS REALES
    // ─────────────────────────────────────────────

    private Bounds CalculatePrefabBounds(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation)
    {
        BoxCollider[] colliders =
            prefab.GetComponentsInChildren<BoxCollider>(true);

        // Fallback
        if (colliders.Length == 0)
        {
            Bounds fallback =
                new Bounds(position, Vector3.one);

            fallback.Expand(margenExtra);

            return fallback;
        }

        bool initialized = false;

        Bounds combined = new Bounds();

        foreach (BoxCollider box in colliders)
        {
            Transform t = box.transform;

            Vector3 scale = t.lossyScale;

            Vector3 scaledCenter =
                Vector3.Scale(box.center, scale);

            Vector3 worldCenter =
                position +
                rotation * (t.localPosition + scaledCenter);

            Vector3 scaledSize =
                Vector3.Scale(box.size, scale);

            Matrix4x4 matrix = Matrix4x4.TRS(
                worldCenter,
                rotation * t.localRotation,
                Vector3.one
            );

            Vector3 ext = scaledSize * 0.5f;

            Vector3[] corners = new Vector3[8]
            {
                matrix.MultiplyPoint3x4(
                    new Vector3(-ext.x, -ext.y, -ext.z)
                ),

                matrix.MultiplyPoint3x4(
                    new Vector3(ext.x, -ext.y, -ext.z)
                ),

                matrix.MultiplyPoint3x4(
                    new Vector3(-ext.x, -ext.y, ext.z)
                ),

                matrix.MultiplyPoint3x4(
                    new Vector3(ext.x, -ext.y, ext.z)
                ),

                matrix.MultiplyPoint3x4(
                    new Vector3(-ext.x, ext.y, -ext.z)
                ),

                matrix.MultiplyPoint3x4(
                    new Vector3(ext.x, ext.y, -ext.z)
                ),

                matrix.MultiplyPoint3x4(
                    new Vector3(-ext.x, ext.y, ext.z)
                ),

                matrix.MultiplyPoint3x4(
                    new Vector3(ext.x, ext.y, ext.z)
                )
            };

            Bounds colliderBounds =
                new Bounds(corners[0], Vector3.zero);

            for (int i = 1; i < corners.Length; i++)
            {
                colliderBounds.Encapsulate(corners[i]);
            }

            if (!initialized)
            {
                combined = colliderBounds;
                initialized = true;
            }
            else
            {
                combined.Encapsulate(colliderBounds);
            }
        }

        combined.Expand(margenExtra);

        return combined;
    }

    // ─────────────────────────────────────────────
    // LIMPIAR
    // ─────────────────────────────────────────────

    [ContextMenu("Limpiar")]
    public void ClearChildren()
    {
        List<GameObject> children = new();

        foreach (Transform child in transform)
        {
            if (child != null)
                children.Add(child.gameObject);
        }

        foreach (GameObject go in children)
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        placedObjects.Clear();
    }

    // ─────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Área
        Gizmos.color =
            new Color(0f, 0.8f, 1f, 0.15f);

        Gizmos.DrawCube(
            transform.position,
            new Vector3(ladoArea, 0.05f, ladoArea)
        );

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(ladoArea, 0.05f, ladoArea)
        );

        // Bounds
        Gizmos.color =
            new Color(1f, 0.4f, 0f, 0.75f);

        foreach (var p in placedObjects)
        {
            Gizmos.DrawWireCube(
                p.bounds.center,
                p.bounds.size
            );
        }
    }
}