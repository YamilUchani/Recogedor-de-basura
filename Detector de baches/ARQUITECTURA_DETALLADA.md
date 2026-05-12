# 🏗️ ARQUITECTURA DETALLADA DEL SIMULADOR

**Documento**: Arquitectura completa con diagramas  
**Fecha**: Mayo 5, 2026  
**Versión**: 2.0

---

## 🔄 FLUJO GENERAL DE LA APLICACIÓN

```
┌────────────────────────────────────────────────────────────────────┐
│                      UNITY ENGINE LOOP                             │
│                   (60 FPS @ 1920x1080)                            │
└────────────────────────────────────────────────────────────────────┘
                              │
                ┌─────────────┼─────────────┐
                │             │             │
                ▼             ▼             ▼
          ┌──────────┐  ┌──────────┐  ┌──────────┐
          │ Awake()  │  │ OnEnable │  │ Start()  │
          │ (ONCE)   │  │ (ONCE)   │  │ (ONCE)   │
          └──────────┘  └──────────┘  └──────────┘
                │             │             │
                └─────────────┼─────────────┘
                              │
                ┌─────────────▼─────────────┐
                │                           │
        ┌───────▼─────────┐        ┌───────▼──────────┐
        │   CADA FRAME    │        │ PHYSICS FRAME    │
        │   (Update)      │        │ (FixedUpdate)    │
        └───────┬─────────┘        └───────┬──────────┘
                │                          │
         ┌──────▼────────────────────────▼──────┐
         │   Input Processing                    │
         │   └─ Teclado (V, ESC, SPACE, P, etc.)│
         │   └─ Mouse (clicks, scroll)           │
         └──────┬────────────────────────────────┘
                │
         ┌──────▼────────────────────────────────┐
         │   RVO2 Physics Simulation              │
         │   ├─ PrepareStep()                     │
         │   ├─ doStep()                          │
         │   └─ ApplyVelocities()                 │
         │   (40 times per frame si step=0.025s)  │
         └──────┬────────────────────────────────┘
                │
         ┌──────▼────────────────────────────────┐
         │   Agent Updates                        │
         │   ├─ CarPatrol.FixedUpdate() x5        │
         │   ├─ RVOAgentNavigator x5              │
         │   ├─ RectangularPatrol.Update() x3     │
         │   └─ RVOAgentController x3             │
         └──────┬────────────────────────────────┘
                │
         ┌──────▼────────────────────────────────┐
         │   Collision Detection                  │
         │   ├─ OnTriggerEnter (baches)           │
         │   ├─ OnCollisionEnter (vehículos)      │
         │   └─ DataLogger.LogEvent()             │
         └──────┬────────────────────────────────┘
                │
         ┌──────▼────────────────────────────────┐
         │   Rendering                            │
         │   ├─ Scene 3D                          │
         │   ├─ UI Canvas                         │
         │   ├─ Update Stats Panel                │
         │   └─ Update Event Log                  │
         └──────┬────────────────────────────────┘
                │
         ┌──────▼────────────────────────────────┐
         │   Display Frame                        │
         │   └─ Screen.Present()                  │
         └──────┬────────────────────────────────┘
                │
                └───────────────┬─────────────────┐
                                │                 │
                        ┌───────▼─────────┐      │
                        │  ¿ESC?          │      │
                        │  ├─ Sí: Menú    │      │
                        │  └─ No: Bucle   │      │
                        └─────────────────┘      │
                                                 │
                          ┌──────────────────────▼┐
                          │  ¿App cierra?         │
                          │  ├─ Sí: OnDestroy()   │
                          │  └─ No: Continúa      │
                          └───────────────────────┘
```

---

## 📊 ARQUITECTURA DE CAPAS

