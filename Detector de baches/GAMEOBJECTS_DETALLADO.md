# 🎮 ANÁLISIS DETALLADO DE GAMEOBJECTS Y EDITOR DE UNITY

**Documento**: Referencia de GameObjects  
**Fecha**: Mayo 5, 2026  
**Audiencia**: Desarrolladores, Arquitectos de Sistema

---

## 📦 JERARQUÍA COMPLETA DE GAMEOBJECTS

### Estructura por Capas

```
[ROOT SCENE]
│
├─ LAYER 0: MANAGERS (Singleton Pattern)
│  │
│  ├─ SceneInitializer [Prefab/Script]
│  │  ├─ Component: SceneInitializer (Script)
│  │  │  ├─ Public var: calleGenerator (Transform)
│  │  │  ├─ Public var: bachesGenerator (Transform)
│  │  │  ├─ Public var: navMeshDrone (Transform)
│  │  │  ├─ Public var: gameLogicToggle (Transform)
│  │  │  ├─ Public method: BeginInitialization()
│  │  │  ├─ Public method: IsInitializeComplete { get; }
│  │  │  └─ Coroutine: InitializeSceneSequence()
│  │  │
│  │  └─ Child: GeneradorDeCalle [GameObject]
│  │     ├─ Component: GeneradorDeCalle (Script)
│  │     │  ├─ [Range] streetWidth: 30-50m
│  │     │  ├─ [Range] streetLength: 100-300m
│  │     │  ├─ [Range] blockSize: 30-100m
│  │     │  ├─ [Range] sidewalkWidth: 1-3m
│  │     │  ├─ Material[] streetMaterials
│  │     │  ├─ Prefab[] housePrefabs
│  │     │  ├─ Public method: Generate()
│  │     │  └─ [SerializeField] debugMode: bool
│  │     │
│  │     └─ Output: Procedural Mesh
│  │        ├─ Mesh filter con vértices XZ
│  │        ├─ Material aplicado
│  │        └─ Static batching FLAG
│  │
│  ├─ RVOSimulationManager [Singleton]
│  │  ├─ Component: RVOSimulationManager (Script)
│  │  │  ├─ Static: Instance { get; }
│  │  │  ├─ [HideInInspector] Simulator rvoSimulator
│  │  │  ├─ Private: List<RVOAgentNavigator> navigators
│  │  │  ├─ Private: List<RVOAgentController> controllers
│  │  │  ├─ Public method: RegisterNavigator(nav)
│  │  │  ├─ Public method: UnregisterNavigator(nav)
│  │  │  ├─ Public method: RegisterAgent(agent)
│  │  │  ├─ Public method: UnregisterAgent(agent)
│  │  │  ├─ FixedUpdate: Ejecuta simulación RVO2
│  │  │  └─ Coroutine: InitializeSimulator()
│  │  │
│  │  └─ Data: RVO.Simulator
│  │     ├─ agents_: List<Agent> (todos los agentes)
│  │     ├─ obstacles_: List<Obstacle> (muros/límites)
│  │     ├─ kdTree_: KDTree (búsqueda rápida)
│  │     ├─ timeStep_: 0.05 seg (constante)
│  │     └─ Method: doStep() [ejecutado cada FixedUpdate]
│  │
│  └─ DataLogger [Optional]
│     ├─ Component: DataLogger (Script)
│     │  ├─ [SerializeField] outputFilePath: string
│     │  ├─ [SerializeField] debugLog: bool
│     │  ├─ Private: List<LogEntry> eventBuffer
│     │  ├─ Public method: LogEvent(type, agentId, pos, data)
│     │  ├─ Public method: SaveData()
│     │  ├─ Public method: GetStatistics()
│     │  └─ Coroutine: FlushToFile()
│     │
│     └─ Output:
│        ├─ CSV: simulation_log_YYYY-MM-DD.csv
│        ├─ JSON: statistics.json
│        └─ Console: [DataLogger] eventos
│
├─ LAYER 1: TERRAIN & ENVIRONMENT
│  │
│  ├─ Terrain [GameObject - Static]
│  │  ├─ Component: MeshFilter
│  │  │  └─ mesh: Procedurally Generated Street Mesh
│  │  ├─ Component: MeshCollider
│  │  │  ├─ convex: false
│  │  │  ├─ isTrigger: false
│  │  │  └─ Layer: Default (colisiones)
│  │  ├─ Component: MeshRenderer
│  │  │  └─ Material: Street_Diffuse (gris/asfalto)
│  │  └─ Tag: "Untagged"
│  │  └─ Layer: 0 (Default)
│  │
│  ├─ Sidewalks [GameObject - Static Parent]
│  │  │
│  │  └─ Sidewalk_Segment [x20] [GameObject - Static]
│  │     ├─ Component: BoxCollider
│  │     │  ├─ isTrigger: false
│  │     │  ├─ Size: (2, 0.5, 100) típicamente
│  │     │  └─ Center: (0, 0.25, 0)
│  │     ├─ Component: MeshRenderer (opcional visual)
│  │     │  └─ Material: Sidewalk_Diffuse (blanco/crema)
│  │     │
│  │     ├─ Tag: "Acera"
│  │     └─ Layer: 2 (Ignore Raycast)
│  │           [Para que CarPatrol lo detecte pero no cause problemas]
│  │
│  ├─ Potholes [Parent GameObject]
│  │  │
│  │  └─ Pothole_00 [x50-200] [Procedurally Generated]
│  │     ├─ Component: Transform
│  │     │  └─ Position: Random(minX, maxX), 0, Random(minZ, maxZ)
│  │     │
│  │     ├─ Component: SphereCollider
│  │     │  ├─ isTrigger: true ← IMPORTANTE
│  │     │  ├─ radius: Random(0.5, 2.0) metros
│  │     │  └─ Center: (0, -0.5, 0) [hundido]
│  │     │
│  │     ├─ Component: MeshRenderer
│  │     │  └─ Material: Pothole_Red (rojo/naranja)
│  │     │
│  │     ├─ Component: DetectionTrigger [Script]
│  │     │  ├─ onTriggerEnter: DataLogger.LogEvent(POTHOLE_DETECTED)
│  │     │  ├─ Public var: potholeId: int
│  │     │  ├─ Public var: depth: float
│  │     │  └─ Public var: severity: enum (MINOR, MAJOR, CRITICAL)
│  │     │
│  │     ├─ Tag: "bache"
│  │     └─ Layer: 0 (Default)
│  │
│  ├─ Buildings [Parent GameObject]
│  │  │
│  │  └─ House_00 [Procedurally Generated x20-50]
│  │     ├─ Component: Transform
│  │     │  ├─ Position: Intersección de calles (procedural)
│  │     │  ├─ Rotation: (0, Random(0,360), 0)
│  │     │  └─ Scale: (1, 1, 1) [sin modificar]
│  │     │
│  │     ├─ Component: BoxCollider
│  │     │  ├─ isTrigger: false
│  │     │  ├─ Size: Random(10-20, 8-12, 10-20) metros
│  │     │  ├─ Center: (0, 4, 0) [centrado en casa]
│  │     │  └─ Layer: Default
│  │     │
│  │     ├─ Component: MeshRenderer
│  │     │  ├─ Mesh: House_Model (from asset)
│  │     │  └─ Material: Building_Diffuse (ladrillo/piedra)
│  │     │
│  │     ├─ Child: Roof [Mesh Component]
│  │     ├─ Child: Door [Visual]
│  │     ├─ Child: Windows [Visual]
│  │     │
│  │     ├─ Tag: "Houses"
│  │     └─ Layer: 0 (Default)
│  │
│  └─ Obstacles [Parent]
│     ├─ Tree_00 (BoxCollider)
│     ├─ Fence_00 (MeshCollider)
│     └─ etc.
│
├─ LAYER 2: AGENTS - VEHICLES
│  │
│  ├─ Vehicle_00 [Instantiated]
│  │  ├─ Transform
│  │  │  ├─ Position: Random en calle
│  │  │  ├─ Rotation: Aligned con calle
│  │  │  └─ Scale: (1, 1, 1)
│  │  │
│  │  ├─ Component: Rigidbody
│  │  │  ├─ Mass: 1000 kg (ficticio)
│  │  │  ├─ Drag: 0.1
│  │  │  ├─ Angular Drag: 0.05
│  │  │  ├─ Use Gravity: false [controlado por script]
│  │  │  ├─ Is Kinematic: false [pero movido por script]
│  │  │  ├─ Constraints:
│  │  │  │  ├─ Freeze Position Y: true [no levanta vuelo]
│  │  │  │  ├─ Freeze Rotation X: true
│  │  │  │  ├─ Freeze Rotation Z: true
│  │  │  │  └─ Freeze Rotation Y: false [puede girar]
│  │  │  ├─ Collision Detection: Continuous
│  │  │  └─ Rigidbody Interpolation: Interpolate
│  │  │
│  │  ├─ Component: CapsuleCollider
│  │  │  ├─ isTrigger: false
│  │  │  ├─ Radius: 0.8 m
│  │  │  ├─ Height: 1.8 m
│  │  │  └─ Center: (0, 0.9, 0) [al centro del vehículo]
│  │  │
│  │  ├─ Component: CarPatrol [Script - IA del auto]
│  │  │  ├─ Public var: moveSpeed: 10 m/s
│  │  │  ├─ Public var: rotationSpeed: 8 rad/s
│  │  │  ├─ Public var: maxTurnAngle: 60° [CRÍTICO]
│  │  │  ├─ Public var: waypointMemorySize: 8 [CRÍTICO]
│  │  │  ├─ Public var: detectionDistance: 5 m
│  │  │  ├─ Public var: antiTargetMargin: 1.0 m [dist a acera]
│  │  │  ├─ Private: Transform[] waypoints [auto-descubiertos]
│  │  │  ├─ Private: Transform[] antiTargets [auto-descubiertos]
│  │  │  ├─ Private: int currentIndex [waypoint actual]
│  │  │  ├─ Private: Queue<int> recentWaypoints
│  │  │  ├─ Private: float stuckTimer
│  │  │  ├─ Private: float reversingTimer
│  │  │  ├─ Private: int rutStuckCount
│  │  │  ├─ Method: Update()
│  │  │  ├─ Method: SelectNextWaypoint()
│  │  │  ├─ Method: IsPathClearToWaypoint(pos)
│  │  │  ├─ Method: GetDistanceToAntiTarget()
│  │  │  └─ Method: IsObstacleAhead()
│  │  │
│  │  ├─ Component: RVOAgentNavigator [Script - Evasión RVO2]
│  │  │  ├─ Public var: neighborDist: 15 m
│  │  │  ├─ Public var: maxNeighbors: 10
│  │  │  ├─ Public var: timeHorizon: 5 seg
│  │  │  ├─ Public var: timeHorizonObst: 5 seg
│  │  │  ├─ Public var: radius: 0.5 m
│  │  │  ├─ Public var: maxSpeed: 10 m/s
│  │  │  ├─ Public var: stoppingDistance: 0.5 m
│  │  │  ├─ Private: int rvoAgentId [ID en simulador]
│  │  │  ├─ Private: Rigidbody rb
│  │  │  ├─ Method: PrepareStep() [antes de RVO]
│  │  │  ├─ Method: ApplyRVOVelocity() [después de RVO]
│  │  │  ├─ Method: ComputePreferredVelocity()
│  │  │  └─ Method: OnDrawGizmos()
│  │  │
│  │  ├─ Component: CollisionDetector [Script]
│  │  │  ├─ onCollisionEnter: DataLogger.LogEvent(COLLISION)
│  │  │  └─ onTriggerEnter: [Detecta baches]
│  │  │
│  │  ├─ Child: Model_Vehicle [Mesh Visual]
│  │  │  ├─ MeshFilter: car_body_lod0.fbx
│  │  │  ├─ MeshRenderer: CarPaint_Blue
│  │  │  └─ Transform: (0, 0, 0)
│  │  │
│  │  ├─ Child: Lights [Visual]
│  │  │  ├─ Light_Front_Left
│  │  │  └─ Light_Front_Right
│  │  │
│  │  ├─ Tag: "Vehicle"
│  │  └─ Layer: 0 (Default)
│  │
│  ├─ Vehicle_01 [similar a Vehicle_00]
│  ├─ Vehicle_02
│  ├─ Vehicle_03
│  └─ Vehicle_04
│
├─ LAYER 3: AGENTS - PEDESTRIANS
│  │
│  ├─ Pedestrian_00 [Instantiated]
│  │  ├─ Transform
│  │  │  ├─ Position: Cerca de casa
│  │  │  ├─ Rotation: Facing hacia casa
│  │  │  └─ Scale: (1, 1, 1)
│  │  │
│  │  ├─ Component: Rigidbody
│  │  │  ├─ Mass: 80 kg
│  │  │  ├─ Drag: 0.3
│  │  │  ├─ Angular Drag: 0.5
│  │  │  ├─ Use Gravity: false [script]
│  │  │  ├─ Is Kinematic: false
│  │  │  ├─ Constraints:
│  │  │  │  ├─ Freeze Rotation X: true
│  │  │  │  ├─ Freeze Rotation Z: true
│  │  │  │  └─ Freeze Rotation Y: false
│  │  │  ├─ Collision Detection: Continuous
│  │  │  └─ Interpolation: Interpolate
│  │  │
│  │  ├─ Component: CapsuleCollider
│  │  │  ├─ isTrigger: false
│  │  │  ├─ Radius: 0.3 m
│  │  │  ├─ Height: 1.8 m
│  │  │  └─ Center: (0, 0.9, 0)
│  │  │
│  │  ├─ Component: RectangularPatrol [Script - IA patrulla]
│  │  │  ├─ Public var: targetHouse: Transform [auto-asignado]
│  │  │  ├─ Public var: paddingDistance: 2 m
│  │  │  ├─ Public var: moveSpeed: 5 m/s
│  │  │  ├─ Public var: rotationSmoothness: 0.1
│  │  │  ├─ Public var: clockwise: true
│  │  │  ├─ Public var: switchDistance: 20 m
│  │  │  ├─ Private: Vector3[] corners [4 puntos rectángulo]
│  │  │  ├─ Private: int currentCornerIndex
│  │  │  ├─ Private: float blockTargetSearchTimer
│  │  │  ├─ Private: float stuckTimer
│  │  │  ├─ Method: Update()
│  │  │  ├─ Method: CalculateCorners()
│  │  │  ├─ Method: TrySelectNextTarget()
│  │  │  ├─ Method: TryGetAvoidanceDir()
│  │  │  └─ Method: HasLineOfSightToTarget()
│  │  │
│  │  ├─ Component: RVOAgentController [Script - Evasión RVO2]
│  │  │  ├─ Public var: neighborDist: 15 m
│  │  │  ├─ Public var: maxNeighbors: 10
│  │  │  ├─ Public var: timeHorizon: 5 seg
│  │  │  ├─ Public var: timeHorizonObst: 2 seg
│  │  │  ├─ Public var: radius: 0.5 m
│  │  │  ├─ Public var: maxSpeed: 5 m/s
│  │  │  ├─ Private: int rvoAgentId
│  │  │  ├─ Method: UpdatePreferredVelocity()
│  │  │  ├─ Method: SyncPositionFromRVO()
│  │  │  └─ Method: OnDestroy()
│  │  │
│  │  ├─ Component: Animator
│  │  │  ├─ Avatar: humanoid_generic
│  │  │  ├─ Parameters:
│  │  │  │  ├─ float: Speed
│  │  │  │  ├─ bool: IsWalking
│  │  │  │  └─ bool: IsStunned
│  │  │  └─ Layers:
│  │  │     └─ Base Layer (locomotion)
│  │  │
│  │  ├─ Component: AudioSource
│  │  │  ├─ Clip: footstep_concrete
│  │  │  ├─ Volume: 0.5
│  │  │  └─ Spatial Blend: 1.0 (3D)
│  │  │
│  │  ├─ Child: Model_Pedestrian [Skinned Mesh]
│  │  │  ├─ SkinnedMeshRenderer: human_body
│  │  │  ├─ Animator (shared)
│  │  │  └─ Bones [Armature]
│  │  │     ├─ Hips
│  │  │     ├─ LeftLeg
│  │  │     ├─ RightLeg
│  │  │     ├─ Spine
│  │  │     ├─ LeftArm
│  │  │     └─ RightArm
│  │  │
│  │  ├─ Tag: "Pedestrian"
│  │  └─ Layer: 0 (Default)
│  │
│  ├─ Pedestrian_01 [similar]
│  └─ Pedestrian_02
│
├─ LAYER 4: NAVIGATION
│  │
│  ├─ Waypoints [Parent Empty]
│  │  ├─ Waypoint_0
│  │  │  ├─ Transform: Position (25, 0.5, -30)
│  │  │  ├─ Tag: "Waypoint"
│  │  │  └─ Layer: 0
│  │  │
│  │  ├─ Waypoint_1
│  │  ├─ Waypoint_2
│  │  ├─ Waypoint_3
│  │  └─ ... [auto-descubiertos por CarPatrol]
│  │
│  └─ NavMesh (baked)
│     └─ NavMesh-Default [asset file]
│        ├─ Agents: Car (1.0m radius)
│        ├─ Obstacles: Houses, Trees
│        └─ Bake Settings: Default
│
├─ LAYER 5: UI & HUD
│  │
│  ├─ Canvas [UI Root]
│  │  ├─ Render Mode: Screen Space - Overlay
│  │  ├─ Canvas Scaler: Scale Mode = Scale with Screen Size
│  │  │
│  │  ├─ Panel_Stats [UI Group]
│  │  │  ├─ RectTransform: (10, 10) top-left
│  │  │  ├─ LayoutGroup: Vertical
│  │  │  │
│  │  │  ├─ Text_FPS
│  │  │  │  ├─ Text: "FPS: 60.0"
│  │  │  │  ├─ Font: Arial
│  │  │  │  └─ Color: Green
│  │  │  │
│  │  │  ├─ Text_AgentCount
│  │  │  │  ├─ Text: "Vehicles: 5 | Pedestrians: 3"
│  │  │  │  └─ Color: White
│  │  │  │
│  │  │  ├─ Text_Speed
│  │  │  │  ├─ Text: "Speed: 1x"
│  │  │  │  └─ Color: Yellow
│  │  │  │
│  │  │  └─ Text_Time
│  │  │     ├─ Text: "Time: 00:05:23"
│  │  │     └─ Color: Cyan
│  │  │
│  │  ├─ Panel_Controls [UI Group]
│  │  │  ├─ RectTransform: right side
│  │  │  ├─ Button_Pause
│  │  │  │  ├─ Interactable: true
│  │  │  │  ├─ OnClick: ToggleActiveExclusive.TogglePause()
│  │  │  │  └─ Colors: Normal=Blue, Pressed=Cyan
│  │  │  │
│  │  │  ├─ Button_SpeedUp
│  │  │  │  ├─ OnClick: Time.timeScale *= 2
│  │  │  │  └─ Max: 8x
│  │  │  │
│  │  │  ├─ Button_SpeedDown
│  │  │  │  ├─ OnClick: Time.timeScale /= 2
│  │  │  │  └─ Min: 0.25x
│  │  │  │
│  │  │  └─ Button_Menu
│  │  │     ├─ OnClick: SceneManager.LoadScene("Mode_Menu")
│  │  │     └─ Colors: Red when hovered
│  │  │
│  │  ├─ Slider_Speed
│  │  │  ├─ Min Value: 0
│  │  │  ├─ Max Value: 8
│  │  │  ├─ Value: 1
│  │  │  ├─ OnValueChanged: SetTimeScale()
│  │  │  └─ Handle: draggable
│  │  │
│  │  ├─ Panel_Logs [UI Scroll View]
│  │  │  ├─ ScrollRect component
│  │  │  ├─ Content: VerticalLayoutGroup
│  │  │  │
│  │  │  └─ Text_Log_00 through Text_Log_09
│  │  │     ├─ Text: event logs
│  │  │     ├─ Font Size: 12
│  │  │     └─ Color: Color-coded by type
│  │  │
│  │  └─ Minimap [Panel]
│  │     ├─ RectTransform: bottom-right corner
│  │     ├─ RawImage: renders minimap camera
│  │     └─ Size: (256, 256) pixels
│  │
│  └─ GraphicRaycaster [component]
│
└─ LAYER 6: CAMERAS
   │
   ├─ Main Camera
   │  ├─ Tag: "MainCamera"
   │  ├─ Camera component
   │  │  ├─ Projection: Perspective
   │  │  ├─ FOV: 60°
   │  │  ├─ Near Clip: 0.3 m
   │  │  ├─ Far Clip: 1000 m
   │  │  └─ Rendering Path: Forward
   │  │
   │  ├─ AudioListener
   │  │  └─ (para sonido 3D)
   │  │
   │  ├─ Transform: Position (0, 20, 0) [vista aérea]
   │  └─ Cinemachine Brain [component]
   │     └─ Live Virtual Camera: VirtualCamera_Follow
   │
   ├─ VirtualCamera_Follow [Cinemachine]
   │  ├─ Component: CinemachineVirtualCamera
   │  │  ├─ Priority: 10
   │  │  ├─ Follow Target: (auto selecciona vehículo)
   │  │  ├─ Look At Target: (mismo que follow)
   │  │  │
   │  │  ├─ Body: Framing Transposer
   │  │  │  ├─ Distance: 10 m
   │  │  │  ├─ Damping: 0.3
   │  │  │  └─ Offset: (0, 3, 0)
   │  │  │
   │  │  └─ Lens: Orthographic (false)
   │  │     └─ FOV: 45°
   │  │
   │  ├─ Transform: (controlled by Cinemachine)
   │  └─ Tag: "CinemachineCamera"
   │
   └─ Minimap_Camera
      ├─ Component: Camera
      │  ├─ Projection: Orthographic
      │  ├─ Size: 100 m (cubre toda la escena)
      │  ├─ Near Clip: 0.1 m
      │  ├─ Far Clip: 100 m
      │  ├─ Rendering Path: Forward
      │  └─ Target Texture: Minimap_RT (render target)
      │
      ├─ Transform: Position (0, 50, 0)
      ├─ Rotation: (90, 0, 0) [mira hacia abajo]
      └─ Layer Mask: Everything except UI
```

