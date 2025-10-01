using UnityEngine;
using System.Collections.Generic;

public class ToggleActiveExclusive : MonoBehaviour
{
    public List<GameObject> groupA;
    public List<GameObject> groupB;

    public bool energia; // false = grupo A activo, true = grupo B activo

    // Referencia al controlador de energía
    public EnergyController energyController;

    private void OnEnable()
    {
        ActivateGroupA();
    }

    public void ActivateGroupA()
    {
        foreach (GameObject obj in groupA)
            obj.SetActive(true);

        foreach (GameObject obj in groupB)
            obj.SetActive(false);

        energia = false;

        // Notificar a EnergyController
        if (energyController != null)
            energyController.energiaActivo = energia;
    }

    public void ActivateGroupB()
    {
        foreach (GameObject obj in groupB)
            obj.SetActive(true);

        foreach (GameObject obj in groupA)
            obj.SetActive(false);

        energia = true;

        // Notificar a EnergyController
        if (energyController != null)
            energyController.energiaActivo = energia;
    }
}
