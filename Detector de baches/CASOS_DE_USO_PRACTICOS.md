# 🎯 CASOS DE USO Y EJEMPLOS PRÁCTICOS

**Documento**: Guía con ejemplos reales de uso  
**Fecha**: Mayo 5, 2026  
**Versión**: 1.0

---

## 📌 CASO DE USO 1: Ver la Simulación en Acción

**Objetivo**: Observar cómo los vehículos patrullan y detectan baches.

**Duración**: ~15 minutos

### Paso a Paso:

**Paso 1: Inicia la aplicación**
```
- Doble clic en "Simulador de Patrullas"
- Espera 3-5 segundos mientras carga
- Ves Mode_Menu con 5 botones grandes
```

**Paso 2: Selecciona "INICIAR SIMULACIÓN"**
```
- Presiona el botón verde [INICIAR SIMULACIÓN]
- Se carga Mode_Load (pantalla de carga)
- Barra progresa de 0% → 100% (15 segundos aprox)
```

**Paso 3: Espera a que se cargue la simulación**
```
Observas:
├─ 0-30%: "Activando objetos..."
├─ 30-60%: "Generando ciudad..."
├─ 60-90%: "Horneando NavMesh..."
└─ 90-100%: "¡Simulación lista!"
```

**Paso 4: Se carga Mode_Model**
```
Ves:
├─ Calle procedural generada
├─ 20-50 casas
├─ 5 vehículos (azules)
├─ 3 peatones (rojos)
├─ 50-200 baches (esferas rojas)
├─ Estadísticas en esquina superior derecha
└─ Panel de control en esquina inferior izquierda
```

**Paso 5: Observa la simulación**
```
Los vehículos:
├─ Se mueven hacia waypoints (intersecciones)
├─ Cuando se acercan, cambian dirección
├─ Cuando tocan un bache, aparece evento en log
└─ Panel Stats muestra FPS y contadores

Los peatones:
├─ Caminan alrededor de las casas (patrulla rectangular)
├─ Evitan a los vehículos
└─ Caminan en círculo
```

**Paso 6: Cambia de cámara**
```
Presiona V (una vez por vista):
├─ V (1ª vez): Vista aérea (arriba mirando abajo)
├─ V (2ª vez): Primera persona (dentro de vehículo)
├─ V (3ª vez): Vista lateral (lado)
└─ V (4ª vez): Vuelve a aérea

Nota: En modo primera persona ves cómo navega el vehículo
```

**Paso 7: Pausa la simulación**
```
Presiona SPACE:
├─ Todo se congela
├─ Panel muestra "[PAUSA]" en lugar de "[REANUDAR]"
├─ Puedes observar detalles sin movimiento

Presiona SPACE de nuevo:
└─ Reanuda movimiento
```

**Paso 8: Abre el profiler**
```
Presiona P:
├─ Se abre ventana de performance
├─ Muestra FPS en tiempo real
├─ Muestra memory usage
└─ Muestra physics calls por frame
```

**Paso 9: Observa los eventos**
```
Busca el Panel_Log en esquina inferior derecha:
├─ "[00:00] Simulación iniciada"
├─ "[00:12] Vehicle_0 detectó bache en (45.2, 0, 32.1)"
├─ "[00:45] Pedestrian_1 alcanzó destino"
└─ Cada evento muestra timestamp
```

**Paso 10: Vuelve al menú**
```
Presiona ESC:
├─ Descarga toda la escena
├─ GC.Collect() libera memoria
└─ Vueltas a Mode_Menu
```

**Resultado**: Viste la simulación completa funcionando en tiempo real.

---

## 📊 CASO DE USO 2: Recopilar Datos para Análisis

**Objetivo**: Simular 5 minutos y exportar datos a CSV para análisis.

**Duración**: ~20 minutos

### Paso a Paso:

**Paso 1: Abre la aplicación**
```
Menú Principal aparece
```

**Paso 2: Presiona "RECOLECCIÓN DE DATOS"**
```
- Botón naranja en Mode_Menu
- Se carga Mode_Load (10 segundos)
- Se carga Mode_Data (sin visual gráfica pesada)
```

