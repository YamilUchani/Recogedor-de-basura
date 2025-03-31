using UnityEngine;
using UnityEngine.UI;

public class CanvasElementPositionToggle : MonoBehaviour
{
    public GameObject[] defaultElements;
    public GameObject[] alternateElements;
    private bool isAlternate = false;

    void Start()
    {
        SetElementsActive(defaultElements, true);
        SetElementsActive(alternateElements, false);
    }

    public void ToggleElements()
    {
        isAlternate = !isAlternate;
        SetElementsActive(defaultElements, !isAlternate);
        SetElementsActive(alternateElements, isAlternate);
    }

    private void SetElementsActive(GameObject[] elements, bool state)
    {
        if (elements != null)
        {
            foreach (GameObject element in elements)
            {
                if (element != null)
                {
                    element.SetActive(state);
                }
            }
        }
    }
}
