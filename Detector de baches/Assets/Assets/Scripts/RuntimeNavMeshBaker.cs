using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class RuntimeNavMeshBaker : MonoBehaviour
{
    public KeyCode bakeKey = KeyCode.B;
    public float bakeSize = 100f;

    private NavMeshData navMeshData;
    private NavMeshDataInstance navMeshDataInstance;

    void Start()
    {
        navMeshData = new NavMeshData();
        navMeshDataInstance = NavMesh.AddNavMeshData(navMeshData);
    }

    void Update()
    {
        if (Input.GetKeyDown(bakeKey))
        {
            BakeNavMesh();
        }
    }

    public void BakeNavMesh()
    {
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        MeshFilter[] meshes = FindObjectsOfType<MeshFilter>();

        foreach (MeshFilter mf in meshes)
        {
            // ⚠ Ignorar si el objeto está en el layer "Vehicle"
            if (mf.gameObject.layer == LayerMask.NameToLayer("Default"))
                continue;

            if (!mf.gameObject.activeInHierarchy) continue;
            if (mf.sharedMesh == null) continue;

            NavMeshBuildSource src = new NavMeshBuildSource();
            src.shape = NavMeshBuildSourceShape.Mesh;
            src.sourceObject = mf.sharedMesh;
            src.transform = mf.transform.localToWorldMatrix;
            src.area = 0;
            sources.Add(src);
        }

        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * bakeSize);

        NavMeshBuilder.UpdateNavMeshData(navMeshData,
                                         NavMesh.GetSettingsByID(0),
                                         sources,
                                         bounds);

        navMeshDataInstance = NavMesh.AddNavMeshData(navMeshData);
        Debug.Log("NavMesh horneado ignorando todos los objetos en layer 'Vehicle'.");
    }
}
