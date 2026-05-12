# 🎮 REFERENCIA RÁPIDA DE CONTROLES Y BOTONES

**Simulador de Patrullas - Detector de Baches**  
**Versión**: 3.0  
**Fecha**: Mayo 5, 2026

---

## ⌨️ TECLAS GLOBALES (Disponibles en todas las escenas de simulación)

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                      TECLAS DE CONTROL PRINCIPAL                          ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                            ║
║  TECLA      ESCENA           FUNCIÓN              EFECTO                  ║
║  ─────────────────────────────────────────────────────────────────────  ║
║                                                                            ║
║  V          Model/Data/      Cambiar cámara       Cicla entre:            ║
║             Debug            activa               1. Aérea                ║
║                                                   2. 1ª persona           ║
║                                                   3. Lateral              ║
║                                                                            ║
║  ESC        Model/Data/      Salir a menú         Descarga escena         ║
║             Debug/Capture    principal           + guarda datos           ║
║                                                                            ║
║  SPACE      Model/Data/      Pausa/Resume         timeScale ← → 0         ║
║             Debug            simulación           Pausa completa          ║
║                                                                            ║
║  P          Model/Data/      Abrir profiler       Muestra FPS +           ║
║             Debug                                 Memory stats            ║
║                                                                            ║
║  [          Debug            Ralentizar           timeScale /= 1.5        ║
║                              simulación                                   ║
║                                                                            ║
║  ]          Debug            Acelerar             timeScale *= 1.5        ║
║                              simulación                                   ║
║                                                                            ║
║  O          Debug            Toggle Physics       Muestra colliders       ║
║                              Debug Draw           visibles                ║
║                                                                            ║
║  U          Debug            Toggle NavMesh       Muestra triangulación   ║
║                              Visualization        NavMesh                 ║
║                                                                            ║
║  I          Debug            Teleport agente      Mueve a waypoint        ║
║                              seleccionado         aleatorio               ║
║                                                                            ║
║  K          Debug            Destruir agente      Elimina vehículo        ║
║                              seleccionado         actual                  ║
║                                                                            ║
║  L          Debug            Guardar debug log    Exporta console         ║
║                                                   a archivo               ║
║                                                                            ║
║  ↑ ↓        Capture          Mover cámara         Move.y += speed         ║
║             (drone)          vertical             (0.5-25m)               ║
║                                                                            ║
║  W A S D    Debug/Model      Movimiento cámara    Solo si está            ║
║             (drone)          horizontal           habilitado              ║
║                                                                            ║
║  Q          Debug            Frenar vehículo      Solo DEBUG              ║
║                              (manual control)     seleccionado            ║
║                                                                            ║
║  Mouse      Capture          Zoom in/out          Ajusta FOV              ║
║  Wheel                       cámara drone         30° - 90°               ║
║                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

---

## 🖱️ BOTONES DE UI POR ESCENA

### 🎮 MODE_MENU.unity

```
╔═════════════════════════════════════════════════════════════════════════╗
║                         BOTONES DE MENÚ PRINCIPAL                       ║
╠═════════════════════════════════════════════════════════════════════════╣
║                                                                         ║
║  BOTÓN                      ACCIÓN                  RESULTADO           ║
║  ─────────────────────────────────────────────────────────────────────║
║                                                                         ║
║  🎬 INICIAR SIMULACIÓN      Carga Mode_Load       Simulación visual     ║
║  (Play Button)              sceneIndex = 1        (Mode_Model)          ║
║  Atajo: Click o ENTER       → Mode_Model          FPS: 60               ║
║                                                    Duración: 10-15s      ║
║                                                    de carga              ║
║                                                                         ║
║  🐛 MODO DEBUG              Carga Debug Scene     Simulación con        ║
║  (Debug Button)             Directo sin Load      debugging tools       ║
║  Atajo: D                   → Mode_Debug          Gizmos visibles       ║
║                                                    Panel de control      ║
║                                                    Duración: 10-15s      ║
║                                                                         ║
║  📊 RECOLECCIÓN DE DATOS    Carga Mode_Load       Simulación sin        ║
║  (Data Button)              sceneIndex = 0        visual 4x más rápido  ║
║  Atajo: A                   → Mode_Data           CSV + JSON stats      ║
║                                                    Duración: 5-10s       ║
║                                                    de carga              ║
║                                                                         ║
║  📷 MODO CAPTURA            Carga directo         Herramienta para      ║
║  (Capture Button)           → Mode_Capture       capturar imágenes     ║
║  Atajo: C                                         de baches             ║
║                                                    Screenshots: 1270x950 ║
║                                                                         ║
║  ⚙️ CONFIGURACIÓN           Abre panel de        Ajustes de:            ║
║  (Settings Button)          opciones (modal)      • Volumen              ║
║  Atajo: S                                         • Gráficos             ║
║                                                    • Sensibilidad        ║
║                                                    • Idioma               ║
║                                                                         ║
║  ❌ SALIR                   Cierra app            Termina proceso        ║
║  (Exit Button)              Application.Quit()   FIN EJECUCIÓN          ║
║  Atajo: ESC                                                             ║
║                                                                         ║
╚═════════════════════════════════════════════════════════════════════════╝
```

