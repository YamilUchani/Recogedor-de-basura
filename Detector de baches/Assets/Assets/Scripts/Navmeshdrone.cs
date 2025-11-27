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
    string[] tagsObjetivo = { "bache", "Pothole", "Crocodile" };

    int contador = 0;

    foreach (string tag in tagsObjetivo)
    {
        GameObject[] objetos = GameObject.FindGameObjectsWithTag(tag);

        foreach (var obj in objetos)
        {
            obj.isStatic = true;
            obj.layer = 7;
            contador++;
        }
    }

    Debug.Log($"Preparados {contador} objetos con tags: bache, Pothole, Crocodile");
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
