using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.IO;
using FF = Unity.InferenceEngine.Functional;

public class RunYOLO : MonoBehaviour
{
    public enum InputType { Video, Image, Camera }
    public InputType inputType = InputType.Camera;

    [Tooltip("YOLO model (.onnx)")]
    public Unity.InferenceEngine.ModelAsset modelAsset;

    [Tooltip("Classes file (classes.txt)")]
    public TextAsset classesAsset;

    [Tooltip("Raw Image para mostrar la salida")]
    public RawImage displayImage;

    [Tooltip("Textura de borde para las cajas")]
    public Texture2D borderTexture;

    [Tooltip("Fuente para las etiquetas")]
    public Font font;

    [Tooltip("Nombre del archivo de video (en StreamingAssets)")]
    public string videoFilename = "giraffes.mp4";

    [Tooltip("Nombre del archivo de imagen (en StreamingAssets)")]
    public string imageFilename = "image.jpg";

    [Tooltip("Cámara de Unity para usar como entrada")]
    public Camera sourceCamera;

    [Tooltip("Controla si se ejecuta el modelo")]
    public bool runModel = true;

    [Tooltip("Muestra las cajas detectadas")]
    public bool showBoxes = true;

    [Header("Controles adicionales")]
    public TextMeshProUGUI toggleModelText;
    public Camera cameraA;
    public Camera cameraB;
    private bool usingCameraA = true;

    const Unity.InferenceEngine.BackendType backend = Unity.InferenceEngine.BackendType.GPUCompute;

    private Transform displayLocation;
    private Unity.InferenceEngine.Worker worker;
    private string[] labels;
    private RenderTexture targetRT;
    private RenderTexture displayRT;
    private Sprite borderSprite;
    private const int imageWidth = 640;
    private const int imageHeight = 640;
    private VideoPlayer video;
    private Texture currentInput;
    private RenderTexture cameraRT;

    List<GameObject> boxPool = new();

    [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;
    [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;

    Unity.InferenceEngine.Tensor<float> centersToCorners;

    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
        public string label;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        labels = classesAsset.text.Split('\n');
        LoadModel();

        targetRT = new RenderTexture(imageWidth, imageHeight, 0);
        targetRT.Create();

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        displayRT = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.ARGB32);
        displayRT.Create();

        displayImage.texture = displayRT;
        displayLocation = displayImage.transform;

        if (cameraA != null)
        {
            sourceCamera = cameraA;
            usingCameraA = true;
        }


        SetupInput();

        borderSprite = Sprite.Create(borderTexture, new Rect(0, 0, borderTexture.width, borderTexture.height), new Vector2(borderTexture.width / 2f, borderTexture.height / 2f));
    }

    void LoadModel()
    {
        var model1 = Unity.InferenceEngine.ModelLoader.Load(modelAsset);

        centersToCorners = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(4, 4),
        new float[]
        {
            1, 0, 1, 0,
            0, 1, 0, 1,
            -0.5f, 0, 0.5f, 0,
            0, -0.5f, 0, 0.5f
        });

        var graph = new Unity.InferenceEngine.FunctionalGraph();
        var inputs = graph.AddInputs(model1);
        var modelOutput = FF.Forward(model1, inputs)[0];
        var boxCoords = Unity.InferenceEngine.Functional.Transpose(modelOutput[0, 0..4, ..], 0, 1);
        var allScores = modelOutput[0, 4.., ..];
        var scores = FF.ReduceMax(allScores, 0);
        var classIDs = FF.ArgMax(allScores, 0);
        var boxCorners = FF.MatMul(boxCoords, FF.Constant(centersToCorners));
        var indices = FF.NMS(boxCorners, scores, iouThreshold, scoreThreshold);
        var coords = FF.IndexSelect(boxCoords, 0, indices);
        var labelIDs = FF.IndexSelect(classIDs, 0, indices);

