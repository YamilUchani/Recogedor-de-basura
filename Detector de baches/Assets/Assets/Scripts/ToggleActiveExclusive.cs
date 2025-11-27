using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class ToggleActiveExclusive : MonoBehaviour
{
    public string groupATag = "GroupA";
    public string groupBTag = "GroupB";
    public EnergyController energyController;
    public GeneradorDeCalle generadorCalle;

    private bool initialized = false;
    private bool calleGenerada = false;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Suscribirse al evento si ya está asignado
        if (generadorCalle != null)
        {
            generadorCalle.OnGeneracionCalleTerminada += OnCalleGenerada;
        }

        StartCoroutine(InitializeWhenReady());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (generadorCalle != null)
        {
            generadorCalle.OnGeneracionCalleTerminada -= OnCalleGenerada;
        }
    }

    void OnCalleGenerada()
    {
        calleGenerada = true;
        // Aplicar el estado físico correcto SIN cambiar activación
        ActualizarEstadoFisico();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameObject.scene.name)
        {
            StartCoroutine(InitializeWhenReady());
        }
    }

    IEnumerator InitializeWhenReady()
    {
        if (initialized) yield break;

        yield return null;
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();

        var groupA = new List<GameObject>();
        var groupB = new List<GameObject>();
        bool foundAny = false;
        int attempt = 0;
        const int maxAttempts = 10;
        const float waitTime = 0.1f;

        while (attempt < maxAttempts)
        {
            groupA.Clear();
            groupB.Clear();

            var roots = gameObject.scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                CollectRecursive(root, groupA, groupB);
            }

            if (groupA.Count > 0 || groupB.Count > 0)
            {
                foundAny = true;
                break;
            }

            attempt++;
            yield return new WaitForSeconds(waitTime);
        }

        if (!foundAny)
        {
            Debug.LogWarning("[ToggleActiveExclusive] No se encontraron objetos con los tags especificados después de " + maxAttempts + " intentos.");
            yield break;
        }

        bool useGroupB = energyController != null && energyController.energiaActivo;
        SetActiveOnly(groupA, !useGroupB);
        SetActiveOnly(groupB, useGroupB);

        if (energyController != null)
            energyController.energiaActivo = useGroupB;

        initialized = true;

        // Si ya estaba generada la calle, aplicamos físicas ahora
        if (calleGenerada)
        {
            ActualizarEstadoFisico();
        }
    }

    // Solo activa/desactiva, SIN tocar física
    void SetActiveOnly(List<GameObject> list, bool state)
    {
        foreach (var obj in list)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    // Solo actualiza isKinematic, SIN cambiar activación
    void ActualizarEstadoFisico()
    {
        var groupA = new List<GameObject>();
        var groupB = new List<GameObject>();
        var roots = gameObject.scene.GetRootGameObjects();
        foreach (var root in roots)
            CollectRecursive(root, groupA, groupB);

        bool useGroupB = energyController != null && energyController.energiaActivo;
        AplicarKinematic(groupA, !useGroupB);
        AplicarKinematic(groupB, useGroupB);
    }

    void AplicarKinematic(List<GameObject> list, bool debeSerDinamico)
    {
        foreach (var obj in list)
        {
            if (obj == null) continue;
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = !debeSerDinamico;
            }
        }
    }

    void CollectRecursive(GameObject obj, List<GameObject> groupA, List<GameObject> groupB)
    {
        if (obj.CompareTag(groupATag))
            groupA.Add(obj);
        else if (obj.CompareTag(groupBTag))
            groupB.Add(obj);

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            CollectRecursive(obj.transform.GetChild(i).gameObject, groupA, groupB);
        }
    }

    public void ActivateGroupA()
    {
        SetGroupState(false);
        if (calleGenerada) ActualizarEstadoFisico();
    }

    public void ActivateGroupB()
    {
        SetGroupState(true);
        if (calleGenerada) ActualizarEstadoFisico();
    }

    void SetGroupState(bool groupBActive)
    {
        var groupA = new List<GameObject>();
        var groupB = new List<GameObject>();
        var roots = gameObject.scene.GetRootGameObjects();
        foreach (var root in roots)
            CollectRecursive(root, groupA, groupB);

        SetActiveOnly(groupA, !groupBActive);
        SetActiveOnly(groupB, groupBActive);

        if (energyController != null)
            energyController.energiaActivo = groupBActive;
    }
}