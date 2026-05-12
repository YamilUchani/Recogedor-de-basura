using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PotholeCaptureManager : MonoBehaviour
{
    [Header("Dependencies")]
    public TerrainPotholeGenerator potholeGenerator;
    public Camera targetCamera;

    [Tooltip("Generadores de prefabs que se regeneran en cada ciclo con semilla propia.")]
    public List<PrefabObjectGenerator> prefabGenerators = new List<PrefabObjectGenerator>();

    [Header("Movement Settings")]
    public float movementSpeed = 5f;
    public float minHeight = 0.5f;
    public float maxHeight = 25f;

    [Header("Capture Settings")]
    public float autoInterval = 2.0f;
    [Range(0f, 1f)] public float minVisibilityPercentage = 0.4f;
    public Vector2Int resolution = new Vector2Int(1270, 950);
    [Tooltip("Escala del Bounding Box (1 = Ajustado, 0.8 = Más pequeño, 1.2 = Más holgado)")]
    [Range(0.1f, 2f)] public float boundingBoxScale = 1.0f;
    public Color colorPothole = Color.cyan;
    public Color colorCrocodile = Color.yellow;
    public Color colorRajadura = Color.magenta;
    public Color colorPerson = Color.green;
    public Color colorTag = Color.blue;

    [Header("UI & Navigation")]
    public string menuScene = "Mode_Menu";

    private bool isAutoMode = false;
    private Coroutine autoCoroutine;

    // Movement state for UI buttons
    private bool isMovingUp = false;
    private bool isMovingDown = false;

    void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (potholeGenerator == null) potholeGenerator = Object.FindFirstObjectByType<TerrainPotholeGenerator>();
        
        if (targetCamera == null) Debug.LogError("CaptureManager: No Camera found!");
        if (potholeGenerator == null) Debug.LogWarning("CaptureManager: No TerrainPotholeGenerator found in scene!");
    }

    void Update()
    {
        HandleHeightMovement();
    }

    void HandleHeightMovement()
    {
        float move = 0;
        if (isMovingUp || Input.GetKey(KeyCode.UpArrow)) move = 1;
        if (isMovingDown || Input.GetKey(KeyCode.DownArrow)) move = -1;

        if (move != 0)
        {
            Vector3 pos = transform.position;
            pos.y += move * movementSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
            transform.position = pos;
        }
    }

    // ─── UI BUTTON FUNCTIONS ────────────────────────────────────────────────

    public void StartMovingUp() => isMovingUp = true;
    public void StopMovingUp() => isMovingUp = false;
    
    public void StartMovingDown() => isMovingDown = true;
    public void StopMovingDown() => isMovingDown = false;

    public void UIManualGenerate()
    {
        if (!isAutoMode)
        {
            RandomizeAndGenerate();
        }
        else
        {
            Debug.LogWarning("Cannot generate manually while Auto Mode is active. Turn it off first.");
        }
    }

    // CaptureScreenshot() and ToggleAutoMode() are also ready to be used by UI buttons

    public void ReturnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene);
    }

    private int seedCounter = 0; // Add counter to ensure uniqueness

    public void RandomizeAndGenerate()
    {
        if (potholeGenerator == null)
        {
            Debug.LogError("<color=red>[ERROR] potholeGenerator is NULL! Assign it in the Inspector.</color>");
            return;
        }

        long ticks = System.DateTime.Now.Ticks;

        // ── Baches ────────────────────────────────────────────────────────────
        int oldSeed = potholeGenerator.Seed;
        potholeGenerator.Seed = (int)((ticks & 0x7FFFFFFF) + seedCounter);
        seedCounter++;
        int newSeed = potholeGenerator.Seed;

        Debug.Log($"<color=green>[SEED CHANGE] {oldSeed} → {newSeed}</color>");
        potholeGenerator.Generate();
        Debug.Log($"<color=green>[GENERATION] Complete with Seed: {newSeed}</color>");

        // ── Prefab Generators (semilla propia por cada uno) ───────────────────
        if (prefabGenerators != null)
        {
            for (int i = 0; i < prefabGenerators.Count; i++)
            {
                PrefabObjectGenerator gen = prefabGenerators[i];
                if (gen == null) continue;

                // Semilla independiente: base diferente a la de los baches
                gen.seed = (int)(((ticks >> 8) & 0x7FFFFFFF) + seedCounter * 31 + i * 1000003);
                gen.Generate();
            }
        }
    }


    public void CaptureScreenshot()
    {
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        yield return new WaitForEndOfFrame();

        // 1. Setup RenderTexture (Resolution Correcta)
        RenderTexture rt = new RenderTexture(resolution.x, resolution.y, 24);
        RenderTexture previousRT = targetCamera.targetTexture;
        targetCamera.targetTexture = rt;

        // 2. CAPTURA LIMPIA (Clean)
        Texture2D screenShotClean = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
        targetCamera.Render();
        RenderTexture.active = rt;
        screenShotClean.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
        screenShotClean.Apply();
        RenderTexture.active = null; // Liberar temporalmente

        string timeID = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Capture_{timeID}_{Random.Range(0, 1000)}";

        // 3. CALCULAR DATOS (Txt)
        // Calculamos las cajas basándonos en la cámara configurada
        List<BoundingBoxInfo> boxes = GenerateYOLOAnnotations(filename);
        
        // 4. VISUALIZAR EN ESCENA (UI Canvas)
        GameObject canvasObj = CreateVisualizationCanvas(boxes);
        
        // Esperar un frame para que la UI se actualice/renderice?
        // En RenderTextures a veces es inmediato si forzamos Render.
        
        // 5. CAPTURA ANOTADA (Annotated)
        Texture2D screenShotAnnotated = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
        targetCamera.Render(); // Renderizar de nuevo con la UI superpuesta
        RenderTexture.active = rt;
        screenShotAnnotated.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
        screenShotAnnotated.Apply();
        RenderTexture.active = null;

        // 6. LIMPIEZA
        targetCamera.targetTexture = previousRT;
        Destroy(rt);
        Destroy(canvasObj); // Borrar la UI visualizada

        // 7. GUARDAR ARCHIVOS
        byte[] bytesClean = screenShotClean.EncodeToPNG();
        FileHandler.SaveImage(bytesClean, filename + ".png");

        if (boxes.Count > 0)
        {
            byte[] bytesAnnotated = screenShotAnnotated.EncodeToPNG();
            FileHandler.SaveImage(bytesAnnotated, filename + "_annotated.png");
        }

        Destroy(screenShotClean);
        Destroy(screenShotAnnotated);

        Debug.Log($"<color=cyan>Capture Complete: {filename} ({boxes.Count} objects)</color>");
    }

    // ─── VISUALIZATION HELPERS ───────────────────────────────────────────────

    private GameObject CreateVisualizationCanvas(List<BoundingBoxInfo> boxes)
    {
        // 1. Crear Canvas
        GameObject canvasObj = new GameObject("Temp_Vis_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = targetCamera;
        canvas.planeDistance = 1f; // Justo delante de la cámara

        // Ajustar Scaler para que coincida 1:1 con la resolución de captura
        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(resolution.x, resolution.y);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Crear Cajas
        foreach (var box in boxes)
        {
            CreateBoxUI(canvasObj.transform, box);
        }

        return canvasObj;
    }

    private void CreateBoxUI(Transform parent, BoundingBoxInfo box)
    {
        // Contenedor del rect
        GameObject boxObj = new GameObject($"Box_{box.className}");
        boxObj.transform.SetParent(parent, false);
        
        RectTransform rectD = boxObj.AddComponent<RectTransform>();
        
        // CORRECCIÓN DE COORDENADAS:
        // box.screenRect viene en formato Imagen (Origen Top-Left)
        // Unity UI usa formato Cartesiano (Origen Bottom-Left)
        
        // X es igual (Left -> Right)
        float xMin = box.screenRect.x / resolution.x;
        float xMax = (box.screenRect.x + box.screenRect.width) / resolution.x;
        
        // Y hay que invertirlo
        // Top de la imagen (y=0) -> Top del Canvas (anchor=1)
        // Bottom de la imagen (y=res.y) -> Bottom del Canvas (anchor=0)
        
        float yTop_Image = box.screenRect.y;
        float yBottom_Image = box.screenRect.y + box.screenRect.height;
        
        float yMax = 1f - (yTop_Image / resolution.y);      // Top Anchor
        float yMin = 1f - (yBottom_Image / resolution.y);   // Bottom Anchor

        rectD.anchorMin = new Vector2(xMin, yMin);
        rectD.anchorMax = new Vector2(xMax, yMax);
        rectD.offsetMin = Vector2.zero;
        rectD.offsetMax = Vector2.zero;

        // Bordes (4 Imagenes)
        float thickness = 2f; // Grosor en píxeles
        Color c = box.color;

        // Top Border
        CreateLine(boxObj.transform, "Top", 
            new Vector2(0, 1), new Vector2(1, 1), // Anchors: Top Edge
            new Vector2(0, -thickness), new Vector2(0, 0), c); // Height = thickness, downwards

        // Bottom Border
        CreateLine(boxObj.transform, "Bottom", 
            new Vector2(0, 0), new Vector2(1, 0), // Anchors: Bottom Edge
            new Vector2(0, 0), new Vector2(0, thickness), c); // Height = thickness, upwards

        // Left Border
        CreateLine(boxObj.transform, "Left", 
            new Vector2(0, 0), new Vector2(0, 1), // Anchors: Left Edge
            new Vector2(0, 0), new Vector2(thickness, 0), c); // Width = thickness, rightwards

        // Right Border
        CreateLine(boxObj.transform, "Right", 
            new Vector2(1, 0), new Vector2(1, 1), // Anchors: Right Edge
            new Vector2(-thickness, 0), new Vector2(0, 0), c); // Width = thickness, leftwards

        // Etiqueta (Label)
        CreateLabel(boxObj.transform, box.className, c);
    }

    private void CreateLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color c)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent, false);
        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        
        UnityEngine.UI.Image img = line.AddComponent<UnityEngine.UI.Image>();
        img.color = c;
        img.raycastTarget = false;
    }

    private void CreateLabel(Transform parent, string textStr, Color c)
    {
        // Panel para el texto
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent, false);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        // Posicionar arriba-izquierda del box
        labelRect.anchorMin = new Vector2(0, 1); 
        labelRect.anchorMax = new Vector2(0, 1);
        labelRect.pivot = new Vector2(0, 0); // Pivote abajo-izquierda (para crecer hacia arriba)
        labelRect.anchoredPosition = new Vector2(0, 0); // Pegado al borde superior
        
        // Tamaño fijo aproximado para el fondo
        labelRect.sizeDelta = new Vector2(100, 20); 

        // Fondo semi-transparente
        UnityEngine.UI.Image bg = labelObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0,0,0, 0.7f);
        bg.raycastTarget = false;

        // Texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(labelObj.transform, false);
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = textStr;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = c;
        // text.pixelsPerUnitMultiplier = 1; // Removed: Not available in standard Text
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(5,0); // Padding left
        text.rectTransform.offsetMax = Vector2.zero;
        text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 14;
        text.raycastTarget = false;
    }

    private struct BoundingBoxInfo
    {
        public int classId;
        public string className;
        public Rect screenRect;
        public Color color;
    }

    private List<BoundingBoxInfo> GenerateYOLOAnnotations(string filename)
    {
        List<BoundingBoxInfo> boxes = new List<BoundingBoxInfo>();
        
        if (potholeGenerator == null) return boxes;

        List<string> annotations = new List<string>();
        
        // Buscar todos los baches generados en la escena
        GameObject[] potholes = GameObject.FindGameObjectsWithTag("Pothole");
        GameObject[] crocodiles = GameObject.FindGameObjectsWithTag("Crocodile");
        GameObject[] rajaduras = GameObject.FindGameObjectsWithTag("Crack");
        GameObject[] persons = GameObject.FindGameObjectsWithTag("Person");
        GameObject[] tagObjs = GameObject.FindGameObjectsWithTag("Car");

        Debug.Log($"<color=orange>[Capture] Objects Found - Potholes: {potholes.Length}, Crocodiles: {crocodiles.Length}, Cracks: {rajaduras.Length}, Persons: {persons.Length}, Cars: {tagObjs.Length}</color>");

        // Procesar baches normales (clase 0)
        foreach (var pothole in potholes)
        {
            var boxInfo = GetBoundingBoxInfo(pothole, 0, "Bache", colorPothole);
            if (boxInfo.HasValue)
            {
                boxes.Add(boxInfo.Value);
                annotations.Add(FormatYOLOAnnotation(boxInfo.Value));
            }
        }

        // Procesar cocodrilos (clase 1)
        foreach (var croc in crocodiles)
        {
            var boxInfo = GetBoundingBoxInfo(croc, 1, "Cocodrilo", colorCrocodile);
            if (boxInfo.HasValue)
            {
                boxes.Add(boxInfo.Value);
                annotations.Add(FormatYOLOAnnotation(boxInfo.Value));
            }
        }

        // Procesar rajaduras (clase 2)
        foreach (var raj in rajaduras)
        {
            var boxInfo = GetBoundingBoxInfo(raj, 2, "Crack", colorRajadura);
            if (boxInfo.HasValue)
            {
                boxes.Add(boxInfo.Value);
                annotations.Add(FormatYOLOAnnotation(boxInfo.Value));
            }
        }

        // Procesar Personas (clase 3)
        foreach (var p in persons)
        {
            var boxInfo = GetBoundingBoxInfo(p, 3, "Person", colorPerson);
            if (boxInfo.HasValue)
            {
                boxes.Add(boxInfo.Value);
                annotations.Add(FormatYOLOAnnotation(boxInfo.Value));
            }
        }

        // Procesar Tags (clase 4)
        foreach (var t in tagObjs)
        {
            var boxInfo = GetBoundingBoxInfo(t, 4, "Car", colorTag);
            if (boxInfo.HasValue)
            {
                boxes.Add(boxInfo.Value);
                annotations.Add(FormatYOLOAnnotation(boxInfo.Value));
            }
        }

        // Guardar archivo de anotaciones
        if (annotations.Count > 0)
        {
            string annotationText = string.Join("\n", annotations);
            FileHandler.SaveAnnotation(annotationText, filename + ".txt");
        }

        return boxes;
    }

    private BoundingBoxInfo? GetBoundingBoxInfo(GameObject obj, int classId, string className, Color color)
    {
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        // Priorizar BoxColliders para Personas y Coches si existen
        if (className == "Person" || className == "Car")
        {
            BoxCollider[] colliders = obj.GetComponentsInChildren<BoxCollider>();
            if (colliders.Length > 0)
            {
                bounds = colliders[0].bounds;
                boundsInitialized = true;
                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }
        }

        // Fallback a Renderers si no se inicializaron los bounds (o para otras clases)
        if (!boundsInitialized)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;

            bounds = renderers[0].bounds;
            boundsInitialized = true;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }
        
        // Obtener las 8 esquinas del bounding box en espacio mundial
        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = bounds.max;

        // Proyectar esquinas a coordenadas de pantalla (píxeles)
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        bool hasVisibleVertex = false;

        foreach (var corner in corners)
        {
            // Convertir de mundo a viewport (0-1)
            Vector3 viewportPoint = targetCamera.WorldToViewportPoint(corner);
            
            // Verificar si está delante de la cámara
            if (viewportPoint.z > 0)
            {
                hasVisibleVertex = true;
                
                // Convertir viewport a píxeles
                float pixelX = viewportPoint.x * resolution.x;
                float pixelY = (1f - viewportPoint.y) * resolution.y; // Invertir Y
                
                minX = Mathf.Min(minX, pixelX);
                minY = Mathf.Min(minY, pixelY);
                maxX = Mathf.Max(maxX, pixelX);
                maxY = Mathf.Max(maxY, pixelY);
            }
        }

        if (!hasVisibleVertex) return null;
        
        // --- OCCLUSION CHECK ---
        // Verificar qué tan visible es el objeto considerando obstrucciones (ej: un auto tapando parte de un bache)
        float visibilityFactor = GetVisibilityFactor(obj, bounds);
        if (visibilityFactor < 0.2f) return null; // Si menos del 20% es visible, no etiquetar

        // --- VISIBILITY CHECK ---
        float rawWidth = maxX - minX;
        float rawHeight = maxY - minY;
        float rawArea = rawWidth * rawHeight;

        // Clampear a los límites de la imagen
        float clampedMinX = Mathf.Clamp(minX, 0, resolution.x);
        float clampedMinY = Mathf.Clamp(minY, 0, resolution.y);
        float clampedMaxX = Mathf.Clamp(maxX, 0, resolution.x);
        float clampedMaxY = Mathf.Clamp(maxY, 0, resolution.y);

        float visibleWidth = clampedMaxX - clampedMinX;
        float visibleHeight = clampedMaxY - clampedMinY;
        
        // Verificar que el bounding box tenga un tamaño mínimo
        if (visibleWidth < 1 || visibleHeight < 1) return null;

        if (rawArea > 0)
        {
            float visibleArea = visibleWidth * visibleHeight;
            float ratio = visibleArea / rawArea;

            // Descartar si se ve menos del % configurado
            if (ratio < minVisibilityPercentage) return null;
        }

        // Crear rectángulo en coordenadas de píxeles
        // Crear rectángulo en coordenadas de píxeles
        float finalWidth = visibleWidth * boundingBoxScale;
        float finalHeight = visibleHeight * boundingBoxScale;

        float centerX = clampedMinX + (visibleWidth * 0.5f);
        float centerY = clampedMinY + (visibleHeight * 0.5f);

        float finalMinX = centerX - (finalWidth * 0.5f);
        float finalMinY = centerY - (finalHeight * 0.5f);

        Rect screenRect = new Rect(
            finalMinX,
            finalMinY,
            finalWidth,
            finalHeight
        );

        return new BoundingBoxInfo
        {
            classId = classId,
            className = className,
            screenRect = screenRect,
            color = color
        };
    }

    private string FormatYOLOAnnotation(BoundingBoxInfo boxInfo)
    {
        // Las coordenadas ya están en píxeles, normalizamos a 0-1 para YOLO
        float x_center = (boxInfo.screenRect.x + boxInfo.screenRect.width / 2f) / resolution.x;
        float y_center = (boxInfo.screenRect.y + boxInfo.screenRect.height / 2f) / resolution.y;
        float width = boxInfo.screenRect.width / resolution.x;
        float height = boxInfo.screenRect.height / resolution.y;

        // YOLO usa origen arriba-izquierda, nuestras coords ya están en ese sistema
        return $"{boxInfo.classId} {x_center:F6} {y_center:F6} {width:F6} {height:F6}";
    }

    private Texture2D DrawBoundingBoxes(Texture2D original, List<BoundingBoxInfo> boxes)
    {
        Texture2D result = new Texture2D(original.width, original.height, TextureFormat.RGB24, false);
        result.SetPixels(original.GetPixels());

        foreach (var box in boxes)
        {
            // Dibujar rectángulo
            DrawRect(result, box.screenRect, box.color, 3);
            
            // Dibujar etiqueta con fondo
            DrawLabel(result, box.className, new Vector2(box.screenRect.x, box.screenRect.y - 20), box.color);
        }

        result.Apply();
        return result;
    }

    private void DrawRect(Texture2D tex, Rect rect, Color color, int thickness)
    {
        // Dibujar los 4 lados del rectángulo
        for (int t = 0; t < thickness; t++)
        {
            // Top
            for (int x = (int)rect.x; x < rect.x + rect.width; x++)
                SetPixelSafe(tex, x, (int)rect.y + t, color);
            
            // Bottom
            for (int x = (int)rect.x; x < rect.x + rect.width; x++)
                SetPixelSafe(tex, x, (int)(rect.y + rect.height) - t, color);
            
            // Left
            for (int y = (int)rect.y; y < rect.y + rect.height; y++)
                SetPixelSafe(tex, (int)rect.x + t, y, color);
            
            // Right
            for (int y = (int)rect.y; y < rect.y + rect.height; y++)
                SetPixelSafe(tex, (int)(rect.x + rect.width) - t, y, color);
        }
    }

    private void DrawLabel(Texture2D tex, string text, Vector2 position, Color color)
    {
        // Dibujar fondo negro semi-transparente para la etiqueta
        int labelWidth = text.Length * 8;
        int labelHeight = 16;
        
        for (int x = 0; x < labelWidth; x++)
        {
            for (int y = 0; y < labelHeight; y++)
            {
                SetPixelSafe(tex, (int)position.x + x, (int)position.y + y, new Color(0, 0, 0, 0.7f));
            }
        }
        
        // Nota: Para texto real necesitarías una fuente bitmap o usar GUI.
        // Por ahora dibujamos un rectángulo de color con el nombre en el log
    }

    private void SetPixelSafe(Texture2D tex, int x, int y, Color color)
    {
        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
        {
            tex.SetPixel(x, y, color);
        }
    }


    public void ToggleAutoMode()
    {
        isAutoMode = !isAutoMode;
        if (isAutoMode)
        {
            autoCoroutine = StartCoroutine(AutoCaptureLoop());
            Debug.Log("<color=green>Automatic Mode: ENABLED</color>");
        }
        else
        {
            if (autoCoroutine != null) StopCoroutine(autoCoroutine);
            Debug.Log("<color=red>Automatic Mode: DISABLED</color>");
        }
    }

    private bool AreGeneratorsBusy()
    {
        if (prefabGenerators == null) return false;
        foreach (var gen in prefabGenerators)
        {
            if (gen != null && gen.IsGenerating) return true;
        }
        return false;
    }

    private IEnumerator AutoCaptureLoop()
    {
        while (isAutoMode)
        {
            RandomizeAndGenerate();

            // Esperar hasta que todos los generadores de elementos terminen
            yield return new WaitUntil(() => !AreGeneratorsBusy());

            yield return new WaitForSeconds(0.1f);
            CaptureScreenshot();
            yield return new WaitForSeconds(autoInterval);
        }
    }

    private float GetVisibilityFactor(GameObject obj, Bounds bounds)
    {
        Vector3 camPos = targetCamera.transform.position;
        Vector3 center = bounds.center;
        Vector3 ext = bounds.extents;

        // Definir puntos para muestrear visibilidad (Centro + 4 esquinas del área)
        // Usamos un factor de 0.7 para no estar exactamente en el borde del collider
        Vector3[] points = new Vector3[5];
        points[0] = center;
        points[1] = center + new Vector3(ext.x * 0.7f, 0, ext.z * 0.7f);
        points[2] = center + new Vector3(-ext.x * 0.7f, 0, ext.z * 0.7f);
        points[3] = center + new Vector3(ext.x * 0.7f, 0, -ext.z * 0.7f);
        points[4] = center + new Vector3(-ext.x * 0.7f, 0, -ext.z * 0.7f);

        int visibleCount = 0;
        foreach (var p in points)
        {
            Vector3 dir = p - camPos;
            float dist = dir.magnitude;

            if (Physics.Raycast(camPos, dir, out RaycastHit hit, dist + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // Visible si golpeamos el objeto o un hijo
                if (hit.collider.gameObject == obj || hit.collider.transform.IsChildOf(obj.transform))
                {
                    visibleCount++;
                }
                // También es visible si el impacto está a la misma distancia (margen de error)
                else if (hit.distance >= dist - 0.05f)
                {
                    visibleCount++;
                }
            }
            else
            {
                // Si no hay colisión en la trayectoria, asumimos que está despejado
                visibleCount++;
            }
        }

        return (float)visibleCount / points.Length;
    }
}
