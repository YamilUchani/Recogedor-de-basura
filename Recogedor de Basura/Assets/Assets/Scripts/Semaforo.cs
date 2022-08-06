using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Semaforo : MonoBehaviour
{
    private float cambio=30.0f;
    private float lapso=5.0f;
    private bool alto=false;
    private bool espera=true;
    private bool pase=false;
    public GameObject rojo;
    public GameObject amarillo;
    public GameObject verde;
    public GameObject rojoinverso;
    public GameObject amarilloinverso;
    public GameObject verdeinverso;

    private void Start() {
        rojo.SetActive(true);
        amarillo.SetActive(false);
        verde.SetActive(false);
        rojoinverso.SetActive(false);
        amarilloinverso.SetActive(false);
        verdeinverso.SetActive(true);
        StartCoroutine(WaitAndPrint(cambio));
    }
    IEnumerator WaitAndPrint(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if(alto)
        {
            verde.SetActive(false);
            rojo.SetActive(true);
            verdeinverso.SetActive(true);
            rojoinverso.SetActive(false);
            alto=false;
            espera=true;
            StartCoroutine(WaitAndPrint(cambio));
        } 
        else if(espera)
        {
            rojo.SetActive(false);
            amarillo.SetActive(true);
            verdeinverso.SetActive(false);
            amarilloinverso.SetActive(true);
            espera=false;
            pase=true;
            StartCoroutine(WaitAndPrint(lapso));
        }
        else if(pase)
        {
            amarillo.SetActive(false);
            verde.SetActive(true);
            amarilloinverso.SetActive(false);
            rojoinverso.SetActive(true);
            pase=false;
            alto=true;
            StartCoroutine(WaitAndPrint(cambio));
        }          
        
    }
    
}
