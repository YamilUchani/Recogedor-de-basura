# 🎮 Guía Visual: Crear tu Primera Escena RVO

## 📸 Paso 1: Crear el Manager

### En el Inspector:
```
Hierarchy (Click Derecho)
└─ Create Empty
   └─ Nombre: "RVOManager"
```

### Componentes a Añadir:
```
RVOManager Inspector:
┌─────────────────────────────────┐
│ Transform                       │
├─────────────────────────────────┤
│ RVOSimulationManager    [+]     │
│ ├─ Time Step: 0.016             │
│ ├─ Num Workers: 0               │
│ │                               │
│ └─ Default Agent PARAMS:        │
│   ├─ Neighbor Dist: 15          │
│   ├─ Max Neighbors: 10          │
│   ├─ Time Horizon: 5            │
│   ├─ Time Horizon Obst: 2       │
│   ├─ Radius: 0.5                │
│   └─ Max Speed: 5               │
└─────────────────────────────────┘
```

**Nota:** Puedes dejar los valores por defecto para comenzar. Se aplican a todos los agentes nuevos.

---

## 📸 Paso 2: Crear el Setup Handler

### En el Inspector:
```
Hierarchy (Click Derecho)
└─ Create Empty
   └─ Nombre: "RVOSetup"
```

### Componentes a Añadir:
```
RVOSetup Inspector:
┌─────────────────────────────────┐
│ Transform                       │
├─────────────────────────────────┤
│ RVOSceneSetup           [+]     │
│ ├─ Auto Setup On Start: ☑️      │
└─────────────────────────────────┘
```

**✅ IMPORTANTE:** Marcar "Auto Setup On Start" para que se ejecute automáticamente.

---

## 📸 Paso 3: Crear Obstáculos

### Crear un Obstáculo Simple (Muro):

```
Hierarchy (Click Derecho)
└─ 3D Object → Cube
   └─ Nombre: "Obstacle_Wall1"

Inspector:
┌─────────────────────────────────┐
│ Transform                       │
│ ├─ Position: (5, 0, 5)          │
│ ├─ Rotation: (0, 0, 0)          │
│ └─ Scale: (10, 2, 1)   ⬅️ largo│
├─────────────────────────────────┤
│ BoxCollider                     │
│ └─ Is Trigger: ☐ (desmarcar)   │
├─────────────────────────────────┤
│ RVOObstacle             [+]     │
│ ├─ Is Convex: ☑️                │
│ └─ Is Clockwise: ☐              │
│                                 │
│ (¡Quitar MeshRenderer opcional!)│
└─────────────────────────────────┘
```

**Resultado Visual:**
```
Vista desde arriba (plano XZ):

  ┌────────────────────┐
  │   Viewport         │
  │                    │
  │        Z      Wall │
  │        ▲      ███  │
  │        │           │
  ├─ X ───●────────────┤
  │       N           S │
  │        E           │
  │                    │
  │                    │
  └────────────────────┘
```

---

## 📸 Paso 4: Crear Agentes

### Crear un Agente (Esfera):

```
Hierarchy (Click Derecho)
└─ 3D Object → Sphere
   └─ Nombre: "Agent_1"

Primero, BORRAR el SphereCollider automático:
├─ Agent_1
│  └─ SphereCollider [⋮] → Remove Component

Luego agregar componentes necesarios:

Inspector:
┌─────────────────────────────────┐
│ Transform                       │
│ ├─ Position: (-5, 0.5, 0)       │
│ ├─ Rotation: (0, 0, 0)          │
│ └─ Scale: (1, 1, 1)             │
├─────────────────────────────────┤
│ MeshRenderer (existente)        │
├─────────────────────────────────┤
│ CapsuleCollider         [+]     │
│ ├─ Height: 2                    │
│ ├─ Radius: 0.3                  │
│ └─ Is Trigger: ☐                │
├─────────────────────────────────┤
│ Rigidbody               [+]     │
│ ├─ Mass: 1                      │
│ ├─ Drag: 0                      │
│ ├─ Constraints:                 │
│ │  └─ Freeze Rotation: X,Y,Z   │
│ └─ Use Gravity: ☐ (APAGADO)     │
├─────────────────────────────────┤
│ RVOAgentController      [+]     │
│ ├─ Neighbor Dist: 15            │
│ ├─ Max Neighbors: 10            │
│ ├─ Time Horizon: 5              │
│ ├─ Time Horizon Obst: 2         │
│ ├─ Radius: 0.3                  │
│ ├─ Max Speed: 5                 │
│ ├─ Target: [Arrastra otro obj]  │
│ ├─ Use Manual Velocity: ☐       │
│ └─ Manual Velocity: (0, 0)      │
└─────────────────────────────────┘
```

### Crear Target (Destino):

```
Hierarchy (Click Derecho)
└─ 3D Object → Sphere
   └─ Nombre: "Target_1"

Inspector:
┌─────────────────────────────────┐
│ Transform                       │
│ ├─ Position: (5, 0.5, 0)        │
│ ├─ Rotation: (0, 0, 0)          │
│ └─ Scale: (0.5, 0.5, 0.5)       │
├─────────────────────────────────┤
│ MeshRenderer                    │
│ └─ Material: Material rojo/verde│
├─────────────────────────────────┤
│ SphereCollider                  │
│ └─ Is Trigger: ☑️ (ACTIVAR)     │
└─────────────────────────────────┘

LUEGO: En Agent_1 → RVOAgentController → Target
       Arrastra Target_1 aquí 👆
```

