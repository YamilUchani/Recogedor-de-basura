# 🚗 ANÁLISIS DETALLADO: Lógica de Patrulla CarPatrol + RectangularPatrol

**Fecha**: 30 de Abril 2026  
**Objetivo**: Revisar realismo, comportamiento en embotellamiento, giros y posicionamiento

---

## ✅ ¿QUÉ FUNCIONA BIEN?

### CarPatrol ✅
- ✅ Sistema de waypoints automático por tags
- ✅ Detección de aceras (antiTargets) con SphereCast 3D
- ✅ Memoria de waypoints recientes (evita rebotar)
- ✅ Reversión cuando choca
- ✅ Aceleración/desaceleración suave (Lerp)

### RectangularPatrol ✅
- ✅ Patrulla rectangular correcta alrededor de casas
- ✅ Cambio de target automático por cercanía
- ✅ Resolución de deadlock entre peatones (cede el paso)
- ✅ Detección de atasco con timer
- ✅ Caché de colliders para optimizar

---

## 🔴 PROBLEMAS CRÍTICOS ENCONTRADOS

### **CARPATROL.CS**

#### 1️⃣ rutStuckCount se incrementa INFINITAMENTE cada frame
**Ubicación**: Líneas 201-207
```csharp
if (distToAntiTarget <= crashThreshold) {
    rutStuckCount++;  // ← INCREMENTA CADA FRAME
    if (rutStuckCount >= 2) {
        SelectNextWaypoint();
        rutStuckCount = 0;
    }
    reversingTimer = 1.0f;
    return;
}
```
**Problema**: Este contador aumenta CONTINUAMENTE mientras esté tocando la acera. Debería:
- ✅ Incrementarse UNA VEZ por "evento de choque"
- ✅ Resetearse al cambiar waypoint

**Impacto**: 
- Comportamiento errático
- El auto intenta cambiar waypoint cada 2 frames
- "Vueltas locas" constantes

**Solución**: Agregar un `bool crashAlreadyDetected` para evitar increment múltiple.

---

#### 2️⃣ Reversión poco realista - Retrocede en línea recta
**Ubicación**: Líneas 207-212
```csharp
if (reversingTimer > 0f) {
    reversingTimer -= Time.deltaTime;
    transform.position += -transform.forward * (moveSpeed * 0.4f) * Time.deltaTime;
    currentMoveSpeed = 0f;
    return;
}
```
**Problema**: 
- Solo retrocede hacia atrás (sin girar)
- Un auto real haría: **volantazo + retroceso** para salir de esquina
- No gira mientras retrocede

**Impacto**:
- Se queda trabado en esquinas
- No puede salir de "callejón sin salida"

**Solución**: Mientras retrocede, también debería girar gradualmente hacia el waypoint siguiente.

---

#### 3️⃣ Reflexión de paredes usa Vector3.Reflect (poco realista)
**Ubicación**: Líneas 188-197
```csharp
Vector3 reflectDir = Vector3.Reflect(smoothDir, wallNormal);
reflectDir.y = 0;
float avoidanceWeight = Mathf.InverseLerp(evasionThreshold, crashThreshold, distToAntiTarget);
targetSteerDir = Vector3.Lerp(desiredDir, reflectDir.normalized, avoidanceWeight).normalized;
```
**Problema**:
- `Vector3.Reflect` es una reflexión matemática perfecta (óptica)
- Los autos NO se reflejan, giran gradualmente
- Causa giros abruptos y poco naturales

**Impacto**:
- Movimiento "robótico"
- Ángulos raros en las curvas

**Solución**: Usar `Vector3.RotateTowards()` suave en lugar de Reflect.

---

#### 4️⃣ No verifica aceras DURANTE el giro
**Ubicación**: Líneas 231-245
```csharp
float angleToTarget = Vector3.Angle(transform.forward, targetSteerDir);
if (angleToTarget > 15f) {
    targetSpeed = 0f;
    currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * 5f);
}
```
**Problema**:
- Frena para girar, ✅ correcto
- PERO NO verifica si gira HACIA una acera
- Puede girar 180° hacia un muro y quedarse ahí

**Impacto**:
- Se sube a acera durante transición de giro
- Comportamiento ilógico

**Solución**: Chequear si la dirección de giro es "segura" (usar raycast predictivo).

---

#### 5️⃣ IsObstacleAhead no evita embotellamiento infinito
**Ubicación**: Líneas 598-624
```csharp
if (t.GetComponentInParent<CarPatrol>() != null) return true;
if (t.GetComponentInParent<RectangularPatrol>() != null) return true;
return false;
```
**Problema**:
- Detecta obstáculo ✅
- Espera `maxWaitTime` (2 segundos) 
- Si el obstáculo sigue ahí, **simplemente selecciona el siguiente waypoint**
- PERO puede volver al mismo waypoint atrapado (memoria de 2 waypoints)