**Paso 3: Simulación sin visual comienza**
```
Observas:
├─ Pantalla NEGRA (sin renderizado 3D)
├─ Solo pequeño panel de texto con logs
├─ FPS: 120+ (4x más rápido que visual)
├─ Simulación corre a 4x velocidad real

En background:
├─ Vehículos patrullan
├─ Peatones caminan
├─ Baches se detectan
├─ Eventos se registran en memoria
```

**Paso 4: Espera a que recopile datos**
```
El panel muestra:
├─ [00:00] Simulación iniciada
├─ [00:15] Vehicle_0 activo
├─ [00:45] Vehicle_1 detectó bache
├─ [01:12] Pedestrian_2 activo
├─ [02:30] Primer evento importante
└─ ...más eventos...

Duración: ~5 minutos reales = 20 minutos simulados
```

**Paso 5: Presiona ESC para terminar**
```
El sistema:
├─ Detiene simulación
├─ Calcula estadísticas
├─ Guarda CSV
├─ Guarda JSON
├─ Guarda LOG
└─ Vuelve a Mode_Menu
```

**Paso 6: Verifica los archivos generados**
```
Abre explorador de archivos:
Navega a: Assets/Output/SimulationData_[TIMESTAMP]/

Ves archivos:
├─ events.csv (1000+ líneas)
│  ├─ timestamp,eventType,vehicleID,x,y,z,severity
│  ├─ 12.34,POTHOLE_DETECTED,0,45.2,0.0,32.1,0.85
│  ├─ 45.67,COLLISION,1,120.0,0.0,100.0,0.50
│  └─ ...
│
├─ statistics.json
│  ├─ "total_events": 1234
│  ├─ "total_potholes": 45
│  ├─ "total_collisions": 12
│  ├─ "avg_vehicle_speed": 7.8
│  ├─ "path_coverage_km": 45.3
│  ├─ "simulation_duration": 300
│  └─ ...
│
└─ simulation.log
   ├─ Logs detallados por segundo
   ├─ [00:00] Simulation started
   ├─ [00:01] Vehicle_0 initialized
   └─ ...
```

**Paso 7: Análisis en Excel**
```
Abre CSV en Excel:
├─ 1000+ filas de datos
├─ Columnas: timestamp, type, vehicleID, x, y, z, severity

Crea gráficos:
├─ Histograma de baches detectados por hora
├─ Mapa de calor de posiciones (X, Z)
├─ Gráfico de velocidades promedio por vehículo
└─ Análisis de patrones de patrullaje
```

**Resultado**: Exportaste 5 minutos de simulación a datos analizables.

---

## 📷 CASO DE USO 3: Capturar Dataset de Baches

**Objetivo**: Generar 100+ imágenes de baches para entrenar modelo de IA.

**Duración**: ~30 minutos

### Paso a Paso:

**Paso 1: Inicia la aplicación**
```
Mode_Menu aparece
```

**Paso 2: Presiona "MODO CAPTURA"**
```
- Se carga Mode_Capture directo (sin Mode_Load)
- Ves cámara drone mirando hacia abajo
- Panel de control con botones
```

**Paso 3: Generaposición de baches**
```
Panel muestra:
├─ "[GENERAR NUEVOS BACHES]"
├─ "[CAPTURAR SCREENSHOT]"
├─ "[MODO AUTO]" checkbox
└─ Altura: ████░░░ 15.2m
```

**Paso 4: Presiona "[GENERAR NUEVOS BACHES]"**
```
Sistema:
├─ Destruye baches anteriores
├─ Genera 50-200 nuevos con semilla aleatoria
├─ Posiciona cámara a altura 15m
└─ Baches visibles desde arriba (esferas rojas)
```

**Paso 5: Posiciona la cámara (Opción A: Manual)**
```
Controles:
├─ ↑ / ↓ teclas: Sube/baja cámara
├─ W / S: Adelante/atrás
├─ A / D: Izquierda/derecha
├─ Mouse rueda: Zoom
└─ Mouse botón derecho: Rotar vista

Objetivo:
└─ Posicionar cámara para ver baches claramente
```

