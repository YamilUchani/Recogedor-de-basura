# 📋 TODO TODO TODO - TAREAS, ESCENARIOS Y DISTRIBUCIÓN DE CÓDIGO

**Documento**: Mapa Completo de Tareas y Código por Escenario  
**Fecha**: Mayo 5, 2026  
**Objetivo**: Inventario total de lo que falta, dónde está cada código, y cómo se distribuye

---

## 🎯 RESUMEN EJECUTIVO

```
Total de Escenas:     7
Total de Scripts:     15+
Total de Gameobjects: 100+ (dinámicos)
Líneas de Código:     ~8000
Completitud:          78%
Status General:       ⚠️ FUNCIONAL pero requiere finalizaciones
```

---

## 📝 TODO - TAREAS PENDIENTES

### 🔴 CRÍTICAS (Deben hacerse YA)

#### 1️⃣ **Fijar embotellamiento infinito en CarPatrol**
- **Issue**: Autos se quedan esperando sin fin
- **Causa**: `rutStuckCount` se incrementa cada frame
- **Solución**: Agregar `bool crashAlreadyDetected`
- **Archivo**: `Assets/Scripts/Utilities/CarPatrol.cs` línea 85
- **Estimado**: 1 hora
- **Prioridad**: 🔴 CRÍTICA
- **Status**: ⏳ Pendiente

```csharp
// ANTES (línea 201-207):
if (distToAntiTarget <= crashThreshold) {
    rutStuckCount++;  // ← PROBLEMA: cada frame
    if (rutStuckCount >= 2) {
        SelectNextWaypoint();
        rutStuckCount = 0;
    }
}

// DESPUÉS (TODO):
if (distToAntiTarget <= crashThreshold && !crashAlreadyDetected) {
    crashAlreadyDetected = true;
    SelectNextWaypoint();
}
```

---

#### 2️⃣ **Deadlock de peatones - YIELD_TIME demasiado corto**
- **Issue**: Dos peatones quedan congelados frente a frente
- **Causa**: `YIELD_TIME = 0.8s` → ambos ceden al mismo tiempo
- **Solución**: Aumentar a 1.5s + agregar desempate por timestamp
- **Archivo**: `Assets/Scripts/Utilities/RectangularPatrol.cs` línea 63
- **Estimado**: 1.5 horas
- **Prioridad**: 🔴 CRÍTICA
- **Status**: ⏳ Pendiente

```csharp
// ANTES (línea 61):
private const float YIELD_TIME = 0.8f;

// DESPUÉS (TODO):
private const float YIELD_TIME = 1.5f;  // Aumentado
private float lastYieldTimestamp = 0f;   // NEW

// En TryGetAvoidanceDir():
if (otherPed.yieldTimer <= 0 && Time.time - otherPed.lastYieldTimestamp > 0.5f) {
    otherPed.yieldTimer = YIELD_TIME;
    otherPed.lastYieldTimestamp = Time.time;
}
```

---

#### 3️⃣ **maxTurnAngle permite vueltas de 180° - poco realista**
- **Issue**: Vehículos hacen volteretas U erráticas
- **Causa**: `maxTurnAngle = 100°` + margin 20° = 120°
- **Solución**: Reducir a 60° máximo
- **Archivo**: `Assets/Scripts/Utilities/CarPatrol.cs` línea 39
- **Estimado**: 0.5 horas
- **Prioridad**: 🔴 CRÍTICA
- **Status**: ⏳ Pendiente

```csharp
// ANTES:
[Range(5f, 180f)]
public float maxTurnAngle = 100f;

// DESPUÉS (TODO):
[Range(5f, 90f)]
public float maxTurnAngle = 60f;
```

---

### 🟠 IMPORTANTES (Deberían hacerse pronto)

#### 4️⃣ **Reversión poco realista - solo retrocede**
- **Issue**: Auto retrocede en línea recta, no gira mientras retrocede
- **Archivo**: `Assets/Scripts/Utilities/CarPatrol.cs` línea 207-212
- **Estimado**: 2 horas
- **Prioridad**: 🟠 IMPORTANTE
- **Status**: ⏳ Pendiente