```
┌──────────────────────────────────────────────────────────────────────┐
│  LAYER 0: PRESENTATION (UI/UX)                                       │
│  ├─ Canvas UI (ScreenSpace)                                          │
│  ├─ Event Log Panel                                                  │
│  ├─ Stats Display                                                    │
│  └─ Buttons (PAUSA, MENÚ, etc.)                                     │
└──────────────────────────────────────────────────────────────────────┘
                              ▲
                              │ (UI Events)
┌──────────────────────────────┼──────────────────────────────────────┐
│  LAYER 1: CONTROL (Input)                                           │
│  ├─ Input.GetKey() (teclado)                                        │
│  ├─ Input.GetMouseButtonDown() (mouse)                              │
│  ├─ UI Button OnClick()                                             │
│  └─ Event listeners (mediated pattern)                              │
└──────────────────────────────┼──────────────────────────────────────┘
                              ▲
                              │ (Control Signals)
┌──────────────────────────────┼──────────────────────────────────────┐
│  LAYER 2: ORCHESTRATION                                             │
│  ├─ SceneInitializer (loader + sequencer)                           │
│  ├─ LoadingScreenController (progress manager)                      │
│  ├─ SceneSwitcher (scene transitions)                               │
│  ├─ ToggleActiveExclusive (game state manager)                      │
│  └─ RVOSimulationManager (physics coordinator)                      │
└──────────────────────────────┼──────────────────────────────────────┘
                              ▲
                              │ (State Updates)
┌──────────────────────────────┼──────────────────────────────────────┐
│  LAYER 3: SIMULATION (Core Logic)                                   │
│  ├─ GENERATION SUBSYSTEM:                                           │
│  │  ├─ GeneradorDeCalle (procedural mesh)                           │
│  │  ├─ TerrainPotholeGenerator (bache placement)                    │
│  │  └─ PrefabObjectGenerator (casas, árboles, etc.)                 │
│  │                                                                   │
│  ├─ NAVIGATION SUBSYSTEM:                                           │
│  │  ├─ NavMeshdrone (baker)                                         │
│  │  └─ NavMesh (runtime agent pathfinding)                          │
│  │                                                                   │
│  ├─ AGENT SUBSYSTEM:                                                │
│  │  ├─ CarPatrol (vehicle AI)                                       │
│  │  ├─ RVOAgentNavigator (vehicle collision avoid.)                 │
│  │  ├─ RectangularPatrol (pedestrian routine)                       │
│  │  └─ RVOAgentController (pedestrian collision avoid.)             │
│  │                                                                   │
│  ├─ DETECTION SUBSYSTEM:                                            │
│  │  ├─ DetectionTrigger (pothole collision events)                  │
│  │  ├─ CollisionDetector (vehicle collision events)                 │
│  │  └─ DataLogger (event persistence)                               │
│  │                                                                   │
│  └─ PHYSICS SUBSYSTEM:                                              │
│     ├─ RVO2.Simulator (external collision lib)                      │
│     ├─ Unity Physics (Rigidbody, Collider)                          │
│     └─ Time.timeScale (speed control)                               │
│                                                                      │
└──────────────────────────────┼──────────────────────────────────────┘
                              ▲
                              │ (Rendering Requests)
┌──────────────────────────────┼──────────────────────────────────────┐
│  LAYER 4: RENDERING (Graphics)                                      │
│  ├─ MeshRenderer (city meshes)                                      │
│  ├─ MeshFilter (geometry data)                                      │
│  ├─ Material System (colors, shaders)                               │
│  ├─ Lighting (directional + ambient)                                │
│  └─ PostProcessing (optional effects)                               │
└──────────────────────────────┼──────────────────────────────────────┘
                              ▲
                              │ (Transform Updates)
┌──────────────────────────────┼──────────────────────────────────────┐
│  LAYER 5: PERSISTENCE (File I/O)                                    │
│  ├─ CSV Export (events log)                                         │
│  ├─ JSON Export (statistics)                                        │
│  ├─ PNG Export (screenshots)                                        │
│  └─ Binary Cache (scene data)                                       │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 COMPONENTES CLAVE Y SUS RESPONSABILIDADES

### 1️⃣ SceneInitializer (MAESTRO ORQUESTADOR)

```
┌────────────────────────────────────────────────────┐
│         SceneInitializer (Singleton)               │
├────────────────────────────────────────────────────┤
│                                                     │
│ Responsabilidades:                                 │
│ ├─ Inicializar secuencia de carga                  │
│ ├─ Coordinar generadores                           │
│ ├─ Hornear NavMesh                                 │
│ ├─ Activar sistemas de juego                       │
│ └─ Marcar cuando está listo (IsInitializeComplete) │
│                                                     │
│ Atributos:                                         │
│ ├─ calleGenerator: GeneradorDeCalle               │
│ ├─ bachesGenerator: TerrainPotholeGenerator       │
│ ├─ navMeshDrone: NavMeshdrone                     │
│ └─ gameLogicToggle: ToggleActiveExclusive         │
│                                                     │
│ Métodos Públicos:                                  │
│ └─ BeginInitialization() IEnumerator              │
│                                                     │
│ Métodos Privados:                                  │
│ └─ InitializeSceneSequence() IEnumerator          │
│    ├─ GenerarBaches()                              │
│    ├─ GenerarCalle()                               │
│    ├─ Esperar física (0.5s)                        │
│    ├─ HornearNavMesh()                             │
│    └─ ActivarLógicaJuego()                         │
│                                                     │
│ Timeline:                                          │
│ T+0.0s: Start()                                    │
│ T+0.1s: GenerarBaches()                            │
│ T+1.0s: GenerarCalle()                             │
│ T+1.5s: WaitForSeconds(0.5)                        │
│ T+2.0s: HornearNavMesh()                           │
│ T+5.0s: ActivarLógicaJuego()                       │
│ T+5.2s: IsInitializeComplete = true               │
│                                                     │
└────────────────────────────────────────────────────┘
```

### 2️⃣ LoadingScreenController (CONTROLADOR DE CARGA)

```
┌────────────────────────────────────────────────────┐
│      LoadingScreenController (Singleton)           │
├────────────────────────────────────────────────────┤
│                                                     │
│ Responsabilidades:                                 │
│ ├─ Cargar escena aditivamente                      │
│ ├─ Mostrar barra de progreso                       │
│ ├─ Esperar a SceneInitializer                      │
│ ├─ Activar objetos progresivamente                 │
│ └─ Descargar Mode_Load cuando esté listo           │
│                                                     │
│ Atributos:                                         │
│ ├─ sceneIndex: 0=Data, 1=Model, 2=Capture        │
│ ├─ barraProgreso: Image fillAmount                │
│ ├─ sceneToLoad: string determinado por index      │
│ ├─ rootGameObjects: List<GameObject>              │
│ └─ activationTimePerObject: float = 2.0           │
│                                                     │
│ Static Variables:                                  │
│ └─ sceneIndex (leído desde Mode_Menu)             │
│                                                     │
│ Input:                                             │
│ ├─ LoadingScreenController.sceneIndex             │
│    (establecido por ChangeSceneIndex.cs)           │
│                                                     │
│ Output:                                            │
│ ├─ barraProgreso.fillAmount (0 → 1)              │
│ ├─ Text_Message actualizados                      │
│ └─ SceneManager.LoadSceneAsync(sceneToLoad)      │
│                                                     │
│ Flujo:                                             │
│ 1. Start() lee sceneIndex                          │
│ 2. Determina escena a cargar                       │
│ 3. LoadSceneAsync(sceneToLoad, Additive)          │
│ 4. asyncLoad.completed += OnSceneLoaded()         │
│ 5. OnSceneLoaded():                                │
│    ├─ Busca SceneInitializer                       │
│    ├─ Recoge objetos raíz                          │
│    └─ StartCoroutine(ActivateObjectsProgressively)│
│ 6. ActivateObjectsProgressively():                 │
│    ├─ Para cada objeto:                            │
│    │  ├─ SetActive(true)                           │
│    │  ├─ fillAmount += paso                        │
│    │  └─ yield WaitForSeconds(2.0)                 │
│    └─ Llamar SceneInitializer.BeginInitialization│
│ 7. Esperar IsInitializeComplete = true             │
│ 8. Mostrar 100% por 2 segundos                     │
│ 9. UnloadScene("Mode_Load")                        │
│                                                     │
└────────────────────────────────────────────────────┘
```

### 3️⃣ RVOSimulationManager (FÍSICA RVO2)

```
┌────────────────────────────────────────────────────┐
│      RVOSimulationManager (Singleton)              │
├────────────────────────────────────────────────────┤
│                                                     │
│ Responsabilidades:                                 │
│ ├─ Gestionar instancia de RVO2.Simulator          │
│ ├─ Registrar agentes (5 vehículos + 3 peatones)  │
│ ├─ Ejecutar pasos de física cada FixedUpdate()    │
│ └─ Aplicar velocidades calculadas                 │
│                                                     │
│ Atributos:                                         │
│ ├─ simulator: RVO2.Simulator (instancia única)    │
│ ├─ agents: List<RVOAgent>                         │
│ ├─ agentIndices: Dictionary<GameObject, int>      │
│ └─ timeStep: 0.25s (4 substeps por frame @ 60FPS) │
│                                                     │
│ Métodos Públicos:                                  │
│ ├─ RegisterAgent(GameObject, RVOAgent)            │
│ ├─ UnregisterAgent(GameObject)                    │
│ ├─ SetAgentVelocity(int index, Vector3)           │
│ ├─ GetAgentPosition(int index) → Vector3          │
│ └─ GetAgentVelocity(int index) → Vector3          │
│                                                     │
│ FixedUpdate() Workflow:                            │
│ (Llamado 50 veces por segundo @ 0.02s timestep)  │
│                                                     │
│ 1. TickSimulation(0.25) [se ejecuta 4 veces]     │
│    ├─ PrepareStep()                               │
│    │  └─ Agentes comunican velocidades deseadas   │
│    ├─ doStep()                                    │
│    │  └─ Calcula velocidades de evasión           │
│    └─ ApplyVelocities()                           │
│       └─ Agentes aplican nuevas velocidades       │
│                                                     │
│ 2. Resultado: Collision avoidance bidireccional  │
│    ├─ Vehículos evitan vehículos                  │
│    ├─ Peatones evitan peatones                    │
│    ├─ Vehículos evitan peatones                   │
│    └─ Todos respetan obstáculos (Rigidbody)      │
│                                                     │
│ Integración con CarPatrol:                         │
│ CarPatrol.FixedUpdate()                            │
│ ├─ Calcula dirección a waypoint                    │
│ ├─ Aplica velocidad deseada                        │
│ │  └─ SetAgentVelocity(index, desiredVel)        │
│ └─ Move += RVO output velocity                     │
│                                                     │
└────────────────────────────────────────────────────┘
```

### 4️⃣ GeneradorDeCalle (GENERACIÓN PROCEDURAL)

```
┌────────────────────────────────────────────────────┐
│     GeneradorDeCalle (Procedural Generation)       │
├────────────────────────────────────────────────────┤
│                                                     │
│ Responsabilidades:                                 │
│ ├─ Generar mesh de calle                           │
│ ├─ Crear casas en intersecciones                   │
│ ├─ Crear aceras con colliders                      │
│ ├─ Crear waypoints                                 │
│ └─ Organizar en jerarquía                          │
│                                                     │
│ Parámetros Configurables:                          │
│ ├─ gridWidth: 20 unidades                          │
│ ├─ gridHeight: 20 unidades                         │
│ ├─ cellSize: 10 unidades                           │
│ ├─ sidewalkWidth: 2 unidades                       │
│ ├─ seed: int (para reproducibilidad)              │
│ └─ houseCount: 20-50 random                        │
│                                                     │
│ Output GameObject Hierarchy:                       │
│ Street_Mesh                                        │
│ ├─ MeshFilter (vertices, triangles)               │
│ ├─ MeshCollider (convex=false)                    │
│ ├─ MeshRenderer (material asphalt)                │
│ └─ Tag: "Street"                                   │
│                                                     │
│ Houses_Container                                   │
│ ├─ House_0 (pos en grid[0][0])                    │
│ │  ├─ MeshFilter (modelo casa procedural)         │
│ │  ├─ BoxCollider (límite físico)                 │
│ │  ├─ MeshRenderer                                │
│ │  └─ Tag: "House"                                │
│ └─ House_N (N = 20-50)                            │
│                                                     │
│ Sidewalks_Container                                │
│ ├─ Sidewalk_0 (lado norte)                        │
│ │  ├─ MeshCollider                                │
│ │  ├─ Tag: "Acera"                                │
│ │  └─ Material gris                               │
│ └─ Sidewalk_3 (lado sur)                          │
│                                                     │
│ Waypoints_Container                                │
│ ├─ Waypoint_0 (intersección)                      │
│ │  ├─ Empty GameObject                            │
│ │  ├─ Position: (x, 0, z) en grid               │
│ │  └─ Tag: "Waypoint"                             │
│ └─ Waypoint_N (N = 30-40)                         │
│                                                     │
│ Generate() Workflow:                               │
│ 1. New Random(seed) [inicializa RNG]              │
│ 2. GenerateMesh():                                 │
│    ├─ Crea vertices 2D grid                        │
│    ├─ Crea triangles                               │
│    └─ MeshCollider.SetConvex(false)               │
│ 3. CreateHouses():                                 │
│    ├─ Para cada celda grid:                        │
│    │  ├─ 50% prob de crear casa                    │
│    │  ├─ Random tamaño (2-8 unidades)             │
│    │  └─ Instancia prefab house_procedural         │
│    └─ Agrega a Houses_Container                    │
│ 4. CreateSidewalks():                              │
│    ├─ 4 aceras (norte, sur, este, oeste)          │
│    └─ Recorren perímetro                           │
│ 5. DiscoverWaypoints():                            │
│    ├─ Para cada intersección:                      │
│    │  ├─ Instancia GameObject vacío                │
│    │  ├─ Position = intersección                   │
│    │  └─ Tag = "Waypoint"                          │
│    └─ FindObjectsByTag("Waypoint")                 │
│ 6. SetupTags():                                    │
│    └─ Tag todas las casas como "House"             │
│                                                     │
└────────────────────────────────────────────────────┘
```

### 5️⃣ CarPatrol (IA DE VEHÍCULOS)

```
┌────────────────────────────────────────────────────┐
│  CarPatrol (Vehicle AI - 5 instancias)             │
├────────────────────────────────────────────────────┤
│                                                     │
│ Responsabilidades:                                 │
│ ├─ Elegir waypoint aleatorio cada X segundos      │
│ ├─ Navegar hacia waypoint                          │
│ ├─ Evitar obstáculos (RVO2)                        │
│ ├─ Detectar baches (trigger)                       │
│ └─ Reportar eventos a DataLogger                   │
│                                                     │
│ Parámetros Configurables:                          │
│ ├─ targetSpeed: 5-10 m/s                          │
│ ├─ accelerationTime: 2-5 segundos                  │
│ ├─ waypointThreshold: 1.0 (metros)                │
│ ├─ waypointChangeInterval: 30 segundos            │
│ └─ vehicleID: 0-4 (identificador)                 │
│                                                     │
│ Componentes Requeridos:                            │
│ ├─ Rigidbody (isKinematic=true)                    │
│ ├─ CapsuleCollider (trigger)                       │
│ ├─ RVOAgentNavigator                              │
│ ├─ CollisionDetector                              │
│ └─ MeshRenderer (modelo)                           │
│                                                     │
│ Update() Workflow:                                 │
│ 1. Cada frame:                                     │
│    ├─ CheckWaypointChange():                       │
│    │  ├─ if (time since last change) > 30s:       │
│    │  │  ├─ FindObjectsByTag("Waypoint")          │
│    │  │  ├─ Random.Range(0, count)                │
│    │  │  └─ targetWaypoint = selected              │
│    │  └─ else: continúa hacia waypoint actual      │
│    │                                               │
│    ├─ CalculateDirection():                        │
│    │  ├─ direction = (targetWaypoint.pos - pos)   │
│    │  ├─ direction.Normalize()                     │
│    │  └─ desiredVelocity = direction * targetSpeed │
│    │                                               │
│    ├─ ApproachWaypoint():                          │
│    │  ├─ distance = Vector3.Distance(...)          │
│    │  ├─ if distance < threshold:                  │
│    │  │  └─ velocity *= 0 (frena)                  │
│    │  └─ else: acelera gradualmente                │
│    │                                               │
│    └─ Move():                                      │
│       ├─ actualVelocity = RVOOutput + desiredVel   │
│       ├─ transform.position += actualVelocity     │
│       └─ Renderer.Rotate(forward)                  │
│                                                     │
│ OnTriggerEnter(Collider pothole):                 │
│ ├─ if pothole.tag == "Pothole":                    │
│ │  ├─ DataLogger.LogEvent({                        │
│ │  │    type: "POTHOLE_DETECTED",                  │
│ │  │    vehicleID: this.vehicleID,                 │
│ │  │    position: transform.position,              │
│ │  │    timestamp: Time.time                       │
│ │  │  })                                           │
│ │  └─ PlaySFX("bump.wav")                          │
│ └─ Continúa patrulando                             │
│                                                     │
│ Estadísticas por Simulación:                       │
│ ├─ Potholes Detectados: 0-10                       │
│ ├─ Distancia Recorrida: 500-1000m                  │
│ ├─ Tiempo Patrullando: 300-600s                    │
│ └─ Velocidad Promedio: 7.5 m/s                     │
│                                                     │
└────────────────────────────────────────────────────┘
```

### 6️⃣ DataLogger (PERSISTENCIA DE DATOS)

```
┌────────────────────────────────────────────────────┐
│     DataLogger (Event Logging - Singleton)         │
├────────────────────────────────────────────────────┤
│                                                     │
│ Responsabilidades:                                 │
│ ├─ Recibir eventos de detectores                   │
│ ├─ Bufferizar en memoria                           │
│ ├─ Exportar a archivos (CSV, JSON, LOG)           │
│ └─ Generar reportes analíticos                     │
│                                                     │
│ Estructura de Evento:                              │
│ class SimulationEvent {                            │
│   float timestamp;           // Tiempo en sim       │
│   string eventType;          // POTHOLE, COLLISION │
│   int agentID;               // 0-7 (5v + 3p)     │
│   Vector3 position;          // Dónde ocurrió       │
│   Dictionary<string,object>  // Datos específicos  │
│     metadata;                                      │
│ }                                                   │
│                                                     │
│ Buffer en Memoria:                                 │
│ List<SimulationEvent> events = new();              │
│ ├─ Agregar O(1) por evento                         │
│ ├─ Capacidad: ~100,000 eventos/sesión             │
│ └─ Límite de RAM: 2-4 GB                           │
│                                                     │
│ Métodos Públicos:                                  │
│ ├─ LogEvent(SimulationEvent)                       │
│ │  └─ events.Add(event)                            │
│ │                                                  │
│ ├─ ExportData():                                   │
│ │  ├─ ExportCSV()                                  │
│ │  ├─ ExportJSON()                                 │
│ │  └─ ExportLOG()                                  │
│ │                                                  │
│ ├─ GenerateReport():                               │
│ │  ├─ Calcula estadísticas                         │
│ │  ├─ Genera gráficos                              │
│ │  └─ Crea resumen ejecutivo                       │
│ │                                                  │
│ └─ GetStatistics() → Dictionary                    │
│    ├─ total_potholes_detected                      │
│    ├─ total_collisions                             │
│    ├─ avg_vehicle_speed                            │
│    ├─ path_coverage_km                             │
│    └─ ...                                           │
│                                                     │
│ Output Files:                                      │
│ Assets/Output/SimData_[TIMESTAMP]/                │
│ ├─ events.csv                                      │
│ │  ├─ timestamp,type,agentID,x,y,z,severity       │
│ │  └─ 12.34,POTHOLE,0,45.2,0.0,32.1,0.85         │
│ │                                                  │
│ ├─ statistics.json                                 │
│ │  ├─ total_events: 1234                           │
│ │  ├─ potholes_detected: 45                        │
│ │  └─ ...                                           │
│ │                                                  │
│ └─ simulation.log                                  │
│    └─ [00:00] Simulación iniciada                 │
│       [00:01] Vehicle_0 activo                     │
│       [00:45] Primer bache detectado en (45,0,32) │
│                                                     │
│ Llamadas Típicas:                                  │
│ En CarPatrol.OnTriggerEnter():                     │
│   DataLogger.LogEvent(new SimulationEvent {        │
│     timestamp = Time.time,                         │
│     eventType = "POTHOLE_DETECTED",                │
│     agentID = vehicleID,                           │
│     position = transform.position                  │
│   });                                              │
│                                                     │
└────────────────────────────────────────────────────┘
```

---

## 🔗 DIAGRAMA DE DEPENDENCIAS

```
┌─────────────────────────────────────┐
│     LoadingScreenController         │
│ (Carga Mode_Menu desde Mode_Menu)   │
└────────────────┬────────────────────┘
                 │
    ┌────────────▼──────────────┐
    │ SceneManager.LoadSceneAsync│
    │ (Cargar escena aditiva)    │
    └────────────┬───────────────┘
                 │
    ┌────────────▼──────────────┐
    │ SceneInitializer.          │
    │ BeginInitialization()      │
    └────────────┬───────────────┘
                 │
    ┌────────────┴──────────────────────────────────┐
    │                                               │