**Paso 6: Captura screenshot único**
```
Presiona "[CAPTURAR SCREENSHOT]":
├─ Se toma foto 1270x950 px
├─ Se guarda en Assets/Captures/Pothole_Dataset/Images/
├─ Se crea JSON metadata con:
│  ├─ Posición cámara
│  ├─ Bounding boxes de baches visibles
│  ├─ Severidad de cada bache
│  └─ Timestamp
└─ Contador: "Baches capturados: 1/100"
```

**Paso 7: Opción B - Modo Automático**
```
Marca checkbox "[MODO AUTO]":
├─ Cada 2 segundos:
│  ├─ Verifica visibilidad de baches
│  ├─ Si visibilidad > 40%:
│  │  ├─ Toma screenshot
│  │  ├─ Guarda PNG + JSON
│  │  └─ Mueve cámara leve
│  └─ Si visibilidad < 40%:
│     └─ Reposiciona cámara
│
├─ Continúa hasta ~100 imágenes
└─ Toma ~5-10 minutos
```

**Paso 8: Genera nuevo lote**
```
Presiona "[GENERAR NUEVOS BACHES]" nuevamente:
├─ Nueva semilla aleatoria
├─ 50-200 nuevos baches
├─ Repite captura automática
└─ Acumula más imágenes
```

**Paso 9: Repite 3-5 veces para tener 300-500 imágenes**
```
Estadísticas acumuladas:
├─ Total imágenes: ~400
├─ Baches únicos: ~2000 (porque varían con cada generación)
├─ Tiempo total: ~2 horas (todo automático)
└─ Dataset listo para IA
```

**Paso 10: Verifica dataset**
```
Explorador:
Assets/Captures/Pothole_Dataset_2026-05-05/
├─ Images/
│  ├─ pothole_0001.png (1270x950)
│  ├─ pothole_0002.png
│  ├─ ...
│  └─ pothole_0423.png
├─ Metadata/
│  ├─ pothole_0001.json
│  │  {
│  │    "camera": {"x": 45.2, "y": 15.0, "z": 32.1},
│  │    "bounding_boxes": [
│  │      {"id": 0, "x": 100, "y": 250, "w": 50, "h": 40}
│  │    ]
│  │  }
│  └─ ...
└─ Index/
   └─ dataset_index.csv (lista de todas imágenes)
```

**Paso 11: Sube a servidor de IA**
```
├─ Comprime dataset
├─ Sube a Google Cloud / AWS
├─ Entrena modelo YOLOv8 / R-CNN
└─ Obtén detector de baches personalizado
```

**Resultado**: Generaste dataset de 400+ imágenes de baches para entrenar modelos de IA.

---

## 🐛 CASO DE USO 4: Debugging de Comportamiento

**Objetivo**: Investigar por qué un vehículo no patrulla correctamente.

**Duración**: ~20 minutos

### Paso a Paso:

**Paso 1: Inicia en Modo Debug**
```
Mode_Menu → [MODO DEBUG]
Se carga Mode_Debug (10-15 segundos)
```

**Paso 2: Se abre Panel de Debug**
```
Ves panel grande en pantalla:
├─ Simulation Control (botones pause/step)
├─ Vehicle Control (selecciona Vehicle_0..4)
├─ Waypoint Editor (muestra o edita waypoints)
├─ Performance (gráficos FPS, memory)
└─ Events Log (últimos 50 eventos)
```

**Paso 3: Selecciona el vehículo a investigar**
```
En "Vehicle Control":
├─ Dropdown muestra: Vehicle_0, Vehicle_1, ..., Vehicle_4
├─ Selecciona: Vehicle_2 (el problemático)
└─ Ahora todos los comandos afectan a Vehicle_2
```

**Paso 4: Pausa la simulación**
```
Presiona [PAUSE]:
├─ timeScale = 0 (simulación congelada)
├─ Botón cambia a [STEP]
└─ Puedes avanzar 1 frame a la vez
```