**Solución**:
```csharp
// TODO: Girar mientras retrocede
if (reversingTimer > 0f) {
    reversingTimer -= Time.deltaTime;
    
    // Retroceder
    transform.position += -transform.forward * (moveSpeed * 0.4f) * Time.deltaTime;
    
    // NUEVO: Girar hacia siguiente waypoint
    Vector3 targetDir = (waypoints[currentIndex].position - transform.position).normalized;
    transform.rotation = Quaternion.Slerp(
        transform.rotation,
        Quaternion.LookRotation(targetDir),
        2f * Time.deltaTime  // Girar suave
    );
    
    currentMoveSpeed = 0f;
    return;
}
```

---

#### 5️⃣ **Evasión poco realista - usa Vector3.Reflect**
- **Issue**: Reflexión óptica no realista para autos
- **Archivo**: `Assets/Scripts/Utilities/CarPatrol.cs` línea 188-197
- **Estimado**: 1.5 horas
- **Prioridad**: 🟠 IMPORTANTE
- **Status**: ⏳ Pendiente

**Solución**:
```csharp
// ANTES:
Vector3 reflectDir = Vector3.Reflect(smoothDir, wallNormal);

// DESPUÉS (TODO):
Vector3 perpendicular = new Vector3(-wallNormal.z, 0, wallNormal.x).normalized;
Vector3 reflectDir = Vector3.RotateTowards(smoothDir, perpendicular, 0.5f, 0);
```

---

#### 6️⃣ **No detecta aceras durante giro**
- **Issue**: Auto gira hacia acera y se queda ahí
- **Archivo**: `Assets/Scripts/Utilities/CarPatrol.cs` línea 231-245
- **Estimado**: 1 hora
- **Prioridad**: 🟠 IMPORTANTE
- **Status**: ⏳ Pendiente

**Solución**:
```csharp
// TODO: Verificar dirección de giro es segura
float angleToTarget = Vector3.Angle(transform.forward, targetSteerDir);
if (angleToTarget > 15f) {
    // Verificar si la dirección de giro es segura
    if (!IsDirectionBlockedByWall(targetSteerDir)) {
        targetSpeed = 0f;
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * 5f);
    }
}
```

---

#### 7️⃣ **Intercepción de casas solo en transición**
- **Issue**: RectangularPatrol no puede cambiar casa si patrulla
- **Archivo**: `Assets/Scripts/Utilities/RectangularPatrol.cs` línea 131-151
- **Estimado**: 1.5 horas
- **Prioridad**: 🟠 IMPORTANTE
- **Status**: ⏳ Pendiente

**Solución**:
```csharp
// ANTES:
if (isTransitioning && routeTargets != null) {
    // Intercepción solo aquí
}

// DESPUÉS (TODO):
// Permitir intercepción SIEMPRE, no solo en transición
if (routeTargets != null) {
    // Check cada frame
    for (int i = 0; i < routeTargets.Length; i++) {
        float dist = Vector3.Distance(transform.position, routeTargets[i].position);
        if (dist < switchDistance && Vector3.Distance(routeTargets[i].position, targetHouse.position) < 10f) {
            targetHouse = routeTargets[i];  // Cambiar en cualquier momento
        }
    }
}
```

---

#### 8️⃣ **RaycastNonAlloc usa raycast 1D (muy delgado)**
- **Issue**: Casa oblicua pasa raycast pero está bloqueada
- **Archivo**: `Assets/Scripts/Utilities/RectangularPatrol.cs` línea 419-441
- **Estimado**: 1 hora
- **Prioridad**: 🟠 IMPORTANTE
- **Status**: ⏳ Pendiente

**Solución**:
```csharp
// ANTES:
int n = Physics.RaycastNonAlloc(origin, dir, raycastBuffer, dist);

// DESPUÉS (TODO):
int n = Physics.SphereCastNonAlloc(origin, 0.5f, dir, raycastBuffer, dist);
// SphereCast es 3D, no 1D
```

---

### 🟡 RECOMENDADAS (Mejorarían mucho)