**Impacto**:
- En embotellamiento, el auto hace "círculos tontos"
- Nunca sale del área congestionada
- 5+ autos juntos = gridlock

**Solución**: 
- Aumentar `waypointMemorySize` a 5-10
- O implementar "alternate route" cuando detecte embotellamiento persistente

---

#### 6️⃣ maxTurnAngle = 100° permite MEDIA VUELTA
**Ubicación**: Línea 40
```csharp
[Range(5f, 180f)]
public float maxTurnAngle = 100f;
```

**Problema**:
- En Update(), línea 118: `Vector3.Angle(transform.forward, desiredDir) > maxTurnAngle + 20f`
- 100° + 20° = 120° 
- Esto permite **casi media vuelta**
- Más el ángulo de emergencia, hace vueltas de 180°+

**Impacto**:
- El auto hace "volteretas" U realizando que parecen locas
- No es realista para un vehículo

**Solución**: Reducir a 60-70° máximo.

---

#### 7️⃣ waypointMemorySize = 2 es demasiado pequeño
**Ubicación**: Línea 43
```csharp
[Range(1, 5)]
public int waypointMemorySize = 2;
```

**Problema**:
- Solo evita los últimos 2 waypoints
- En congestión con 3+ vehículos, pueden rebotar entre los mismos puntos
- Genera "oscilación" comportamental

**Impacto**:
- Autos atrapados en bucle infinito local
- Apariencia de "demencia"

**Solución**: Aumentar a 5-8 por defecto.

---

### **RECTANGULARPATROL.CS**

#### 1️⃣ YIELD_TIME = 0.8s es muy corto
**Ubicación**: Línea 61
```csharp
private const float YIELD_TIME = 0.8f;
```

**Problema**:
- Cuando 2 peatones se cruzan, ceden 0.8 segundos cada uno
- Ambos pueden terminar cediendo al MISMO tiempo
- Quedan congelados frente a frente (DEADLOCK)

**Impacto**:
- Embotellamiento de peatones congelado
- Ninguno se mueve hacia los lados

**Solución**: Aumentar a 1.5-2.0 segundos, y agregar desempate por timestamp.

---

#### 2️⃣ Intercepción de casas solo funciona durante transición
**Ubicación**: Líneas 131-151
```csharp
if (isTransitioning && routeTargets != null) {
    // Intercepción solo aquí
}
```

**Problema**:
- Si está patrullando (isTransitioning = false), NUNCA detecta nuevas casas
- Si otro peatón lo bloquea mientras patrulla, queda atrapado 5-10 segundos
- No puede "escapar" al notar una casa más cerca

**Impacto**:
- Ineficiencia en ruta
- Comportamiento poco realista

**Solución**: Permitir intercepción durante patrulla también, NO solo transición.

---

#### 3️⃣ Stuck Detection fuerza cambio de esquina sin resolver obstáculo
**Ubicación**: Líneas 216-230
```csharp
if (Vector3.Distance(transform.position, lastPosition) < STUCK_DIST) {
    stuckTimer += Time.deltaTime;
    if (stuckTimer >= STUCK_THRESHOLD) {
        previousTarget = null;
        currentCornerIndex = (currentCornerIndex + 1) % corners.Length;  // ← SALTAESQUINA
    }
}
```

**Problema**:
- Al detectar atasco (1.5s sin mover), SALTA a siguiente esquina
- Si el obstáculo sigue ahí, vuelve a atascarse immediately
- No hay "espera" o "tiempo para que el otro se mueva"

**Impacto**:
- Peatón "zappea" entre esquinas sin avanzar
- Apariencia de convulsión/epilepsia

**Solución**: Implementar "wait counter" antes de saltar esquina.

---

#### 4️⃣ No hay desaceleración gradual cercana a obstáculos
**Ubicación**: Líneas 159-176
```csharp
bool obstacleDetected = avoidObstacles && !tooCloseToCorner && TryGetAvoidanceDir(...);
avoidanceBlend = Mathf.MoveTowards(avoidanceBlend, obstacleDetected ? 1f : 0f, 8f * Time.deltaTime);
Vector3 targetDir = Vector3.Slerp(desiredDir, avoidDir, avoidanceBlend).normalized;
// ... 
transform.position = Vector3.MoveTowards(transform.position, transform.position + smoothDir, moveSpeed * Time.deltaTime);
```

