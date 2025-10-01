using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshdrone : MonoBehaviour
{
    void OnEnable()
    {
        PrepararObjetosBache();
        BakeAllNavMeshes();
    }

    // Marcar objetos con tag "bache" como estáticos y layer 7
    void PrepararObjetosBache()
    {
        GameObject[] baches = GameObject.FindGameObjectsWithTag("bache");

        foreach (var bache in baches)
        {
            bache.isStatic = true;
            bache.layer = 7;
        }

        Debug.Log($"Preparados {baches.Length} objetos con tag 'bache'");
    }

    public void BakeAllNavMeshes()
    {
        // Buscar todos los NavMeshSurface en la escena
        NavMeshSurface[] surfaces = FindObjectsOfType<NavMeshSurface>();

        foreach (var surface in surfaces)
        {
            surface.BuildNavMesh();
        }

        Debug.Log($"Baked {surfaces.Length} NavMesh surfaces");
    }
}