---

## 🏷️ TAGS UTILIZADOS

| Tag | Asignado a | Uso |
|-----|-----------|-----|
| `"Waypoint"` | Empty GameObjects en intersecciones | CarPatrol auto-descubre puntos de ruta |
| `"Acera"` | BoxColliders en bordes | CarPatrol evita subirse |
| `"Houses"` | Edificios con BoxCollider | RectangularPatrol selecciona objetivos |
| `"bache"` | SphereColliders (trigger) | DetectionTrigger registra impactos |
| `"Vehicle"` | Todos los autos | Para queries globales |
| `"Pedestrian"` | Todos los peatones | Para queries globales |
| `"MainCamera"` | Main Camera | Acceso rápido por tag |

---

## 🎨 LAYERS UTILIZADOS

| Layer # | Nombre | Propósito | Collisions |
|---------|--------|----------|-----------|
| 0 | Default | Vehículos, peatones, obstáculos | Sí |
| 1 | TransparentFX | (reservado por Unity) | - |
| 2 | Ignore Raycast | Aceras, UI | Raycasts: No |
| 3 | Water | (no usado) | - |
| 4 | UI | Canvas, elementos UI | - |
| 5-7 | (custom) | Opcional para expansión | - |

---

## ⚙️ COMPONENTES CRÍTICOS POR TIPO

