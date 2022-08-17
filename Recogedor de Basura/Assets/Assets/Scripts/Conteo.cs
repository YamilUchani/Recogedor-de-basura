using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
 
public class Conteo : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI vidrio;
    public TextMeshProUGUI metal;
    public TextMeshProUGUI papel;
    void Start() {
        vidrio.text ="Vidrio: "+Conteoglobal.conteovidrio.ToString(); 
        metal.text = "Metal: "+Conteoglobal.conteometal.ToString(); 
        papel.text = "Papel: "+Conteoglobal.conteopapel.ToString(); 
    }
    void Update() {
        vidrio.text ="Vidrio: "+Conteoglobal.conteovidrio.ToString(); 
        metal.text = "Metal: "+Conteoglobal.conteometal.ToString(); 
        papel.text = "Papel: "+Conteoglobal.conteopapel.ToString(); 
    }
}
