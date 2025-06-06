using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

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
    public List<TMP_Text> velocityTexts;
    public List<TMP_Text> angleTexts;
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

        if (!angulo_mando)
        {
            if (velocidad.linearVelocity.magnitude > 0.05f)
            {
                Vector3 flatVelocity = new Vector3(velocidad.linearVelocity.x, 0, velocidad.linearVelocity.z);
                if (flatVelocity != Vector3.zero)
                {
                    angle = Vector3.SignedAngle(Vector3.forward, flatVelocity.normalized, Vector3.up);
                }
            }
            else
            {
                angle = 0f;
            }
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

        string velocityTextValue = "Velocity: " + speed.ToString("F2") + " Km/H";
        string angleTextValue = "Angle = " + angle.ToString("F2") + " degrees";

        foreach (TMP_Text velocityText in velocityTexts)
        {
            velocityText.text = velocityTextValue;
        }

        foreach (TMP_Text angleText in angleTexts)
        {
            angleText.text = angleTextValue;
        }
    }

    private HashSet<string> detectedPotholes = new HashSet<string>();
    private string currentPothole = null;

    private void Capture()
    {
        if (captureCameras == null || captureCameras.Count == 0)
        {
            Debug.LogError("No capture cameras assigned!");
            return;
        }

        if (speed <= 0.05f && Mathf.Abs(angle) <= 1f)
        {
            Debug.Log("Velocity and angle below thresholds. Image capture skipped.");
            return;
        }

        Vector3 midPoint = Vector3.zero;
        foreach (Camera cam in captureCameras) midPoint += cam.transform.position;
        midPoint /= captureCameras.Count;

        Vector3 forwardDir = captureCameras[0].transform.forward;
        Vector3 rightDir = captureCameras[0].transform.right;
        Vector3 upDir = captureCameras[0].transform.up;

        float rayLength = 20f;
        float raySpacing = 0.2f;

        Vector3[] rayOrigins = new Vector3[]
        {
            midPoint,
            midPoint + (upDir * raySpacing),
            midPoint - (upDir * raySpacing),
            midPoint - (rightDir * raySpacing),
            midPoint + (rightDir * raySpacing)
        };

        bool hitDetected = false;
        string potholeID = "";
        RaycastHit bestHit = new RaycastHit();

        foreach (Vector3 origin in rayOrigins)
        {
            Debug.DrawRay(origin, forwardDir * rayLength, Color.red, 2f);

            if (Physics.Raycast(origin, forwardDir, out RaycastHit hit, rayLength))
            {
                string hitTag = hit.collider.tag;
                string hitName = hit.collider.gameObject.name;

                if (hitTag == "bache" && hitName.IndexOf("bache", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hitDetected = true;
                    potholeID = hitName;
                    bestHit = hit;
                    break;
                }
                else
                {
                    Debug.Log($"Objeto detectado no válido. Tag: {hitTag}, Nombre: {hitName}");
                }
            }
        }

        if (!hitDetected)
        {
            Debug.Log("No pothole detected by any ray.");
            return;
        }

        if (detectedPotholes.Contains(potholeID))
        {
            Debug.Log($"Pothole {potholeID} already captured. Skipping.");
            return;
        }

        detectedPotholes.Add(potholeID);
        currentPothole = potholeID;

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = Path.Combine(Application.persistentDataPath, "Imagenes", "Imagenes_de_baches", timestamp);
        Directory.CreateDirectory(folderPath);

        foreach (Camera cam in captureCameras)
        {
            string filename = $"Image{count}_{cam.name}.png";
            string outputPath = Path.Combine(folderPath, filename);

            bool convertToGrayscale = cam.name.Contains("L") || cam.name.Contains("R");
            RenderTexture renderTexture = new RenderTexture(1270, 950, 24);

            cam.targetTexture = renderTexture;
            Texture2D screenshot = new Texture2D(1270, 950, TextureFormat.RGB24, false);
            cam.Render();

            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, 1270, 950), 0, 0);

            if (convertToGrayscale)
            {
                Color[] pixels = screenshot.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    float gray = pixels[i].grayscale;
                    pixels[i] = new Color(gray, gray, gray);
                }
                screenshot.SetPixels(pixels);
            }

            screenshot.Apply();
            File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());

            cam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
            Destroy(screenshot);

            Debug.Log($"Captured image saved to: {outputPath}");
        }

        count++;
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
