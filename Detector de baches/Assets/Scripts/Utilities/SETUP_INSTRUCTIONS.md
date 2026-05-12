# 🧑 PersonController - Prueba de Cápsula con Ruta Definida

## Resumen de lo Creado

Se ha implementado un sistema completo para crear personas (cápsulas) que se mueven siguiendo una ruta definida de waypoints.

### Archivos Creados

#### 1. **PersonController.cs** 
- Script principal que controla el movimiento de la persona
- Características:
  - Movimiento automático entre waypoints
  - Rotación hacia el destino
  - Sistema de loop (ruta circular o final)
  - Física integrada automáticamente
  - Métodos públicos para control dinámico

#### 2. **PersonWaypointEditor.cs**
- Helper para editar waypoints en el editor
- Métodos útiles:
  - `AddWaypointHere()` - Agregar waypoint en posición actual
  - `AddWaypointRelative()` - Agregar desplazado
  - `PlayRoute()` / `PauseRoute()` - Control durante testing

#### 3. **PersonControllerExample.cs**
- Script de ejemplo con métodos útiles
- `CreatePeople()` - Crear múltiples personas
- `GenerateCircularRoute()` - Ruta en círculo
- `CreatePersonAt()` - Crear persona en posición específica

#### 4. **PERSON_CONTROLLER_README.md**
- Documentación completa en Markdown
- Guía paso a paso
- Ejemplos de código
- Tips y trucos

### Archivos .meta
- `PersonController.cs.meta`
- `PersonWaypointEditor.cs.meta`
- `PersonControllerExample.cs.meta`
- `PERSON_CONTROLLER_README.md.meta`

---

## Quick Start (Inicio Rápido)

### Opción 1: Manual en el Editor
1. **Crear Cápsula**
   - Hierarchy → 3D Object → Capsule
   - Renombrar a "Person"

2. **Agregar PersonController**
   - Seleccionar la cápsula
   - Inspector → Add Component → PersonController

3. **Definir Waypoints**
   - Expandir "Waypoints" en PersonController
   - Aumentar Size a 4
   - Asignar Vector3 para cada waypoint:
     ```
     [0] (0, 0, 0)
     [1] (5, 0, 0)
     [2] (5, 0, 5)
     [3] (0, 0, 5)
     ```

4. **Configurar Movimiento**
   - Move Speed: 2
   - Loop Route: ✓ habilitado
   - Play para ver resultado

### Opción 2: Por Script
```csharp
// En cualquier script
PersonController person = GetComponent<PersonController>();
person.SetWaypoints(new List<Vector3>
{
    new Vector3(0, 0, 0),
    new Vector3(10, 0, 0),
    new Vector3(10, 0, 10)
});
```

### Opción 3: Crear Varias Personas
```csharp
// Usar PersonControllerExample
1. Crear GameObject vacío llamado "PeopleManager"
2. Agregar componente PersonControllerExample
3. Configurar numberOfPeople = 3
4. Click derecho en el componente → "Crear Personas"
```

---

## Parámetros Principales

| Parámetro | Default | Rango | Descripción |
|-----------|---------|-------|-------------|
| Move Speed | 2 | 0.1-10 | Velocidad en m/s |
| Rotation Speed | 5 | 0-20 | Velocidad de giro |
| Waypoint Tolerance | 0.2 | 0.05-1.0 | Distancia para llegar |
| Loop Route | true | bool | ¿La ruta se repite? |
| Capsule Height | 1.8 | 0.5-2.5 | Alto de persona |
| Capsule Radius | 0.3 | 0.1-0.5 | Ancho de persona |

---

## Métodos Públicos Principales

### Control
```csharp
person.SetWaypoints(List<Vector3> waypoints)  // Establecer ruta
person.AddWaypoint(Vector3 position)           // Agregar waypoint
person.SetMoving(bool moving)                  // Pausar/reanudar
person.RestartRoute()                          // Reiniciar desde inicio
```

### Información
```csharp
person.GetCurrentWaypoint()                    // Waypoint objetivo actual
person.GetCurrentWaypointIndex()               // Índice (0, 1, 2...)
person.GetDistanceToCurrentWaypoint()          // Metros faltantes
```

---

## Integración con Detector de Baches

### Ejemplo 1: Detectar Persona cerca del Drone
```csharp
void OnTriggerEnter(Collider col)
{
    PersonController person = col.GetComponent<PersonController>();
    if (person != null)
    {
        // Hacer que la persona se detenga
        person.SetMoving(false);
        Debug.Log("Persona detectada cerca");
    }
}
```

### Ejemplo 2: Hacer que Huya
```csharp
void OnCameraSee(PersonController person)
{
    Vector3 escapePoint = transform.position - dronePosition;
    person.SetWaypoints(new List<Vector3> { escapePoint });
}
```

### Ejemplo 3: Patrulla en Área
```csharp
// Crear patrulla rectangular
List<Vector3> patrol = new List<Vector3>
{
    areaMin,
    areaMin + Vector3.right * areaSize,
    areaMin + Vector3.right * areaSize + Vector3.forward * areaSize,
    areaMin + Vector3.forward * areaSize
};
person.SetWaypoints(patrol);
```

---

## Visualización en Editor

Cuando seleccionas un GameObject con PersonController verás en Scene view:
- 🔵 **Esferas Cyan**: Waypoints no alcanzados
- 🔴 **Esfera Roja**: Waypoint objetivo actual
- 🟢 **Líneas Verdes**: Conexión entre waypoints
- 🟡 **Cuadrado Amarillo**: Solo en modo edición (cápsula)

---

## Velocidades Realistas

| Tipo | Move Speed | Rotation Speed |
|------|-----------|----------------|
| Caminata lenta | 0.5 | 3 |
| Caminata normal | 1.4 | 5 |
| Caminata rápida | 2.5 | 6 |
| Trote | 3.5 | 7 |
| Corrida | 5.0 | 10 |

---

## Notas Técnicas

- **Física Automática**: PersonController crea Rigidbody y CapsuleCollider
- **Modo Edición**: Los gizmos aparecen siempre
- **Modo Play**: El movimiento se actualiza en Update()
- **Compatible con**: NavMesh, raycast, colisiones normales

---

## Archivos Ubicación

```
Assets/Scripts/Utilities/
├── PersonController.cs
├── PersonController.cs.meta
├── PersonWaypointEditor.cs
├── PersonWaypointEditor.cs.meta
├── PersonControllerExample.cs
├── PersonControllerExample.cs.meta
├── PERSON_CONTROLLER_README.md
└── PERSON_CONTROLLER_README.md.meta
```

---

## Próximos Pasos Opcionales

1. **Animaciones**: Agregar AnimationController para caminar/correr
2. **Sonidos**: Pasos detectados por el drone
3. **Visión**: Detectar al drone y reaccionar
4. **Colisiones**: Evitar obstáculos dinámicamente
5. **Interacción**: Entidades NPC más complejas

---

**Proyecto**: Detector de Baches  
**Sistema**: PersonController  
**Fecha**: Febrero 2026  
**Estado**: ✅ Listo para usar