        worker = new Unity.InferenceEngine.Worker(graph.Compile(coords, labelIDs), backend);
    }

    void SetupInput()
    {
        if (inputType == InputType.Camera)
        {
            if (sourceCamera == null)
            {
                Debug.LogError("No se ha asignado una cámara.");
                return;
            }

            cameraRT = new RenderTexture(imageWidth, imageHeight, 0);
            cameraRT.Create();
            sourceCamera.targetTexture = cameraRT;
            currentInput = cameraRT;
        }
        else if (inputType == InputType.Video)
        {
            video = gameObject.AddComponent<VideoPlayer>();
            video.renderMode = VideoRenderMode.APIOnly;
            video.source = VideoSource.Url;
            video.url = Path.Join(Application.streamingAssetsPath, videoFilename);
            video.isLooping = true;
            video.Play();
        }
        else if (inputType == InputType.Image)
        {
            string path = Path.Join(Application.streamingAssetsPath, imageFilename);
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D temp = new Texture2D(2, 2);
            temp.LoadImage(bytes);
            currentInput = CreatePaddedTexture(temp, imageWidth);
            Destroy(temp);
        }
    }

    Texture2D CreatePaddedTexture(Texture2D source, int targetSize)
    {
        float aspect = (float)source.width / source.height;
        int newWidth, newHeight;
        if (source.width > source.height)
        {
            newWidth = targetSize;
            newHeight = Mathf.RoundToInt(targetSize / aspect);
        }
        else
        {
            newHeight = targetSize;
            newWidth = Mathf.RoundToInt(targetSize * aspect);
        }

        RenderTexture tempRT = new RenderTexture(newWidth, newHeight, 0);
        tempRT.Create();
        Graphics.Blit(source, tempRT);

        Texture2D resized = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        RenderTexture.active = tempRT;
        resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = null;
        tempRT.Release();

        Texture2D padded = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
        Color[] blackPixels = new Color[targetSize * targetSize];
        for (int i = 0; i < blackPixels.Length; i++) blackPixels[i] = Color.black;
        padded.SetPixels(blackPixels);

        int offsetX = (targetSize - newWidth) / 2;
        int offsetY = (targetSize - newHeight) / 2;
        padded.SetPixels(offsetX, offsetY, newWidth, newHeight, resized.GetPixels());
        padded.Apply();

        Destroy(resized);
        return padded;
    }

    void Update()
    {
        if (!runModel)
        {
            ClearAnnotations();
            return;
        }

        if (inputType == InputType.Video && video && video.texture)
        {
            currentInput = video.texture;
        }
        else if (inputType == InputType.Image && currentInput == null)
        {
            Debug.LogError("Image input not set.");
            return;
        }
        else if (inputType == InputType.Camera && sourceCamera != null)
        {
            currentInput = cameraRT;
        }

        ExecuteML();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void ExecuteML()
    {
        ClearAnnotations();

        if (currentInput == null || !runModel) return;

        Graphics.Blit(currentInput, targetRT);
        using Unity.InferenceEngine.Tensor<float> inputTensor = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, 3, imageHeight, imageWidth));
        Unity.InferenceEngine.TextureConverter.ToTensor(targetRT, inputTensor, default);
        worker.Schedule(inputTensor);
        Graphics.Blit(targetRT, displayRT);

        float displayWidth = displayImage.rectTransform.rect.width;
        float displayHeight = displayImage.rectTransform.rect.height;
        float scaleX = displayWidth / imageWidth;
        float scaleY = displayHeight / imageHeight;

        using var output = (worker.PeekOutput("output_0") as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
        using var labelIDs = (worker.PeekOutput("output_1") as Unity.InferenceEngine.Tensor<int>).ReadbackAndClone();

        int boxesFound = output.shape[0];
        for (int n = 0; n < Mathf.Min(boxesFound, 200); n++)
        {
            var box = new BoundingBox
            {
                centerX = output[n, 0] * scaleX - displayWidth / 2,
                centerY = output[n, 1] * scaleY - displayHeight / 2,
                width = output[n, 2] * scaleX,
                height = output[n, 3] * scaleY,
                label = labels[labelIDs[n]],
            };

            if (showBoxes)
                DrawBox(box, n, displayHeight * 0.05f);
        }
    }

    public void DrawBox(BoundingBox box, int id, float fontSize)
    {
        GameObject panel;
        if (id < boxPool.Count)
        {
            panel = boxPool[id];
            panel.SetActive(true);
        }
        else
        {
            panel = CreateNewBox(Color.yellow);
        }
        panel.transform.localPosition = new Vector3(box.centerX, -box.centerY);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(box.width, box.height);

        var label = panel.GetComponentInChildren<Text>();
        label.text = box.label;
        label.fontSize = (int)fontSize;
    }

    public GameObject CreateNewBox(Color color)
    {
        var panel = new GameObject("ObjectBox");
        panel.AddComponent<CanvasRenderer>();
        Image img = panel.AddComponent<Image>();
        img.color = color;
        img.sprite = borderSprite;
        img.type = Image.Type.Sliced;
        panel.transform.SetParent(displayLocation, false);

        var text = new GameObject("ObjectLabel");
        text.AddComponent<CanvasRenderer>();
        text.transform.SetParent(panel.transform, false);
        Text txt = text.AddComponent<Text>();
        txt.font = font;
        txt.color = color;
        txt.fontSize = 40;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rt2 = text.GetComponent<RectTransform>();
        rt2.offsetMin = new Vector2(20, 0);
        rt2.offsetMax = new Vector2(0, 100);
        rt2.anchorMin = new Vector2(0, 0);
        rt2.anchorMax = new Vector2(1, 1);

        boxPool.Add(panel);
        return panel;
    }

    public void ClearAnnotations()
    {
        foreach (var box in boxPool)
        {
            box.SetActive(false);
        }
    }

    public void ActivateModel()
    {
        runModel = true;
        if (toggleModelText != null)
        {
            toggleModelText.text = "Stop";
        }
    }
