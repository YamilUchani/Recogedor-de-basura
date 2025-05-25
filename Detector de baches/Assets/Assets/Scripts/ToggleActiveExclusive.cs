using UnityEngine;
using System.Collections.Generic;

public class ToggleActiveExclusive : MonoBehaviour
{
    public List<GameObject> groupA;
    public List<GameObject> groupB;

    private void OnEnable()
    {
        // Activar grupo A y desactivar grupo B al inicio
        ActivateGroupA();
    }

    public void ActivateGroupA()
    {
        foreach (GameObject obj in groupA)
            obj.SetActive(true);

        foreach (GameObject obj in groupB)
            obj.SetActive(false);
    }

    public void ActivateGroupB()
    {
        foreach (GameObject obj in groupB)
            obj.SetActive(true);

        foreach (GameObject obj in groupA)
            obj.SetActive(false);
    }
}
