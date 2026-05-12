# 🎬 Flujo Visual: Cómo Funciona Todo Junto

## 🔄 Ciclo Completo de un Frame

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    FRAME N en RVOSimulationManager                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌─ UPDATE DE AGENTES ────────────────────────────────────────────────┐  │
│  │                                                                     │  │
│  │  Para cada RVOAgentNavigator:                                      │  │
│  │  ┌──────────────────────────────────────────────────────────────┐  │  │
│  │  │ 1. DetectCurrentTerrain()                                    │  │  │
│  │  │    └─ Busca cercano RVOTerrainDetector                       │  │  │
│  │  │       ├─ Si entra a nuevo → OnEnterTerrain()               │  │  │
│  │  │       └─ Si sale → OnExitTerrain()                          │  │  │
│  │  │                                                               │  │  │
│  │  │ 2. UpdateDynamicObstacles()                                  │  │  │
│  │  │    └─ Recorre lista RVODynamicObstacle                       │  │  │
│  │  │       └─ Si cercano → Evitar (aumentar buffer)              │  │  │
│  │  │                                                               │  │  │
│  │  │ 3. CalculatePreferredVelocity()                              │  │  │
│  │  │    ├─ Si target → Dirección hacia target                    │  │  │
│  │  │    ├─ Si manual → Usar velocidad manual                     │  │  │
│  │  │    └─ AdjustVelocityWithRaycasts():                          │  │  │
│  │  │       ├─ Lanzar rayCount rayos adelante                     │  │  │
│  │  │       ├─ Si > 50% rayos impactados → Ralentizar 30%         │  │  │
│  │  │       └─ Retornar velocidad ajustada                         │  │  │
│  │  │                                                               │  │  │
│  │  │ 4. Simulator.setAgentPrefVelocity()                           │  │  │
│  │  │    └─ Pasar velocidad preferida a RVO kernel                │  │  │
│  │  │                                                               │  │  │
│  │  └──────────────────────────────────────────────────────────────┘  │  │
│  │                                                                     │  │
│  │  [Fin loop: próximo agente]                                        │  │
│  │                                                                     │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  ┌─ PASO DE SIMULACIÓN RVO ───────────────────────────────────────────┐  │
│  │                                                                     │  │
│  │  Simulator.doStep()                                                │  │
│  │  ├─ Construir KdTree de agentes                                   │  │
│  │  │                                                                 │  │
│  │  ├─ Para cada agente (PARALELO en threads):                       │  │
│  │  │  ├─ computeNeighbors() → Buscar en KdTree                     │  │
│  │  │  └─ computeNewVelocity() → Calcular ORCA                      │  │
│  │  │                                                                 │  │
│  │  └─ Para cada agente:                                             │  │
│  │     └─ update() → Actualizar posición con nueva velocidad         │  │
│  │                                                                     │  │
│  │  [Resultado: Agentes se mueven sin colisiones]                    │  │
│  │                                                                     │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  ┌─ SINCRONIZACIÓN CON UNITY ─────────────────────────────────────────┐  │
│  │                                                                     │  │
│  │  Para cada RVOAgentNavigator:                                      │  │
│  │  ├─ SyncPositionFromRVO()                                          │  │
│  │  │  ├─ Obtener nueva posición de RVO                              │  │
│  │  │  └─ Actualizar Transform.position                              │  │
│  │  │                                                                 │  │
│  │  └─ ApplyTerrainModifiers()                                        │  │
│  │     ├─ Aplicar speedMultiplier del terreno                        │  │
│  │     ├─ Calcular y aplicar daño si es peligroso                    │  │
│  │     └─ Detener si no puede pasar                                  │  │
│  │                                                                     │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🗺️ Arquitectura de Componentes

```
ESCENA UNITY
└─ Vacío con componentes/scripts
   │
   ├─ RVOManager [Singleton]
   │  └─ RVOSimulationManager
   │     └─ Controla TODO → llama Update() cada frame
   │
   ├─ Obstáculos Estáticos + RVOObstacle
   │  └─ Se registran en Simulator una vez
   │
   ├─ Obstáculos Dinámicos
   │  ├─ GameObject con Rigidbody (se mueve)
   │  └─ RVODynamicObstacle
   │     ├─ Trackea: posición, velocidad
   │     └─ Los agentes los detectan y evitan
   │
   ├─ Zonas de Terreno
   │  ├─ Planes u objetos con Collider
   │  └─ RVOTerrainDetector
   │     ├─ Define: tipo, velocidad, peligro
   │     └─ Los agentes los detectan y adaptan
   │
   └─ Agentes Inteligentes
      ├─ Sphere/Capsule con Rigidbody, Collider
      └─ RVOAgentNavigator
         ├─ Hereda de RVOAgentController
         ├─ Agrega: raycast, terrain detect, dynamic obs detect
         ├─ TODO frame:
         │  ├─ Detecta terreno actual
         │  ├─ Detecta obstáculos dinámicos cercanos
         │  ├─ Lanza rayos adelante (8 direcciones)
         │  ├─ Calcula velocidad preferida
         │  └─ Paso simulación RVO
         └─ Resultado: Agente se mueve inteligentemente  
```