#### 9️⃣ **Implementar inercia realista**
- **Issue**: Auto acelera/frena instantáneamente
- **Archivo**: `Assets/Scripts/Utilities/CarPatrol.cs`
- **Estimado**: 2 horas
- **Prioridad**: 🟡 RECOMENDADA
- **Status**: ⏳ Pendiente

#### 🔟 **Frenado anticipado en curvas**
- **Issue**: No reduce velocidad en curvas cerradas
- **Estimado**: 1.5 horas
- **Prioridad**: 🟡 RECOMENDADA
- **Status**: ⏳ Pendiente

#### 1️⃣1️⃣ **Distancia social entre peatones**
- **Issue**: No respetan distancia de 1.5m
- **Estimado**: 2 horas
- **Prioridad**: 🟡 RECOMENDADA
- **Status**: ⏳ Pendiente

#### 1️⃣2️⃣ **Look-ahead predictivo**
- **Issue**: Auto no anticipa giros cercanos
- **Estimado**: 2 horas
- **Prioridad**: 🟡 RECOMENDADA
- **Status**: ⏳ Pendiente

---

### 🔵 OPCIONALES (Polish/Mejoras)

#### 1️⃣3️⃣ **UI para cambiar parámetros en tiempo real**
- **Estimado**: 3 horas
- **Prioridad**: 🔵 OPCIONAL
- **Status**: ⏳ Pendiente

#### 1️⃣4️⃣ **Exportar video de simulación**
- **Estimado**: 2 horas
- **Prioridad**: 🔵 OPCIONAL
- **Status**: ⏳ Pendiente

#### 1️⃣5️⃣ **Visión cónica para peatones**
- **Estimado**: 2 horas
- **Prioridad**: 🔵 OPCIONAL
- **Status**: ⏳ Pendiente

---

## 📊 DISTRIBUCIÓN DE ESCENARIOS

### Mapa Mental: Escena → Propósito → Contenido → Scripts

