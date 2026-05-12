# 📚 GUÍA VISUAL ULTRA DE ESCENAS - Simulador de Patrullas

**Documento**: Referencia rápida visual de todas las escenas  
**Fecha**: Mayo 5, 2026  
**Versión**: 2.0

---

## 🗺️ MAPA DE NAVEGACIÓN DE ESCENAS

```
┌──────────────────────────────────────────────────────────────────┐
│                     PUNTO DE ENTRADA                              │
│                    (Aplicación Inicia)                            │
└──────────────────────┬───────────────────────────────────────────┘
                       │
                       ▼
         ┌─────────────────────────────────┐
         │   MODE_MENU.unity 🎮            │
         │                                  │
         │  [INICIAR] [DEBUG] [DATOS]      │
         │  [CAPTURA] [CONFIG] [SALIR]    │
         └─────────┬─────┬─────┬──────────┘
                   │     │     │
         ┌─────────▼┐    │     │
         │   sceneIndex=1 │     │     sceneIndex=2
         │                │     │
         │       sceneIndex=0  │
         │                │     │
    ┌────▼─────────────┬─▼─┬───▼──────────┐
    │  MODE_LOAD       │   │              │
    │  (Loading...)    │   ▼              ▼
    │                  │  MODE_DEBUG    MODE_CAPTURE
    │ [Barra progreso] │  🐛             📷
    │ [Animación]      │
    │                  │
    │  50-70%: Load    │   Direct Load   Direct Load
    │  70-90%: NavMesh │   (DEBUG)       (CAPTURE)
    │  90-100%: Init   │
    └────┬─────────────┘
         │
    ┌────▼──────────────┐
    │  Carga Aditiva    │
    │  de:              │
    │  ├─ MODE_MODEL    │
    │  ├─ MODE_DATA     │
    │  └─ MODE_CAPTURE  │
    └────┬──────────────┘
         │
    ┌────▼─────────────────────────────┐
    │ ESCENA ACTIVA (una de las 3)      │
    │                                   │
    │ ├─ MODE_MODEL (Simulación)       │
    │ ├─ MODE_DATA (Sin visual)         │
    │ └─ MODE_CAPTURE (Captura datos)  │
    │                                   │
    │ [Simulación en marcha...]        │
    │ [Usuario interactúa]            │
    │ [ESC para volver]               │
    └────┬─────────────────────────────┘
         │
         ▼ (Usuario presiona ESC)
    ┌─────────────────────┐
    │  Unload Escena      │
    │  GC.Collect()       │
    │                     │
    │  ↻ Vuelve a Mode_Menu
    └─────────────────────┘
```

---

## 📊 COMPARATIVA DE ESCENAS