---

## 📡 Sensores del Agente Navigator

```
┌─────────────────────────────────────────────────────────────┐
│                  RVOAgentNavigator                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──  SENSOR 1: RAYCAST ─────────────────────────────────┐  │
│  │ ┌─────────────────────────────────────────────────┐   │  │
│  │ │              AGENTE (vista desde arriba)       │   │  │
│  │ │                    O  (vista-rayo)            │   │  │
│  │ │                 /  |  \                         │   │  │
│  │ │              R/  /R|\R  \R  (8 rayos)           │   │  │
│  │ │              / /R/ | \R\ \                      │   │  │
│  │ │                   \ | /   Target                │   │  │
│  │ │                     \|/                         │   │  │
│  │ │  Ray Distance: 5m   TO (destino)               │   │  │
│  │ │  Ray Count: 8                                   │   │  │
│  │ │  Ray Height: 0.5m (hacia arriba desde suelo)  │   │  │
│  │ │                                                 │   │  │
│  │ │  ¿Qué detecta?                                  │   │  │
│  │ │  ✓ Obstáculos estáticos adelante               │   │  │
│  │ │  ✓ Terreno impasable                           │   │  │
│  │ │  ✓ Otros agentes                               │   │  │
│  │ │  ✓ Paredes, edificios, etc                     │   │  │
│  │ │                                                 │   │  │
│  │ └─────────────────────────────────────────────────┘   │  │
│  │                                                         │  │
│  │  Si > 50% rayos golpean:                              │  │
│  │  → Reducir velocidad a 70%                            │  │
│  │  → Resultado: Anticipación de colisión               │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──  SENSOR 2: TERRAIN DETECTOR ────────────────────────┐  │
│  │                                                       │  │
│  │  Cada frame:                                          │  │
│  │  ┌─ Busca RVOTerrainDetector más cercano            │  │
│  │  │  (dentro de terrainDetectionRadius)              │  │
│  │  │                                                   │  │
│  │  ├─ ¿Encontrado?                                    │  │
│  │  │   ├─ Sí, ¿Diferente del actual?                │  │
│  │  │   │  ├─ Sí → OnEnterTerrain() evento             │  │
│  │  │   │  └─ No → Continuar                           │  │
│  │  │   └─ No → OnExitTerrain() evento                 │  │
│  │  │                                                   │  │
│  │  └─ Resultado: currentTerrain actualizado           │  │
│  │                                                       │  │
│  │  Datos disponibles del terreno:                      │  │
│  │  ✓ tipo (Ground, Grass, Water, etc)                │  │
│  │  ✓ speedMultiplier (0.6 = 60% velocidad)           │  │
│  │  ✓ isDangerous (boolean)                           │  │
│  │  ✓ damagePerSecond (float)                         │  │
│  │                                                       │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──  SENSOR 3: DYNAMIC OBSTACLE DETECTOR ────────────────┐  │
│  │                                                        │  │
│  │  Cada frame:                                           │  │
│  │  ┌─ Recorrer lista de RVODynamicObstacle            │  │
│  │  │  │                                                │  │
│  │  │  ├─ Para cada obstáculo:                         │  │
│  │  │  │  ├─ ¿EstáCercano(radio=10m)?                 │  │
│  │  │  │  │  ├─ Sí → Añadir a lista de cercanos       │  │
│  │  │  │  │  └─ No → Ignorar                          │  │
│  │  │  │  └─ Obtener: posición, velocidad            │  │
│  │  │  │                                                │  │
│  │  │  └─ Ajustar velocidad preferida:                │  │
│  │  │     ├─ Calcular dirección AWAY from obstacle    │  │
│  │  │     └─ Aplicar pequeño ajuste (+0.5 factor)    │  │
│  │  │                                                   │  │
│  │  └─ Resultado: preferredVelocity ajustada          │  │
│  │                                                        │  │
│  │  Datos disponibles del obstáculo:                     │  │
│  │  ✓ posición (Vector2)                               │  │
│  │  ✓ velocidad (Vector2)                              │  │
│  │  ✓ radio de detección                               │  │
│  │                                                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──  INTEGRACIÓN: ¿Qué hace con SensoRes? ────────────┐   │
│  │                                                     │   │
│  │  preferredVelocity = CalculatePreferredVelocity()  │   │
│  │                                                     │   │
│  │  Pasos:                                             │   │
│  │  1. SI target → dirección a target                 │   │
│  │  2. SINO SI manual → velocidad manual              │   │
│  │  3. SINO → velocidad cero                          │   │
│  │  4. Ajustar con raycast                            │   │
│  │  5. Enviar a Simulator.setAgentPrefVelocity()      │   │
│  │                                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔀 Flujo de Datos en Tiempo Real

```
FRAME 1:
  Ratón clic → SetTarget(goal)
    ↓
  Update() en Manager
    ├─ Agente 1: Raycast → goal bloqueado por muro
    │  └─ preferredVelocity = (ajustado 70%)
    ├─ Agente 2: En agua (impasable)
    │  └─ OnEnterTerrain(Water) evento
    └─ Agente 3: Detecta enemigo cerca
       └─ preferredVelocity = (esquiva)
    ↓
  Simulator.doStep() 
    └─ ORCA calcula velocidades seguras
    ↓
  Todos los agentes se mueven 1 frame

