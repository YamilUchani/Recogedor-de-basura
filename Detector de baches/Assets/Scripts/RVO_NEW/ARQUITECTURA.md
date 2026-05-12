# 🏗️ Arquitectura y Diagrama del Sistema RVO2

## 📊 Diagrama General del Flujo

```
┌─────────────────────────────────────────────────────────────┐
│                    ESCENA UNITY                              │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │  RVOManager      │         │  RVOSetup        │          │
│  │  (Singleton)     │         │  (Auto Init)     │          │
│  └────────┬─────────┘         └──────────────────┘          │
│           │                                                   │
│           │         Inicializa y Coordina                    │
│           │                                                   │
│      ┌────▼──────────────────────────────┐                  │
│      │   Simulator (RVO2.dll)            │                  │
│      │   - Kernel de simulación          │                  │
│      │   - KdTree para búsquedas         │                  │
│      │   - Cálculo ORCA                  │                  │
│      └────┬────────────────────┬─────────┘                  │
│           │                    │                             │
│      ┌────▼──────┐        ┌────▼──────┐                     │
│      │ Agentes   │        │ Obstáculos│                     │
│      │ - Pos     │        │ - Vértices│                     │
│      │ - Vel     │        │ - Edges   │                     │
│      │ - Vecinos │        │ - Convex  │                     │
│      └────────────┘        └───────────┘                     │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔄 Ciclo de Actualización por Frame

```
┌─────────────────────────────────────────────────────────┐
│             Update() - RVOSimulationManager               │
└───────────────────┬─────────────────────────────────────┘
                    │
        ┌───────────▼──────────────┐
        │ accumulatedTime += deltaTime
        └───────────┬──────────────┘
                    │
        ┌───────────▼────────────────────────┐
        │ Para cada RVOAgentController:      │
        │ UpdatePreferredVelocity()          │
        │ - Calcula dirección hacia target   │
        │ - Establece velocidad preferida    │
        └───────────┬────────────────────────┘
                    │
        ┌───────────▼────────────────────────┐
        │ Mientras (accumulatedTime >= step) │
        │ Simulator.Instance.doStep()        │
        │  ├─ Construir KdTree               │
        │  ├─ Calcular vecinos (PARALELO)    │
        │  ├─ Calcular velocidades (ORCA)    │
        │  └─ Actualizar posiciones          │
        └───────────┬────────────────────────┘
                    │
        ┌───────────▼────────────────────────┐
        │ Para cada RVOAgentController:      │
        │ SyncPositionFromRVO()              │
        │ - Lee posición de RVO              │
        │ - Actualiza GameObject             │
        └─────────────────────────────────────┘
```

---

## 🧩 Estructura de Clases

### RVO_NEW Components

```
RVOSimulationManager
├─ Propiedades:
│  ├─ agents: List<RVOAgentController>
│  ├─ obstacles: List<RVOObstacle>
│  ├─ timeStep: float
│  └─ accumulatedTime: float
├─ Métodos:
│  ├─ RegisterAgent(agent)
│  ├─ RegisterObstacle(obstacle)
│  ├─ ProcessAllObstacles()
│  └─ Update() [loop principal]
└─ Singleton Pattern: Instance

RVOAgentController : MonoBehaviour
├─ Propiedades:
│  ├─ rvoAgentId: int
│  ├─ target: Transform
│  ├─ maxSpeed: float
│  ├─ radius: float
│  └─ preferredVelocity: Vector2
├─ Métodos:
│  ├─ UpdatePreferredVelocity()
│  ├─ SyncPositionFromRVO()
│  ├─ SetTarget(transform)
│  └─ SetManualVelocity(velocity)
└─ Conecta: GameObject ←→ RVO Simulator

RVOObstacle : MonoBehaviour
├─ Propiedades:
│  ├─ vertices: List<Vector2>
│  ├─ rvoObstacleId: int
│  └─ isClockwise: bool
├─ Métodos:
│  ├─ ExtractVertices()
│  └─ RegisterInRVO()
└─ Soporta: BoxCollider, MeshCollider, PolygonCollider2D

RVOSceneSetup : MonoBehaviour
├─ Métodos:
│  └─ SetupRVOScene()
│     ├─ Registra todos los obstáculos
│     ├─ Procesa el árbol de obstáculos
│     └─ Valida agentes
└─ Auto-ejecutable en Start()
```

### RVO2 Core (Existente)

```
Simulator (Singleton)
├─ agents_: List<Agent>
├─ obstacles_: List<Obstacle>
├─ kdTree_: KdTree
├─ timeStep_: float
├─ Métodos clave:
│  ├─ addAgent(pos, params...)
│  ├─ addObstacle(vertices)
│  ├─ doStep() [Paso de simulación]
│  ├─ getAgentPosition(id)
│  ├─ getAgentVelocity(id)
│  └─ setAgentPrefVelocity(id, vel)
└─ Multithreading: Worker threads

Agent
├─ position_: Vector2
├─ velocity_: Vector2
├─ prefVelocity_: Vector2
├─ agentNeighbors_: List<KeyValuePair>
├─ obstacleNeighbors_: List<KeyValuePair>
├─ orcaLines_: List<Line> [restricciones]
└─ Métodos:
   ├─ computeNeighbors()
   ├─ computeNewVelocity() [ORCA calc]
   └─ update()

