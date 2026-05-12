# 🎯 Tutorial Rápido: Agente Inteligente Básico

## 🚀 Objetivo: En 5 minutos tendrás un agente moviendose

**Lo que crearemos:**
- 1 Terreno (piso)
- 1 Obstáculo (pared) 
- 1 Agente (esfera que se mueve)
- 1 Target (destino a donde ir)

---

## Paso 1: CREAR NUEVA ESCENA

```
File → New Scene
Nombre: "RVO_Test"
Guarda en: Assets/Scenes/
```

---

## Paso 2: CREAR TERRENO (El Piso)

En **Hierarchy** (lado izquierdo):

```
Click derecho → 3D Object → Plane
Renombra: "Ground"
```

En **Inspector** (lado derecho):

```
Transform:
├─ Position: (0, 0, 0)
├─ Rotation: (0, 0, 0)
└─ Scale: (30, 1, 30)

Add Component → RVOTerrainDetector
├─ Terrain Type: Ground
├─ Is Walkable: ☑️ (activo)
└─ Speed Multiplier: 1.0
```

**Resultado:** Ya tienes el piso donde pueden caminar.

---

## Paso 3: CREAR OBSTÁCULO (Pared)

```
Click derecho → 3D Object → Cube
Renombra: "Wall"
```

En **Inspector**:

```
Transform:
├─ Position: (5, 0.5, 0)
├─ Rotation: (0, 0, 0)
└─ Scale: (1, 2, 10)

Add Component → RVOObstacle
(BoxCollider se añade solo)
```

**Resultado:** Una pared que el agente evitará.

---

## Paso 4: CREAR AGENTE (El que se mueve)

```
Click derecho → 3D Object → Sphere
Renombra: "Agent"
```

En **Inspector**:

```
Transform:
├─ Position: (-10, 0.5, 0)
├─ Rotation: (0, 0, 0)
└─ Scale: (1, 1, 1)

QUITA: Click en SphereCollider → ⋮ → Remove Component

ADD: Add Component → CapsuleCollider
├─ Height: 2
└─ Radius: 0.3

ADD: Add Component → Rigidbody
├─ Gravity: ☐ (OFF - importante!)
└─ Constraints: Freeze Rotation X,Y,Z (marca todas)

ADD: Add Component → RVOAgentNavigator
(Deja todo por defecto)
```

**Resultado:** Un agente listo para moverse.

---

## Paso 5: CREAR TARGET (Donde ir)

```
Click derecho → 3D Object → Sphere
Renombra: "Target"
```

En **Inspector**:

```
Transform:
├─ Position: (10, 0.5, 0)
└─ Scale: (0.5, 0.5, 0.5)

QUITA: SphereCollider

ADD: Add Component → SphereCollider
└─ ☑️ Is Trigger (marca esto)

Material: (opcional) Cambiar color a algo visible
```

**Resultado:** El destino que el agente buscará.

---

## Paso 6: CONECTAR TODO

1. **Selecciona** "Agent" en Hierarchy
2. **En Inspector**, busca "RVOAgentNavigator"
3. **En el campo "Target"**, arrastra "Target" desde Hierarchy

```
RVOAgentNavigator
└─ Target: [Arrastra aquí "Target"]
```

---

## Paso 7: AÑADIR MANAGER (Lo que coordina todo)

```
Click derecho en vacío → Create Empty
Renombra: "RVOManager"

Selecciona RVOManager
Add Component → RVOSimulationManager
```

```
Click derecho en vacío → Create Empty
Renombra: "RVOSetup"

Selecciona RVOSetup
Add Component → RVOSceneSetup
☑️ Auto Setup: MARCA ESTO
```

---

## ¡PRUEBA!

```
Presiona PLAY en Unity

Deberías ver:
✅ El agente (esfera) se mueve solo
✅ Va hacia el Target
✅ Evita la pared
✅ Se detiene cerca del Target
```

---

## Si NO funciona - Checklist

```
❌ Gravity OFF en Rigidbody del Agent?
❌ Target asignado correctamente?
❌ RVOSimulationManager en escena?
❌ Auto Setup = ON en RVOSetup?
```

---

## ✅ ¡LISTO! Tu primer agente inteligente funciona 🚀