┌───▼────────────┐  ┌──────────────┐  ┌───────────┐│
│Generador Calle  │  │Generador Baches
│GeneradorDeCalle │  │TerrainPotholeGen│  NavMeshD │
└────────────────┘  └──────────────┘  └──────────┘│
                                                    │
             ┌──────────────────────┐               │
             │ RVOSimulationManager │               │
             │ (register agents)    │               │
             └──────────────┬───────┘               │
                            │                       │
         ┌──────────────────┴──────────────────┐   │
         │                                     │    │
    ┌────▼──────┐  ┌───────────┐  ┌──────────┐│   │
    │ CarPatrol │  │ Pedestrian│  │Detection ││   │
    │ (x5)      │  │ Patrol    │  │Trigger   ││   │
    │           │  │ (x3)      │  │ (x200)   ││   │
    └────┬──────┘  └───────────┘  └──────────┘│   │
         │                                     │   │
         │         ┌──────────────────────┐   │   │
         └────────▶│   DataLogger         │   │   │
                   │ (logging events)     │   │   │
                   └──────────────────────┘   │   │
                                              │   │
                        ┌─────────────────────┘   │
                        │                         │
                        ▼                         │
                   ┌──────────┐              ┌───▼────┐
                   │ CSV/JSON │              │Toggle  │
                   │ FILES    │              │Active  │
                   └──────────┘              └────────┘
