using UnityEngine;

public class CuboDesaparecer : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("bache"))
        {
            Destroy(gameObject); // Destruir el cubo al colisionar con un objeto con el tag "calle"
        }
        else if (collision.gameObject.CompareTag("calle"))
        { 
            Invoke("StaticCreate", 0.01f);
            
        }
    }
    private void StaticCreate()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        

        // Eliminar Box Collider
        Destroy(boxCollider);

        // Eliminar Rigidbody
        Destroy(rigidbody);

        // Reposicionar para mantener la posición Y en 0
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        // Reactivar Mesh Collider (si existe)
        if (meshCollider != null)
        {
            meshCollider.enabled = true;
        }
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();

        // Activar los MeshColliders
        foreach (MeshCollider collider in meshColliders)
        {
            collider.enabled = true;
        }
    }
    
}
