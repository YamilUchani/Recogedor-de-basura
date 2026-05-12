# 🚀 Guía Paso a Paso: Implementar RVO2 en Unity

## 📋 Tabla de Contenidos
1. [Configuración Inicial](#configuración-inicial)
2. [Crear el Manager](#paso-1-crear-rvo-manager)
3. [Crear Agente de River](#paso-2-crear-rvo-agent-controller)
4. [Configurar Obstáculos](#paso-3-configurar-obstáculos)
5. [Crear Escena de Prueba](#paso-4-crear-escena-de-prueba)
6. [Integración Final](#paso-5-integración-final)

---

## Configuración Inicial

### ✅ Lo que ya tienes:
- Librería RVO2 completa en `Assets/Scripts/RVO2/`
- Namespace: `RVO`
- Clase principal: `Simulator` (Singleton)

### 📦 Estructura de carpetas que crearemos:
```
Assets/
├── Scripts/
│   ├── RVO2/                    (existente)
│   └── RVO_NEW/                 (nueva)
│       ├── Manager/
│       │   └── RVOSimulationManager.cs
│       ├── Controllers/
│       │   └── RVOAgentController.cs
│       ├── Obstacles/
│       │   └── RVOObstacle.cs
│       └── IMPLEMENTACION_PASO_A_PASO.md
```

---

## PASO 1: Crear RVO Simulation Manager

### Objetivo: 
Gestionar la simulación RVO en la escena y coordinar agentes/obstáculos

### Archivo: `RVO_NEW/Manager/RVOSimulationManager.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using RVO;

public class RVOSimulationManager : MonoBehaviour
{
    [Header("Simulación RVO")]
    [SerializeField] private float timeStep = 0.016f; // 60 FPS
    [SerializeField] private int numWorkers = 0; // 0 = auto-detectar
    
    [Header("Parámetros por defecto de Agentes")]
    [SerializeField] private float defaultNeighborDist = 15f;
    [SerializeField] private int defaultMaxNeighbors = 10;
    [SerializeField] private float defaultTimeHorizon = 5f;
    [SerializeField] private float defaultTimeHorizonObst = 2f;
    [SerializeField] private float defaultRadius = 0.5f;
    [SerializeField] private float defaultMaxSpeed = 5f;
    
    private static RVOSimulationManager instance;
    private List<RVOAgentController> agents = new List<RVOAgentController>();
    private List<RVOObstacle> obstacles = new List<RVOObstacle>();
    private float accumulatedTime = 0f;
    
    public static RVOSimulationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RVOSimulationManager>();
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("RVOSimulationManager");
                    instance = managerObj.AddComponent<RVOSimulationManager>();
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeSimulation();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSimulation()
    {
        // Limpiar simulación existente
        Simulator.Instance.Clear();
        
        // Configurar parámetros
        Simulator.Instance.setTimeStep(timeStep);
        Simulator.Instance.SetNumWorkers(numWorkers);
        
        // Configurar agentes por defecto
        Simulator.Instance.setAgentDefaults(
            defaultNeighborDist,
            defaultMaxNeighbors,
            defaultTimeHorizon,
            defaultTimeHorizonObst,
            defaultRadius,
            defaultMaxSpeed,
            new Vector2(0f, 0f) // velocidad inicial
        );
        
        Debug.Log("[RVO] Simulación inicializada correctamente");
    }
    
    private void Update()
    {
        // Acumular tiempo para evitar múltiples pasos por frame
        accumulatedTime += Time.deltaTime;
        
        // Actualizar velocidades preferentes de todos los agentes
        foreach (RVOAgentController agent in agents)
        {
            agent.UpdatePreferredVelocity();
        }
        
        // Ejecutar pasos de simulación cuando sea necesario
        while (accumulatedTime >= timeStep)
        {
            Simulator.Instance.doStep();
            accumulatedTime -= timeStep;
        }
        
        // Sincronizar posiciones de Unity con RVO
        foreach (RVOAgentController agent in agents)
        {
            agent.SyncPositionFromRVO();
        }
    }
    
    public void RegisterAgent(RVOAgentController agent)
    {
        if (!agents.Contains(agent))
        {
            agents.Add(agent);
            Debug.Log($"[RVO] Agente '{agent.gameObject.name}' registrado");
        }
    }
    
    public void UnregisterAgent(RVOAgentController agent)
    {
        agents.Remove(agent);
        Debug.Log($"[RVO] Agente '{agent.gameObject.name}' desregistrado");
    }
    
    public void RegisterObstacle(RVOObstacle obstacle)
    {
        if (!obstacles.Contains(obstacle))
        {
            obstacles.Add(obstacle);
        }
    }
    
    public void UnregisterObstacle(RVOObstacle obstacle)
    {
        obstacles.Remove(obstacle);
    }
    
    public void ProcessAllObstacles()
    {
        Simulator.Instance.processObstacles();
        Debug.Log($"[RVO] {Simulator.Instance.getNumObstacleVertices()} vértices de obstáculos procesados");
    }
    
    public int GetAgentCount() => agents.Count;
    public List<RVOAgentController> GetAgents() => agents;
}
```

---

## PASO 2: Crear RVO Agent Controller

### Objetivo:
Conectar GameObject de agente con el motor RVO

### Archivo: `RVO_NEW/Controllers/RVOAgentController.cs`

```csharp
using UnityEngine;
using RVO;

public class RVOAgentController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float neighborDist = 15f;
    [SerializeField] private int maxNeighbors = 10;
    [SerializeField] private float timeHorizon = 5f;
    [SerializeField] private float timeHorizonObst = 2f;
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float maxSpeed = 5f;
    
    [Header("Comportamiento")]
    [SerializeField] private Transform target;
    [SerializeField] private bool useManualVelocity = false;
    [SerializeField] private Vector2 manualVelocity = Vector2.zero;
    
    private int rvoAgentId = -1;
    private Rigidbody rb;
    private Vector2 preferredVelocity = Vector2.zero;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                           RigidbodyConstraints.FreezeRotationY | 
                           RigidbodyConstraints.FreezeRotationZ;
            rb.useGravity = false;
        }
        
        // Registrar en el manager RVO
        RVOSimulationManager.Instance.RegisterAgent(this);
        
        // Registrar en simulador RVO
        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        rvoAgentId = Simulator.Instance.addAgent(
            pos,
            neighborDist,
            maxNeighbors,
            timeHorizon,
            timeHorizonObst,
            radius,
            maxSpeed,
            Vector2.zero
        );
        
        Debug.Log($"[RVO] Agente '{gameObject.name}' creado con ID: {rvoAgentId}");
    }
    
    public void UpdatePreferredVelocity()
    {
        if (rvoAgentId < 0) return;
        
        if (useManualVelocity)
        {
            // Usar velocidad manual
            preferredVelocity = manualVelocity;
        }
        else if (target != null)
        {
            // Calcular dirección hacia objetivo
            Vector3 direction = (target.position - transform.position).normalized;
            preferredVelocity = new Vector2(direction.x, direction.z) * maxSpeed;
        }
        else
        {
            // Sin objetivo
            preferredVelocity = Vector2.zero;
        }
        
        // Establecer velocidad preferida en RVO
        Simulator.Instance.setAgentPrefVelocity(rvoAgentId, preferredVelocity);
    }
    
    public void SyncPositionFromRVO()
    {
        if (rvoAgentId < 0) return;
        
        // Obtener nueva posición y velocidad de RVO
        Vector2 rvoPos = Simulator.Instance.getAgentPosition(rvoAgentId);
        Vector2 rvoVel = Simulator.Instance.getAgentVelocity(rvoAgentId);
        
        // Actualizar posición del GameObject
        transform.position = new Vector3(rvoPos.x, transform.position.y, rvoPos.y);
        
        // Aplicar velocidad al Rigidbody (si existe)
        if (rb != null)
        {
            rb.velocity = new Vector3(rvoVel.x, rb.velocity.y, rvoVel.y);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetManualVelocity(Vector2 velocity)
    {
        manualVelocity = velocity;
        useManualVelocity = true;
    }
    
    public void ClearManualVelocity()
    {
        useManualVelocity = false;
        manualVelocity = Vector2.zero;
    }
    
    public int GetRVOAgentId() => rvoAgentId;
    public Vector2 GetPreferredVelocity() => preferredVelocity;
    
    private void OnDestroy()
    {
        if (RVOSimulationManager.Instance != null)
        {
            RVOSimulationManager.Instance.UnregisterAgent(this);
        }
    }
}
```

---

## PASO 3: Configurar Obstáculos

### Objetivo:
Añadir obstáculos estáticos a la simulación RVO

### Archivo: `RVO_NEW/Obstacles/RVOObstacle.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using RVO;

public class RVOObstacle : MonoBehaviour
{
    [SerializeField] private bool isConvex = true;
    [SerializeField] private bool isClockwise = false; // true para obstáculos negativos
    
    private int rvoObstacleId = -1;
    private List<Vector2> vertices = new List<Vector2>();
    
    private void Start()
    {
        // Extraer vértices del collider
        if (!ExtractVertices())
        {
            Debug.LogError($"[RVO] No se pudieron extraer vértices de '{gameObject.name}'");
            return;
        }
        
        // Registrar en manager
        RVOSimulationManager.Instance.RegisterObstacle(this);
        
        // Registrar obstáculo en RVO después de completar la scene setup
        // Esto se hace manualmente después de añadir todos los obstáculos
        Debug.Log($"[RVO] Obstáculo '{gameObject.name}' preparado con {vertices.Count} vértices");
    }
    
    private bool ExtractVertices()
    {
        vertices.Clear();
        
        // Opción 1: Usar BoxCollider
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Vector3 size = boxCollider.size;
            Vector3 center = boxCollider.center;
            
            Vector3[] corners = new Vector3[4]
            {
                transform.TransformPoint(center + new Vector3(-size.x/2, 0, -size.z/2)),
                transform.TransformPoint(center + new Vector3(size.x/2, 0, -size.z/2)),
                transform.TransformPoint(center + new Vector3(size.x/2, 0, size.z/2)),
                transform.TransformPoint(center + new Vector3(-size.x/2, 0, size.z/2))
            };
            
            foreach (Vector3 corner in corners)
            {
                vertices.Add(new Vector2(corner.x, corner.z));
            }
            
            return true;
        }
        
        // Opción 2: Usar MeshCollider
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null && meshCollider.convex)
        {
            Mesh mesh = meshCollider.sharedMesh;
            if (mesh != null)
            {
                // Projectar vértices a plano XZ
                foreach (Vector3 vert in mesh.vertices)
                {
                    Vector3 worldVert = transform.TransformPoint(vert);
                    vertices.Add(new Vector2(worldVert.x, worldVert.z));
                }
                return true;
            }
        }
        
        // Opción 3: Usar PolygonCollider2D (en 3D, ignorar Y)
        PolygonCollider2D polyCollider = GetComponent<PolygonCollider2D>();
        if (polyCollider != null)
        {
            Vector2[] points = polyCollider.points;
            foreach (Vector2 point in points)
            {
                Vector3 worldPoint = transform.TransformPoint(new Vector3(point.x, 0, point.y));
                vertices.Add(new Vector2(worldPoint.x, worldPoint.z));
            }
            return true;
        }
        
        return false;
    }
    
    public void RegisterInRVO()
    {
        if (vertices.Count < 2)
        {
            Debug.LogError($"[RVO] Obstáculo '{gameObject.name}' requiere al menos 2 vértices");
            return;
        }
        
        // Invertir orden si es clockwise (para obstáculos negativos)
        if (isClockwise)
        {
            vertices.Reverse();
        }
        
        // Añadir a simulador RVO
        rvoObstacleId = Simulator.Instance.addObstacle(vertices);
        
        if (rvoObstacleId >= 0)
        {
            Debug.Log($"[RVO] Obstáculo '{gameObject.name}' registrado con ID: {rvoObstacleId}");
        }
        else
        {
            Debug.LogError($"[RVO] Error registrando obstáculo '{gameObject.name}'");
        }
    }
    
    public int GetRVOObstacleId() => rvoObstacleId;
    
    private void OnDestroy()
    {
        if (RVOSimulationManager.Instance != null)
        {
            RVOSimulationManager.Instance.UnregisterObstacle(this);
        }
    }
}
```

---

## PASO 4: Crear Escena de Prueba

### Crear Escena Setup Script

### Archivo: `RVO_NEW/RVOSceneSetup.cs`

```csharp
using UnityEngine;

public class RVOSceneSetup : MonoBehaviour
{
    [SerializeField] private bool autoSetupOnStart = true;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupRVOScene();
        }
    }
    
    public void SetupRVOScene()
    {
        Debug.Log("[RVO] Iniciando setup de escena RVO...");
        
        // Paso 1: Procesar todos los obstáculos
        RVOObstacle[] allObstacles = FindObjectsOfType<RVOObstacle>();
        foreach (RVOObstacle obstacle in allObstacles)
        {
            obstacle.RegisterInRVO();
        }
        
        // Paso 2: Procesar obstáculos en simulador
        RVOSimulationManager.Instance.ProcessAllObstacles();
        
        // Paso 3: Registrar agentes (automático en Start() de RVOAgentController)
        RVOAgentController[] allAgents = FindObjectsOfType<RVOAgentController>();
        Debug.Log($"[RVO] {allAgents.Length} agentes encontrados en escena");
        
        Debug.Log("[RVO] Setup de escena completado");
    }
}
```

---

## PASO 5: Instrucciones de Setup en Escena

### 📍 Pasos en el Editor de Unity:

#### 1️⃣ **Crear Manager**
```
Botón derecho en Hierarchy
→ Create Empty
→ Nombre: "RVOManager"
→ Añadir componente: RVOSimulationManager
→ Configurar parámetros si es necesario
```

#### 2️⃣ **Crear Setup Script**
```
Botón derecho en Hierarchy
→ Create Empty
→ Nombre: "RVOSetup"
→ Añadir componente: RVOSceneSetup
→ Marcar "Auto Setup On Start"
```

#### 3️⃣ **Crear Obstáculos**
```
Para cada obstáculo:
- Create Empty → Nombre: "Obstacle_X"
- Añadir: BoxCollider (o MeshCollider)
- Ajustar posición/escala
- Añadir componente: RVOObstacle
```

#### 4️⃣ **Crear Agentes (Ejemplo)**
```
Para cada agente:
- Create 3D Object → Sphere
- Nombre: "Agent_X"
- Remover SphereCollider automático
- Añadir: Capsule Collider (altura 2, radio 0.5)
- Añadir: Rigidbody (Body Type: Dynamic, Gravity: OFF)
- Añadir componente: RVOAgentController
  - Configurar Target (otro objeto o deixar vacío)
  - Ajustar parámetros si es necesario
```

#### 5️⃣ **Configurar Comportamiento** (Opcional)
```
Script adicional para controlar agentes:

public class RVOAgentBehavior : MonoBehaviour {
    void Start() {
        RVOAgentController agent = GetComponent<RVOAgentController>();
        // Ejemplo 1: Seguir un target
        agent.SetTarget(targetObject);
        
        // Ejemplo 2: Velocidad manual
        agent.SetManualVelocity(new Vector2(3, 2));
    }
}
```

---

## 🧪 Testing Básico

### Script de Prueba Rápida

```csharp
// En cualquier script en escena:
private void Update() {
    if (Input.GetKeyDown(KeyCode.Space)) {
        DebugRVOState();
    }
}

private void DebugRVOState() {
    var manager = RVOSimulationManager.Instance;
    Debug.Log($"Agentes activos: {manager.GetAgentCount()}");
    
    foreach (var agent in manager.GetAgents()) {
        var rvoId = agent.GetRVOAgentId();
        Debug.Log($"  - {agent.name}: pos={Simulator.Instance.getAgentPosition(rvoId)}");
    }
}
```

---

## 📊 Parámetros Recomendados por Caso

### 🏃 Agentes Pedestres (Rápidos)
```
neighborDist: 15
maxNeighbors: 15
timeHorizon: 5
timeHorizonObst: 2
radius: 0.5
maxSpeed: 5.0
```

### 🚗 Vehículos (Lentos, Grandes)
```
neighborDist: 25
maxNeighbors: 8
timeHorizon: 8
timeHorizonObst: 3
radius: 1.5
maxSpeed: 3.0
```

### 🐜 Multitudes (Muchos Agentes)
```
neighborDist: 10
maxNeighbors: 5
timeHorizon: 3
timeHorizonObst: 1.5
radius: 0.3
maxSpeed: 6.0
```

---

## ⚠️ Problemas Comunes

| Problema | Causa | Solución |
|----------|-------|----------|
| Agentes se atraviesan | Radius muy pequeño | Aumentar `radius` |
| Sin movimiento | Velocidad preferente = 0 | Asignar `target` o velocidad manual |
| Choque con obstáculos | timeHorizonObst muy bajo | Aumentar a 2-3 |
| Desempeño bajo | Demasiados neighbors | Reducir `maxNeighbors` o `neighborDist` |
| Comportamiento erático | Parámetros inconsistentes | Usar presets recomendados |

---

## 📚 Próximos Pasos

1. **Visualizar velocidades**: Dibujar líneas de velocidad esperada vs actual
2. **Pathfinding**: Integrar A* para rutas globales
3. **Grupos**: Agrupar agentes por comportamiento común
4. **Eventos**: Añadir callbacks en colisiones/llegada a destino
5. **Optimización**: Usar Job System para cálculos paralelizados

---

**¡Listo! Ahora tienes un sistema RVO2 funcional integrado en Unity.**