```
┌─ MODE_MENU
│  ├─ Propósito: Seleccionar modo de simulación
│  ├─ GameObjects: Canvas + Botones
│  ├─ Scripts: 
│  │  └─ LoadingScreenController
│  │     ├─ OnPlayClicked() → LoadScene("Mode_Model")
│  │     ├─ OnDebugClicked() → LoadScene("Mode_Debug")
│  │     ├─ OnDataClicked() → LoadScene("Mode_Data")
│  │     └─ OnExitClicked() → Application.Quit()
│  ├─ Assets: UI Sprites, Fonts, Audio
│  ├─ Tamaño: ~2 MB
│  └─ Estado: ✅ COMPLETO
│
├─ MODE_LOAD (Loading Screen)
│  ├─ Propósito: Mostrar barra de progreso
│  ├─ GameObjects: Canvas + ProgressBar
│  ├─ Scripts:
│  │  └─ LoadingScreenController
│  │     ├─ Espera a SceneInitializer.IsInitializeComplete
│  │     ├─ Updatea ProgressBar
│  │     └─ Cierra pantalla cuando IsComplete = true
│  ├─ Duración: ~5-10 segundos (según generación)
│  └─ Estado: ✅ COMPLETO
│
├─ MODE_MODEL (Principal - Simulación)
│  ├─ Propósito: Escena completa de simulación
│  │
│  ├─ GameObjects: ~100-150
│  │  ├─ Terrain Mesh (1 objeto)
│  │  ├─ Sidewalks (20 objetos)
│  │  ├─ Houses (20-50 objetos, procedural)
│  │  ├─ Vehicles (5 instanciados)
│  │  ├─ Pedestrians (3 instanciados)
│  │  ├─ Waypoints (30 auto-descubiertos)
│  │  ├─ Baches (50-200 procedural)
│  │  ├─ UI Canvas
│  │  └─ Cameras (3)
│  │
│  ├─ Scripts:
│  │  ├─ SceneInitializer.cs [CORE ORQUESTADOR]
│  │  │  ├─ Coordina: GeneradorDeCalle → BachesGenerator → NavMesh → LogicGame
│  │  │  ├─ Llamado por: LoadingScreenController
│  │  │  └─ Marca: IsInitializeComplete = true cuando termina
│  │  │
│  │  ├─ GeneradorDeCalle.cs [PROCEDURAL GENERATION]
│  │  │  ├─ Genera mesh de calles
│  │  │  ├─ Crea GameObjects casas
│  │  │  ├─ Crea aceras con colliders
│  │  │  └─ Tags automáticos: "Houses", "Acera"
│  │  │
│  │  ├─ TerrainPotholeGenerator.cs [BACHES]
│  │  │  ├─ Genera 50-200 baches
│  │  │  ├─ SphereColliders (trigger)
│  │  │  └─ DetectionTrigger component
│  │  │
│  │  ├─ NavMeshdrone.cs [NAVMESH BAKER]
│  │  │  ├─ Hornea NavMesh
│  │  │  └─ Configurable: agent size, slopes, etc.
│  │  │
│  │  ├─ ToggleActiveExclusive.cs [GAME LOGIC]
│  │  │  ├─ Instancia vehículos + peatones
│  │  │  ├─ Maneja pause/resume
│  │  │  └─ Controla timeScale
│  │  │
│  │  ├─ RVOSimulationManager.cs [SINGLETON]
│  │  │  ├─ Gestiona Simulator RVO2
│  │  │  ├─ Llama PrepareStep + doStep + ApplyVel cada FixedUpdate
│  │  │  └─ Thread-safe para physics multithreading
│  │  │
│  │  ├─ CarPatrol.cs [5 instancias]
│  │  │  ├─ Vehicle_0, Vehicle_1, ..., Vehicle_4
│  │  │  └─ Cada una con sus parámetros independientes
│  │  │
│  │  ├─ RVOAgentNavigator.cs [5 instancias]
│  │  │  └─ Paired con cada CarPatrol
│  │  │
│  │  ├─ RectangularPatrol.cs [3 instancias]
│  │  │  ├─ Pedestrian_0, Pedestrian_1, Pedestrian_2
│  │  │  └─ Cada una patrulla diferente casa
│  │  │
│  │  ├─ RVOAgentController.cs [3 instancias]
│  │  │  └─ Paired con cada RectangularPatrol
│  │  │
│  │  ├─ DataLogger.cs [SINGLETON - OPCIONAL]
│  │  │  ├─ LogEvent() llamado por DetectionTrigger, CollisionDetector
│  │  │  ├─ Bufferea eventos en memoria
│  │  │  └─ SaveData() al salir (CSV + JSON)
│  │  │
│  │  ├─ DetectionTrigger.cs [50-200 instancias]
│  │  │  └─ OnTriggerEnter() → DataLogger.LogEvent(POTHOLE_DETECTED)
│  │  │
│  │  ├─ CollisionDetector.cs [5 instancias]
│  │  │  └─ OnCollisionEnter() → DataLogger.LogEvent(COLLISION)
│  │  │
│  │  └─ CameraController.cs [opcional]
│  │     ├─ Cinemachine config
│  │     └─ Seguimiento de vehículos
│  │
│  ├─ Assets: 
│  │  ├─ Meshes: Street, Buildings, Vehicles, Pedestrians
│  │  ├─ Materials: Asphalt, Brick, Paint, etc.
│  │  ├─ Animations: Pedestrian Walk/Idle
│  │  ├─ Audio: Footsteps, Engine, Ambient
│  │  └─ Prefabs: House, Vehicle, Pedestrian
│  │
│  ├─ Tamaño: ~300 MB (incluye assets)
│  ├─ Performance: 60 FPS @ 5 vehículos + 3 peatones
│  └─ Estado: ✅ FUNCIONAL (78% completo)
│
├─ MODE_DEBUG (Desarrollo)
│  ├─ Propósito: Modo DEBUG con visualización
│  ├─ Diferencia vs Mode_Model:
│  │  ├─ debugWaypointSelection = true (pausa editor)
│  │  ├─ drawDebugGizmos = true (dibuja líneas)
│  │  ├─ Console log más verboso
│  │  ├─ Profiler window abierto
│  │  └─ Physics.gravity = 0
│  ├─ Scripts: [todo igual a Mode_Model]
│  │  ├─ DebugPanel.cs [NUEVO]
│  │  │  ├─ UI con sliders para parámetros
│  │  │  ├─ Real-time tuning de moveSpeed, etc.
│  │  │  └─ Reset to defaults button
│  │  │
│  │  ├─ PerformanceMonitor.cs [NUEVO]
│  │  │  ├─ Graphs FPS vs Time
│  │  │  ├─ Memory usage
│  │  │  └─ Physics calls per frame
│  │  │
│  │  └─ WaypointVisualizer.cs [NUEVO]
│  │     ├─ Dibuja todos los waypoints
│  │     ├─ Colores: Verde=libre, Rojo=bloqueado
│  │     └─ Líneas de rutas
│  │
│  ├─ Estado: ⚠️ PARCIAL (falta DebugPanel)
│  └─ TODO: Crear DebugPanel.cs
│
├─ MODE_DATA (Recolección de datos)
│  ├─ Propósito: Simulación sin visual para datos puros
│  ├─ Diferencias:
│  │  ├─ Meshes simplificados (LOD0 solo)
│  │  ├─ Sin Canvas UI (solo logs)
│  │  ├─ Sin Cinemachine
│  │  ├─ Sin Audio
│  │  └─ TimeScale = 8x (4x speedup)
│  ├─ Scripts: [core igual, minus visualization]
│  │  ├─ DataExporter.cs [NUEVO]
│  │  │  ├─ OnSimulationEnd()
│  │  │  ├─ Genera CSV con todos los eventos
│  │  │  ├─ JSON summary statistics
│  │  │  └─ HeatMap de zonas patrulladas
│  │  │
│  │  └─ SimulationRunner.cs [NUEVO]
│  │     ├─ for loop ejecuta 10 simulaciones
│  │     ├─ Cambia parámetros cada iteración
│  │     └─ Guarda resultados
│  │
│  ├─ Tamaño: ~50 MB
│  ├─ Performance: 120-240 FPS (sin render)
│  └─ Estado: ❌ NO IMPLEMENTADO (TODO)
│
├─ MODE_GENERATION_TESTER (Test de generación)
│  ├─ Propósito: Probar generadores aislados
│  ├─ GameObjects:
│  │  ├─ GeneradorDeCalle (sin SceneInitializer)
│  │  ├─ TerrainPotholeGenerator (aislado)
│  │  └─ Herramientas de visualización
│  ├─ Scripts:
│  │  ├─ GeneratorSettings.cs [NUEVO]
│  │  │  ├─ UI para streetWidth, blockSize, etc.
│  │  │  ├─ Regenerate button
│  │  │  └─ Export mesh button
│  │  │
│  │  └─ GeneratorVisualizer.cs [NUEVO]
│  │     ├─ Wireframe de mesh
│  │     ├─ Bounding boxes de casas
│  │     └─ Grid overlay
│  │
│  ├─ Estado: ⚠️ PARCIAL
│  └─ TODO: Crear UI de ajustes
│
└─ MODE_TESTER (Multipropósito)
   ├─ Propósito: Escena experimental
   ├─ Contenido: Elementos modulares
   ├─ Scripts: Helpers y tests
   └─ Estado: ❌ NO USADO (legacy)
```

