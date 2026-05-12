# PersonController - Guía de Uso

## Descripción
`PersonController` es un componente que permite controlar una cápsula (representando a una persona) que se mueve automáticamente siguiendo una ruta definida de waypoints.

## Características
- ✅ Movimiento automático entre waypoints
- ✅ Rotación hacia el destino
- ✅ Sistema de loop (ruta circular o final)
- ✅ Visualización de ruta en gizmos
- ✅ Física integrada (Rigidbody + CapsuleCollider)
- ✅ Control por script

## Instalación

### 1. Crear un GameObject con Cápsula
```csharp
// En el editor:
1. Right-click en Hierarchy → 3D Object → Capsule
2. Renombrar a "Person" o lo que prefieras
3. Posicionar donde desees que comience
```

### 2. Agregar el Componente PersonController
```csharp
// En el editor:
1. Seleccionar el GameObject de la cápsula
2. Add Component → PersonController
3. Configurar los parámetros (ver sección Parámetros)
```

### 3. Agregar Waypoints

#### Opción A: En el Inspector (Manual)
```csharp
1. En PersonController, expandir "Waypoints"
2. Aumentar el tamaño de la lista a N waypoints
3. Asignar posiciones manualmente
```

#### Opción B: Por Script
```csharp
PersonController person = GetComponent<PersonController>();
List<Vector3> route = new List<Vector3>
{
    new Vector3(0, 0, 0),
    new Vector3(5, 0, 0),
    new Vector3(5, 0, 5),
    new Vector3(0, 0, 5)
};
person.SetWaypoints(route);
```

#### Opción C: Dinámico
```csharp
PersonController person = GetComponent<PersonController>();
person.AddWaypoint(new Vector3(0, 0, 0));
person.AddWaypoint(new Vector3(5, 0, 0));
person.AddWaypoint(new Vector3(5, 0, 5));
```

## Parámetros del Inspector

### Waypoints
- **List**: Array de posiciones Vector3
- **Loop Route**: Si es true, la ruta se repite; si es false, para al último waypoint
- **Waypoint Tolerance**: Distancia en metros para considerar que llegó al waypoint (default: 0.2m)

### Movimiento
- **Move Speed**: Velocidad de movimiento en m/s (default: 2)
- **Rotation Speed**: Velocidad de rotación hacia waypoints (default: 5)
- **Rotate Towards Waypoint**: Si debe rotar hacia el waypoint (default: true)

### Evitación de Personas ⭐ NUEVO
- **Enable Avoidance**: Activar/desactivar sistema de evitación (default: true)
- **Detection Radius**: Radio en metros para detectar otras personas (default: 2m)
- **Pause When Too Close**: Distancia para pausar cuando hit otra persona (default: 0.8m)
- **Avoidance Force**: Intensidad del desvío (0-1, default: 0.5)

### Área de Influencia ⭐ NUEVO
- **Influence Area Center**: Centro del área donde se puede mover
- **Influence Area Size**: Tamaño del área (width, height, depth)
- **Constrain To Area**: Si true, la persona se queda dentro del área (default: true)

### Física
- **Capsule Height**: Altura de la cápsula en metros (default: 1.8)
- **Capsule Radius**: Radio de la cápsula en metros (default: 0.3)

### Visualización
- **Show Waypoints**: Mostrar esferas en los waypoints
- **Show Path**: Dibujar líneas conectando los waypoints
- **Show Avoidance Radius**: Mostrar círculo de detección (solo en Play)
- **Show Influence Area**: Mostrar el área de influencia como caja
- **Waypoint Color**: Color de los waypoints
- **Path Color**: Color de las líneas de la ruta
- **Avoidance Color**: Color del radio de detección

## Métodos Públicos

### Control Básico
```csharp
// Agregar un waypoint
person.AddWaypoint(Vector3 position);

// Establecer múltiples waypoints
person.SetWaypoints(List<Vector3> waypoints);

// Pausar/Reanudar
person.SetMoving(bool moving);

// Reiniciar desde el principio
person.RestartRoute();
```

### Información
```csharp
// Obtener waypoint actual
Vector3 current = person.GetCurrentWaypoint();

// Obtener índice del waypoint actual
int index = person.GetCurrentWaypointIndex();

// Obtener distancia al waypoint objetivo
float distance = person.GetDistanceToCurrentWaypoint();
```

### Evitación y Área ⭐ NUEVO
```csharp
// Establecer área de influencia
person.SetInfluenceArea(Vector3 center, Vector3 size);

// Habilitar/deshabilitar evitación
person.SetAvoidanceEnabled(bool enabled);

// Verificar si está evitando a alguien
bool isEvading = person.IsAvoiding();
```

## Ejemplo: Crear una Persona Patrullando

