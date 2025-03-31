using UnityEngine;

public class CameraUI : MonoBehaviour
{
    public Camera[] cameras;
    public Camera singleCamera; // Cámara adicional para el modo individual
    private bool singleCameraMode = false;

    void Start()
    {
        foreach (Camera camera in cameras)
        {
            camera.depth += 10;
        }
        singleCamera.depth += 20; // Asegurar que la cámara individual tenga mayor prioridad
        UpdateCameraLayout();
    }

    void Update()
    {
        UpdateCameraLayout();
    }

    void UpdateCameraLayout()
    {
        if (singleCameraMode)
        {
            foreach (Camera camera in cameras)
            {
                camera.enabled = false;
            }
            singleCamera.enabled = true;
            singleCamera.rect = new Rect(0, 0, 1, 1); // Pantalla completa
        }
        else
        {
            singleCamera.enabled = false;
            int numCameras = cameras.Length;
            for (int i = 0; i < numCameras; i++)
            {
                cameras[i].enabled = true;
                Rect cameraRect = CalculateCameraRect(i, numCameras);
                cameras[i].rect = cameraRect;
            }
        }
    }

    Rect CalculateCameraRect(int cameraIndex, int totalCameras)
    {
        float width = 0.5f;
        float height = 0.5f;
        float x = cameraIndex % 2 == 0 ? 0.0f : 0.5f;
        float y = cameraIndex < 2 ? 0.5f : 0.0f;

        return new Rect(x, y, width, height);
    }

    public void ToggleSingleCameraMode()
    {
        singleCameraMode = !singleCameraMode;
        UpdateCameraLayout();
    }
}