---

### 📥 MODE_LOAD.unity

```
╔═════════════════════════════════════════════════════════════════════════╗
║                    ELEMENTOS DE PANTALLA DE CARGA                       ║
╠═════════════════════════════════════════════════════════════════════════╣
║                                                                         ║
║  ELEMENTO              VISUALIZACIÓN            FUNCIÓN                ║
║  ─────────────────────────────────────────────────────────────────────║
║                                                                         ║
║  Barra de Progreso     [████░░░░░░░░] 40%       Muestra avance de:     ║
║                        0% → 100%                • Activación objetos   ║
║                        Verde (completo)         • Generación ciudad    ║
║                        Gris (fondo)             • Horneado NavMesh     ║
║                                                 • Inicialización       ║
║                                                                         ║
║  Porcentaje Texto      "40% COMPLETO"           Actualización cada     ║
║                        "85% COMPLETO"           0.5 segundos           ║
║                        "100% LISTO!"                                   ║
║                                                                         ║
║  Mensaje de Estado     "Activando objetos..."   Cambia según fase:     ║
║                        "Generando ciudad..."    • 0-30%: Activación    ║
║                        "Horneando NavMesh..."   • 30-60%: Generación   ║
║                        "¡Listo!"                • 60-90%: NavMesh      ║
║                                                 • 90-100%: Init        ║
║                                                                         ║
║  Contador Objetos      "Objetos: 45/120"        Cuántos se han         ║
║                        "Objetos: 120/120"       activado vs total      ║
║                                                                         ║
║  Spinner (animación)   ⟳ (rotación)             Indica proceso activo  ║
║                        Gira infinitamente       No presionar nada      ║
║                                                 durante carga          ║
║                                                                         ║
║  Logo/Imagen           [   LOGO    ]            Branding empresa       ║
║                                                 Fondo visual           ║
║                                                                         ║
║  [NO HAY BOTONES]      Escena pasiva            Espera a que termine   ║
║                        NO interactive           la carga (10-15s)      ║
║                        Solo visualización       Luego carga automática ║
║                                                 escena destino         ║
║                                                                         ║
╚═════════════════════════════════════════════════════════════════════════╝
```

---

### 🚗 MODE_MODEL.unity (Simulación Principal)