---

## 📸 Paso 5: Agregar Más Agentes

### Duplicar Agent_1:

```
En Hierarchy:
Agent_1 (Click Derecho)
└─ Duplicate
   └─ Renombrar a "Agent_2"
   
En Inspector (Agent_2):
├─ Position: (-5, 0.5, 5)   ← Cambia posición
│
└─ RVOAgentController
   └─ Target: Target_1 (igual para todos)
```

**Repite 3-5 veces para una buena escena de prueba.**

---

## 📺 Jerarquía Final

```
Hierarchy:
│
├─ RVOManager
│  └─ RVOSimulationManager [config]
│
├─ RVOSetup
│  └─ RVOSceneSetup [auto-setup]
│
├─ Obstacle_Wall1
│  ├─ BoxCollider
│  └─ RVOObstacle
│
├─ Obstacle_Wall2
│  ├─ BoxCollider
│  └─ RVOObstacle
│
├─ Agent_1
│  ├─ MeshRenderer
│  ├─ CapsuleCollider
│  ├─ Rigidbody
│  └─ RVOAgentController
│
├─ Agent_2
│  ├─ MeshRenderer
│  ├─ CapsuleCollider
│  ├─ Rigidbody
│  └─ RVOAgentController
│
├─ Agent_3
│  ├─ MeshRenderer
│  ├─ CapsuleCollider
│  ├─ Rigidbody
│  └─ RVOAgentController
│
├─ Target_1
│  ├─ MeshRenderer (Material rojo)
│  └─ SphereCollider (Is Trigger)
│
├─ Plane (opcional - piso)
│  └─ MeshCollider
│
├─ Lighting
│  └─ Main Camera
│
└─ Canvas (UI opcional)
```

---

## 🎬 Ejecutar Escena

### En el Editor:

1. **Salvar escena**
   ```
   Ctrl+S → Scenes/MyRVOTest.unity
   ```

2. **Presionar Play**
   ```
   ▶️ Botón Play en la parte superior
   ```

3. **Observe cómo:**
   - Los agentes se mueven hacia el target
   - Se evitan automáticamente entre sí
   - Se evitan los obstáculos

4. **Abrir Consola** (Window → Console)
   ```
   Presiona ENTER en Game window
   → Verás debug info
   ```

---

## 🎨 Visualización Mejorada (Opcional)

### Agregar colores diferentes:

```
Para cada Agent:
├─ MeshRenderer
│  └─ Material: [+] Nuevo
│     └─ Color: Rojo/Verde/Azul
```

### Agregar etiquetas:

```
Scene View → Escribe etiqueta
└─ Para ver nombres de GameObjects
```

---

## 📊 Monitoreo en Tiempo Real

### Crear un Debug Canvas:

```
Hierarchy (Click Derecho)
└─ UI → Panel
   └─ Nombre: "DebugPanel"

Agregar componente:
├─ RVODebugger
│  ├─ Draw Positions: ☑️
│  ├─ Draw Velocities: ☑️
│  ├─ Draw Neighbors: ☑️
│  └─ Arrow Scale: 0.5

En Game View:
→ Verás líneas dibujadas (Gizmos)
→ Presiona ENTER para consola
```

---

## ✅ Checklist Visual

```
┌─ Setup Inicial
│ ├─ [✓] RVOManager en escena
│ ├─ [✓] RVOSetup con Auto Setup ON
│ └─ [✓] Al menos 2 obstáculos
│
├─ Agentes
│ ├─ [✓] Al menos 3 agentes
│ ├─ [✓] Cada uno con RVOAgentController
│ ├─ [✓] Rigidbody con Gravity OFF
│ ├─ [✓] Target asignado
│ └─ [✓] Diferentes posiciones iniciales
│
├─ Obstáculos
│ ├─ [✓] BoxCollider o MeshCollider
│ ├─ [✓] RVOObstacle component
│ └─ [✓] Is Trigger DESACTIVADO
│
└─ Test
  ├─ [✓] Escena salva
  ├─ [✓] Presionar Play
  ├─ [✓] Agentes se mueven
  ├─ [✓] Evitan obstáculos
  └─ [✓] Se evitan entre sí
```

---

## 🎥 Video Esperado

```
Cuando presiones Play:

1. Agentes en posiciones iniciales (puntos rojos/azules)
   ↓
2. Comienzan a moverse hacia Target (punto verde)
   ↓
3. Si se acercan demasiado → Se apartan automáticamente
   ↓
4. Si hay muro en el camino → Lo rodean
   ↓
5. Llegan al Target sin choques
   ↓
6. Consola muestra velocidades y vecinos
```

---

## 🐛 Troubleshooting Rápido

| Problema | Solución |
|----------|----------|
| Agentes congelados | Asignar Target en Inspector |
| Pasan a través del muro | Aumentar `timeHorizonObst` a 3 |
| Chocan entre sí | Aumentar `radius` a 0.6-0.8 |
| Muy lento con muchos | Reducir `maxNeighbors` a 5 |
| Comportamiento raro | Presiona ENTER, revisa consola |

---

**¡Listo! Tu escena RVO está completa. Disfruta la simulación! 🚀**