```

---

## 📈 FLUJO DE DATOS DURANTE SIMULACIÓN

```
Segundo 0.00 - Inicialización:
├─ SceneInitializer.BeginInitialization()
├─ GeneradorDeCalle.Generate()
│  ├─ Crea 2000+ vértices
│  ├─ Instancia 20-50 casas
│  └─ Descubre 30-40 waypoints
├─ TerrainPotholeGenerator.Generate()
│  └─ Instancia 50-200 baches con SphereColliders
├─ NavMeshdrone.ManualBake()
│  └─ Hornea NavMesh de superficie
└─ ToggleActiveExclusive.Initialize()
   ├─ Instancia Vehicle_0..4
   ├─ Instancia Pedestrian_0..2
   └─ Inicia simulación (timeScale=1)

Segundo 0.10 - Primera Iteración:

Physics Step (FixedUpdate x 50 Hz):
  RVOSimulationManager.FixedUpdate()
  ├─ PrepareStep()
  │  ├─ Vehicle_0.GetDesiredVel() = direction*7.5
  │  ├─ Vehicle_1.GetDesiredVel() = direction*8.2
  │  ├─ ... (x5 vehículos)
  │  ├─ Pedestrian_0.GetDesiredVel() = direction*1.5
  │  └─ ... (x3 peatones)
  ├─ doStep()
  │  ├─ Calcula velocidades de evasión
  │  ├─ RVO2.Simulator.doStep()
  │  └─ Output: newVelocities[]
  └─ ApplyVelocities()
     ├─ Vehicle_0.velocity = newVelocities[0]
     ├─ ... (todos los agentes)
     └─ rigidbody.velocity = calculatedVel

  CarPatrol[0-4].FixedUpdate()
  ├─ Calcula distancia a waypoint
  ├─ Aplica aceleración/frenado
  ├─ rigidbody.position += velocity * deltaTime
  └─ Si hay colisión trigger → OnTriggerEnter()

  RectangularPatrol[0-2].Update()
  ├─ Calcula siguiente punto de patrulla
  ├─ Evita usando RVO2
  └─ Anima personaje