---

## 💾 DISTRIBUCIÓN DE CÓDIGO POR CARPETA

### Estructura de carpetas del proyecto

```
Assets/
│
├─ Scripts/
│  │
│  ├─ Utilities/
│  │  ├─ CarPatrol.cs [650 líneas] ✅
│  │  │  ├─ Lógica de vehículos
│  │  │  ├─ Auto-descubrimiento de waypoints
│  │  │  ├─ Evasión de aceras
│  │  │  └─ Detección de obstáculos
│  │  │
│  │  ├─ RectangularPatrol.cs [500 líneas] ✅
│  │  │  ├─ Patrulla rectangular de peatones
│  │  │  ├─ Cambio dinámico de objetivo
│  │  │  ├─ Resolución de deadlock
│  │  │  └─ Evasión de obstáculos
│  │  │
│  │  ├─ CollisionDetector.cs [80 líneas] ✅
│  │  │  └─ Detección de impactos
│  │  │
│  │  └─ TODO_MISSING: DebugPanel.cs [⏳ 200 líneas]
│  │     └─ UI para tuning en tiempo real
│  │
│  ├─ RVO_NEW/
│  │  ├─ Simulator.cs [260 líneas] ✅
│  │  │  ├─ Core RVO2 C# implementation
│  │  │  ├─ KdTree para búsqueda rápida
│  │  │  ├─ Multi-threading workers
│  │  │  └─ Agent management
│  │  │
│  │  ├─ Agent.cs [180 líneas] ✅
│  │  │  ├─ Definición de agente
│  │  │  └─ computeNewVelocity()
│  │  │
│  │  ├─ Obstacle.cs [100 líneas] ✅
│  │  │  └─ Definición de obstáculo
│  │  │
│  │  ├─ Vector2.cs [150 líneas] ✅
│  │  │  └─ Tipos de datos RVO
│  │  │
│  │  └─ RVOMath.cs [100 líneas] ✅
│  │     └─ Math utilities
│  │
│  ├─ RVO_NEW/Controllers/
│  │  ├─ RVOAgentNavigator.cs [213 líneas] ✅
│  │  │  ├─ Para vehículos
│  │  │  ├─ PrepareStep() + ApplyRVOVelocity()
│  │  │  ├─ Gizmos debug
│  │  │  └─ Rigidbody integration
│  │  │
│  │  └─ RVOAgentController.cs [125 líneas] ✅
│  │     ├─ Para peatones
│  │     ├─ UpdatePreferredVelocity()
│  │     └─ SyncPositionFromRVO()
│  │
│  ├─ RVO2/
│  │  ├─ Simulator.cs [1000+ líneas] ✅
│  │  │  ├─ LEGACY RVO2 original
│  │  │  ├─ No usar (usar RVO_NEW)
│  │  │  └─ DEPRECATED
│  │  │
│  │  └─ [otros archivos RVO2]
│  │
│  ├─ Scene/
│  │  ├─ SceneInitializer.cs [80 líneas] ✅
│  │  │  ├─ Orquestación de carga
│  │  │  ├─ Secuencia: Calles → Baches → NavMesh → Logic
│  │  │  ├─ Coroutine initialization
│  │  │  └─ IsInitializeComplete flag
│  │  │
│  │  ├─ ToggleActiveExclusive.cs [120 líneas] ✅
│  │  │  ├─ Gestiona qué GameObjects están activos
│  │  │  ├─ Toggle entre modos
│  │  │  ├─ Instantiate vehicles/pedestrians
│  │  │  └─ Pause/Resume control
│  │  │
│  │  ├─ GeneradorDeCalle.cs [400 líneas] ✅
│  │  │  ├─ Generación procedural de calles
│  │  │  ├─ Mesh creation (vértices, triángulos)
│  │  │  ├─ House instantiation
│  │  │  ├─ Sidewalk colliders
│  │  │  └─ Tags + Layers automáticos
│  │  │
│  │  ├─ TerrainPotholeGenerator.cs [200 líneas] ✅
│  │  │  ├─ Generación de baches
│  │  │  ├─ Random placement
│  │  │  ├─ SphereCollider (trigger)
│  │  │  ├─ Prefab instantiation
│  │  │  └─ Tags: "bache"
│  │  │
│  │  ├─ NavMeshdrone.cs [150 líneas] ✅
│  │  │  ├─ Baking automático de NavMesh
│  │  │  ├─ Configuración de agentes
│  │  │  └─ Validación de mesh
│  │  │
│  │  ├─ DetectionTrigger.cs [60 líneas] ✅
│  │  │  └─ OnTriggerEnter() en baches
│  │  │
│  │  ├─ DataLogger.cs [200 líneas] ⚠️
│  │  │  ├─ Buffer de eventos
│  │  │  ├─ CSV export
│  │  │  ├─ JSON statistics
│  │  │  └─ TODO: Mejor format
│  │  │
│  │  └─ TODO_MISSING: LoadingScreenController.cs [⏳ 100 líneas]
│  │     └─ Maneja UI de carga
│  │
│  └─ UI/
│     ├─ UIManager.cs [150 líneas] ⚠️
│     │  ├─ Actualiza FPS/Stats
│     │  ├─ Maneja botones
│     │  └─ TODO: Refactor completo
│     │
│     └─ TODO_MISSING: DebugUIPanel.cs [⏳ 250 líneas]
│        └─ Panel de debug con sliders
│
├─ Prefabs/
│  ├─ Vehicle.prefab ✅
│  │  ├─ Rigidbody
│  │  ├─ CapsuleCollider
│  │  ├─ CarPatrol
│  │  ├─ RVOAgentNavigator
│  │  ├─ CollisionDetector
│  │  └─ Mesh (child)
│  │
│  ├─ Pedestrian.prefab ✅
│  │  ├─ Rigidbody
│  │  ├─ CapsuleCollider
│  │  ├─ RectangularPatrol
│  │  ├─ RVOAgentController
│  │  ├─ Animator
│  │  ├─ AudioSource
│  │  └─ SkinnedMesh (child)
│  │
│  ├─ House.prefab ✅
│  │  ├─ BoxCollider
│  │  ├─ MeshRenderer
│  │  └─ Tag: "Houses"
│  │
│  ├─ Pothole.prefab ✅
│  │  ├─ SphereCollider (trigger)
│  │  ├─ MeshRenderer
│  │  ├─ DetectionTrigger
│  │  └─ Tag: "bache"
│  │
│  └─ Waypoint.prefab ✅
│     ├─ Empty GameObject
│     └─ Tag: "Waypoint"
│
├─ Materials/
│  ├─ M_Asphalt.mat ✅
│  ├─ M_Sidewalk.mat ✅
│  ├─ M_Building.mat ✅
│  ├─ M_CarPaint.mat ✅
│  └─ M_Pothole.mat ✅
│
├─ Meshes/
│  ├─ Street_Generated/ [procedural]
│  ├─ car_body_lod0.fbx ✅
│  ├─ humanoid_model.fbx ✅
│  ├─ house_model.fbx ✅
│  └─ [otros assets 3D]
│
├─ Animations/
│  ├─ Pedestrian_Walk.anim ✅
│  ├─ Pedestrian_Idle.anim ✅
│  └─ Pedestrian_AnimController.controller ✅
│
├─ Audio/
│  ├─ footstep_concrete.wav ✅
│  ├─ car_engine.wav ⚠️
│  ├─ ambient_city.wav ✅
│  └─ [otros sonidos]
│
└─ Scenes/
   ├─ Mode_Menu.unity ✅
   ├─ Mode_Load.unity ✅
   ├─ Mode_Model.unity ✅
   ├─ Mode_Debug.unity ⚠️ [falta DebugPanel]
   ├─ Mode_Data.unity ❌ [NO IMPLEMENTADO]
   ├─ Mode_Generation_tester.unity ⚠️ [incompleto]
   └─ Mode_Tester.unity ⚠️ [legacy]
```