### CarPatrol Component (Vehículos)

```
┌─ CarPatrol.cs
│
├─ PUBLIC CONFIGURATION
│  ├─ moveSpeed: 10 m/s
│  ├─ rotationSpeed: 8 rad/s
│  ├─ maxTurnAngle: 60° [CRÍTICO - evita vueltas locas]
│  ├─ waypointMemorySize: 8 [CRÍTICO - evita rebotes]
│  ├─ detectionDistance: 5 m
│  ├─ antiTargetMargin: 1.0 m [distancia a aceras]
│  ├─ maxWaitTime: 2 seg [timeout ante obstáculos]
│  ├─ debugWaypointSelection: bool
│  └─ useRandomWaypoints: true
│
├─ AUTO-DISCOVERED (en Start)
│  ├─ waypoints: Transform[] → GameObject con tag "Waypoint"
│  └─ antiTargets: Transform[] → tag "Acera" + "Houses"
│
├─ INTERNAL STATE
│  ├─ currentIndex: int
│  ├─ recentWaypoints: Queue<int>
│  ├─ stuckTimer: float
│  ├─ reversingTimer: float
│  ├─ rutStuckCount: int
│  └─ smoothDir: Vector3
│
└─ KEY METHODS
   ├─ Update(): Main loop lógica
   ├─ SelectNextWaypoint(): Elegir siguiente ruta
   ├─ IsPathClearToWaypoint(): SphereCast check
   ├─ GetDistanceToAntiTarget(): raycast busca aceras
   └─ IsObstacleAhead(): raycast frontal
```

