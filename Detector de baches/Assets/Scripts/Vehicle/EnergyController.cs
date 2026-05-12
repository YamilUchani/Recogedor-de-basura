using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EnergyController : MonoBehaviour
{
    public float energia = 100f;
    public float duracionMinutos = 30f;
    private float energiaMaxima = 100f;

    public TMP_Text energyText;

    public bool energiaActivo;

    public DroneNavMeshController droneController;

    private bool retornoIniciado = false;
    private bool esperandoApagado = false;

    [Header("Skip Mode — Recarga")]
    [Tooltip("Velocidad de recarga en base para el modo Skip (% por segundo)")]
    public float tasaRecarga = 20f;
    private bool recargaActiva = false;

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

            // Recarga activa (modo Skip en base)
            if (recargaActiva)
            {
                energia += tasaRecarga * Time.deltaTime;
                energia = Mathf.Clamp(energia, 0f, energiaMaxima);
                if (energia >= energiaMaxima) recargaActiva = false;  // recarga completa
            }

            // Mostrar energía en los textos
            string energyDisplay = "Energy  : " + energia.ToString("F1") + " %";
            if (energyText != null)
            {
                energyText.text = energyDisplay;
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

    /// <summary>Inicia la recarga activa de energía (usado por el modo Skip al llegar a base).</summary>
    public void IniciarRecarga()
    {
        recargaActiva = true;
        Debug.Log($"[EnergyController] Recarga activa iniciada ({tasaRecarga}%/s).");
    }

    /// <summary>Devuelve true si la energía supera el umbral indicado (default 95%).</summary>
    public bool EstaCompleta(float umbral = 95f) => energia >= umbral;
}
