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

        RenderTexture rt = new RenderTexture(resolution.x, resolution.y, 24);
        RenderTexture previousRT = targetCamera.targetTexture;
        
        targetCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
        
        targetCamera.Render();
        
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
        screenShot.Apply();

        targetCamera.targetTexture = previousRT;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string timeID = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Capture_{timeID}_{Random.Range(0, 1000)}.png";
        
        FileHandler.SaveImage(bytes, filename);
        
        Destroy(screenShot);
        Debug.Log($"<color=cyan>Screenshot Saved: {filename}</color>");
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
