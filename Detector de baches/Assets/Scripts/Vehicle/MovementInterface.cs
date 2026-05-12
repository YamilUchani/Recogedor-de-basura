using UnityEngine;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using UnityEngine.AI;

public class MovementInterface : MonoBehaviour
{
    public DroneNavMeshController droneController;
    private NavMeshAgent navAgent; // Se obtiene automáticamente del droneController
    public Rigidbody velocidad;
    public GameObject angulo;
    private float speed;
    private float angle;
    public Direccion direction;
    public List<Camera> captureCameras = new List<Camera>();
    public string outputFolder = "CapturedImages";
    public float captureInterval = 2f;
    
    [Header("Configuración de Rayos")]
    [Tooltip("Distancia base entre rayos. Si useDynamicRaySpacing es true, se multiplicará por la altura.")]
    public float raySpacing = 0.05f;
    public bool useDynamicRaySpacing = true;
    
    [Tooltip("Tiempo de espera desde que se detecta el bache hasta que se toma la foto (para centrar la toma).")]
    public float captureDelay = 0.15f;
    
    public TMP_Text buttonText;
    public TMP_Text velocityText;
    public TMP_Text angleText;
    private bool isCapturing = false;
    public bool angulo_mando;
    // private bool container = false; // Unused
    private float baseAngle;
    private bool baseAngleSet = false;
    private float timer = 0f;
    private int count;

    string timestamp;
    string folderPath;
    private float lastAngle;
    private const float TOLERANCIA = 0.1f;
    private bool initialized = false;
    public float toleranciaVelocidad = 0.1f;

    private HashSet<string> detectedPotholes = new HashSet<string>();

    // Solo lectura en el inspector
    [SerializeField, TextArea(5, 20)]
    private string visiblePotholes;

    private string currentPothole = null;

    private void Start()
    {
        // 1. Buscar en el propio objeto
        navAgent = GetComponent<NavMeshAgent>();
        
        // 2. Buscar en el padre (por si este script está en la cámara)
        if (navAgent == null) navAgent = GetComponentInParent<NavMeshAgent>();

        if (droneController != null)
        {
            droneController.onObstacleCleared += CheckPotholeAtClearedPosition;
            // 3. Buscar en el droneController asignado
            if (navAgent == null) navAgent = droneController.GetComponent<NavMeshAgent>();
        }
        
        // 4. Búsqueda desesperada en toda la escena (si hay un solo dron)
        if (navAgent == null) navAgent = FindFirstObjectByType<NavMeshAgent>();
        
        Debug.Log($"[MovementInterface Init] navAgent encontrado: {navAgent != null}");
    }

    private void OnDestroy()
    {
        if (droneController != null)
            droneController.onObstacleCleared -= CheckPotholeAtClearedPosition;
    }

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

        // Velocidad: si el NavMeshAgent está activo (modo automático), usamos su velocidad.
        // Si está en control manual (Rigidbody activo), usamos la física del Rigidbody.
        float velocidadActual;
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            velocidadActual = navAgent.velocity.magnitude;
            Debug.Log($"[Velocidad] FUENTE=NavMesh | navAgent={navAgent != null} | enabled={navAgent.enabled} | onMesh={navAgent.isOnNavMesh} | speed={velocidadActual:F2}");
        }
        else if (velocidad != null)
        {
            velocidadActual = velocidad.linearVelocity.magnitude;
            Debug.Log($"[Velocidad] FUENTE=Rigidbody | navAgent={navAgent != null} | speed={velocidadActual:F2}");
        }
        else
        {
            velocidadActual = 0f;
            Debug.Log($"[Velocidad] FUENTE=Ninguna | navAgent={navAgent != null}");
        }

        speed = (velocidadActual < toleranciaVelocidad) ? 0f : velocidadActual;

        // --- ÁNGULO ---
        if (!angulo_mando)
        {
            if (angulo != null)
            {
                float currentY = angulo.transform.eulerAngles.y;

                if (!initialized)
                {
                    lastAngle = currentY;
                    baseAngle = currentY;
                    baseAngleSet = true;
                    initialized = true;
                    angle = 0f;
                }
                else if (Mathf.Abs(Mathf.DeltaAngle(currentY, lastAngle)) < TOLERANCIA)
                {
                    if (!baseAngleSet) { baseAngle = currentY; baseAngleSet = true; }
                    angle = 0f;
                }
                else
                {
                    baseAngleSet = false;
                    angle = Mathf.DeltaAngle(baseAngle, currentY);
                }
                lastAngle = currentY;
            }
        }
        else
        {
            angle = direction.anguloactual;
        }

        if (angle >= 134 && angle <= 226) angle -= 180;
        if (angle < 360 && angle >= 314)  angle -= 360;

        // --- ACTUALIZAR UI SIEMPRE ---
        string velocityTextValue = "Velocity : " + speed.ToString("F2") + " Km/H";
        string angleTextValue    = "Angle    : " + angle.ToString("F2") + " º";

        if (velocityText != null)
            velocityText.text = velocityTextValue;

        if (angleText != null)
            angleText.text = angleTextValue;
    }
