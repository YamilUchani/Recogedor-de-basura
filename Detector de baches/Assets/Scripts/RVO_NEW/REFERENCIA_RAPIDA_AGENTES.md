# 🎯 Referencia Rápida: Agentes Inteligentes RVO2

## 🚀 En 2 minutos: Qué se puede hacer

```csharp
// 1. DETECTAR TERRENO
RVOTerrainDetector terrain = navigator.GetCurrentTerrain();
if (terrain != null) {
    Debug.Log($"Tipo: {terrain.GetTerrainType()}");
    Debug.Log($"Velocidad: x{terrain.GetSpeedModifier()}");
    if (terrain.IsDangerous()) Debug.Log("¡PELIGRO!");
}

// 2. EVITAR OBSTÁCULOS DINÁMICOS
List<RVODynamicObstacle> obstacles = 
    navigator.GetNearbyDynamicObstacles(15f);
foreach (var obs in obstacles) {
    Vector2 pos = obs.GetPosition();
    Vector2 vel = obs.GetVelocity();
}

// 3. CONTROLAR AGENTE
navigator.SetTarget(targetTransform);        // IA automática
navigator.SetManualVelocity(new Vector2(3, 2)); // Control manual
navigator.ClearManualVelocity();              // Volver a IA

// 4. VISUALIZAR RAYOS
// Inspector: Draw Debug Rays = ON
// En Scene View verás líneas verdes (raycast)
```

---

## 📂 Archivos Nuevos

| Archivo | Función |
|---------|---------|
| **RVOTerrainDetector.cs** | Marca superficies caminables |
| **RVODynamicObstacle.cs** | Obstáculos que se mueven |
| **RVOAgentNavigator.cs** | Agente inteligente con sensores |
| **GUIA_AGENTES_INTELIGENTES.md** | Guía completa de 100 líneas |

---

## ⚡ Setup en 3 cambios

### Cambio 1: Reemplazar Componente
```
Agente original:
├─ RVOAgentController [X eliminar]
└─ RVOAgentNavigator [+ agregar]
```

### Cambio 2: Agregar Terreno
```
GameObject → Plane
├─ MeshCollider
└─ RVOTerrainDetector
   ├─ Terrain Type: Ground/Grass/Water/Lava
   ├─ Is Walkable: true/false
   └─ Speed Multiplier: 0.5-2.0
```

### Cambio 3: Agregar Obstáculo Dinámico
```
GameObject → Cube/Sphere
├─ Rigidbody
└─ RVODynamicObstacle
   ├─ Update Frequency: 0.1
   └─ Radius: 0.5
```

---

## 🎮 Terrenos Predefinidos

```
GROUND (Normal)
- speedMultiplier: 1.0
- dangerous: false
- Uso: Terreno plano normal

GRASS (Pasto)
- speedMultiplier: 0.6
- dangerous: false
- Uso: Ralentiza 40%

WATER (Agua)
- speedMultiplier: 0.0
- isWalkable: false
- dangerous: true
- Uso: Barrera impenetrable

LAVA (Lava)
- speedMultiplier: 0.3
- dangerous: true
- damagePerSecond: 10
- Uso: Terreno peligroso a cruzar

SAND (Arena)
- speedMultiplier: 0.75
- dangerous: false
- Uso: Cambia ligeramente velocidad

ICE (Hielo)
- speedMultiplier: 1.3
- dangerous: false
- Uso: Acelera (resbaladizo)

MUD (Fango)
- speedMultiplier: 0.5
- dangerous: false
- Uso: Muy lento

STONE (Piedra)
- speedMultiplier: 1.0
- dangerous: false
- Uso: Normal (más grip)
```

---

## 🔦 Sensores del Agente

### Raycast (Detección Frontal)
```
┌─ Qué hace: Lanza rayos en todas direcciones
├─ Cuándo: Cada frame
├─ Para qué: Detectar obstáculos adelante
└─ Configurable:
   - Ray Count: 8 rayos
   - Ray Distance: 5 unidades
   - Ray Height: 0.5 hacia arriba
```

### Terrain Detection
```
┌─ Qué hace: Detecta en qué terreno está
├─ Radio: terrainDetectionRadius
├─ Para qué: Aplicar modificadores
└─ Resultado: CurrentTerrain
```

### Dynamic Obstacle Detection
```
┌─ Qué hace: Busca obstáculos móviles cercanos
├─ Radio: dynamicObstacleDetectionRadius
├─ Para qué: Evitar enemigos/plataformas
└─ Resultado: List<RVODynamicObstacle>
```

---

## 🎯 Casos de Uso

### Caso 1: Agente Persigue Target Evitando Peligros
```csharp
navigator.SetTarget(goalTransform);
// Automático:
// - Detecta agua → no entra
// - Detecta lava → ralentiza
// - Detecta enemigos → esquiva
// - Usa raycast → anticipa colisiones
```

### Caso 2: Enemigo patrulla + Jugador esquiva
```csharp
// En EnemyPatrol: mueve a Waypoints (RVODynamicObstacle)
// En Jugador: detecta con GetNearbyDynamicObstacles()
// Resultado: Reacción a presencia de enemigo
```

### Caso 3: Múltiples Terrenos con Propósitos
```
Ground (Seguro)     → velocidad normal
Grass (Lento)       → velocidad 60%
Water (Barrera)     → imposible pasar
Lava (Mortal)       → dañino pero cruzable
```

---

## 📊 Parámetros por Tipo de Agente

### Agente Turístico (Lento, Seguro)
```
Ray Count: 6
Ray Distance: 3
Base Max Speed: 2
Time Horizon: 8
Avoid Dangerous Terrain: true
```

