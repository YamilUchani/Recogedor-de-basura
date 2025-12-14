using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;

public static class FileHandler
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadFileFromBase64(string fileName, string base64);
#endif

    public static void SaveImage(byte[] pngBytes, string filename)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: Trigger browser download
        string b64 = System.Convert.ToBase64String(pngBytes);
        DownloadFileFromBase64(filename, b64);
#else
        // PC / Android: Save to disk
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = Path.Combine(Application.persistentDataPath, "Imagenes", "Imagenes_de_baches", timestamp);
        
        // Ensure directory exists
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        
        string fullPath = Path.Combine(folderPath, filename);
        File.WriteAllBytes(fullPath, pngBytes);
        Debug.Log($"File saved to: {fullPath}");
#endif
    }
}