### RectangularPatrol Component (Peatones)

```
┌─ RectangularPatrol.cs
│
├─ PUBLIC CONFIGURATION
│  ├─ targetHouse: Transform [auto-asignado]
│  ├─ paddingDistance: 2 m
│  ├─ moveSpeed: 5 m/s
│  ├─ rotationSmoothness: 0.1
│  ├─ clockwise: true
│  ├─ switchDistance: 20 m
│  ├─ minPatrolTime: 5 seg
│  ├─ maxPatrolTime: 10 seg
│  ├─ avoidObstacles: true
│  └─ debugTargetSelection: bool
│
├─ INTERNAL STATE
│  ├─ corners: Vector3[4] [esquinas rectángulo]
│  ├─ currentCornerIndex: int
│  ├─ stuckTimer: float
│  ├─ yieldTimer: float [espera a otros peatones]
│  ├─ blockTargetSearchTimer: float
│  └─ smoothDir: Vector3
│
├─ CONSTANTS
│  ├─ YIELD_TIME: 1.5 seg [tiempo cede paso]
│  ├─ STUCK_DIST: 0.05 m
│  ├─ STUCK_THRESHOLD: 1.5 seg
│  └─ MUTUAL_BLOCK_THRESHOLD: 2.0 seg
│
└─ KEY METHODS
   ├─ Update(): Main loop
   ├─ CalculateCorners(): Calcula rectángulo alrededor casa
   ├─ TrySelectNextTarget(): Cambiar objetivo
   ├─ TryGetAvoidanceDir(): Evasión de obstáculos
   └─ HasLineOfSightToTarget(): SphereCast check
```

