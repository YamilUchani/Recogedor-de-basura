using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camaras : MonoBehaviour
{
    public Camera Espectador;
    public Camera Recogedor;
    private Camera[] Cams;
    private Camera currentCamera;
    private int currentCameraIndex=0;
    private string camName; 
    // Start is called before the first frame update
    void Start()
    {
        Cams=new Camera[]{Espectador,Recogedor};
        currentCamera=Espectador;
        Cambio();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("v"))
        {
            currentCameraIndex++;
            if(currentCameraIndex>1)
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
