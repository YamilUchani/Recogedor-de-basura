# PersonController - Guía de Evitación

## 🎯 Visión General

El sistema de **evitación automática** permite que múltiples personas se detecten mutuamente y se eviten sin colisionar, manteniéndose dentro de un área de influencia definida.

## ⚙️ Cómo Funciona

### Detección
1. Cada persona detecta otras personas en un radio definido (`Detection Radius`)
2. Si hay personas cercanas, calcula una dirección de escape
3. Si la distancia es muy pequeña (`Pause When Too Close`), pausa completamente

### Evitación
1. Desvía su dirección original hacia la dirección de escape
2. El desvío se controla con `Avoidance Force` (0-1)
3. Después de evadir, continúa hacia su waypoint original

### Confinamiento
1. Si `Constrain To Area` está habilitado, el movimiento se limita
2. No puede salir del área rectangular definida
3. Útil para evitar que personas escapen de una zona

## 🔧 Configuración Básica

### Paso 1: Crear una Persona con Evitación

```csharp
GameObject personGO = new GameObject("Person");
PersonController person = personGO.AddComponent<PersonController>();

// Agregar waypoints
person.SetWaypoints(waypoints);

// Configurar área
person.SetInfluenceArea(
    center: new Vector3(0, 0, 0),
    size: new Vector3(20, 2, 20)
);

// Habilitar evitación
person.SetAvoidanceEnabled(true);
```

### Paso 2: Ajustar Parámetros en el Inspector

**Para zona pequeña (5m × 5m):**
- Detection Radius: 1.5
- Pause When Too Close: 0.5
- Avoidance Force: 0.6

**Para zona mediana (20m × 20m):**
- Detection Radius: 2.5
- Pause When Too Close: 0.8
- Avoidance Force: 0.5

**Para zona grande (50m × 50m):**
- Detection Radius: 3.5
- Pause When Too Close: 1.0
- Avoidance Force: 0.4

## 📊 Parámetros Detallados

### Detection Radius (Rango de Detección)
- **Rango**: 0.5 - 10 metros
- **Default**: 2 metros
- **Función**: Distancia máxima para detectar otras personas
- **Efecto en rendimiento**: Mayor radio = más cálculos

```
Pequeño (1.5m) = Evitación muy tarde, casi tangecio
Normal (2m)    = Equilibrio entre detección y naturalidad
Grande (4m)    = Evita bien antes, pero a veces excesivo
```

### Pause When Too Close (Distancia de Pausa)
- **Rango**: 0.3 - 2 metros
- **Default**: 0.8 metros
- **Función**: Distancia a la que pausa completamente
- **Sensibilidad**: Valor bajo = pausa casi nunca, valor alto = pausa frecuente

```
Pequeño (0.5m) = Solo pausa si casi chocan
Normal (0.8m)  = Pausa natural cuando está cerca
Grande (1.2m)  = Pausa anticipadamente
```

### Avoidance Force (Intensidad del Desvío)
- **Rango**: 0 - 1.0
- **Default**: 0.5
- **Función**: Cuánto desvía su trayectoria

```
Bajo (0.2)   = Desvío leve, mantiene rumbo principal
Normal (0.5) = Equilibrio evitación/objetivo
Alto (0.8)   = Desvío pronunciado
```

### Influence Area Center & Size
- **Center**: Posición del centro del área
- **Size**: Dimensiones (ancho, alto, profundidad)
- **Ejemplo**: Center: (0,0,0), Size: (20,2,20) = Área 20×20m

## 🎮 Uso Avanzado

### Script: Monitorear Evitación

```csharp
void Update()
{
    if (person.IsAvoiding())
    {
        Debug.Log("¡Persona evitando a otra!");
        audioSource.PlayOneShot(avoidanceSound);
    }
}
```

### Script: Múltiples Personas Coordinadas