### RVOAgentNavigator Component

```
┌─ RVOAgentNavigator.cs (para vehículos)
│
├─ PUBLIC CONFIGURATION
│  ├─ neighborDist: 15 m
│  ├─ maxNeighbors: 10
│  ├─ timeHorizon: 5 seg
│  ├─ timeHorizonObst: 5 seg
│  ├─ radius: 0.5 m [radio físico]
│  ├─ maxSpeed: 10 m/s
│  ├─ stoppingDistance: 0.5 m
│  └─ drawDebugGizmos: bool
│
├─ REGISTRATION
│  ├─ Start(): Registra en RVOSimulationManager
│  ├─ rvoAgentId: int [ID único en simulador]
│  └─ isRegistered: bool
│
├─ RVO INTEGRATION
│  ├─ PrepareStep() [FixedUpdate 1]
│  │  ├─ Actualiza posición en RVO
│  │  └─ Calcula velocidad preferida
│  │
│  └─ ApplyRVOVelocity() [FixedUpdate 2]
│     ├─ Obtiene velocidad de RVO
│     └─ Aplica a Rigidbody.MovePosition()
│
└─ TARGET TRACKING
   └─ SetTarget(transform): Manual control
```

---

## 📋 TABLA DE ASIGNACIÓN DE SCRIPTS

