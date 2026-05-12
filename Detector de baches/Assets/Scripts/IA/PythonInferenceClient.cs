using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class PythonInferenceClient : MonoBehaviour
{
    public static PythonInferenceClient Instance;

    [Tooltip("URL del servidor Python")]
    public string apiUrl = "http://127.0.0.1:5000/predict";

    // Clases para parsear el JSON
    [Serializable]
    public class BoxData {
        public string clase;
        public float confianza;
        public int[] caja; // [x1, y1, x2, y2]
    }

    [Serializable]
    public class ResponseData {
        public BoxData[] detecciones;
    }

    void Awake()
    {
        Instance = this;
    }

    // Método público para ser llamado desde MovementInterface
    public void AnalyzeImageBytes(byte[] imageBytes, string imageID)
    {
        StartCoroutine(SendFrameToPython(imageBytes, imageID));
    }

    IEnumerator SendFrameToPython(byte[] imageBytes, string imageID)
    {
        // Crear formulario Multipart
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, imageID, "image/png");

        // Enviar petición POST a Python
        using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                ParsearYDibujarCajas(jsonResponse, imageID);
            }
            else
            {
                Debug.LogWarning($"Error de conexión con Python al analizar {imageID}: {www.error}");
            }
        }
    }

    void ParsearYDibujarCajas(string jsonArray, string imageID)
    {
        string wrappedJson = "{\"detecciones\":" + jsonArray + "}";
        ResponseData data = JsonUtility.FromJson<ResponseData>(wrappedJson);

        if (data.detecciones != null && data.detecciones.Length > 0)
        {
            foreach (var box in data.detecciones)
            {
                Debug.Log($"[IA API -> {imageID}] Detectado: {box.clase} ({box.confianza*100:F1}%) en X:{box.caja[0]}, Y:{box.caja[1]}");
            }
        }
        else
        {
            Debug.Log($"[IA API -> {imageID}] La red neuronal no detectó ningún objeto válido en esta imagen.");
        }
    }
}