```
╔═════════════════════════════════════════════════════════════════════════╗
║                   BOTONES Y CONTROLES EN SIMULACIÓN                     ║
╠═════════════════════════════════════════════════════════════════════════╣
║                                                                         ║
║  BOTÓN                  LOCALIZACIÓN          FUNCIÓN                  ║
║  ─────────────────────────────────────────────────────────────────────║
║                                                                         ║
║  ⏸ PAUSA               Panel inferior        Detiene la simulación    ║
║  (Pause Button)         izquierda             timeScale = 0            ║
║  Atajo: SPACE           Centro                Permanece pausada hasta  ║
║                                               que presiones REANUDAR   ║
║                                                                         ║
║  ▶ REANUDAR            Panel inferior        Reanuda la simulación    ║
║  (Resume Button)        izquierda             timeScale = 1            ║
║  Atajo: SPACE           Centro                Solo aparece cuando      ║
║                                               está pausada             ║
║                                                                         ║
║  🏠 MENÚ               Esquina superior      Vuelve a Mode_Menu      ║
║  (Menu Button)          derecha               Descarga toda la escena ║
║  Atajo: ESC                                   + libera memoria         ║
║                                                                         ║
║  🔄 REINICIAR          Esquina superior      Recarga Mode_Model      ║
║  (Restart Button)       derecha               Borra estado actual      ║
║                                               Genera nueva simulación  ║
║                                                                         ║
║  📊 ESTADÍSTICAS       Esquina superior      MOSTRADOR (no botón):    ║
║  (Stats Panel)          derecha               • FPS: XX.X              ║
║  [VISTA]                                      • Vehículos: 5           ║
║  - FPS: 60.0                                  • Peatones: 3            ║
║  - Vehículos: 5/5                             • Baches: 125           ║
║  - Peatones: 3/3                              • Tiempo: HH:MM:SS      ║
║  - Baches: 125                                                         ║
║  - Tiempo: 00:05:23                                                    ║
║                                                                         ║
║  📋 EVENTOS LOG        Esquina inferior      MOSTRADOR (últimas 10):   ║
║  (Event Log)            derecha               [HH:MM] Evento tipo     ║
║  [VISTA]                                      [HH:MM] Evento tipo     ║
║  - [12:34] Vehicle_0...                       [HH:MM] Evento tipo     ║
║  - [12:35] Pedestrian..                       ...                     ║
║                                                                         ║
║  ⌨️ TECLAS DISPONIBLES Esquina inferior      REFERENCIA (no botón):   ║
║  [VISTA]                izquierda              V: Cambiar cámara       ║
║  - V: Cambiar cámara                          ESC: Menú               ║
║  - ESC: Menú                                   SPACE: Pausa            ║
║  - SPACE: Pausa                                P: Profiler             ║
║  - P: Profiler                                                         ║
║                                                                         ║
╚═════════════════════════════════════════════════════════════════════════╝
```

---

### 📊 MODE_DATA.unity (Recopilación de Datos)

```
╔═════════════════════════════════════════════════════════════════════════╗
║                   CONTROLES Y SALIDA DE DATOS                           ║
╠═════════════════════════════════════════════════════════════════════════╣
║                                                                         ║
║  CONTROL              TIPO          ACCIÓN              RESULTADO       ║
║  ─────────────────────────────────────────────────────────────────────║
║                                                                         ║
║  ESC                  Tecla         Termina simulación  Guarda datos:   ║
║                                     + vuelve a menú     • CSV eventos   ║
║                                                         • JSON stats    ║
║                                                         • LOG detallado ║
║                                                         en: Assets/     ║
║                                                            Output/      ║
║                                                                         ║
║  SPACE                Tecla         Pausa simulación    Para debugging  ║
║                                     (opcional)          Útil si algo    ║
║                                                         sale mal        ║
║                                                                         ║
║  P                    Tecla         Mostrar stats       Abre profiler   ║
║                                                         en tiempo real  ║
║                                                                         ║
║  V                    Tecla         Cambiar cámara      Aunque sea sin  ║
║                                     (no visible)        visual, útil    ║
║                                                         para debugging  ║
║                                                                         ║
║  [SOLO LOG DE TEXTO]  UI Mínima     Muestra últimos     10 líneas:      ║
║  Console Panel                      eventos             "[00:00]        ║
║  [VISTA]                            registrados         Iniciado"       ║
║  - [00:00] Iniciado                                     "[00:45]        ║
║  - [00:45] Vehicle_0 activo                            Vehicle_0..."   ║
║  - [01:12] Pothole detectado                           "[01:12]...     ║
║  ...                                                                    ║
║                                                                         ║
║  [ARCHIVOS GENERADOS] OUTPUT        Automáticamente     Cuando presiona ║
║  en Assets/Output/                  al salir o cada     ESC:            ║
║                                     10 minutos          ├─ events.csv   ║
║                                                         ├─ stats.json   ║
║                                                         └─ sim.log      ║
║                                                                         ║
╚═════════════════════════════════════════════════════════════════════════╝
```

---

