using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MostrarSoloAlPasarMouse : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private CanvasGroup canvasGroup;

    [Header("Posiciones para el dron")]
    public Vector3 primeraPosicion = new Vector3(-13.35f, 0f, 142.6f);
    public Vector3 posicionFinal = new Vector3(-2.85f, 0f, 105.75f);

    [Header("Acciones adicionales al hacer clic")]
    public GameObject objetoActivar;
    public Minimapa minimapa;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Al inicio, ocultamos
        canvasGroup.alpha = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        canvasGroup.alpha = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Activar objeto específico
        if (objetoActivar != null)
        {
            objetoActivar.SetActive(true);
        }

        // Llamar función del minimapa
        if (minimapa != null)
        {
            minimapa.AlternarAnchors();
        }

        // Enviar posiciones al dron
        DroneNavMeshController droneController = FindObjectOfType<DroneNavMeshController>();
        if (droneController != null)
        {
            droneController.SetSearchArea(primeraPosicion, posicionFinal);
        }
        else
        {
            Debug.LogWarning("No se encontró el DroneNavMeshController en la escena");
        }
    }
}