---

## 🔗 DEPENDENCIAS ENTRE SCRIPTS

```
LoadingScreenController
    ↓
    └─→ SceneInitializer
         ├─→ GeneradorDeCalle
         │   ├─→ Instancia House prefabs
         │   └─→ Crea Sidewalk colliders
         ├─→ TerrainPotholeGenerator
         │   └─→ Instancia Pothole prefabs
         ├─→ NavMeshdrone
         │   └─→ Bake NavMesh
         └─→ ToggleActiveExclusive
              ├─→ RVOSimulationManager
              │   └─→ Simulator (RVO2 core)
              ├─→ Instancia Vehicle prefabs
              │   ├─→ CarPatrol
              │   ├─→ RVOAgentNavigator
              │   └─→ CollisionDetector
              └─→ Instancia Pedestrian prefabs
                  ├─→ RectangularPatrol
                  ├─→ RVOAgentController
                  └─→ Animator
```

---

## 📈 ESTADÍSTICAS DE CÓDIGO

### Por Archivo

| Archivo | Líneas | Importancia | Estado |
|---------|--------|-----------|--------|
| CarPatrol.cs | 650 | 🔴 CRÍTICA | ✅ |
| RectangularPatrol.cs | 500 | 🔴 CRÍTICA | ✅ |
| Simulator.cs (RVO) | 1000+ | 🔴 CRÍTICA | ✅ |
| SceneInitializer.cs | 80 | 🔴 CRÍTICA | ✅ |
| GeneradorDeCalle.cs | 400 | 🟠 IMPORTANTE | ✅ |
| RVOAgentNavigator.cs | 213 | 🟠 IMPORTANTE | ✅ |
| RVOAgentController.cs | 125 | 🟠 IMPORTANTE | ✅ |
| DataLogger.cs | 200 | 🟡 RECOMENDADA | ⚠️ |
| ToggleActiveExclusive.cs | 120 | 🟡 RECOMENDADA | ✅ |
| TerrainPotholeGenerator.cs | 200 | 🟡 RECOMENDADA | ✅ |
| UIManager.cs | 150 | 🔵 OPCIONAL | ⚠️ |
| NavMeshdrone.cs | 150 | 🟡 RECOMENDADA | ✅ |
| DetectionTrigger.cs | 60 | 🟡 RECOMENDADA | ✅ |
| CollisionDetector.cs | 80 | 🟡 RECOMENDADA | ✅ |
| **TOTAL** | **~4000** | - | **75% ✅** |