| Script | Asignado a | Purpose | Crítico |
|--------|-----------|---------|---------|
| CarPatrol | Vehicle_XX | Lógica de vehículos | ✅ SÍ |
| RVOAgentNavigator | Vehicle_XX | Evasión RVO | ✅ SÍ |
| RectangularPatrol | Pedestrian_XX | Patrulla peatones | ✅ SÍ |
| RVOAgentController | Pedestrian_XX | Evasión RVO | ✅ SÍ |
| Animator | Pedestrian_XX | Animaciones | ❌ NO |
| SceneInitializer | SceneInitializer | Orquestación | ✅ SÍ |
| GeneradorDeCalle | GeneradorDeCalle | Generación calles | ✅ SÍ |
| TerrainPotholeGenerator | (auto) | Generación baches | ✅ SÍ |
| RVOSimulationManager | (singleton) | Gestor RVO | ✅ SÍ |
| DataLogger | DataLogger | Registra eventos | ❌ NO |
| ToggleActiveExclusive | ToggleActiveExclusive | Gestor lógica juego | ❌ NO |
| DetectionTrigger | Pothole_XX | Detecta impactos | ✅ SÍ |
| CollisionDetector | Vehicle_XX | Detecta colisiones | ❌ NO |

---

## 🔍 Inspeccionando un Vehicle en el Editor

