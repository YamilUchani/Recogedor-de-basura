using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Meshbaches : MonoBehaviour {
  public List<GameObject> objetosGenerados = new List<GameObject>();
// La imagen de referencia
public Texture2D imagen;
public List<Texture2D> generatedImages;
// El material para la malla
public Material material;

// El tamaño de cada píxel en unidades de mundo
public float tamañoPixel = 0.1f;

// El umbral para determinar si un píxel es blanco o negro
public float threshold = 0.5f;
// El porcentaje de azul para los píxeles blancos
[Range(0, 100)]
public int bluePercentage = 50;

// Las imágenes de reserva
public List<Texture2D> reservaImages;

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

// Los vértices de la malla
private List<Vector3> verticesbach;

// Las coordenadas UV de la malla
private List<Vector2> uvsbach;

// Los triángulos de la malla
private List<int> triangles;

private List<int> trianglesbach;

// La nueva imagen modificada
private Texture2D newimagen;

private GameObject meshObject;

public float heightScale = 1.0f;
public Material materialbache;
public int subdivisiones = 2;
public float size = 1.0f;
public float scale = 1;
public int randomvalue;
public float borderMarginfinal;
public float borderMargininitial;
public int RangeEdgeInitial;
float borderMargin;
public GameObject bachesTotales;
private List<GameObject> _clusters = new List<GameObject>();
Vector3 objectPosition;

void Start()
{
    bachesTotales = new GameObject("Baches totales");
    
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
    verticesbach = new List<Vector3>();
    uvsbach = new List<Vector2>();
    trianglesbach = new List<int>();

    // Generar la nueva imagen a partir de la original
    GenerateNewImage();

    // Guardar la nueva imagen en Assets/Textures
    SaveNewImage();

    // Generar la malla a partir de la nueva imagen
    GenerateMeshFromimagen();
    // Generar los baches a partir de la nueva imagen
    CreateBaches();

    GenerateMeshFromImageList(generatedImages);
    meshFilter.mesh = mesh;
    meshCollider.sharedMesh = meshFilter.sharedMesh;
    bachesTotales.transform.position=new Vector3(transform.position.x-0.125f, transform.position.y, transform.position.z-0.125f);
    gameObject.AddComponent<MeshCollisionGenerator>();

}

void GenerateNewImage()
{
    // Obtener el ancho y el alto de la imagen original en píxeles
    int width = imagen.width;
    int height = imagen.height;

    // Crear la nueva imagen con el mismo tamaño que la original
    newimagen = new Texture2D(width, height);

    // Agregar las formas de reserva
    for (int i = 0; i < bluePercentage; i++)
    {
        int randomValue = Random.Range(0, 100);
        if(randomValue<bluePercentage)
        {
            int reserveIndex = Random.Range(0, reservaImages.Count);
            Texture2D reserveImage = reservaImages[reserveIndex];

            int reserveX = Random.Range(0, width - reserveImage.width);
            int reserveY = Random.Range(0, height - reserveImage.height);

            for (int rx = 0; rx < reserveImage.width; rx++)
            {
                for (int ry = 0; ry < reserveImage.height; ry++)
                {
                    Color reservePixelColor = reserveImage.GetPixel(rx, ry);
                    if (reservePixelColor == Color.white)
                    {
                        newimagen.SetPixel(reserveX + rx, reserveY + ry, Color.blue);
                    }
                }
            }
        }
    }

    // Aplicar los cambios a la nueva imagen
    newimagen.Apply();
}

// El resto del código permanece sin cambios
void SaveNewImage() 
{
    // Obtener los bytes de la nueva imagen en formato PNG
    byte[] bytes = newimagen.EncodeToPNG();

    // Crear el directorio Assets/Textures si no existe
    Directory.CreateDirectory(Application.dataPath + "/Textures");

    // Escribir los bytes en un archivo con el nombre Newimagen.png en el directorio Assets/Textures
    File.WriteAllBytes(Application.dataPath + "/Textures/Newimagenbaches.png", bytes);
    
    Debug.Log("Nueva imagenn guardada en Assets/Textures/Newimagenbaches.png");
}    

void GenerateMeshFromimagen() {
        // Obtener el ancho y el alto de la nueva imagen en píxeles
        int width = newimagen.width;
        int height = newimagen.height;
        

        // Recorrer cada píxel de la nueva imagen
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // Obtener el valor del píxel en escala de grises
                float pixelValue = newimagen.GetPixel(x, y).grayscale;
                float xreal=x-0.5f;
                float yreal=y-0.5f;
                // Si el valor es mayor que el umbral, crear un cuadrado en la malla
                if (pixelValue > threshold) {
                    CreateSquare(xreal, yreal);
                }
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

void CreateBaches()
{
 Color[] pixels = newimagen.GetPixels();
        int imageWidth = newimagen.width;
        int imageHeight = newimagen.height;
        
        bool[] processedPixels = new bool[pixels.Length];
        List<Texture2D> groupedImages = new List<Texture2D>();

        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                int pixelIndex = y * imageWidth + x;

                if (processedPixels[pixelIndex] || pixels[pixelIndex] != Color.blue)
                {
                    continue;
                }

                Texture2D blueGroup = CreateBlueGroup(pixels, imageWidth, imageHeight, x, y, processedPixels);
                groupedImages.Add(blueGroup);
            }
        }

        Debug.Log("Número total de agrupaciones de píxeles azules encontradas: " + groupedImages.Count);

        // Almacenar las imágenes generadas en una lista de tipo Texture2D
        generatedImages = new List<Texture2D>();

        // Guardar las imágenes generadas
        string folderPath = Application.dataPath;
        for (int i = 0; i < groupedImages.Count; i++)
        {
            Texture2D generatedImage = groupedImages[i];
            generatedImages.Add(generatedImage);

            byte[] bytes = generatedImage.EncodeToPNG();
            string filePath = Path.Combine(folderPath, "newimagen_group_" + i + ".png");
            File.WriteAllBytes(filePath, bytes);
        }

        // Utilizar la lista de imágenes generadas (generatedImages) según sea necesario
    }

    private Texture2D CreateBlueGroup(Color[] pixels, int imageWidth, int imageHeight, int startX, int startY, bool[] processedPixels)
    {
        Texture2D blueGroup = new Texture2D(imageWidth, imageHeight);
        Color[] bluePixels = new Color[imageWidth * imageHeight];

        Queue<int> pixelQueue = new Queue<int>();
        pixelQueue.Enqueue(startY * imageWidth + startX);

        while (pixelQueue.Count > 0)
        {
            int pixelIndex = pixelQueue.Dequeue();
            int x = pixelIndex % imageWidth;
            int y = pixelIndex / imageWidth;

            if (processedPixels[pixelIndex] || pixels[pixelIndex] != Color.blue)
            {
                continue;
            }

            processedPixels[pixelIndex] = true;
            bluePixels[pixelIndex] = pixels[pixelIndex];

            if (x > 0)
            {
                pixelQueue.Enqueue(pixelIndex - 1); // Izquierda
            }
            if (x < imageWidth - 1)
            {
                pixelQueue.Enqueue(pixelIndex + 1); // Derecha
            }
            if (y > 0)
            {
                pixelQueue.Enqueue(pixelIndex - imageWidth); // Arriba
            }
            if (y < imageHeight - 1)
            {
                pixelQueue.Enqueue(pixelIndex + imageWidth); // Abajo
            }
        }

        blueGroup.SetPixels(bluePixels);
        blueGroup.Apply();

        return blueGroup;
    }
    void GenerateMeshFromImageList(List<Texture2D> imageList) {
        
        foreach (Texture2D image in imageList) {
            GameObject bache = new GameObject("Bache_" + (bachesTotales.transform.childCount + 1));
            bache.tag = "bache";
            bache.transform.parent = bachesTotales.transform;
            MeshFilter mfbach = bache.AddComponent<MeshFilter>();
            MeshRenderer mrbach = bache.AddComponent<MeshRenderer>();
            MeshCollider mcrbach = bache.AddComponent<MeshCollider>();
            Mesh meshcolbache = new Mesh();
            GenerateMeshFromImagebach(meshcolbache,image, mfbach, mrbach, mcrbach);
            bache.AddComponent<MeshCollisionGenerator>();
        }
    }

    public void GenerateMeshFromImagebach(Mesh bachemesh, Texture2D imagenbach, MeshFilter meshFilterbach, MeshRenderer meshRendererbach, MeshCollider meshColliderbach) {
        // Obtener el ancho y el alto de la imagen en píxeles
        int width = imagenbach.width;
        int height = imagenbach.height;

        // Arreglos para almacenar los vértices, los triángulos y las coordenadas UV
        List<Vector3> verticesbach = new List<Vector3>();
        List<int> trianglesbach = new List<int>();
        List<Vector2> uvsbach = new List<Vector2>();

        // Recorrer cada píxel de la imagen
for (int x = 0; x < width; x++) {
    for (int y = 0; y < height; y++) {
        // Obtener el valor del píxel en escala de grises
        float pixelValue = imagenbach.GetPixel(x, y).grayscale;
        float xreal = x - 0.5f;
        float yreal = y - 0.5f;

        // Si el valor es mayor que el umbral, crear un cuadrado en la malla
        if (pixelValue > 0.1f) {
            float centerX = (imagenbach.width - 1) * tamañoPixel / 2f;
            float centerZ = (imagenbach.height - 1) * tamañoPixel / 2f;
            float vx = (x * tamañoPixel) - centerX + transform.position.x;
            float vz = (y * tamañoPixel) - centerZ + transform.position.z;

            // Calcular el tamaño de cada cuadrado subdividido
            float subdividedSize = tamañoPixel / subdivisiones;

            for (int i = 0; i < subdivisiones; i++) {
                for (int j = 0; j < subdivisiones; j++) {
                    // Calcular la posición del vértice en la subdivisión actual
                    float subdividedVx = vx + (i * subdividedSize);
                    float subdividedVz = vz + (j * subdividedSize);

                    // Añadir los cuatro vértices del cuadrado subdividido a la lista de vértices
                    int vertexIndex = verticesbach.Count;
                    verticesbach.Add(new Vector3(subdividedVx, 0, subdividedVz)); // inferior izquierdo
                    verticesbach.Add(new Vector3(subdividedVx + subdividedSize, 0, subdividedVz)); // inferior derecho
                    verticesbach.Add(new Vector3(subdividedVx + subdividedSize, 0, subdividedVz + subdividedSize)); // superior derecho
                    verticesbach.Add(new Vector3(subdividedVx, 0, subdividedVz + subdividedSize)); // superior izquierdo

                    // Añadir las cuatro coordenadas UV del cuadrado a la lista de coordenadas UV
                    uvsbach.Add(new Vector2(1, 1));
                    uvsbach.Add(new Vector2(0, 1));
                    uvsbach.Add(new Vector2(0, 0));
                    uvsbach.Add(new Vector2(1, 0));

                    // Añadir los seis índices de los vértices que forman los dos triángulos del cuadrado a la lista de triángulos
                    trianglesbach.Add(vertexIndex); // Primer triángulo: inferior izquierdo
                    trianglesbach.Add(vertexIndex + 3); // Primer triángulo: inferior derecho
                    trianglesbach.Add(vertexIndex + 2); // Primer triángulo: superior derecho
                    trianglesbach.Add(vertexIndex); // Segundo triángulo: inferior izquierdo
                    trianglesbach.Add(vertexIndex + 2); // Segundo triángulo: superior derecho
                    trianglesbach.Add(vertexIndex + 1); // Segundo triángulo: superior izquierdo
                }
            }
        }
    }
}


        // Calcular el centro de la malla
        bachemesh.RecalculateBounds();
        Vector3 center = objectPosition;

        // Centrar la malla restando el centro a los vértices
        for (int i = 0; i < verticesbach.Count; i++) {
            verticesbach[i] -= center;
        }
        center += objectPosition;
        
        // Asignar los vértices, las coordenadas UV y los triángulos a la malla
        bachemesh.vertices = verticesbach.ToArray();
        bachemesh.uv = uvsbach.ToArray();
        bachemesh.triangles = trianglesbach.ToArray();

        // Optimizar y recalcular las normales de la malla
        bachemesh.Optimize();
        bachemesh.RecalculateNormals();
        bachemesh.RecalculateBounds();
        bachemesh = GenerateTerrainFromMesh(bachemesh);
        meshRendererbach.material = materialbache;
        meshFilterbach.mesh = bachemesh;
        meshColliderbach.sharedMesh = bachemesh;
         
        
        return;
    }
