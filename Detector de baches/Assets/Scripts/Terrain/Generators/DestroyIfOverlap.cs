using UnityEngine;

public class DestroyIfOverlap : MonoBehaviour
{
    public string[] targetTags = { "Car", "Person" };

    private bool destroyed = false;

    private void OnTriggerEnter(Collider other)
    {
        TryDestroy(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Esto cubre el caso de “atravesados”
        TryDestroy(other);
    }

    private void TryDestroy(Collider other)
    {
        if (!enabled || destroyed) return;

        foreach (string tagName in targetTags)
        {
            if (other.CompareTag(tagName))
            {
                destroyed = true;

                Debug.Log($"[DestroyIfTouch] {gameObject.name} destruido por {other.name}");

                Destroy(gameObject);
                return;
            }
        }
    }
}