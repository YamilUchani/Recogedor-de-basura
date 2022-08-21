using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generador : MonoBehaviour
{
    public List<GameObject> taco = new List<GameObject>();
    public int conteo;
    public int seccion;
    public Recoger conteorecolector;
    private void Awake() 
    {
        conteo=taco.Count;
    }
    private void FixedUpdate() {
        if(Conteoglobal.elementos<Conteoglobal.basura_total)
        {
            seccion=Random.Range(0,4);
            switch(seccion)
            {
                case 0:
                    Instantiate(taco[Random.Range(0,conteo)], new Vector3(Random.Range(-14f,-15f), 0.4f,Random.Range(-51.5f,-14.5f)), Quaternion.Euler(45, 45,45));
                    Conteoglobal.elementos++;
                    break;
                case 1:
                    Instantiate(taco[Random.Range(0,conteo)], new Vector3(Random.Range(-14.5f,-94.5f), 0.4f,Random.Range(-14f,-15f)), Quaternion.Euler(45, 45,45));
                    Conteoglobal.elementos++;
                    break;
                case 2:
                    Instantiate(taco[Random.Range(0,conteo)], new Vector3(Random.Range(-94f,-95f), 0.4f,Random.Range(-14.5f,-51.5f)), Quaternion.Euler(45, 45,45));
                    Conteoglobal.elementos++;
                    break;
                case 3:
                    Instantiate(taco[Random.Range(0,conteo)], new Vector3(Random.Range(-94.5f,-14.5f), 0.4f,Random.Range(-51f,-52f)), Quaternion.Euler(45, 45,45));
                    Conteoglobal.elementos++;
                    break;
                
            }
            
        }
        if(Conteoglobal.elementos==Conteoglobal.conteototal)
        {
            Conteoglobal.conteototal=0;
            Conteoglobal.elementos=0;
        }
    }
}