### Por Categoría

| Categoría | Líneas | % | Estado |
|-----------|--------|---|--------|
| IA & Comportamiento | 1650 | 41% | ✅ |
| RVO & Física | 1500 | 38% | ✅ |
| Generación | 800 | 20% | ✅ |
| UI & Logging | 350 | 9% | ⚠️ |
| Scene Management | 200 | 5% | ✅ |

---

## 🎯 TESTING MATRIX

### Qué necesita testing

| Componente | Test Type | Status | TODO |
|-----------|-----------|--------|------|
| CarPatrol | Unit | ⏳ In Progress | Crear test suite |
| RectangularPatrol | Unit | ⏳ In Progress | Crear test suite |
| RVO2 Integration | Integration | ❌ No existe | Crear benchmarks |
| Scene Generation | Manual | ✅ OK | - |
| Data Export | Manual | ⚠️ Parcial | Validar CSV format |
| Performance | Profiler | ✅ OK | - |

---

## 📋 CHECKLIST PARA COMPLETAR

### Antes de Release 1.0

```
[] 1. Fijar embotellamiento infinito (CarPatrol)
[] 2. Resolver deadlock peatones (RectangularPatrol)
[] 3. Ajustar maxTurnAngle
[] 4. Mejorar reversión de autos
[] 5. Mejorar evasión (Vector3.Reflect → Slerp)
[] 6. Implementar DebugPanel para tuning
[] 7. Crear LoadingScreenController completo
[] 8. Completar Mode_Data.unity
[] 9. Documentar todos los parámetros ajustables
[] 10. Testing cross-platform (Windows, Mac, Linux)
[] 11. Crear user guide completo
[] 12. Performance optimization (profile y optimize)
```

### Antes de Release 2.0 (Futuro)

```
[] 13. Modo multijugador local
[] 14. Exportar video de simulación
[] 15. Machine learning integration
[] 16. Datos reales de mapas
[] 17. Clima dinámico
[] 18. Peatones con IA mejorada (FSM)
[] 19. Sistema de reportes automático
[] 20. API REST para control externo
```

---

## 🚀 ROADMAP RECOMENDADO

### Semana 1: Críticas
- Lunes: Fijar CarPatrol embotellamiento
- Martes: Resolver RectangularPatrol deadlock
- Miércoles: Ajustar ángulos y giros
- Jueves: Mejorar evasión (Reflect → Slerp)
- Viernes: Testing y validación

### Semana 2: Importantes
- Lunes: Reversión realista
- Martes: Inercia
- Miércoles: Frenado en curvas
- Jueves: Distancia social
- Viernes: Testing

### Semana 3: Opcionales
- Lunes: DebugPanel UI
- Martes: Mode_Data
- Miércoles: Exportar video
- Jueves: Polish
- Viernes: Release 1.0 🎉

---

**FIN DE DOCUMENTO**

*Guía completa de TODO TODO TODO - Tareas, Escenarios y Código*