### 📷 MODE_CAPTURE.unity (Captura de Baches)

```
╔═════════════════════════════════════════════════════════════════════════╗
║                   BOTONES Y CONTROLES DE CAPTURA                        ║
╠═════════════════════════════════════════════════════════════════════════╣
║                                                                         ║
║  BOTÓN/CONTROL        LOCALIZACIÓN      ACCIÓN                         ║
║  ─────────────────────────────────────────────────────────────────────║
║                                                                         ║
║  🎬 GENERAR            Panel principal   Regenera baches con           ║
║  NUEVOS BACHES         centro-superior   nueva semilla aleatoria       ║
║                                          Destruye los antiguos         ║
║                                          Crea 50-200 nuevos            ║
║                                                                         ║
║  📸 CAPTURAR           Panel principal   Toma screenshot actual:        ║
║  SCREENSHOT            centro            • Resolución: 1270x950        ║
║                                          • Formato: PNG                ║
║                                          • + JSON metadata              ║
║                                          Guarda en: Assets/            ║
║                                                     Captures/           ║
║                                                                         ║
║  ✅ MODO AUTO          Toggle            Activa captura automática:    ║
║  (checkbox)            panel             • Cada 2 segundos             ║
║                                          • Ajusta cámara               ║
║                                          • Captura si visibilidad>40%  ║
║                                          • Genera 30+ imágenes min     ║
║                                                                         ║
║  ⬆️ SUBIR              Botones laterales Sube cámara drone:             ║
║  CÁMARA                (hold + drag)     • Speed: 10 m/s               ║
║  O ↑                                     • Rango: 0.5 - 25m            ║
║                                          • Atajo: ↑ (tecla arriba)     ║
║                                                                         ║
║  ⬇️ BAJAR              Botones laterales Baja cámara drone:             ║
║  CÁMARA                (hold + drag)     • Speed: 10 m/s               ║
║  O ↓                                     • Rango: 0.5 - 25m            ║
║                                          • Atajo: ↓ (tecla abajo)      ║
║                                                                         ║
║  📏 ALTURA             Slider horizontal Ajusta altura de cámara:      ║
║  (Slider)              panel             • Rango: 0.5m - 25m           ║
║                                          • Actualización real-time     ║
║                                          • Muestra valor actual        ║
║                                                                         ║
║  📐 ESCALA             Slider horizontal Escala del bounding box:      ║
║  (Slider)              panel             • Rango: 0.5x - 2.0x          ║
║                                          • 1.0x = Ajustado perfectamente
║                                          • Mayor = más espacio         ║
║                                                                         ║
║  ℹ️ INFO              Texto informativo  MOSTRADOR (no botón):         ║
║  [VISTA]              panel              • Baches capturados: 42/200   ║
║  - Baches: 42/200                        • Altura actual: 15.2m        ║
║  - Altura: 15.2m                         • Ángulo: 87%                 ║
║  - Ángulo: 87%                           • Severidad promedio: 0.65    ║
║                                                                         ║
║  🏠 MENÚ              Botón esquina      Vuelve a Mode_Menu            ║
║  (Menu Button)         inferior-derecha  Guarda sesión de captura      ║
║                                          Genera index CSV               ║
║                                                                         ║
║  🖱️ MOUSE             Cámara 3D          Movimiento libre:              ║
║  LIBRE                 viewport           • Rueda: Zoom in/out         ║
║                                          • Botón central: Rotar        ║
║                                          • Botón derecho: Pan          ║
║                                                                         ║
╚═════════════════════════════════════════════════════════════════════════╝
```

---

### 🐛 MODE_DEBUG.unity (Modo Debug)