**Paso 5: Visualiza todos los waypoints**
```
Presiona "[Show All Waypoints]":
├─ Se dibuja círculo verde en cada waypoint
├─ Ves 30-40 puntos distribuidos en la ciudad
├─ Ahora es claro dónde debería navegar
```

**Paso 6: Verifica ruta actual de Vehicle_2**
```
Panel muestra:
├─ Current waypoint: "Waypoint_12" en (120.5, 0, 85.2)
├─ Distance: 5.3 metros
├─ Current speed: 0 m/s (pausado)
├─ Desired speed: 8.5 m/s (cuando reanude)
```

**Paso 7: Activa Physics Debug Draw**
```
Presiona [Toggle Physics Debug Draw]:
├─ Se ven todos los colliders como wireframes
├─ Ves límites de casas (cajas amarillas)
├─ Ves zona de colisión del vehículo (cápsula azul)
├─ Ves baches como esferas rojas
```

**Paso 8: Avanza paso a paso**
```
Presiona [STEP] múltiples veces:
├─ Frame 1: Vehicle_2 continúa hacia waypoint
├─ Frame 2: Evita objeto (gizmo azul)
├─ Frame 3: Cambio de dirección
├─ Frame 4: Acelera hacia nuevo waypoint

Observa en Panel_Events cada cambio registrado
```

**Paso 9: Ajusta parámetros en tiempo real**
```
"Vehicle Control" Sliders:
├─ Speed: mueve a ████░░░░░ 10.0 m/s
├─ Accel: mueve a ██░░░░░░░░ 1.0 m/s²
└─ Ves cambio inmediato en comportamiento (cuando reanudes)
```

**Paso 10: Teleporta a waypoint específico**
```
Presiona "[Teleport to Waypoint]":
├─ Vehicle_2 se teletransporta a waypoint aleatorio
├─ Útil para testear navegación desde diferentes posiciones
└─ Observa comportamiento en nueva posición
```

**Paso 11: Reanuda y observa**
```
Presiona [PAUSE] nuevamente (o SPACE):
├─ timeScale = 1 (simulación corre)
├─ Vehicle_2 continúa con nuevos parámetros
├─ Velocidad: 10.0 m/s (la que ajustaste)
└─ Observa si comportamiento es correcto ahora
```

**Paso 12: Exporta debug log**
```
Presiona "[Export Log]":
├─ Se guarda archivo: debug_log_[timestamp].txt
├─ Contiene últimos 50 eventos
├─ Útil para reportar bugs
```

**Resultado**: Debuggeaste comportamiento de vehículo y ajustaste parámetros.

---

## 📈 CASO DE USO 5: Comparar Múltiples Simulaciones

**Objetivo**: Ejecutar 3 simulaciones diferentes y comparar resultados.

**Duración**: ~1 hora

### Paso a Paso:

**Simulación 1: Configuración Base**
```
1. Mode_Menu → RECOLECCIÓN DATOS
2. Espera 5 minutos de simulación
3. ESC → Guardar en: Output/Sim_Base_001/
4. Archivo: events.csv, statistics.json
```

**Simulación 2: Velocidad 2x**
```
1. Mode_Menu → MODO DEBUG → Load
2. Espera a que cargue
3. En Panel: Speed slider = 2x normal
4. Presiona SPACE para reanudar
5. Espera 5 minutos
6. ESC → Guardar en: Output/Sim_Speed2x_001/
```

**Simulación 3: Más baches**
```
1. (Requiere editar parámetro en código)
2. Abre: Assets/Scripts/Terrain/TerrainPotholeGenerator.cs
3. Cambia: maxPotholes = 400 (en lugar de 200)
4. Mode_Menu → RECOLECCIÓN DATOS
5. Espera 5 minutos (más baches detectados)
6. ESC → Guardar en: Output/Sim_MorePotholes_001/
```