```csharp
using System.Collections.Generic;
using UnityEngine;

public class PersonPatrol : MonoBehaviour
{
    void Start()
    {
        // Crear una cápsula
        GameObject personGO = new GameObject("Guard");
        personGO.transform.position = new Vector3(0, 0, 0);
        
        // Agregar componentes
        PersonController controller = personGO.AddComponent<PersonController>();
        
        // Definir ruta de patrulla
        List<Vector3> patrolRoute = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(10, 0, 10),
            new Vector3(0, 0, 10),
            new Vector3(0, 0, 0)
        };
        
        controller.SetWaypoints(patrolRoute);
    }
}
```

## Ejemplo: Evitar a Otras Personas ⭐ NUEVO

```csharp
void Start()
{
    PersonController person = GetComponent<PersonController>();
    
    // Establecer ruta
    person.SetWaypoints(waypoints);
    
    // Configurar evitación
    person.SetInfluenceArea(new Vector3(0, 0, 0), new Vector3(20, 2, 20));
    person.SetAvoidanceEnabled(true);
}

void Update()
{
    // Verificar si está evitando
    if (person.IsAvoiding())
    {
        Debug.Log("¡Persona evitando a otra!");
    }
}
```

## Ejemplo: Múltiples Personas que se Evitan

```csharp
// Usar PersonAvoidanceDemo (el script de ejemplo)
1. Crear GameObject vacío llamado "PeopleManager"
2. Agregar componente PersonAvoidanceDemo
3. Configurar numberOfPeople = 4
4. Configurar areaSize = (20, 2, 20)
5. Marcar enableAvoidance = true
6. Click derecho → "Crear Personas con Evitación"

// El sistema automáticamente:
// ✓ Distribuye personas en el área
// ✓ Genera rutas aleatorias
// ✓ Detecta y evita colisiones
// ✓ Se mantiene dentro del área
```

## Ejemplo: Ruta Manual en Editor

1. Crear GameObject vacío llamado "Person"
2. Agregar componente PersonController
3. En el Inspector, expandir Waypoints → Establecer Size = 4
4. Asignar posiciones:
   - Element 0: (0, 0, 0)
   - Element 1: (5, 0, 0)
   - Element 2: (5, 0, 5)
   - Element 3: (0, 0, 5)
5. Ajustar Move Speed a 2
6. Marcar Loop Route como true
7. Play para ver la persona patrullando

## Tips y Trucos

### 1. Velocidad Realista
Para una persona caminando: **Move Speed = 1.4** m/s
Para una persona corriendo: **Move Speed = 4-5** m/s

### 2. Rotación Suave
Aumentar **Rotation Speed** para rotaciones más rápidas (default 5, máximo ~10)

### 3. Patrulla Realista
- Usar múltiples waypoints cercanos entre sí
- Permitir que pause en ciertos puntos con coroutines
- Combinar con animaciones de caminar

### 4. Debugging
Los waypoints se muestran en el editor con Gizmos:
- **Esfera Cyan**: Waypoints normales
- **Esfera Roja**: Waypoint objetivo actual  
- **Líneas Verdes**: Ruta conexión

### 5. Evitación - Parámetros Óptimos ⭐ NUEVO
```
Para zona pequeña (5m x 5m):
  Detection Radius = 1.5m
  Pause Distance = 0.5m
  Avoidance Force = 0.6

Para zona mediana (20m x 20m):
  Detection Radius = 2.5m
  Pause Distance = 0.8m
  Avoidance Force = 0.5

Para zona grande (50m x 50m):
  Detection Radius = 3.5m
  Pause Distance = 1.0m
  Avoidance Force = 0.4
```

### 6. Área de Influencia
- Si `Constrain To Area` está habilitado, no saldrá del área definida
- El área es un cuadrado/rectángulo alineado con los ejes
- Perfecto para delimitar una zona de patrulla o edificio

### 7. Monitoreo en Runtime
```csharp
// Desde el script que use PersonController:
if (person.IsAvoiding())
{
    // El personaje está evitando a alguien
    // Puede reducir velocidad o cambiar comportamiento
}
```

## Componentes Autogenerados

PersonController crea automáticamente:
- **Rigidbody**: Para física (congelando rotaciones X, Z)
- **CapsuleCollider**: Colisión automática

Puedes personalizarlos manualmente después de agregar el componente.

## Integración con Otras Scripts

### Detener una persona al ver al drone
```csharp
PersonController person = GetComponent<PersonController>();
if (CanSeeDrone())
{
    person.SetMoving(false);
}
```

### Cambiar ruta dinámicamente
```csharp
// Huir de la cámara del drone
PersonController person = GetComponent<PersonController>();
person.SetWaypoints(new List<Vector3> { escapePoint });
```

---
**Autor**: Detector de Baches
**Versión**: 1.0
**Fecha**: Febrero 2026