### Agente Militar (Rápido, Reactivo)
```
Ray Count: 16
Ray Distance: 10
Base Max Speed: 8
Time Horizon: 3
Avoid Dangerous Terrain: false
```

### Agente Civíl (Balanceado)
```
Ray Count: 8 [defecto]
Ray Distance: 5
Base Max Speed: 5
Time Horizon: 5
Avoid Dangerous Terrain: true
```

---

## 🔧 Debug Rápido

### Ver raycasts
```
Inspector → RVOAgentNavigator:
Draw Debug Rays: ON
↓
En Scene View ves líneas verdes
```

### Ver detección de terreno
```
Inspector → RVOAgentNavigator:
Draw Terrain Detection: ON
↓
En Scene View ves círculo cyan
```

### Monitorear en consola
```
Inspector → RVODebugger:
Presiona ENTER durante Play
↓
Consola muestra toda la info RVO
```

### Ver colores de terreno
```
En Scene View durante Play:
- Ground: verde
- Grass: verde claro
- Water: azul
- Lava: naranja
- Ice: cyan
- etc.
```

---

## ⚙️ Ajustes Finos

### Si los agentes chocan mucho
```
→ Aumentar Ray Count (8 → 12)
→ Aumentar Ray Distance (5 → 7)
→ Aumentar timeHorizon (5 → 8)
```

### Si son demasiado lentos
```
→ Aumentar Base Max Speed
→ Reducir Ray Count si hay lag
→ Aumentar Ray Height
```

### Si ignoran obstáculos dinámicos
```
→ Aumentar dynamicObstacleDetectionRadius
→ Reducir updateFrequency en RVODynamicObstacle
```

### Si se comportan erráticamente
```
→ Reducir Ray Count (simplificar)
→ Aumentar Update Frequency
→ Reducir Speed Multipliers en terrenos
```

---

## 🎬 Eventos Que Puedes Escuchar

```csharp
// En script personalizado, escucha cambios de terreno:
if (navigator.GetCurrentTerrain() != lastTerrain) {
    // Terreno cambió
    // Reproducir sonido, animación, efecto, etc.
}

// Detecta si hay obstáculos peligrosos
List<RVODynamicObstacle> obstacles = 
    navigator.GetNearbyDynamicObstacles(10f);
if (obstacles.Count > 0) {
    // Hay peligro cerca
    // Cambiar sonido ambient, UI alert, etc.
}
```

---

## 🚨 Problemas Comunes

| Síntoma | Solución |
|--------|----------|
| Rayos no se ven | `drawDebugRays = true` |
| Terreno ignorado | Verificar Collider en terrain |
| Velocidad incorrecta | Revisar `baseMaxSpeed` vs multiplicador |
| Enemigos atraviesan | Aumentar `dynamicObstacleDetectionRadius` |
| Muy lento overall | Reducir `rayCount` o `updateFrequency` |
| Comportamiento aleatorio | Consistenciar parámetros de tiempo |

---

## 📝 Script Template: Tu Propio Controlador

```csharp
using UnityEngine;

public class MySmartAgent : MonoBehaviour {
    private RVOAgentNavigator navigation;
    private Transform target;
    
    void Start() {
        navigation = GetComponent<RVOAgentNavigator>();
    }
    
    void Update() {
        // Lógica de IA aquí
        UpdateTarget();
        HandleTerrainEffects();
        HandleDynamicObstacles();
    }
    
    void UpdateTarget() {
        if (target != null) {
            navigation.SetTarget(target);
        }
    }
    
    void HandleTerrainEffects() {
        var terrain = navigation.GetCurrentTerrain();
        if (terrain != null && terrain.IsDangerous()) {
            // Reaccionar al peligro
            Debug.Log("¡En terreno peligroso!");
        }
    }
    
    void HandleDynamicObstacles() {
        var obstacles = navigation.GetNearbyDynamicObstacles(15f);
        if (obstacles.Count > 0) {
            // Reaccionar a amenazas
            Debug.Log($"{obstacles.Count} amenazas detectadas");
        }
    }
}
```

---

## 🎓 Entender el Flujo

```
Update() del Manager RVO
    ↓
1. UpdatePreferredVelocity() en cada agente
   ├─ Calcula dirección al target
   └─ Ajusta con raycast
    ↓
2. Simulator.doStep() cálculo RVO
   ├─ Busca vecinos (KdTree)
   └─ Calcula velocidad segura (ORCA)
    ↓
3. SyncPositionFromRVO() en cada agente
   ├─ Lee nueva posición
   ├─ Aplica modificadores de terreno
   └─ Actualiza Transform
    ↓
4. Game Logic (tu código)
   └─ Responde a terreno/obstáculos
```

---

## 💡 Tips Avanzados

### Tip 1: Terrenos Estratégicos
```
Crear "mapas de terreno" donde:
- Sand = ruta rápida pero visible
- Water = impenetrable
- Grass = ruta lenta pero oculta
```

### Tip 2: Enemigos como Obstáculos
```
Posicionar enemigos como RVODynamicObstacle
→ Agentes automáticamente los esquivan
→ Crea "fuerzas" naturales sin pathfinding
```

### Tip 3: Feedback Visual
```
Cambiar color de agente según terreno:
- Verde en Ground
- Marrón en Mud
- Rojo en Lava
```

### Tip 4: Performance
```
Si hay lag:
1. Reducir Ray Count
2. Aumentar Update Frequency
3. Limitar Dynamic Obstacle Detection Radius
4. Reducir Ray Distance
```

---

**¡Eres un experto en agentes inteligentes! 🎓**

Para más detalles: Lee [GUIA_AGENTES_INTELIGENTES.md](GUIA_AGENTES_INTELIGENTES.md)