```
Vehicle_00 (GameObject)
│
├─ ✓ Transform
│  ├─ Position: (45.3, 0.5, -23.7)
│  ├─ Rotation: (0, 45, 0)
│  └─ Scale: (1, 1, 1)
│
├─ ✓ Rigidbody
│  ├─ Mass: 1000
│  ├─ Drag: 0.1
│  ├─ Constraints: Y=Frozen, RxRyRz=Frozen, Ry=Free
│  └─ Collision Detection: Continuous
│
├─ ✓ CapsuleCollider
│  ├─ Radius: 0.8
│  ├─ Height: 1.8
│  └─ Center: (0, 0.9, 0)
│
├─ ✓ CarPatrol
│  ├─ Move Speed: 10
│  ├─ Rotation Speed: 8
│  ├─ Max Turn Angle: 60 [slider visual]
│  ├─ Waypoint Memory Size: 8
│  ├─ Detection Distance: 5
│  ├─ Anti-Target Margin: 1
│  ├─ Max Wait Time: 2
│  ├─ Debug Waypoint Selection: ☐ [checkbox]
│  └─ Use Random Waypoints: ☑ [checkbox]
│
├─ ✓ RVOAgentNavigator
│  ├─ Neighbor Dist: 15
│  ├─ Max Neighbors: 10
│  ├─ Time Horizon: 5
│  ├─ Time Horizon Obst: 5
│  ├─ Radius: 0.5
│  ├─ Max Speed: 10
│  ├─ Stopping Distance: 0.5
│  ├─ Draw Debug Gizmos: ☐
│  └─ Target: None (auto-managed)
│
├─ ✓ CollisionDetector
│  └─ On Collision Enter: [event]
│
└─ ▶ Model_Vehicle [Child]
   ├─ Transform: (0, 0, 0)
   ├─ MeshFilter: car_body_lod0
   ├─ MeshRenderer: CarPaint_Blue
   └─ MeshCollider: (disabled, visual only)
```