FRAME 2:
  Agente 1: gotea aproximándose al muro
    ├─ raycast aún bloqueado
    └─ preferredVelocity sigue ajustada
    ↓
  Agente 2: Intenta rodear agua
    ├─ sale de agua (OnExitTerrain)
    └─ speedMultiplier vuelve a 1.0
    ↓
  Agente 3: Enemigo se acerca más
    ├─ distancia < threshold
    ├─ Rayos detectan colisión inminente
    └─ Velocidad se reduce aún más
    ↓
  Simulator.doStep()
    └─ Todos evitan colisiones mutuamente

FRAME 3+:
  Agente 1: Enfila hacia goal diferente
    └─ muro evitado, nuevo raycast sigue
    ↓
  Agente 2: En grass (ralentiza 60%)
    └─ llega a goal más lento
    ↓
  Agente 3: En lava (daño!)
    ├─ OnTakeDamage(10) evento
    ├─ speedMultiplier = 0.3
    └─ Continúa pero debilitarse

[Loop continúa cada frame]
```

---

## 🎯 Ejemplo Específico: Agente Pasa por 4 Terrenos

```
FRAME 0-50:
  AGENTE en GROUND (normal)
  ├─ speedMultiplier: 1.0
  ├─ velocidad: 5 m/s
  ├─ daño: 0
  └─ Evento: OnEnterTerrain(Ground)

FRAME 51-100:
  AGENTE en GRASS (más lento)
  ├─ speedMultiplier: 0.6
  ├─ velocidad: 5 * 0.6 = 3 m/s
  ├─ daño: 0
  └─ Evento: OnEnterTerrain(Grass)
     → Sonido de pasto, animación diferente, etc.

FRAME 101-150:
  AGENTE en LAVA (peligroso)
  ├─ speedMultiplier: 0.3
  ├─ velocidad: 5 * 0.3 = 1.5 m/s
  ├─ daño: 10 HP/s
  └─ Evento: OnEnterTerrain(Lava)
     → Animación panic, sonido alarma, UI mostrando daño
  
  Cada 3 frames:
  └─ OnTakeDamage(daño_por_frame)
     → Reducir health, mostrar visual damage

FRAME 151-200:
  AGENTE en GROUND (escapó!)
  ├─ speedMultiplier: 1.0
  ├─ velocidad: 5 m/s
  ├─ daño: 0
  └─ Evento: OnExitTerrain(Lava)
     → Sonido alivio, UI recuperación, etc.

FRAME 201-250:
  AGENTE en WATER (impasable!)
  ├─ isWalkable: false
  ├─ velocidad: 0 (DETENIDO)
  ├─ daño: 100/s rápido (mortal)
  └─ Evento: OnEnterTerrain(Water)
     → Fuerza stop agente, UI warning crítico
```

---

## 🎬 Interacción Agente + Obstáculo Dinámico

```
FRAME 0-20: Agente se acerca a enemy (RVODynamicObstacle)
  ├─ Distancia agente-enemy: 15m
  ├─ dynamicObstacleDetectionRadius: 10m
  ├─ ¿Cercano? NO → Sin reacción

