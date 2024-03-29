using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camaras : MonoBehaviour
{
    //Estas son las variables  donde se cargan las distintas camaras que se utilizaran para ver al recogedor de basura
    public Camera Espectador;
    public Camera Recogedor;
    public Camera Delado;
    //Esta variable se encargara de almacenar las variables de cada camara
    private Camera[] Cams;
    //Esta variable se encarga de almacenar a la camara seleccionada  
    private Camera currentCamera;
    //En esta variable se almacena el orden actual de la camara
    public int currentCameraIndex=2;
    //Se realizar estas operaciones al iniciar el programa
    void Start()
    {
        //Se almacenan las camaras
        Cams=new Camera[]{Espectador,Recogedor,Delado};
        //Se pone la camara por defecto
        currentCamera=Espectador;
        //Se hace el cambio de camara
        Cambio();
    }
    //Se realizan las operaciones en cada frame que hay
    void Update()
    {
        //Se aumenta el valor del orden de la camara cada vez que se presione el boton "v"
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