Rendering Step (Update x 60 Hz):
  Camaras.Update()
  ├─ Input.GetKeyDown('v')
  └─ Cambiar cámara activa (0→1→2→0)

  UI Update:
  ├─ PerformanceManager.Update()
  │  ├─ Calcula FPS
  │  └─ Actualiza Panel_Stats
  ├─ EventLog.Update()
  │  └─ Muestra últimos 10 eventos
  └─ Input.GetKeyDown(KeyCode.Escape)
     └─ SceneManager.LoadScene("Mode_Menu")

Colisión (OnTrigger):
  Vehicle_0 → trigger Pothole_34:
  ├─ DetectionTrigger.OnTriggerEnter(collider)
  ├─ if collider.tag == "Pothole"
  │  └─ DataLogger.LogEvent({
  │       timestamp: 12.34,
  │       eventType: "POTHOLE_DETECTED",
  │       vehicleID: 0,
  │       position: (45.2, 0, 32.1)
  │     })
  └─ Panel_Log.Add("[12:34] Vehicle_0 detectó bache")

Segundo 300.00 - Usuario presiona ESC:
├─ Input.GetKeyDown(KeyCode.Escape)
├─ SceneManager.LoadScene("Mode_Menu")
├─ OnDestroy() en todos los GameObjects
├─ RVO2.Simulator destruido
├─ DataLogger.ExportData()
│  ├─ events.csv (1234 líneas)
│  ├─ statistics.json ({totals})
│  └─ simulation.log
└─ GC.Collect() libera memoria
```

---

## 🎬 RESUMEN VISUAL DE ARQUITECTURA

```
┌──────────────────────────────────────────────────────────┐
│                    UNITY SCENES                          │
│                                                           │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐         │
│  │ Mode_Menu  │  │ Mode_Load  │  │ Mode_Model │         │
│  │ (Selector) │  │ (Progress) │  │ (Simulation)         │
│  └────────────┘  └────────────┘  └────────────┘         │
│                                                           │
│  OTHERS: Mode_Data, Mode_Debug, Mode_Capture            │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│            GAMEOBJECTS EN SIMULACIÓN                     │
│                                                           │
│  ├─ SceneInitializer (orquestador)                      │
│  ├─ RVOSimulationManager (física)                       │
│  ├─ Terrain (calle + baches + casas)                    │
│  ├─ Vehicles x5 (con CarPatrol + RVO)                   │
│  ├─ Pedestrians x3 (con Patrol + RVO)                   │
│  ├─ Waypoints x30-40 (puntos navegación)                │
│  ├─ NavMesh (mapa de navegación)                        │
│  ├─ Cameras x3 (espectador, 1ªpersona, lateral)        │
│  └─ Canvas (UI + botones)                              │
│                                                           │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│            SCRIPTS Y COMPONENTES                          │
│                                                           │
│  Core:          CarPatrol, RectangularPatrol, RVO2Agt    │
│  Detection:     DetectionTrigger, CollisionDetector      │
│  Generation:    GeneradorDeCalle, PotholeGenerator       │
│  Management:    SceneInitializer, LoadingScreenCtrl      │
│  Physics:       RVOSimulationManager, Physics Engine     │
│  UI:            DataLogger, PerformanceManager           │
│                                                           │
└──────────────────────────────────────────────────────────┘
```

---

**Fin de Arquitectura Detallada** ✨

*Este documento describe la estructura completa del sistema de simulación.*