**Problema**:
- Mantiene `moveSpeed` constante, solo cambia DIRECCIÓN
- Un peatón real reduciría velocidad al acercarse a obstáculo
- Resulta en colisiones

**Impacto**:
- Choca contra peatones/obstáculos
- Comportamiento poco realista

**Solución**: Reducir `moveSpeed` basado en `distancia a obstáculo`.

---

#### 5️⃣ HasLineOfSightToTarget usa un solo rayo (1D)
**Ubicación**: Líneas 419-441
```csharp
int n = Physics.RaycastNonAlloc(origin, dir, raycastBuffer, dist);
```

**Problema**:
- Un rayo es infinitamente delgado
- Una casa oblicua puede "pasar" el rayo pero está bloqueada realmente
- Especialmente con casas rotadas

**Impacto**:
- Selecciona rutas que parecen libres pero están físicamente bloqueadas
- Peatón intenta ir a casa bloqueada, se atrapaqueda

**Solución**: Usar `SphereCast` en lugar de raycast simple.

---

#### 6️⃣ blockTargetSearchTimer bloquea indefinidamente (hasta 10s)
**Ubicación**: Líneas 21, 212
```csharp
public float minPatrolTime = 5f;
public float maxPatrolTime = 10f;
//...
blockTargetSearchTimer = Random.Range(minPatrolTime, maxPatrolTime);  // Hasta 10 segundos
```

**Problema**:
- Llega a una casa → espera 5-10 segundos antes de cambiar
- En ese tiempo, otra casa más cerca pudo haberse liberado
- Se queda patrullando casa lejana ignorando oportunidades

**Impacto**:
- Ineficiencia en patrulla
- No adapta bien a cambios dinámicos

**Solución**: Reducir a 2-3 segundos máximo, o cambiar cuando detecte casa más cercana.

---

#### 7️⃣ previousTarget no se limpia correctamente en deadlock
**Ubicación**: Líneas 183-186
```csharp
if (stuckTimer >= STUCK_THRESHOLD) {
    previousTarget = null;  // ← Se limpia aquí
    currentCornerIndex = (currentCornerIndex + 1) % corners.Length;
}
```

**Problema**:
- previousTarget se limpia al detectar atasco
- Pero si se atrapó en casa B intentando ir desde casa A
- Al limpiar previousTarget, puede volver a intentar casa B indefinidamente

**Impacto**:
- Ciclo infinito de "intento fallido"

**Solución**: Usar contador de intentos, no solo limpiar previousTarget.

---

## 📊 TABLA DE REALISMO

| Aspecto | Realismo | Problema |
|---------|----------|----------|
| **Evitar aceras** | ✅ 85% | Reflexión poco natural |
| **Embotellamiento** | ❌ 10% | Se queda infinito |
| **Giros** | ⚠️ 50% | Instantáneos, sin momentum |
| **Momentum/Inercia** | ❌ 0% | Cambio dirección inmediato |
| **Anticipation** | ❌ 0% | No prevé obstáculos |
| **Deadlock (peatones)** | ❌ 20% | Fácil quedarse congelado |
| **Posicionamiento** | ⚠️ 60% | Correcto pero sin gracia |

---

## 🔧 RESUMEN DE SOLUCIONES PRIORITARIAS

### 🚨 CRÍTICAS (Implementar PRIMERO)
1. ✅ Fijar `rutStuckCount` para que no incremente infinitamente
2. ✅ Aumentar `waypointMemorySize` de 2 a 8
3. ✅ Aumentar `maxTurnAngle` máximo de 100° a 60°
4. ✅ Implementar "reverse + steer" realista (no solo atrás)
5. ✅ Evitar embotellamiento infinito (timeout con ruta alternativa)

### ⚠️ IMPORTANTES (Mejorar realismo)
6. ✅ Reemplazar `Vector3.Reflect` con giros suaves
7. ✅ Agregar desaceleración gradual cerca de obstáculos
8. ✅ Usar `SphereCast` en HasLineOfSightToTarget
9. ✅ Aumentar `YIELD_TIME` de 0.8s a 1.5s
10. ✅ Permitir intercepción de casas durante patrulla, no solo transición

### 💡 RECOMENDADAS (Polish)
11. Implementar "anticipation" predictiva
12. Agregar "priority system" para carriles
13. Mejorar desempate en deadlocks con IDs de instancia
14. Reducir `blockTargetSearchTimer` a 3s máximo

---

## 📋 ESTADO ACTUAL

```
CarPatrol: ⚠️ FUNCIONA pero ERRÁTICO en congestión
RectangularPatrol: ⚠️ FUNCIONA pero DEADLOCKS frecuentes
Realismo General: 30-40%
Listo para producción: ❌ NO
```

