using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recoger : MonoBehaviour
{
    public int conteovidrio;
    public int conteometal;
    public int conteopapel;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Vidrio")
        {
            Debug.Log(other.gameObject.tag);
            conteovidrio++;
        }
        if (other.gameObject.tag == "Metal")
        {
            Debug.Log(other.gameObject.tag);
            conteometal++;
        }
        if (other.gameObject.tag == "Papel")
        {
            Debug.Log(other.gameObject.tag);
            conteopapel++;
        }
    }
}