```
┌───────────┬──────────────┬──────────────┬──────────────┬──────────────┐
│ ESCENA    │ MODE_MENU    │ MODE_LOAD    │ MODE_MODEL   │ MODE_DATA    │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Propósito │ Seleccionar  │ Mostrar      │ Simular +    │ Simular      │
│           │ modo         │ progreso     │ visualizar   │ sin visual   │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Duración  │ Infinito     │ 10-15s       │ Infinito     │ Infinito     │
│           │ (espera)     │ (loading)    │ (usuario)    │ (usuario)    │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Contenido │ Botones UI   │ Barra        │ Ciudad       │ Ciudad       │
│           │ Canvas       │ progreso     │ completa     │ (simple)     │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Vehículos │ No           │ No           │ Sí (5)       │ Sí (5)       │
│ Peatones  │ No           │ No           │ Sí (3)       │ Sí (3)       │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Baches    │ No           │ No           │ Sí (50-200)  │ Sí (50-200)  │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ FPS       │ 60           │ 30-60        │ 60           │ 120+         │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Memoria   │ 50 MB        │ 200 MB       │ 2-4 GB       │ 500 MB-1 GB  │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Tiempo    │ 1x (real)    │ Variado      │ 1x (real)    │ 4x-8x        │
│ Sim       │              │              │              │ (acelerado)  │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Audio     │ Sí           │ Opcional     │ Sí           │ No           │
├───────────┼──────────────┼──────────────┼──────────────┼──────────────┤
│ Teclas    │ Click        │ Espera       │ V, ESC,      │ V, ESC,      │
│           │              │              │ SPACE, P     │ SPACE, P     │
└───────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

---

## 🎮 TECLAS RÁPIDAS GLOBALES

```
╔════════════════════════════════════════════════════════════╗
║  TECLAS DISPONIBLES EN: MODE_MODEL, MODE_DATA, MODE_DEBUG  ║
╠════════════════════════════════════════════════════════════╣
║                                                             ║
║  [V]          Cambiar cámara (3 vistas)                    ║
║               ├─ Vista aérea                               ║
║               ├─ Primera persona vehículo                  ║
║               └─ Vista lateral                             ║
║                                                             ║
║  [ESC]        Salir a menú                                 ║
║               └─ Guarda datos si estás en Mode_Data        ║
║                                                             ║
║  [SPACE]      Pausa / Reanuda simulación                   ║
║               ├─ Pausa: timeScale = 0                      ║
║               └─ Reanuda: timeScale = 1                    ║
║                                                             ║
║  [P]          Performance Profiler                         ║
║               └─ Abre ventana de stats                     ║
║                                                             ║
║  [↑↓]         Subir / bajar cámara drone                   ║
║               └─ SOLO en modo_capture                      ║
║                                                             ║
║  [W/A/S/D]    Control manual (SOLO DEBUG MODE)             ║
║  [Q]          Frenar (SOLO DEBUG MODE)                     ║
║                                                             ║
╚════════════════════════════════════════════════════════════╝
```

---

## 🎬 ESCENAS ESPECIALIZADAS

### MODE_CAPTURE.unity 📷

```
FLUJO DE CAPTURA DE DATOS:

┌──────────────────────────────────────┐
│ 1. Abrir Mode_Capture                │
│    ├─ Cámara en posición inicial     │
│    └─ Baches generados (50-200)      │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│ 2. Posicionar cámara                 │
│    ├─ ↑/↓ para altura (0.5-25m)      │
│    ├─ W/A/S/D para movimiento        │
│    └─ Mouse rueda para zoom          │
└──────────────┬───────────────────────┘
               │
        ┌──────┴──────┐
        │             │
   ┌────▼─────┐  ┌────▼──────┐
   │ Generación│  │Captura    │
   │botón      │  │botón      │
   └────┬─────┘  └────┬──────┘
        │             │
   ┌────▼─────┐  ┌────▼──────┐
   │ Nuevos   │  │Screenshot │
   │baches    │  │guardado   │
   │generados │  │PNG+JSON   │
   └────┬─────┘  └────┬──────┘
        │             │
        └──────┬──────┘
               │
        ┌──────▼──────────────┐
        │ Repetir pasos 2-3   │
        │ hasta N imágenes    │
        └──────┬──────────────┘
               │
        ┌──────▼──────────────┐
        │Dataset generado:    │
        │/Captures/           │
        │├─ 100+ imágenes     │
        │├─ Metadata JSON     │
        │└─ Index CSV         │
        └─────────────────────┘
```

**Botones de Control**:
```
┌────────────────────────────────────────┐
│       PANEL DE CONTROL MODE_CAPTURE    │
├────────────────────────────────────────┤
│                                        │
│  [GENERAR NUEVOS BACHES]              │
│  ├─ Regenera con nueva semilla        │
│  └─ Limpia baches anteriores          │
│                                        │
│  [CAPTURAR SCREENSHOT]                │
│  ├─ Toma imagen 1270x950              │
│  ├─ Guarda PNG                        │
│  └─ Crea JSON metadata                │
│                                        │
│  [MODO AUTO]  ☑                       │
│  ├─ Captura cada 2 segundos           │
│  ├─ Genera 30+ imágenes automáticas   │
│  └─ Con diferentes ángulos            │
│                                        │
│  ALTURA: ██████░░ 15.2m               │
│  ESCALA: ███░░░░░ 1.0x                │
│                                        │
│  Baches capturados: 42/200            │
│  Visibilidad: 87%                     │
│                                        │
│  [↑]  [↓]  [← MENÚ →]                 │
│   U    D                               │
│                                        │
└────────────────────────────────────────┘
```

---

### MODE_DEBUG.unity 🐛

```
PANEL DE DEBUG:

