using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recoger : MonoBehaviour
{
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Vidrio")
        {
            Conteoglobal.conteovidrio++;
            Conteoglobal.conteototal++;
        }
        if (other.gameObject.tag == "Metal")
        {
            Conteoglobal.conteometal++;
            Conteoglobal.conteototal++;
        }
        if (other.gameObject.tag == "Papel")
        {
            Conteoglobal.conteopapel++;
            Conteoglobal.conteototal++;
        }
    }
}
