using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
public class MovementInterface : MonoBehaviour
{
    public Rigidbody velocidad;
    public GameObject angulo;
    private float speed;
    private float angle;
    public Direccion direction;
    public Camera captureCamera;
    public string outputFolder = "CapturedImages";
    public float captureInterval = 2f;
    public TMP_Text buttonText;
    public TMP_Text velocityText;
    public TMP_Text angleText;
    private bool isCapturing = false;
    public bool angulo_mando;
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
        if(!angulo_mando)
        {
            angle = angulo.transform.localEulerAngles.y;
        }
        else
        {
            angle = direction.anguloactual;
        }
        if (angle >= 134 && angle <= 226)
        {
            angle -= 180;
        }

        if (angle < 360 && angle >= 314)
        {
            angle -= 360;
        }
        velocityText.text = "Velocity: " + speed.ToString("F2") + "Km/H";
        angleText.text = "Angle = " + angle.ToString("F2") + " degrees";
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

    if (container)
    {   
        timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string imageNameFolder = "Imagenes_de_baches";
        folderPath = Path.Combine(Application.persistentDataPath, "Imagenes", imageNameFolder, timestamp);
        Directory.CreateDirectory(folderPath);
        container = false;
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

    public void AcDc()
    {
        if (isCapturing)
        {
            isCapturing = false;
            container = false;
            buttonText.text = "Activate";
        }
        else
        {
            isCapturing = true;
            container = true;
            buttonText.text = "Desactivate";
        }
    }
    public void restarted()
    {
        SceneManager.LoadScene("load_scene");
    }


}

