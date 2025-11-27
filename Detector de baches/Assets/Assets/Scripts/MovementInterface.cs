using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class MovementInterface : MonoBehaviour
{
    public DroneNavMeshController droneController;
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
    private float baseAngle;
    private bool baseAngleSet = false;
    private float timer = 0f;
    private int globalCount = 0; // Solo para compatibilidad con UI, no se usa en nombres

    // === NUEVO: Contadores por tipo de defecto ===
    private Dictionary<string, int> captureCounters = new Dictionary<string, int>
    {
        { "Pothole", 0 },
        { "Crocodile", 0 }
    };

    // === NUEVO: Prefabs asignables desde Inspector ===
    public GameObject potholePlanePrefab;   // Plano azul
    public GameObject crocodilePlanePrefab; // Plano amarillo

    // === Evitar duplicados en marcadores e imágenes ===
    private HashSet<string> processedObjects = new HashSet<string>();

    // Solo lectura en el inspector
    [SerializeField, TextArea(5, 20)]
    private string visibleDefects = "";

    private void Update()
    {
        if (isCapturing)
        {
            timer += Time.deltaTime;

            if (timer >= captureInterval)
            {
                Capture();
                timer = 0f;
                globalCount++; // opcional, para otro uso
            }
        }

        // Actualizar velocidad
        float velocidadActual = velocidad.linearVelocity.magnitude;
        speed = (velocidadActual < 0.1f) ? 0f : velocidadActual;

        // Actualizar ángulo
        if (!angulo_mando)
        {
            if (angulo == null) return;

            float currentY = angulo.transform.eulerAngles.y;

            if (!initialized)
            {
                lastAngle = currentY;
                baseAngle = currentY;
                baseAngleSet = true;
                initialized = true;
                angle = 0f;
                return;
            }

            if (Mathf.Abs(Mathf.DeltaAngle(currentY, lastAngle)) < TOLERANCIA)
            {
                if (!baseAngleSet)
                {
                    baseAngle = currentY;
                    baseAngleSet = true;
                }
                angle = 0f;
            }
            else
            {
                baseAngleSet = false;
                angle = Mathf.DeltaAngle(baseAngle, currentY);
            }

            lastAngle = currentY;
        }
        else
        {
            angle = direction.anguloactual;
        }

        // Normalización del ángulo
        if (angle >= 134 && angle <= 226)
        {
            angle -= 180;
        }
        if (angle >= 314 && angle < 360)
        {
            angle -= 360;
        }

        // Actualizar UI
        string velocityTextValue = "Velocity : " + speed.ToString("F2") + " Km/H";
        string angleTextValue = "Angle    : " + angle.ToString("F2") + " º";

        foreach (TMP_Text velocityText in velocityTexts)
        {
            velocityText.text = velocityTextValue;
        }

        foreach (TMP_Text angleText in angleTexts)
        {
            angleText.text = angleTextValue;
        }
    }

    // === Parámetros de captura ===
    [SerializeField] private float offsetDistancia = 0.2f;
    private Vector3 lastMidPoint = Vector3.zero;
    private bool initialized = false;
    private float lastAngle;
    private const float TOLERANCIA = 0.1f;

    // === MÉTODO DE CAPTURA ACTUALIZADO ===
    private void Capture()
    {
        if (captureCameras == null || captureCameras.Count == 0)
        {
            Debug.LogError("No capture cameras assigned!");
            return;
        }

        // Calcular punto medio
        Vector3 midPoint = Vector3.zero;
        foreach (Camera cam in captureCameras) midPoint += cam.transform.position;
        midPoint /= captureCameras.Count;

        Vector3 direccionMovimiento = (midPoint - lastMidPoint).normalized;
        midPoint -= direccionMovimiento * offsetDistancia;
        lastMidPoint = midPoint;

        Vector3 forwardDir = captureCameras[0].transform.forward;
        Vector3 upDir = captureCameras[0].transform.up;
        Vector3 rightDir = captureCameras[0].transform.right;

        float rayLength = 4f;
        float raySpacing = 0.05f;

        Vector3[] rayOrigins = new Vector3[]
        {
            midPoint,
            midPoint + (upDir * raySpacing),
            midPoint - (upDir * raySpacing),
            midPoint - (rightDir * raySpacing),
            midPoint + (rightDir * raySpacing)
        };

        bool validHit = false;
        string hitTag = "";
        string hitName = "";
        Vector3 hitPoint = Vector3.zero;

        foreach (Vector3 origin in rayOrigins)
        {
            Debug.DrawRay(origin, forwardDir * rayLength, Color.red, 2f);

            if (Physics.Raycast(origin, forwardDir, out RaycastHit hit, rayLength))
            {
                string tag = hit.collider.tag;
                string name = hit.collider.gameObject.name;

                if (tag == "Pothole" || tag == "Crocodile")
                {
                    string uniqueID = tag + "_" + name;
                    if (processedObjects.Contains(uniqueID))
                    {
                        Debug.Log($"Objeto {uniqueID} ya procesado. Saltando.");
                        return;
                    }

                    validHit = true;
                    hitTag = tag;
                    hitName = name;
                    hitPoint = hit.point;
                    processedObjects.Add(uniqueID);
                    visibleDefects += $"\n{tag}: {name}";
                    break;
                }
                else
                {
                    Debug.Log($"Objeto no válido. Tag: {tag}, Nombre: {name}");
                }
            }
        }

        if (!validHit)
        {
            Debug.Log("No Pothole or Crocodile detected. No image captured.");
            return;
        }

        // === Incrementar contador del tipo ===
        captureCounters[hitTag]++;
        int currentCount = captureCounters[hitTag];

        // === Generar timestamp ===
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // === Crear carpeta única por captura ===
        string folderPath = Path.Combine(Application.persistentDataPath, "Imagenes", "Imagenes_de_defectos", timestamp);
        Directory.CreateDirectory(folderPath);

        // === Instanciar plano de marcador ===
        GameObject prefabToSpawn = null;

        if (hitTag == "Pothole")
        {
            prefabToSpawn = potholePlanePrefab;
        }
        else if (hitTag == "Crocodile")
        {
            prefabToSpawn = crocodilePlanePrefab;
        }

        if (prefabToSpawn != null)
        {
            // Misma posición X y Z, pero 20 metros más arriba en Y
            Vector3 spawnPosition = new Vector3(hitPoint.x, hitPoint.y + 20f, hitPoint.z);
            
            // Rotación (0, 0, 0)
            Quaternion spawnRotation = Quaternion.identity;
            
            Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
            Debug.Log($"Marcador {hitTag} instanciado en {spawnPosition}");
        }
        else
        {
            Debug.LogWarning($"Prefab no asignado para {hitTag}!");
        }

        // === Capturar imágenes ===
        foreach (Camera cam in captureCameras)
        {
            // Nombre: [Tag]_[Contador]_[Timestamp].png
            string filename = $"{hitTag}_{currentCount}_{timestamp}_{cam.name}.png";
            string outputPath = Path.Combine(folderPath, filename);

            bool convertToGrayscale = cam.name.Contains("L") || cam.name.Contains("R");
            RenderTexture renderTexture = new RenderTexture(1270, 950, 24);

            cam.targetTexture = renderTexture;
            cam.Render();

            RenderTexture.active = renderTexture;
            Texture2D screenshot = new Texture2D(1270, 950, TextureFormat.RGB24, false);
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

            // Limpiar
            cam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
            Destroy(screenshot);

            Debug.Log($"Imagen guardada: {outputPath}");
        }
    }

    public void AcDc()
    {
        if (isCapturing)
        {
            isCapturing = false;
            buttonText.text = "Record";
        }
        else
        {
            isCapturing = true;
            buttonText.text = "Stop";
        }
    }

    public void restarted()
    {
        SceneManager.LoadScene("load_scene");
    }
}