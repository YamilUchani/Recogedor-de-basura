using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
 
public class Conteo : MonoBehaviour
{
    [SerializeField]
    public Recoger conteo;
    public TextMeshProUGUI vidrio;
    public TextMeshProUGUI metal;
    public TextMeshProUGUI papel;
    void Start() {
        vidrio.text ="Vidrio: "+conteo.conteovidrio.ToString(); 
        metal.text = "Metal: "+conteo.conteometal.ToString(); 
        papel.text = "Papel: "+conteo.conteopapel.ToString(); 
    }
    void Update() {
        vidrio.text ="Vidrio: "+conteo.conteovidrio.ToString(); 
        metal.text = "Metal: "+conteo.conteometal.ToString(); 
        papel.text = "Papel: "+conteo.conteopapel.ToString(); 
    }
}