KdTree
├─ agentTree_: AgentTreeNode[]
├─ obstacleTree_: ObstacleTreeNode[]
├─ Métodos:
│  ├─ buildAgentTree()
│  ├─ buildObstacleTree()
│  ├─ computeAgentNeighbors()
│  └─ queryVisibility()
└─ Optimización: O(log n) búsquedas

Vector2
├─ x_, y_: float
└─ Operadores: +, -, *, /, dot, cross

Obstacle
├─ point_: Vector2
├─ direction_: Vector2
├─ next_, previous_: Obstacle
├─ convex_: bool
└─ id_: int

Line
├─ point: Vector2 [punto en línea]
└─ direction: Vector2 [dirección]
```

---

## 🔗 Flujo de Datos

### Inicialización

```csharp
// 1. Manager setup
RVOSimulationManager.Instance.RegisterAgent(agent)
    └─ Agrega a lista agents_

// 2. Agent creación
RVOAgentController.Start()
    ├─ Simulator.Instance.addAgent(...)
    └─ Retorna rvoAgentId

// 3. Obstáculos
RVOObstacle.RegisterInRVO()
    ├─ Simulator.Instance.addObstacle(vertices)
    └─ Retorna rvoObstacleId

// 4. Setup
RVOSceneSetup.SetupRVOScene()
    └─ Simulator.Instance.processObstacles()
       └─ Construye KdTree eficiente
```

### En Cada Frame

```
RVOSimulationManager.Update()
    │
    ├─ agent.UpdatePreferredVelocity()
    │   ├─ Calcular dirección al target
    │   └─ Simulator.setAgentPrefVelocity()
    │
    ├─ Simulator.doStep()
    │   ├─ Para cada agente (PARALELO):
    │   │   ├─ computeNeighbors() - Busca en KdTree
    │   │   └─ computeNewVelocity() - Calcula ORCA
    │   │
    │   └─ Para cada agente:
    │       └─ update() - Actualiza posición
    │
    └─ agent.SyncPositionFromRVO()
        └─ Transform.position = RVO.position
```

---

## 📦 Dependencias

```
RVO_NEW/
├─ Depende de: Assets/Scripts/RVO2/
│   ├─ Simulator.cs
│   ├─ Agent.cs
│   ├─ KdTree.cs
│   ├─ Vector2.cs
│   ├─ RVOMath.cs
│   ├─ Obstacle.cs
│   └─ Line.cs
│
└─ Requiere en Unity:
    ├─ Rigidbody (en agentes)
    ├─ Collider (BoxCollider, MeshCollider, etc)
    └─ Transform
```

---

## 🔀 Patrón Singleton de Manager

```csharp
// Garantiza una sola instancia
public static RVOSimulationManager Instance {
    get {
        if (instance == null) {
            instance = FindObjectOfType<RVOSimulationManager>();
            // Crear si no existe
            if (instance == null) {
                GameObject obj = new GameObject("RVOSimulationManager");
                instance = obj.AddComponent<RVOSimulationManager>();
            }
        }
        return instance;
    }
}

// Acceso global seguro
RVOSimulationManager.Instance.RegisterAgent(agent);
```

---

## 🧵 Multithreading en RVO

```
Main Thread (Start):
    └─ Simulator.Initialize()
       └─ numWorkers = CPU core count

Physics Update:
    └─ doStep()
       ├─ Crear Worker threads (una por core)
       │   ├─ Worker 0: Procesa agentes [0..N/4)
       │   ├─ Worker 1: Procesa agentes [N/4..N/2)
       │   ├─ Worker 2: Procesa agentes [N/2..3N/4)
       │   └─ Worker 3: Procesa agentes [3N/4..N)
       │
       ├─ Barrier: Espera a todos los workers (step)
       │
       └─ Barrier: Espera a todos los workers (update)
```

---

## 🎯 Casos de Uso

### Escena Simple
```
RVOManager → RVOSetup → [Obstáculos] → [Agentes simples]
```

### Escena Compleja
```
RVOManager → RVOSetup ─┬─ [Múltiples zonas de obstáculos]
                      ├─ [Cientos de agentes]
                      ├─ [Pathfinding integrado]
                      └─ [Eventos de colisión]
```

### Integración con Game Logic
```
GameController
    ├─ RVOSimulationManager
    ├─ [Agentes guiados por IA]
    └─ [Eventos: llegada, colisión, etc.]
```

---

## ✅ Checklist de Implementación

- [ ] Agregar RVOManager a escena
- [ ] Agregar RVOSetup a escena
- [ ] Crear obstáculos con RVOObstacle
- [ ] Crear agentes con RVOAgentController
- [ ] Ejecutar RVOSceneSetup.SetupRVOScene()
- [ ] Asignar targets a agentes
- [ ] Probar evitación de colisiones
- [ ] Ajustar parámetros según sean necesarios
- [ ] Usar RVODebugger para troubleshooting

---

**La arquitectura está diseñada para ser modular, escalable y fácil de depurar.** 🚀
