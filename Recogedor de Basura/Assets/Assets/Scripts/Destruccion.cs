using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destruccion : MonoBehaviour
{

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Barredora")
        {
            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "Recogedor" && Conteoglobal.elementos < Conteoglobal.basura_total)
        {
            Destroy(gameObject);
            Conteoglobal.elementos--;
        }
    }
}
