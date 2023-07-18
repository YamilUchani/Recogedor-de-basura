using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Meshbaches : MonoBehaviour {
  public List<GameObject> objetosGenerados = new List<GameObject>();
// La imagen de referencia
public Texture2D imagen;
// El material para la malla
public Material material;
public Material watermaterial;

// El tamaño de cada píxel en unidades de mundo
public float tamañoPixel = 0.1f;
// El componente MeshFilter
private MeshFilter meshFilter;

// El componente MeshRenderer
private MeshRenderer meshRenderer;

// El objeto Mesh
private Mesh mesh;

// Los vértices de la malla
private List<Vector3> vertices;

// Las coordenadas UV de la malla
private List<Vector2> uvs;


// Los triángulos de la malla
private List<int> triangles;

private Vector3 objectPosition;

public List<GameObject> baches;
public GameObject objetoPadre;

public int cantidadBaches = 30;
void Start()
{
    
    objetosGenerados.Add(this.gameObject);
    // Obtener los componentes
    meshFilter = GetComponent<MeshFilter>();
    meshFilter.mesh = mesh;
    meshRenderer = GetComponent<MeshRenderer>();
    objectPosition = transform.position;
    // Asignar el material
    meshRenderer.material = material;
    MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
    // Crear el objeto Mesh
    mesh = new Mesh();

    // Inicializar las listas
    vertices = new List<Vector3>();
    uvs = new List<Vector2>();
    triangles = new List<int>();
    GenerateMeshFromimagen();
    meshFilter.mesh = mesh;
    meshCollider.sharedMesh = meshFilter.sharedMesh;
    GenerarBaches();
    gameObject.AddComponent<MeshCollisionGenerator>();
    Invoke("ClearDebug", 0.3f);

}
void ClearDebug()
{
    Debug.Log("Yamil");
    var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
 
    var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
 
    clearMethod.Invoke(null, null);
}


void GenerateMeshFromimagen() {
        // Obtener el ancho y el alto de la nueva imagen en píxeles
        int width = imagen.width;
        int height = imagen.height;
        

        // Recorrer cada píxel de la nueva imagen
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // Obtener el valor del píxel en escala de grises
                float pixelValue = imagen.GetPixel(x, y).grayscale;
                float xreal=x-0.5f;
                float yreal=y-0.5f;
                CreateSquare(xreal, yreal);
            }
        }

        // Calcular el centro de la malla
        mesh.RecalculateBounds();
        Vector3 center =objectPosition;
        
        // Centrar la malla restando el centro a los vértices
        for (int i = 0; i < vertices.Count; i++) {
            vertices[i] -= center ;
        }
        center += objectPosition;
        // Asignar los vértices, las coordenadas UV y los triángulos a la malla
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        
        // Optimizar y recalcular las normales de la malla
        mesh.Optimize();
        mesh.RecalculateNormals();
        
    }



    void CreateSquare(float x, float y) {

    // Calcular las coordenadas del centro del cuadrado
    float centerX = (imagen.width - 1) * tamañoPixel / 2f;
    float centerZ = (imagen.height - 1) * tamañoPixel / 2f;
    float vx = (x * tamañoPixel) - centerX + transform.position.x;
    float vz = (y * tamañoPixel) - centerZ + transform.position.z;

    // Añadir los cuatro vértices del cuadrado a la lista de vértices
    vertices.Add(new Vector3(vx, 0, vz)); // inferior izquierdo
    vertices.Add(new Vector3(vx + tamañoPixel, 0, vz)); // inferior derecho
    vertices.Add(new Vector3(vx + tamañoPixel, 0, vz + tamañoPixel)); // superior derecho
    vertices.Add(new Vector3(vx, 0, vz + tamañoPixel)); // superior izquierdo

    // Añadir las cuatro coordenadas UV del cuadrado a la lista de coordenadas UV
    // Estas pueden ser las que quieras, dependiendo de cómo quieras mapear la textura
    uvs.Add(new Vector2(1, 1));
    uvs.Add(new Vector2(0, 1));
    uvs.Add(new Vector2(0, 0));
    uvs.Add(new Vector2(1, 0));

    // Añadir los seis índices de los vértices que forman los dos triángulos del cuadrado a la lista de triángulos
    // Estos deben estar en el sentido contrario a las agujas del reloj
    int vertexIndex = vertices.Count - 4; // El índice del primer vértice que hemos añadido
    triangles.Add(vertexIndex); // Primer triángulo: inferior izquierdo
    triangles.Add(vertexIndex + 3); // Primer triángulo: inferior derecho
    triangles.Add(vertexIndex + 2); // Primer triángulo: superior derecho
    triangles.Add(vertexIndex); // Segundo triángulo: inferior izquierdo
    triangles.Add(vertexIndex + 2); // Segundo triángulo: superior derecho
    triangles.Add(vertexIndex + 1); // Segundo triángulo: superior izquierdo
}
void GenerarBaches()
{
    if (baches.Count == 0)
    {
        Debug.Log("La lista de baches está vacía.");
        return;
    }

    for (int i = 0; i < cantidadBaches; i++)
    {
        // Obtener el bache de la lista utilizando el operador módulo para repetir los elementos
        GameObject bachePrefab = baches[i % baches.Count];

        // Obtener el tamaño del plano
        Bounds planoBounds = meshRenderer.bounds;

        float distanciaMinimaBorde = 0.5f; // Distancia mínima deseada desde el borde del plano

        float x = Mathf.Round((Random.Range(planoBounds.min.x + distanciaMinimaBorde, planoBounds.max.x - distanciaMinimaBorde) - planoBounds.min.x) / 0.25f) * 0.25f;
        float z = Mathf.Round((Random.Range(planoBounds.min.z + distanciaMinimaBorde, planoBounds.max.z - distanciaMinimaBorde) - planoBounds.min.z) / 0.25f) * 0.25f;

        Vector3 posicionAleatoria = new Vector3(
            planoBounds.min.x + x,
            meshRenderer.transform.position.y, // Mantener la posición Y del plano
            planoBounds.min.z + z
        );


        // Instanciar el bache en la posición generada
        GameObject bache = Instantiate(bachePrefab, posicionAleatoria, Quaternion.identity);
        GenerarPlanoComoHijo(bache);
        BoxCollider boxCollider = bache.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, 0, 0);
        boxCollider.size = new Vector3(0.02f, 0.003f, 0.02f);
        Rigidbody rigidbody = bache.AddComponent<Rigidbody>();
        rigidbody.useGravity = true;
        rigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation; // Congelar la posición en los ejes X y Z y la rotación
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // Establecer el objeto padre como padre del bache
        bache.transform.SetParent(objetoPadre.transform);
        
    }
}
void GenerarPlanoComoHijo(GameObject bache)
{
    // Crear el plano como hijo del bache
    GameObject plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
    Renderer planoRenderer = plano.GetComponent<Renderer>();
    planoRenderer.material = watermaterial;
    plano.transform.SetParent(bache.transform);
    plano.transform.localPosition = Vector3.zero;
    plano.transform.localRotation = Quaternion.identity;

    // Establecer la escala del plano
    plano.transform.localScale = new Vector3(0.002f, 1f, 0.002f);

    // Generar una posición aleatoria en el eje Y dentro del rango especificado
    float randomPosY = Random.Range(-0.002f, -0.06f);

    // Establecer la posición del plano teniendo en cuenta la posición del bache y la posición aleatoria en Y
    plano.transform.position = new Vector3(bache.transform.position.x, bache.transform.position.y + randomPosY, bache.transform.position.z);

    // Asignar el material al plano
    
}

}
