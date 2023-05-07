using UnityEngine;
using System.IO;

public class MovementInterface : MonoBehaviour
{
    public Rigidbody velocidad;
    public GameObject angulo;
    private float speed;
    private float angle;
    public Camera captureCamera;
    public string outputFolder = "CapturedImages";
    public float captureInterval = 2f;

    private bool isCapturing = false;

    private bool container = false;
    private float timer = 0f;

    string timestamp;
    string folderPath;

    private void Update()
    {
        if (isCapturing)
        {
            timer += Time.deltaTime;

            if (timer >= captureInterval)
            {
                Capture();
                timer = 0f;
            }
        }

        speed = velocidad.velocity.magnitude;
        angle = angulo.transform.localEulerAngles.y;

        if (angle >= 134 && angle <= 226)
        {
            angle -= 180;
        }

        if (angle < 360 && angle >= 314)
        {
            angle -= 360;
        }
    }

    private void Capture()
    {
        
        if (captureCamera == null)
        {
            Debug.LogError("No capture camera assigned!");
            return;
        }

        if (speed <= 0.05f && angle <= 1f)
        {
            Debug.Log("Velocity and angle below thresholds. Image capture skipped.");
            return;
        }
        if(container)
        {   timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            folderPath = Path.Combine(Application.dataPath, outputFolder, timestamp);
            Directory.CreateDirectory(folderPath);
            container=false;
        }   
        string filename = "Image_velocity_" + speed.ToString("F2") + "_angle_" + angle.ToString("F2") + ".png";
        string outputPath = Path.Combine(folderPath, filename);

        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        captureCamera.targetTexture = renderTexture;
        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        captureCamera.Render();
        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(screenshot);

        Debug.Log("Captured image saved to: " + outputPath);
    }

    private void OnGUI()
    {
        GUIStyle estiloFuente = new GUIStyle(GUI.skin.label);
        estiloFuente.fontSize = 24; // Tamaño de fuente deseado
        estiloFuente.fontStyle = FontStyle.Bold; // Tipo de fuente (negrita)
        estiloFuente.font = Font.CreateDynamicFontFromOSFont("Arial", estiloFuente.fontSize); // Tipo de fuente (Arial)
        GUI.Label(new Rect(10, 200, 300, 200), "Velocity: " + speed.ToString("F2") + " m/h", estiloFuente);
        GUI.Label(new Rect(400, 200, 300, 200), "Angle = " + angle.ToString("F2") + " degrees", estiloFuente);

        if (isCapturing)
        {
            if (GUI.Button(new Rect(400, 10, 150, 30), "Deactivate"))
            {
                isCapturing = false;
            }
        }
        else
        {
            if (GUI.Button(new Rect(400, 10, 150, 30), "Activate"))
            {
                isCapturing = true;
                container=true;
            }
        }
    }
}