```csharp
List<PersonController> people = new List<PersonController>();

for (int i = 0; i < 5; i++)
{
    GameObject personGO = new GameObject($"Person_{i}");
    PersonController person = personGO.AddComponent<PersonController>();
    
    // Todas en la misma área
    person.SetInfluenceArea(areaCenter, areaSize);
    people.Add(person);
}
```

### Script: Área Dinámicamente Cambiante

```csharp
public void MoveAreaCenter(Vector3 newCenter)
{
    foreach (var person in people)
    {
        person.SetInfluenceArea(newCenter, areaSize);
    }
}
```

## 🚀 Ejemplos Prácticos

### Ejemplo 1: Guardias Patrullando sin Chocar

```csharp
// DemoPersonInScene.cs ya incluye esto
1. Crear GameObject con DemoPersonInScene
2. Set numberOfPeople = 3
3. Click derecho → "Crear Guardias"
4. Verás 3 personas patrullando sin chocar
```

### Ejemplo 2: Multitud en una Plaza

```csharp
PersonAvoidanceDemo demo = GetComponent<PersonAvoidanceDemo>();
demo.numberOfPeople = 10;
demo.areaSize = new Vector3(30, 2, 30);
demo.detectionRadius = 3f;
demo.CreatePeopleWithAvoidance();
```

### Ejemplo 3: Pánico - Huir de un Punto

```csharp
void MakePeopleFlee(Vector3 dangerPoint)
{
    foreach (var person in people)
    {
        Vector3 escapeDir = (person.transform.position - dangerPoint).normalized;
        Vector3 escapePos = person.transform.position + escapeDir * 10f;
        
        person.SetWaypoints(new List<Vector3> { escapePos });
    }
}
```

## 🐛 Troubleshooting

### Problema: Las personas no se evitan
**Soluciones:**
- Verificar que `Enable Avoidance` = true
- Aumentar `Detection Radius`
- Reducir `Move Speed` para que haya tiempo de reacción

### Problema: Evitan demasiado (casi no se mueven)
**Soluciones:**
- Reducir `Detection Radius`
- Aumentar `Pause When Too Close`
- Reducir `Avoidance Force`

### Problema: Las personas se salen del área
**Soluciones:**
- Verificar que `Constrain To Area` = true
- Revisar que `Influence Area Size` sea lo suficientemente grande
- Aumentar `Avoidance Force` para mayor fuerza de contención

### Problema: Performance lento con muchas personas
**Soluciones:**
- Reducir número de personas
- Reducir `Detection Radius`
- Desactivar gizmos (`Show Avoidance Radius` = false)

## 📈 Rendimiento

### Costo por Persona
- **Physics.OverlapSphere**: ~0.1ms (depende del radius)
- **Cálculos de evitación**: ~0.05ms
- **Total**: ~0.15ms por persona

### Recomendaciones
- Hasta 10 personas: Sin problemas
- 10-20 personas: Reducir detection radius
- 20+ personas: Considerar usar NavMesh + IA

## 🎨 Visualización en Editor

### Gizmos Disponibles

**Show Avoidance Radius** (solo en Play)
- Círculo amarillo alrededor de la persona
- Muestra el área de detección
- Útil para debuggear

**Show Influence Area**
- Caja naranja semi-transparente
- Muestra el área donde se confina
- Visible en Edit y Play mode

```csharp
// Activar en Inspector:
1. Seleccionar persona
2. Check "Show Avoidance Radius"
3. Check "Show Influence Area"
4. Play
```

## 🔌 Integración con Drone

```csharp
void OnDroneNear(DroneNavMeshController drone)
{
    // Hacer que las personas huyan
    foreach (var person in people)
    {
        Vector3 escapeDir = (person.transform.position - drone.transform.position).normalized;
        person.SetWaypoints(new List<Vector3> 
        { 
            person.transform.position + escapeDir * 5f 
        });
    }
}
```

---

**Versión**: 1.0
**Fecha**: Febrero 2026
**Proyecto**: Detector de Baches
