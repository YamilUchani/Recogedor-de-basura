using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EnergyController : MonoBehaviour
{
    public float energia = 100f;
    public float duracionMinutos = 30f;
    private float energiaMaxima = 100f;

    public List<TMP_Text> energyTexts = new List<TMP_Text>();

    public bool energiaActivo;

    public DroneNavMeshController droneController;

    private bool retornoIniciado = false;
    private bool esperandoApagado = false;

    private void Update()
    {
        if (energiaActivo)
        {
            // Consumo de energía
            if (energia > 0f && duracionMinutos > 0f)
            {
                float energiaPorSegundo = energiaMaxima / (duracionMinutos * 60f);
                energia -= energiaPorSegundo * Time.deltaTime;
                energia = Mathf.Clamp(energia, 0f, energiaMaxima);
            }

            // Mostrar energía en los textos
            string energyDisplay = "Energy  : " + energia.ToString("F1") + " %";
            foreach (var text in energyTexts)
            {
                if (text != null)
                    text.text = energyDisplay;
            }

            // Si energía baja, iniciar retorno a base
            if (energia <= 10f && !retornoIniciado && droneController != null)
            {
                retornoIniciado = true;

                if (droneController.IsManualControl())
                    droneController.ToggleControlMode(); // cambia a modo automático

                droneController.ReturnToBase();
            }

            // Cuando llega a base, iniciar apagado
            if (retornoIniciado && droneController.IsReturningToBase() && droneController.IsAtBase() && !esperandoApagado)
            {
                droneController.ApagarDrone();
                esperandoApagado = true;
                Debug.Log("Drone llegó a base, iniciando apagado...");
            }

            // Cuando se apaga completamente, recargar energía y reactivar
            if (esperandoApagado && droneController.IsFullyShutdown())
            {
                energia = energiaMaxima;
                if (!droneController.apagado)
                {
                    energiaActivo = true;
                    retornoIniciado = false;
                    esperandoApagado = false;
                    Debug.Log("Energía recargada en base y dron reactivado");
                    droneController.ToggleControlMode(); // Reactiva modo manual
                }


                
            }
        }
    }
}
