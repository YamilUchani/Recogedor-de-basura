using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{

    public static int sceneIndex;
    public Image barraProgreso; // Referencia a la barra de progreso en la interfaz de usuario
    public List<GameObject> rootGameObjects;
    public List<string> excludedObjectNames; // Lista de nombres de objetos excluidos
    public float activationTimePerObject = 2f; // Tiempo en segundos por cada activación de objeto

    private float totalObjects;
    private float activeObjects = 1;
    private bool activationInProgress; // Variable para controlar si hay una activación en progreso
    private string sceneToLoad;
    AsyncOperation asyncLoad;

    void Start()
    {
        // Determinar qué escena cargar según la variable global
        sceneToLoad = sceneIndex == 0 ? "Demo_scene_low" : "Model_scene_low";
        Debug.Log(sceneIndex);
        // Cargar la escena especificada aditivamente
        asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

        // Registrar el evento completed para obtener la referencia a la escena cargada
        asyncLoad.completed += OnSceneLoaded;
    }

    private void OnSceneLoaded(AsyncOperation operation)
    {
        // Se llama cuando la escena ha terminado de cargarse aditivamente
        Scene sceneCargada = SceneManager.GetSceneByName(sceneToLoad);
        rootGameObjects = sceneCargada.GetRootGameObjects().Where(obj => !excludedObjectNames.Contains(obj.name)).ToList();
        totalObjects = CountParentObjects(rootGameObjects);
        activeObjects = 1;
        activationInProgress = true;

        // Comenzar la activación progresiva
        StartCoroutine(ActivateObjectsProgressively());
    }

    private System.Collections.IEnumerator ActivateObjectsProgressively()
    {
        foreach (GameObject obj in rootGameObjects)
        {
            ActivateObjectAndChildren(obj);

            // Calcular el porcentaje de objetos activados con respecto al total de objetos
            float percentage = activeObjects / totalObjects;

            // Mapear el porcentaje a un valor entre 0 y 1
            float mappedValue = Mathf.Clamp01(percentage);

            // Establecer el valor mapeado en la propiedad fillAmount de la imagen barraProgreso
            barraProgreso.fillAmount = mappedValue;

            // Esperar el tiempo especificado antes de activar el siguiente objeto
            yield return new WaitForSeconds(activationTimePerObject);
            activeObjects++;
        }

        activationInProgress = false; // La activación ha terminado
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync("load_scene");
    }

    private void ActivateObjectAndChildren(GameObject parent)
    {
        if (parent != null && !parent.activeSelf && !excludedObjectNames.Contains(parent.name))
        {
            parent.SetActive(true);

            // Activar los hijos recursivamente
            int childCount = parent.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject child = parent.transform.GetChild(i).gameObject;
                ActivateObjectAndChildren(child);
            }
        }
    }

    private int CountParentObjects(List<GameObject> objects)
    {
        return objects.Count;
    }
}

// Clase global para manejar la selección de escena
    
