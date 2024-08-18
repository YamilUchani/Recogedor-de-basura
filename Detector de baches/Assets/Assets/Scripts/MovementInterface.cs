using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public class MovementInterface : MonoBehaviour
{
    public Rigidbody velocidad;
    public GameObject angulo;
    private float speed;
    private float angle;
    public Direccion direction;
    public List<Camera> captureCameras = new List<Camera>();
    public string outputFolder = "CapturedImages";
    public float captureInterval = 2f;
    public TMP_Text buttonText;
    public TMP_Text velocityText;
    public TMP_Text angleText;
    private bool isCapturing = false;
    public bool angulo_mando;
    private bool container = false;
    private float timer = 0f;
    private int count;

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
                count++;
            }
        }

        speed = velocidad.linearVelocity.magnitude;
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
    if (captureCameras == null)
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
    for (int i = 0; i < captureCameras.Count; i++)
    {
        string filename = "Image" + count.ToString() + captureCameras[i].name + ".png";
        string outputPath = Path.Combine(folderPath, filename);

        bool convertToGrayscale = captureCameras[i].name.Contains("L") || captureCameras[i].name.Contains("R");

        RenderTexture renderTexture = new RenderTexture(1270, 950, 24);
        captureCameras[i].targetTexture = renderTexture;
        Texture2D screenshot = new Texture2D(1270, 950, TextureFormat.RGB24, false); // Always create RGB texture
        captureCameras[i].Render();
        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(new Rect(0, 0, 1270, 950), 0, 0);
        screenshot.Apply();

        if (convertToGrayscale)
        {
            // Convert the screenshot to grayscale
            Color[] pixels = screenshot.GetPixels();
            for (int j = 0; j < pixels.Length; j++)
            {
                float grayscaleValue = pixels[j].grayscale;
                pixels[j] = new Color(grayscaleValue, grayscaleValue, grayscaleValue);
            }
            screenshot.SetPixels(pixels);
            screenshot.Apply();
        }

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        captureCameras[i].targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(screenshot);

        Debug.Log("Captured image saved to: " + outputPath);
    }
}


    public void AcDc()
    {
        if (isCapturing)
        {
            isCapturing = false;
            container = false;
            buttonText.text = "Record";
        }
        else
        {
            isCapturing = true;
            container = true;
            buttonText.text = "Stop";
        }
    }
    public void restarted()
    {
        SceneManager.LoadScene("load_scene");
    }


}