public void DeactivateModel()
{
    runModel = false;
    if (toggleModelText != null)
    {
        toggleModelText.text = "Run";
    }
}


public void SelectCameraA()
{
    if (cameraA != null)
    {
        sourceCamera.targetTexture = null;
        sourceCamera = cameraA;
        usingCameraA = true;
        SetupInput();
    }
}

public void SelectCameraB()
{
    if (cameraB != null)
    {
        sourceCamera.targetTexture = null;
        sourceCamera = cameraB;
        usingCameraA = false;
        SetupInput();
    }
}

public void ActivateBoxes()
{
    showBoxes = true;
}
public void DeactivateBoxes()
{
    showBoxes = false;
}

public void ToggleModelRunning()
    {
        runModel = !runModel;
        if (toggleModelText != null)
        {
            toggleModelText.text = runModel ? "Stop" : "Run";
        }
    }

    public void ToggleCamera()
    {
        usingCameraA = !usingCameraA;

        if (usingCameraA && cameraA != null)
        {
            sourceCamera.targetTexture = null;
            sourceCamera = cameraA;
        }
        else if (!usingCameraA && cameraB != null)
        {
            sourceCamera.targetTexture = null;
            sourceCamera = cameraB;
        }

        SetupInput();
    }

    public void ToggleCollider()
    {
        showBoxes = !showBoxes;
    }
    void OnDestroy()
    {
        centersToCorners?.Dispose();
        worker?.Dispose();

        if (currentInput != null && inputType == InputType.Image)
            Destroy(currentInput);

        if (sourceCamera != null)
        {
            sourceCamera.targetTexture = null;
            if (cameraRT != null)
            {
                cameraRT.Release();
                Destroy(cameraRT);
            }
        }

        if (targetRT != null)
        {
            targetRT.Release();
            Destroy(targetRT);
        }

        if (displayRT != null)
        {
            displayRT.Release();
            Destroy(displayRT);
        }
    }
}
