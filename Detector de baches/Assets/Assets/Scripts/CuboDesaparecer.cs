using UnityEngine;

public class CuboDesaparecer : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("calle"))
        {
            Destroy(gameObject); // Destruir el cubo al colisionar con un objeto con el tag "calle"
        }
    }
}
