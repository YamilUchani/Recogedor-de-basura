using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camaras : MonoBehaviour
{
    public Camera Espectador;
    public Camera Recogedor;
    public Camera Delado;
    private Camera[] Cams;
    private Camera currentCamera;
    private int currentCameraIndex=0;
    private string camName; 
    // Start is called before the first frame update
    void Start()
    {
        Cams=new Camera[]{Espectador,Recogedor,Delado};
        currentCamera=Espectador;
        Cambio();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("v"))
        {
            currentCameraIndex++;
            if(currentCameraIndex>2)
            {
                currentCameraIndex=0;
            }
            Cambio();
        }
        
    }
    void Cambio()
    {
        currentCamera.enabled=false;
        currentCamera=Cams[currentCameraIndex];
        currentCamera.enabled=true;
    }
}