```
╔═════════════════════════════════════════════════════════════════════════╗
║              PANEL DE DEBUG CON CONTROLES AVANZADOS                     ║
╠═════════════════════════════════════════════════════════════════════════╣
║                                                                         ║
║  SECCIÓN: SIMULATION CONTROL                                           ║
║  ──────────────────────────────────────────────────────────────────   ║
║  [PAUSE]     Button    Pausa simulación a timeScale = 0               ║
║  [STEP]      Button    Avanza 1 frame (paso a paso)                   ║
║  Time Scale  Slider    Ajusta velocidad 0.1x - 2x                    ║
║  Physics     Toggle    Dibuja colliders y shapes                     ║
║  Debug Draw  Checkbox                                                 ║
║                                                                         ║
║  SECCIÓN: VEHICLE CONTROL                                              ║
║  ──────────────────────────────────────────────────────────────────   ║
║  Vehicle     Dropdown  Selecciona: Vehicle_0..4                       ║
║  Select:     List      Cambios afectan a ese vehículo                 ║
║                                                                         ║
║  Speed       Slider    Ajusta target speed (0-20 m/s)                 ║
║  Target:     Range     Cambio instantáneo                             ║
║                                                                         ║
║  Accel:      Slider    Ajusta aceleración (0-10 m/s²)                ║
║  Time        Range     Cambio instantáneo                             ║
║                                                                         ║
║  [Teleport   Button    Teletransporta vehículo a                      ║
║   to WP]               waypoint aleatorio                             ║
║                                                                         ║
║  [Clear Path] Button    Resetea ruta actual                           ║
║                         Elige nuevo waypoint                          ║
║                                                                         ║
║  SECCIÓN: WAYPOINT EDITOR                                              ║
║  ──────────────────────────────────────────────────────────────────   ║
║  [Show All   Button    Visualiza TODOS los waypoints                  ║
║   Waypoints]           en la escena (esferas verdes)                 ║
║                                                                         ║
║  [Show Path] Button    Muestra solo ruta actual                       ║
║  Only                  con líneas cyan                                ║
║                                                                         ║
║  Gizmo Size  Slider    Tamaño visual de waypoints                     ║
║                        (0.5m - 5m)                                    ║
║                                                                         ║
║  Lock        Toggle    Impide mover waypoints                         ║
║  Waypoints            en editor (bloquea cambios)                    ║
║                                                                         ║
║  [Add        Button    Crea nuevo waypoint en                         ║
║   Custom              posición custom (modal)                        ║
║   Waypoint]                                                           ║
║                                                                         ║
║  SECCIÓN: PERFORMANCE                                                   ║
║  ──────────────────────────────────────────────────────────────────   ║
║  [Graph:     Gráfico   Línea roja = FPS en tiempo                     ║
║   FPS vs     temporal  Escala: 0-120 FPS                             ║
║   Time]                                                                ║
║                                                                         ║
║  [Graph:     Gráfico   Línea azul = Memoria MB                        ║
║   Memory     temporal  Escala: 0-4000 MB                              ║
║   vs Time]                                                             ║
║                                                                         ║
║  [Graph:     Gráfico   Línea naranja = Llamadas física                ║
║   Physics    temporal  por frame                                      ║
║   Calls]                                                               ║
║                                                                         ║
║  Avg FPS:    Text      Promedio FPS actual                            ║
║  XX.X                  Actualiza cada segundo                        ║
║                                                                         ║
║  Peak Mem:   Text      Pico de memoria usado                          ║
║  YYYY MB                Desde inicio sesión                           ║
║                                                                         ║
║  SECCIÓN: EVENTS LOG                                                    ║
║  ──────────────────────────────────────────────────────────────────   ║
║  [Scroll     Scroll    Últimos 50 eventos registrados                 ║
║   Area]     Area      [00:00] Event type                              ║
║                        [00:01] Event type                              ║
║                        [00:05] Event type                              ║
║                        ...                                             ║
║                                                                         ║
║  Filter:     Dropdown  Filtra por tipo:                               ║
║                        • All (todos)                                   ║
║                        • POTHOLE_DETECTED                              ║
║                        • COLLISION                                     ║
║                        • VEHICLE_INIT                                  ║
║                        • WAYPOINT_REACHED                              ║
║                                                                         ║
║  [Export     Button    Guarda log a archivo                           ║
║   Log]                 Formato: debug_log_[timestamp].txt             ║
║                        En: Assets/                                     ║
║                                                                         ║
║  [Clear]    Button     Borra todos los eventos                        ║
║                        Reinicia contador                              ║
║                                                                         ║
╚═════════════════════════════════════════════════════════════════════════╝
```

---

## 🎯 TABLA RÁPIDA: TECLA → ESCENA → EFECTO

