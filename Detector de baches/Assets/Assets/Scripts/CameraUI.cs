using UnityEngine;

public class CameraUI : MonoBehaviour
{
    public Camera[] cameras;

    void Start()
    {
        // Asegurémonos de que las cámaras de la lista estén en una capa superior
        foreach (Camera camera in cameras)
        {
            camera.depth += 10; // Puedes ajustar este valor según sea necesario
        }

        UpdateCameraLayout();
    }

    void Update()
    {
        UpdateCameraLayout();
    }

    void UpdateCameraLayout()
    {
        int numCameras = cameras.Length;

        for (int i = 0; i < numCameras; i++)
        {
            Rect cameraRect = CalculateCameraRect(i, numCameras);
            cameras[i].rect = cameraRect;
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
}
