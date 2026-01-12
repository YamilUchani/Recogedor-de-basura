using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PotholeCaptureManager : MonoBehaviour
{
    [Header("Dependencies")]
    public TerrainPotholeGenerator potholeGenerator;
    public Camera targetCamera;

    [Header("Movement Settings")]
    public float movementSpeed = 5f;
    public float minHeight = 0.5f;
    public float maxHeight = 25f;

    [Header("Capture Settings")]
    public float autoInterval = 2.0f;
    [Range(0f, 1f)] public float minVisibilityPercentage = 0.4f;
    public Vector2Int resolution = new Vector2Int(1270, 950);
    public Color colorPothole = Color.cyan;
    public Color colorCrocodile = Color.yellow;

    [Header("Controls")]
    public KeyCode keyMoveUp = KeyCode.UpArrow;
    public KeyCode keyMoveDown = KeyCode.DownArrow;
    public KeyCode keyManualSeed = KeyCode.S;
    public KeyCode keyManualCapture = KeyCode.R;
    public KeyCode keyToggleAuto = KeyCode.T;
    public string menuSceneName = "Menu_scene";

    private bool isAutoMode = false;
    private Coroutine autoCoroutine;

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
        HandleInputs();
    }

    void HandleHeightMovement()
    {
        float move = 0;
        if (Input.GetKey(keyMoveUp)) move = 1;
        if (Input.GetKey(keyMoveDown)) move = -1;

        if (move != 0)
        {
            Vector3 pos = transform.position;
            pos.y += move * movementSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
            transform.position = pos;
        }
    }

    void HandleInputs()
    {
        // S: Shuffle Seed (Manual)
        if (Input.GetKeyDown(keyManualSeed) && !isAutoMode)
        {
            RandomizeAndGenerate();
        }

        // R: Manual Capture
        if (Input.GetKeyDown(keyManualCapture))
        {
            CaptureScreenshot();
        }

        // T: Toggle Auto Mode
        if (Input.GetKeyDown(keyToggleAuto))
        {
            ToggleAutoMode();
        }

        // ESC: Return to Menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
        }
    }

    public void RandomizeAndGenerate()
    {
        if (potholeGenerator != null)
        {
            potholeGenerator.Seed = Random.Range(0, 1000000);
            potholeGenerator.Generate();
            Debug.Log($"Potholes Regenerated with Seed: {potholeGenerator.Seed}");
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

        // Procesar baches normales (clase 0)
        foreach (var pothole in potholes)
        {
            var boxInfo = GetBoundingBoxInfo(pothole, 0, "Pothole", colorPothole);
            if (boxInfo.HasValue)
            {
                boxes.Add(boxInfo.Value);
                annotations.Add(FormatYOLOAnnotation(boxInfo.Value));
            }
        }

        // Procesar cocodrilos (clase 1)
        foreach (var croc in crocodiles)
        {
            var boxInfo = GetBoundingBoxInfo(croc, 1, "Crocodile", colorCrocodile);
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
        // Obtener el MeshRenderer para calcular bounds (AABB del editor)
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null) return null;

        Bounds bounds = renderer.bounds;
        
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
        Rect screenRect = new Rect(
            clampedMinX,
            clampedMinY,
            visibleWidth,
            visibleHeight
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


    void ToggleAutoMode()
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

    private IEnumerator AutoCaptureLoop()
    {
        while (isAutoMode)
        {
            RandomizeAndGenerate();
            yield return new WaitForSeconds(0.1f);
            CaptureScreenshot();
            yield return new WaitForSeconds(autoInterval);
        }
    }
}