┌─────────────────────────────────────────────────────┐
│              DEBUG CONTROL PANEL                     │
├─────────────────────────────────────────────────────┤
│                                                      │
│ 🔧 SIMULATION CONTROL                              │
│  [PAUSE]  [STEP]  Time Scale: ███░░ (1.0x)        │
│  ☑ Physics Debug Draw                              │
│                                                      │
│ 🚗 VEHICLE CONTROL                                 │
│  Vehicle: [Dropdown: Vehicle_0 ▼]                  │
│  Speed:      ██████░░ (8.5 m/s)                    │
│  Accel:      ███░░░░░ (2.0 m/s²)                   │
│  [Teleport]  [Clear Path]                          │
│                                                      │
│ 🗺️ WAYPOINT EDITOR                                │
│  [Show All]  [Show Path]  Size: ██░░              │
│  ☑ Lock Waypoints  [Add Custom]                    │
│                                                      │
│ 📈 PERFORMANCE                                      │
│  [Graph: FPS vs Time]   Avg: 58.2 FPS  Peak: 1245 MB
│                                                      │
│ 📋 EVENTS LOG                                       │
│  [00:00] Simulación iniciada                       │
│  [00:01] Vehicle_0 patrulla desde waypoint 5       │
│  [00:45] Pedestrian_1 detectó obstáculo            │
│  [01:12] Pothole detectado en (45.2, 0, 32.1)     │
│  [Filter: All ▼]  [Export Log]  [Clear]            │
│                                                      │
└─────────────────────────────────────────────────────┘
```

---

## 🔄 CICLO COMPLETO DE OPERACIÓN

```
FLUJO TÍPICO DE UN USUARIO:

Inicio App
   │
   ▼
┌─────────────────────────────────────────────────┐
│           MODE_MENU (Menú Principal)            │
│  - Usuario ve 5 botones claros                  │
│  - Puede elegir qué hacer                       │
└─────────────────┬───────────────────────────────┘
                  │
          ┌───────┴────────┐
          │                │
          ▼                ▼
    ┌──────────────┐ ┌──────────────┐
    │Presiona      │ │Presiona      │
    │INICIAR SIM   │ │RECOPILAR     │
    │              │ │DATOS         │
    └──────┬───────┘ └──────┬───────┘
           │                │
           ▼                ▼
    ┌─────────────────────────────┐
    │  MODE_LOAD (10-15s)         │
    │  Barra progreso (0-100%)    │
    │  Inicializa ciudad, NavMesh │
    └──────┬──────────────────────┘
           │
    ┌──────┴──────────────────────────┐
    │                                  │
    ▼                                  ▼
┌──────────────────┐        ┌──────────────────┐
│  MODE_MODEL      │        │   MODE_DATA      │
│  (Visualización) │        │  (Sin visual)    │
│                  │        │                  │
│ - Ciudad visible │        │ - 4x más rápido  │
│ - 3 cámaras      │        │ - CSV+JSON       │
│ - Audio          │        │ - 120+ FPS       │
│ - Control real   │        │ - Análisis       │
│                  │        │                  │
│ Vuelve a menú    │        │ Vuelve a menú    │
│ cuando presiona  │        │ cuando presiona  │
│ ESC              │        │ ESC (+ exporta)  │
└──────┬───────────┘        └──────┬───────────┘
       │                           │
       └─────────────┬─────────────┘
                     │
                     ▼
            ┌─────────────────────┐
            │  Vuelve a MODE_MENU │
            │  (Bucle repetible)  │
            └─────────────────────┘