Mesh GenerateTerrainFromMesh(Mesh bachmesh)
{
    Vector3[] vertices = bachmesh.vertices;
    int vertexCount = vertices.Length;

    Dictionary<Vector3, int> vertexOccurrences = new Dictionary<Vector3, int>();
    List<Vector3> repeatedVertexPositions = new List<Vector3>();
    // Contar las ocurrencias de cada posición de vértice
    for (int i = 0; i < vertexCount; i++)
    {
        Vector3 vertexPosition = vertices[i];

        if (vertexOccurrences.ContainsKey(vertexPosition))
        {
            vertexOccurrences[vertexPosition]++;
        }
        else
        {
            vertexOccurrences[vertexPosition] = 1;
        }
    }

    // Obtener las posiciones de los vértices que se repiten en menos de 4 triángulos
    foreach (var kvp in vertexOccurrences)
    {
        if (kvp.Value < 4)
        {
            repeatedVertexPositions.Add(kvp.Key);
        }
    }

    Debug.Log("Número de vértices que se repiten en menos de 4 triángulos: " + repeatedVertexPositions.Count);

    // Definir el margen adicional para el borde


    // Generación de la malla de ruido de Perlin
    for (int i = 0; i < vertices.Length; i++)
    {
        Vector3 vertex = vertices[i];
        borderMargin = Random.Range(borderMargininitial, borderMarginfinal);
        // Verificar si la posición del vértice está cerca del borde
        if (!IsCloseToBorder(vertex, repeatedVertexPositions, borderMargin))
        {
            float noise = Mathf.PerlinNoise(vertex.x / scale, vertex.z / scale);
            vertex.y = -noise * heightScale;
        }

        vertices[i] = vertex;
    }

    bachmesh.vertices = vertices;

    bachmesh.RecalculateNormals();
    bachmesh.RecalculateBounds();
    return bachmesh;
}

// Función para verificar si la posición del vértice está cerca del borde
private bool IsCloseToBorder(Vector3 vertex, List<Vector3> borderVertices, float margin)
{
    foreach (Vector3 borderVertex in borderVertices)
    {
        if (Mathf.Abs(vertex.x - borderVertex.x) < margin &&
            Mathf.Abs(vertex.y - borderVertex.y) < margin &&
            Mathf.Abs(vertex.z - borderVertex.z) < margin)
        {
            return true;
        }
    }
    return false;
}


}