| Tecla | Mode_Model | Mode_Data | Mode_Debug | Mode_Capture | Efecto |
|-------|-----------|-----------|-----------|--------------|--------|
| V | ✅ | ✅ | ✅ | ✅ | Cambiar cámara |
| ESC | ✅ | ✅ | ✅ | ✅ | Volver menú (+guardar) |
| SPACE | ✅ | ✅ | ✅ | ❌ | Pausa/Resume |
| P | ✅ | ✅ | ✅ | ❌ | Profiler |
| ↑↓ | ❌ | ❌ | ❌ | ✅ | Altura cámara |
| W/A/S/D | ❌ | ❌ | ✅ | ✅ | Movimiento cámara |
| Q | ❌ | ❌ | ✅ | ❌ | Frenar (manual) |
| [ | ❌ | ❌ | ✅ | ❌ | Ralentizar |
| ] | ❌ | ❌ | ✅ | ❌ | Acelerar |
| O | ❌ | ❌ | ✅ | ❌ | Physics Debug |
| U | ❌ | ❌ | ✅ | ❌ | NavMesh Vis |
| I | ❌ | ❌ | ✅ | ❌ | Teleport agente |
| K | ❌ | ❌ | ✅ | ❌ | Destruir agente |
| L | ❌ | ❌ | ✅ | ❌ | Guardar log |

---

## 📋 CHECKLIST: ¿QUÉ BOTÓN/TECLA NECESITO?

```
¿Necesito...?

□ Ver la simulación visual
  → Menú → "INICIAR SIMULACIÓN" → Mode_Model
  
□ Hacer un screenshot de un bache
  → Menú → "MODO CAPTURA" → "CAPTURAR SCREENSHOT"
  
□ Recopilar 1000 eventos para análisis
  → Menú → "RECOLECCIÓN DE DATOS" → ESC (guarda CSV)
  
□ Pausar la simulación
  → SPACE o botón "PAUSA" en pantalla
  
□ Cambiar de cámara
  → V (cicla entre 3 vistas)
  
□ Ver FPS y estadísticas
  → Panel superior derecha (siempre visible)
  
□ Debuggear un vehículo específico
  → Menú → "MODO DEBUG" → Panel "Vehicle Control"
  
□ Volver al menú
  → ESC
  
□ Capturar 100+ imágenes automáticamente
  → Menú → "MODO CAPTURA" → Toggle "MODO AUTO"
  
□ Ver gizmos de waypoints
  → Menú → "MODO DEBUG" → [Show All Waypoints]
  
□ Ralentizar simulación para observar detalles
  → DEBUG MODE → [ tecla (múltiples veces)
  
□ Ver logs detallados
  → Consola Unity (Ctrl+Shift+C) o Panel inferior
```

---

## 📱 QUICK REFERENCE CARD (PRINT & KEEP)

```
╔════════════════════════════════════════════════╗
║  SIMULADOR DE PATRULLAS - CHEAT SHEET         ║
╠════════════════════════════════════════════════╣
║                                                ║
║  TECLAS ESENCIALES:                            ║
║  V    = Cambiar cámara                         ║
║  ESC  = Volver menú                            ║
║  SPACE= Pausa                                  ║
║  P    = Stats                                  ║
║                                                ║
║  BOTONES MENÚ:                                 ║
║  🎬 Iniciar Simulación                        ║
║  🐛 Modo Debug                                 ║
║  📊 Recolectar Datos                           ║
║  📷 Captura de Baches                          ║
║  ⚙️ Configuración                             ║
║                                                ║
║  VISTAS DE CÁMARA (V):                        ║
║  1. Aérea (de arriba)                         ║
║  2. Primera persona (en vehículo)             ║
║  3. Lateral (costado)                         ║
║                                                ║
║  MODOS:                                        ║
║  Model    = Visual (60 FPS)                   ║
║  Data     = Sin visual (120+ FPS)             ║
║  Debug    = Con gizmos                        ║
║  Capture  = Captura imágenes                  ║
║                                                ║
╚════════════════════════════════════════════════╝
```

---

**Fin de Referencia de Controles** ✨

*Imprime este documento para tener a mano mientras usas el simulador.*