```

---

## 📌 CHECKLIST DE FUNCIONALIDADES POR ESCENA

### ✅ MODE_MENU.unity
- [x] Botón INICIAR SIMULACIÓN
- [x] Botón MODO DEBUG
- [x] Botón RECOLECCIÓN DE DATOS
- [x] Botón MODO CAPTURA
- [x] Botón CONFIGURACIÓN
- [x] Botón SALIR
- [x] Logo/branding
- [x] Música de fondo

### ✅ MODE_LOAD.unity
- [x] Barra de progreso lineal
- [x] Mensajes de estado dinámicos
- [x] Spinner de carga
- [x] Contador de objetos activados
- [x] Estimación de tiempo restante
- [x] Transición suave a escena destino

### ✅ MODE_MODEL.unity
- [x] 5 vehículos con IA (CarPatrol)
- [x] 3 peatones con rutas (RectangularPatrol)
- [x] 50-200 baches generados
- [x] Detección de colisiones
- [x] 3 cámaras (aérea, 1ªpersona, lateral)
- [x] UI Stats en pantalla
- [x] Panel de controles
- [x] Log de eventos
- [x] Física RVO2
- [x] Pausa/Reanuda
- [x] Botón volver menú

### ✅ MODE_DATA.unity
- [x] Simulación acelerada 4x
- [x] Sin visualización gráfica pesada
- [x] Logging de eventos a CSV
- [x] Generación de JSON stats
- [x] Export de datos automático
- [x] Consumo mínimo de VRAM

### ✅ MODE_CAPTURE.unity
- [x] Cámara drone posicionable
- [x] Generación de baches on-demand
- [x] Captura de screenshots
- [x] Metadata JSON por imagen
- [x] Modo auto-capture
- [x] Dataset organizado
- [x] Controles de altura/zoom

### ✅ MODE_DEBUG.unity
- [x] Gizmos de waypoints visibles
- [x] Physics debug draw
- [x] NavMesh triangulación visible
- [x] Panel de control avanzado
- [x] Logs detallados en consola
- [x] Profiler integrado
- [x] Teleport de agentes
- [x] Real-time parameter tuning

---

## 🎯 FLUJOS POR CASO DE USO

### 📺 Ver Simulación en Acción
```
Menú → INICIAR → Load (10-15s) → Mode_Model → (Usuario observa)
       ↓ ESC ↑ ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← 
```

### 🔬 Recopilar Datos para Análisis
```
Menú → RECOLECCIONAR DATOS → Load (5-10s) → Mode_Data → (Simulación 4x + logs)
       ↓ ESC ↑ (guarda CSV+JSON) ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← 
```

### 📷 Capturar Imágenes de Baches
```
Menú → MODO CAPTURA → (Carga directa) → Mode_Capture → (Posiciona, captura)
       ↓ ESC ↑ (guarda PNGs) ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← 
```

### 🐛 Debuggear Comportamiento
```
Menú → MODO DEBUG → Load (10-15s) → Mode_Debug → (Panel control, logs, gizmos)
       ↓ ESC ↑ ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← 
```

---

## 💾 UBICACIÓN DE OUTPUTS

```
Assets/
├─ Output/  (Generado por Mode_Data)
│  ├─ SimulationData_2026-05-05_12-34-56/
│  │  ├─ events.csv
│  │  ├─ statistics.json
│  │  ├─ analysis_report.txt
│  │  └─ simulation.log
│
├─ Captures/  (Generado por Mode_Capture)
│  ├─ Pothole_Dataset_2026-05-05/
│  │  ├─ Images/
│  │  │  ├─ pothole_0001.png
│  │  │  ├─ pothole_0002.png
│  │  │  └─ ...
│  │  ├─ Metadata/
│  │  │  ├─ pothole_0001.json
│  │  │  ├─ pothole_0002.json
│  │  │  └─ ...
│  │  └─ Index/
│  │     └─ dataset_index.csv
│
└─ DigitalTwin_Logs/  (Logs de sesión)
   ├─ session_2026-05-05_12-34.log
   └─ ...
```

---

## 🚀 INICIO RÁPIDO (5 MINUTOS)

```
1. Abre aplicación
   ↓ (Carga Mode_Menu)
   
2. Presiona "INICIAR SIMULACIÓN"
   ↓ (Carga Mode_Load + muestra barra)
   
3. Espera 15 segundos
   ↓ (Mode_Model carga completamente)
   
4. ¡Simulación activa! Observa:
   ├─ Vehículos patrullando
   ├─ Peatones caminando
   ├─ Detección de baches
   └─ Estadísticas en pantalla
   
5. Presiona "V" para cambiar cámara
   ├─ 1ª vista: Aérea
   ├─ 2ª vista: Primera persona
   └─ 3ª vista: Lateral
   
6. Presiona "SPACE" para pausar
   ↓ (Simulación se congela)
   
7. Presiona "ESC" para volver al menú
   ↓ (Descarga escena, vuelve a Mode_Menu)
```

---

**Fin de Guía Visual** ✨
