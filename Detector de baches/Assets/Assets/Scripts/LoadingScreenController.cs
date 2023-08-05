using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    public Image barraProgreso; // Referencia a la barra de progreso en la interfaz de usuario
    public List<GameObject> rootGameObjects;
    public float activationTimePerObject = 2f; // Tiempo en segundos por cada activación de objeto

    private float totalObjects;
    private float activeObjects =1;
    private bool activationInProgress; // Variable para controlar si hay una activación en progreso
     AsyncOperation asyncLoad;
    void Start()
    {
        // Cargar la segunda escena aditivamente
        asyncLoad = SceneManager.LoadSceneAsync("Demo_scene_low", LoadSceneMode.Additive);

        // Registrar el evento completed para obtener la referencia a la escena cargada
        asyncLoad.completed += OnSceneLoaded;
    }

    private void OnSceneLoaded(AsyncOperation operation)
    {
        // Se llama cuando la escena ha terminado de cargarse aditivamente
        Scene sceneCargada = SceneManager.GetSceneByName("Demo_scene_low");
        rootGameObjects = sceneCargada.GetRootGameObjects().ToList();
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
        if (parent != null && !parent.activeSelf)
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
        int count = objects.Count;
        return count;
    }
}