[SerializeField] private float offsetDistancia = 0.2f;
private Vector3 lastMidPoint = Vector3.zero;
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
        }

        Vector3 midPoint = Vector3.zero;
        foreach (Camera cam in captureCameras) midPoint += cam.transform.position;
        midPoint /= captureCameras.Count;

         Vector3 direccionMovimiento = (midPoint - lastMidPoint).normalized;

    // Ajustar midPoint en dirección contraria al movimiento
    midPoint -= direccionMovimiento * offsetDistancia;

    // Guardar el punto medio actual para el siguiente frame
    lastMidPoint = midPoint;

        Vector3 forwardDir = captureCameras[0].transform.forward;
        Vector3 rightDir = captureCameras[0].transform.right;
        Vector3 upDir = captureCameras[0].transform.up;

        // Al tener cámaras inclinadas o terrenos irregulares (mallas en vez de Terrain), 
        // el cálculo exacto se queda corto. Es mucho más seguro lanzar un rayo largo fijo.
        float rayLength = 30f;

        // Calcular spacing dinámico según altura
        float dynamicSpacing = raySpacing;
        if (useDynamicRaySpacing)
        {
            // Tomamos la altura actual del dron sobre el punto medio
            dynamicSpacing = raySpacing * midPoint.y; 
        }

        Vector3[] rayOrigins = new Vector3[]
        {
            midPoint,
            midPoint + (upDir * dynamicSpacing),
            midPoint - (upDir * dynamicSpacing),
            midPoint - (rightDir * dynamicSpacing),
            midPoint + (rightDir * dynamicSpacing)
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

                if (hitTag == "Pothole" || hitTag == "Crocodile")
                {
                    hitDetected = true;
                    potholeID = hitName;
                    bestHit = hit;
                    break;
                }
                else
                {
                    // Debug.Log($"Objeto detectado no válido. Tag: {hitTag}, Nombre: {hitName}");
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
        
        // Generar marcador visual (Plano 4x4 a 6m de altura)
        SpawnMarker(bestHit.collider.transform.position, bestHit.collider.tag);

        // Actualiza texto visible en Inspector
        visiblePotholes = string.Join("\n", detectedPotholes);

        // --- INICIAR PROCESO DE FOTO CON RETRASO ---
        StartCoroutine(ExecuteDelayedCapture(potholeID));
    }

    private IEnumerator ExecuteDelayedCapture(string potholeID)
    {
        if (captureDelay > 0)
            yield return new WaitForSeconds(captureDelay);

        foreach (Camera cam in captureCameras)
        {
            string filename = $"Image{count}_{cam.name}_{potholeID}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";

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
            
            // Use FileHandler for cross-platform compatibility
            byte[] pngBytes = screenshot.EncodeToPNG();
            FileHandler.SaveImage(pngBytes, filename);

            // ---> ENVIAR IMAGEN A LA API DE PYTHON <---
            if (PythonInferenceClient.Instance != null)
            {
                PythonInferenceClient.Instance.AnalyzeImageBytes(pngBytes, filename);
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
            Destroy(screenshot);

            Debug.Log($"Captured image: {filename}");
        }

        count++;
    }

    private void SpawnMarker(Vector3 position, string tag)
    {
        // Crear plano primitivo
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        
        // Posición: 18.91 metros arriba del objeto detectado
        plane.transform.position = position + Vector3.up * 18.91f;
        
        // Escala: 8x8 metros (El plano por defecto es 10x10, asi que 0.8f lo hace 8x8)
        plane.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        
        // Rotación: Plano mirando arriba (por defecto ya está así, pero aseguramos)
        plane.transform.rotation = Quaternion.identity;

        // Configurar material/color
        Renderer rend = plane.GetComponent<Renderer>();
        if (rend != null)
        {
            // Usamos material standard temporal o cambiamos color directo si el shader lo permite
            rend.sharedMaterial.shader = Shader.Find("Standard"); 
            if (tag == "Crocodile")
            {
                rend.material.color = new Color(1f, 1f, 0f); // Amarillo puro chillón
            }
            else // Pothole
            {
                rend.material.color = new Color(0f, 1f, 1f); // Cyan/Azul eléctrico chillón
            }
        }

        // Eliminar collider para no interferir con futuros raycasts del dron
        Destroy(plane.GetComponent<Collider>());
        
        // Asignar un nombre informativo
        plane.name = $"Marker_{tag}_{System.DateTime.Now.Ticks}";
    }

    /// <summary>
    /// [Hover Mode] Verifica si hay bache en la posición donde estaba el obstáculo.
    /// La marcha ya fue reanudada por DroneNavMeshController antes de llamar a este método;
    /// si no se encuentra nada el drone simplemente continúa su ruta.
    /// </summary>
    private void CheckPotholeAtClearedPosition(Vector3 worldXZPos)
    {
        float scanHeight = (droneController != null) ? droneController.targetHeight : 5f;
        Vector3 origin   = new Vector3(worldXZPos.x, scanHeight + 1f, worldXZPos.z);
        float   scanDepth = scanHeight + 3f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, scanDepth))
        {
            string hitTag  = hit.collider.tag;
            string hitName = hit.collider.gameObject.name;

            if ((hitTag == "Pothole" || hitTag == "Crocodile") && !detectedPotholes.Contains(hitName))
            {
                detectedPotholes.Add(hitName);
                currentPothole = hitName;
                SpawnMarker(hit.collider.transform.position, hitTag);
                visiblePotholes = string.Join("\n", detectedPotholes);
                Debug.Log($"[Hover] Bache descubierto bajo obstáculo: {hitName}");
            }
            else
            {
                // Sin bache — marcha ya reanudada, no se requiere acción adicional
                Debug.Log($"[Hover] Sin bache en zona despejada (tag={hitTag}). Marcha en curso.");
            }
        }
        else
        {
            // Raycast no impactó nada — zona libre, marcha en curso
            Debug.Log("[Hover] Zona despejada sin bache detectado. Marcha en curso.");
        }
    }

    public void AcDc()
    {
        if (isCapturing)
        {
            isCapturing = false;
            // container = false;
            buttonText.text = "Record";
        }
        else
        {
            isCapturing = true;
            // container = true;
            buttonText.text = "Stop";
        }
    }

    public void restarted()
    {
        SceneManager.LoadScene("Mode_Load");
    }
}