---

## 🔍 Inspeccionando un Pedestrian en el Editor

```
Pedestrian_00 (GameObject)
│
├─ ✓ Transform
│  ├─ Position: (50.1, 0, -25.3)
│  ├─ Rotation: (0, 135, 0)
│  └─ Scale: (1, 1, 1)
│
├─ ✓ Rigidbody
│  ├─ Mass: 80
│  ├─ Drag: 0.3
│  ├─ Constraints: RxRyRz=Frozen, Ry=Free
│  └─ Collision Detection: Continuous
│
├─ ✓ CapsuleCollider
│  ├─ Radius: 0.3
│  ├─ Height: 1.8
│  └─ Center: (0, 0.9, 0)
│
├─ ✓ RectangularPatrol
│  ├─ Target House: House_3 [assigned]
│  ├─ Padding Distance: 2
│  ├─ Move Speed: 5
│  ├─ Rotation Smoothness: 0.1
│  ├─ Clockwise: ☑
│  ├─ Switch Distance: 20
│  ├─ Min Patrol Time: 5
│  ├─ Max Patrol Time: 10
│  ├─ Avoid Obstacles: ☑
│  ├─ Avoidance Distance: 1.2
│  └─ Debug Target Selection: ☐
│
├─ ✓ RVOAgentController
│  ├─ Neighbor Dist: 15
│  ├─ Max Neighbors: 10
│  ├─ Time Horizon: 5
│  ├─ Radius: 0.5
│  └─ Max Speed: 5
│
├─ ✓ Animator
│  ├─ Avatar: humanoid_generic [assigned]
│  ├─ Apply Root Motion: ☐
│  └─ Culling Mode: Always Animate
│
├─ ✓ AudioSource
│  ├─ Audio Clip: footstep_concrete
│  ├─ Volume: 0.5
│  ├─ Pitch: 1
│  └─ Spatial Blend: 1 (3D)
│
└─ ▶ Model_Pedestrian [Child - Skinned Mesh]
   ├─ Transform: (0, 0, 0)
   ├─ SkinnedMeshRenderer: human_body
   ├─ Avatar: humanoid_generic
   └─ Bones: Armature with IK
```

---

**FIN DEL DOCUMENTO**

*Documento con referencias a componentes Unity, propiedades editables y estructura interna*