**Análisis Comparativo**
```
Abre Excel y carga 3 archivos CSV:

Comparación por métrica:
├─ Total Baches Detectados:
│  ├─ Base: 45
│  ├─ Speed2x: 62 (más patrullaje)
│  └─ MorePotholes: 89 (más baches presentes)
│
├─ Promedio Velocidad Vehículos:
│  ├─ Base: 7.8 m/s
│  ├─ Speed2x: 15.6 m/s (2x)
│  └─ MorePotholes: 7.8 m/s (igual)
│
└─ Distancia Recorrida Total:
   ├─ Base: 234 km
   ├─ Speed2x: 468 km (2x)
   └─ MorePotholes: 245 km (similar)

Conclusiones:
├─ Duplicar velocidad no duplica detecciones (física limitaciones)
├─ Más baches = más detecciones (proporcional)
└─ Optimizaciones son ineficientes
```

**Resultado**: Comparaste impacto de diferentes parámetros en simulación.

---

## 🎮 CASO DE USO 6: Sesión de Demostración Ejecutivo

**Objetivo**: Hacer presentación de 15 minutos mostrando capacidades.

**Duración**: ~20 minutos (15 demo + 5 setup)

### Guión:

**Minuto 0-2: Setup**
```
- Abre aplicación
- Presiona INICIAR SIMULACIÓN
- Se carga (15s) mientras explicas
```

**Minuto 2-5: Vista General**
```
- Muestra simulación cargada
- Explica: "Ves 5 vehículos patrullando"
- Señala: Vehículos azules, peatones rojos
- Muestra: Estadísticas FPS, contadores
```

**Minuto 5-8: Interactividad**
```
- Presiona V para cambiar cámara
  "Podemos ver la ciudad desde 3 ángulos diferentes"
- Presiona SPACE para pausa
  "Podemos pausar para analizar cualquier momento"
- Presiona SPACE nuevamente para reanudar
```

**Minuto 8-12: Detección de Baches**
```
- Espera hasta que se detecte un bache
- Señala evento en Panel_Log:
  "[12:34] Vehicle_0 detectó bache en (45.2, 0, 32.1)"
- Explica: "Cada detección se registra automáticamente"
- Muestra gráfico rojo en 3D donde está bache
```

**Minuto 12-15: Capacidades Adicionales**
```
- Explica: "Podemos capturar imágenes para entrenar IA"
- Muestra carpeta Captures con imágenes previas
- Explica: "Cada imagen tiene metadata (posición, severidad)"
- Menciona: "Dataset de 1000+ imágenes disponible"
```

**Minuto 15-18: Datos**
```
- Muestra archivo CSV previo
- "1234 eventos capturados en 5 minutos"
- Explica columnas: timestamp, type, position, severity
- Muestra gráficos Excel generados
```

**Minuto 18-20: Cierre**
```
- Presiona ESC para volver a menú
- Resumen de capacidades
- Preguntas/Respuestas
```

**Resultado**: Exitosa demostración de todas las capacidades.

---

## 💡 TIPS Y TRUCOS

```
📌 PARA OBTENER MEJORES DATOS:
├─ Simula al menos 5-10 minutos (más eventos)
├─ Sube baches a máximo (TerrainPotholeGenerator)
├─ Reduce velocidad de vehículos (más tiempo detectando)
└─ Ejecuta múltiples sesiones (variabilidad)

🎮 PARA MEJOR VISUALIZACIÓN:
├─ Usa vista aérea (V) para ver patrón general
├─ Pausa (SPACE) para analizar detalles
├─ Profiler (P) para verificar rendimiento
└─ Debug mode (V) para gizmos de waypoints

⚡ PARA OPTIMIZAR RENDIMIENTO:
├─ Usa Mode_Data en lugar de Mode_Model (4x más rápido)
├─ Reduce resolución de screenshots (Mode_Capture)
├─ Limpia output files viejos (Assets/Output/)
└─ Cierra otras aplicaciones pesadas

📊 PARA ANÁLISIS:
├─ Exporte a CSV + gráficos Excel
├─ Busque patrones en timestamps
├─ Correlacione velocidad con detecciones
└─ Compare múltiples simulaciones
```

---

**Fin de Casos de Uso** ✨

*Estos ejemplos muestran cómo usar cada escena en situaciones reales.*