FRAME 21: Enemy se acerca más
  ├─ Distancia: 9m
  ├─ ¿Cercano? SÍ → Detectado!
  ├─ GetNearbyDynamicObstacles() retorna [enemy]
  └─ Evento: Agente sabe de enemy

FRAME 22-40: Esquivando
  ├─ Cada frame:
  │  ├─ Obtener: enemy.GetPosition()
  │  ├─ Obtener: enemy.GetVelocity()
  │  ├─ Calcular: dirección AWAY FROM enemy
  │  └─ Aplicar: ajuste +0.5 al buffer de velocidad
  ├─ Raycast TAMBIÉN detecta enemy
  │  └─ Reducir velocidad 30% por seguridad
  └─ Resultado: Agente esquiva natural

FRAME 41: Enemy pasa
  ├─ Distancia: 11m (FUERA de rango)
  ├─ GetNearbyDynamicObstacles() retorna []
  └─ Siguiendo ruta normal otra vez

[Loop continúa]
```

---

## 📊 Tabla de Prioridades: Raycast vs. RVO vs. Terreno

```
┌────────────┬──────────────────┬────────────────────────────┐
│ Situación  │ Prioridad        │ Qué sucede                 │
├────────────┼──────────────────┼────────────────────────────┤
│ Target     │ BAJA (base)      │ Dirección hacia target     │
│ Raycast    │ MEDIA (ajuste)   │ Ralentizar si impacto > 50%│
│ Terreno    │ MEDIA (modif)    │ Mult. var. o bloquea       │
│ Dinámico   │ MEDIA (evita)    │ Pequeño buffer de distancia│
│ RVO ORCA   │ ALTA (final)     │ Evita colisiones reales    │
└────────────┴──────────────────┴────────────────────────────┘

Resultado Final = Target Direction
                + Raycast Adjustment
                + Terrain Modifier
                + Dynamic Obstacle Buffer
                → ORCA Calculation
                → Safe Velocity
```

---

## 🔗 Relación entre los 3 Componentes

```
RVOTerrainDetector ←→ RVOAgentNavigator
           │              ↓
           │         Detecta cada frame
           │         IsAgentOnTerrain()
           │              
           │         ↓ Aplica
           │         speedMultiplier
           │         damagePerSecond
           └─→ Modifica velocidad final
           
RVODynamicObstacle ←→ RVOAgentNavigator
           │              ↓
           │         GetNearbyDynamicObstacles()
           │              ↓
           │         Obtiene: pos, vel, radio
           │              ↓
           │         IsAgentNearby() → bool
           │         
           │         ↓ Aplica
           │         Ajuste de velocidad
           │         Buffer de distancia
           │         Evitación anticipada
           └─→ Modifica velocidad preferida
           
RVOSimulationManager ←→ RVOAgentNavigator
           │              ↓
           │         Llama Update()
           │         Calcula sensors
           │              
           │         ↓ Obtiene
           │         preferredVelocity
           │         ↓ Envía a
           │         Simulator.setAgentPrefVelocity()
           │              ↓
           │              Simulator.doStep()
           │              ↓
           │              Nova velocidad (ORCA)
           │              ↓
           └─← SyncPositionFromRVO()
```

---

## ✨ Características Emergen tes (Que Surgen Naturalmente)

```
Combinación de componentes CREA comportamientos sin código extra:

1. PATRULLAJE INTELIGENTE
   Enemy (RVODynamicObstacle) patrulla
   + Agentes detectan y evitan
   = Comportamiento esquiva natural

2. RUTAS ALTERNATIVAS
   Raycast anticipa muro
   + 3+ agentes convergen
   = Flujo alrededor del obstáculo

3. PÁNICO COORDINADO
   Terreno mortal (lava)
   + Múltiples agentes huyen
   = Evacuación natural (ORCA coordina)

4. COMPORTAMIENTO DE REBAÑO
   Sinsentido target o persecución
   + Detección mutua RVO
   = Movimiento cohesionado automático

5. INTERACCIÓN AMBIENTAL
   Terreno ralentiza
   + Raycast anticipa
   + Enemy detectado
   = Agente toma decisiones contextuales
```

---

**¡Ahora entiendes el flujo completo! 🎓**

Próximo paso: Implementar en tu escena siguiendo [GUIA_AGENTES_INTELIGENTES.md](GUIA_AGENTES_INTELIGENTES.md)
