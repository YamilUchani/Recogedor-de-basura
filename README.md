# 🚗 Simulador de Patrullas - Detector de Baches

**Herramienta de Simulación 3D para Análisis de Patrullaje Urbano con Detección Automática de Infraestructura Dañada**

---

## 📋 Tabla de Contenidos

- [Visión General](#-visión-general)
- [Características Principales](#-características-principales)
- [Arquitectura de Escenas](#-arquitectura-de-escenas)
- [Guía Completa de Uso](#-guía-completa-de-uso)
- [Descripción Detallada de Modos](#-descripción-detallada-de-modos)
- [Controles de Teclado](#-controles-de-teclado)
- [FAQ y Solución de Problemas](#-faq-y-solución-de-problemas)

---

## 🎯 Visión General

### ¿Qué es este Simulador?

Este es un **simulador 3D avanzado de patrullas urbanas** que modela el comportamiento realista de vehículos y peatones en una ciudad generada proceduralmente. El sistema está diseñado para:

- ✅ **Simular patrullas** de vehículos siguiendo rutas inteligentes
- ✅ **Detectar automáticamente baches** y daños en el pavimento
- ✅ **Recopilar datos** detallados sobre comportamiento y eventos
- ✅ **Visualizar en 3D** toda la simulación en tiempo real
- ✅ **Depurar sistemas** con herramientas avanzadas de debugging

### Objetivos

| Objetivo | Descripción |
|----------|-------------|
| 🚗 **Patrullas Realistas** | Vehículos que se mueven naturalmente siguiendo waypoints con evasión de obstáculos |
| 🚶 **Peatones Inteligentes** | Agentes que navegan por la ciudad evitando colisiones |
| 🔍 **Detección de Baches** | Sistema automático que identifica y registra daños viales |
| 📊 **Análisis de Datos** | Exportación de eventos, estadísticas y logs detallados |
| 🎮 **Interactividad Total** | Control completo mediante UI, teclado y API |

---

## ⚡ Características Principales

### 🏙️ Generación Procedural de Ciudades
- Generación automática de manzanas, calles y casas
- NavMesh pre-horneado para navegación óptima
- Sistemas de colisiones y obstáculos realistas

### 🤖 Sistemas de Movimiento Avanzados
- **RVO2** (Reciprocal Collision Avoidance) para evitar colisiones fluidas
- **CarPatrol**: Movimiento de vehículos con evasión de aceras
- **RectangularPatrol**: Movimiento de peatones en patrones definidos
- Detección de deadlocks y situaciones de bloqueo

### 📸 Captura de Imágenes Automatizada
- Generación de screenshots de baches en alta resolución (1270x950)
- Metadata JSON con información de cada captura
- Modo automático para capturar 30+ imágenes por sesión

### 📊 Sistema de Datos Robusto
- Exportación a **CSV** con todos los eventos
- Exportación a **JSON** con estadísticas agregadas
- **Logs detallados** con timestamps de precisión de milisegundos
- Análisis de performance en tiempo real

### 🐛 Herramientas de Debug Profesionales
- Visualización de colliders y physics en tiempo real
- Gráficos de FPS, memoria y rendimiento
- Control manual de vehículos y waypoints
- Panel de eventos con filtrado por tipo

---

## 🏗️ Arquitectura de Escenas

El simulador funciona con **una interfaz central (Mode_Menu)** que controla acceso a múltiples modos de operación:

```
                    ╔═══════════════════════════════════════╗
                    ║       MODE_MENU (Punto de Entrada)   ║
                    ║      Selecciona qué modo ejecutar     ║
                    ╚═════════════════╤═════════════════════╝
                                      │
                    ┌─────────────────┼─────────────────┐
                    │                 │                 │
                ┌───▼───┐         ┌───▼────┐      ┌───▼────┐
                │ Model │         │  Data  │      │Capture │
                │(Visual)│        │(Sin UI)│      │(Fotos) │
                └───────┘        └────────┘      └────────┘
                    │
            ┌───────┴────────┐
            │                │
        ┌───▼─────┐      ┌──▼──┐
        │  Debug  │      │Load │
        │(Gizmos) │      │     │
        └─────────┘      └─────┘
```

### Escenas Disponibles

| Escena | Tipo | Propósito | FPS | Duración Carga |
|--------|------|----------|-----|--------|
| **Mode_Menu** | Control | Interfaz principal de selección | N/A | Instantáneo |
| **Mode_Load** | Transición | Pantalla de carga con barra de progreso | N/A | 10-15s |
| **Mode_Model** | Simulación Visual | Simulación completa con gráficos | 60 FPS | 10-15s |
| **Mode_Data** | Simulación Sin UI | Recopilación de datos sin visual | 120+ FPS | 5-10s |
| **Mode_Capture** | Captura de Imágenes | Sistema para capturar screenshots de baches | 60 FPS | Instantáneo |

---

## 🎮 Guía Completa de Uso

### 🔴 Paso 1: Iniciar la Aplicación

1. Abre el proyecto en **Unity 2022 LTS** o superior
2. Carga la escena `Mode_Menu.unity` desde `Assets/Scenes/`
3. Presiona **Play (▶️)** en Unity Editor o ejecuta el build compilado

**Resultado**: Se abre la interfaz principal con 5 botones principales

---

### 🔵 Paso 2: Seleccionar un Modo desde Mode_Menu

La pantalla inicial muestra **Mode_Menu** con la siguiente estructura:

```
╔═══════════════════════════════════════════════════════════════════╗
║                   SIMULADOR DE PATRULLAS 3.0                      ║
║                    [LOGO DE LA EMPRESA]                           ║
║                                                                   ║
║  ┌─────────────────────────────────────────────────────────────┐ ║
║  │                                                             │ ║
║  │  🎬 INICIAR SIMULACIÓN       (Visual + Interactivo)        │ ║
║  │  [Carga Mode_Load → Mode_Model]                            │ ║
║  │                                                             │ ║
║  │  🐛 MODO DEBUG               (Visual + Gizmos + Control)   │ ║
║  │  [Carga directo Mode_Debug]                                │ ║
║  │                                                             │ ║
║  │  📊 RECOLECCIÓN DE DATOS     (Sin visual + Muy rápido)     │ ║
║  │  [Carga Mode_Load → Mode_Data]                             │ ║
║  │                                                             │ ║
║  │  📷 MODO CAPTURA             (Captura imágenes de baches)  │ ║
║  │  [Carga directo Mode_Capture]                              │ ║
║  │                                                             │ ║
║  │  ⚙️ CONFIGURACIÓN             (Ajustes de simulación)      │ ║
║  │  [Abre panel modal]                                        │ ║
║  │                                                             │ ║
║  │  ❌ SALIR                     (Terminar aplicación)         │ ║
║  │  [Cierra la app]                                           │ ║
║  │                                                             │ ║
║  └─────────────────────────────────────────────────────────────┘ ║
║                                                                   ║
║  Versión 3.0 | Mayo 5, 2026 | © 2026 Equipo de Simulación      ║
╚═══════════════════════════════════════════════════════════════════╝
```

---

## 📍 Descripción Detallada de Modos

---

## 1️⃣ MODE_MENU - Interfaz Principal

### 📌 Ubicación y Estructura

**Archivo de Escena**: `Assets/Scenes/Mode_Menu.unity`

**Estructura de GameObjects**:
```
Mode_Menu (Escena)
├── Canvas (ScreenSpace - Overlay)
│   ├── Panel_Background (fondo visual)
│   ├── Logo_Image (branding)
│   ├── Title_Text ("SIMULADOR DE PATRULLAS 3.0")
│   ├── Button_Play
│   ├── Button_Debug
│   ├── Button_Data
│   ├── Button_Capture
│   ├── Button_Settings
│   └── Button_Exit
├── Main Camera (estatua, sin renderizado de escena)
└── EventSystem (auto-creado por Unity)
```

### 🖱️ Botones Detallados

#### **🎬 INICIAR SIMULACIÓN**
| Propiedad | Valor |
|-----------|-------|
| **Localización** | Centro superior |
| **Función** | Carga el flujo completo: Mode_Load → Mode_Model |
| **Efecto** | Inicia simulación visual con patrullas, peatones y baches |
| **Atajo de Teclado** | `ENTER` o Click directo |
| **Tiempo de Carga** | 10-15 segundos (incluye generación de ciudad) |
| **FPS Esperado** | 60 FPS (optimizado para visual) |
| **Datos Generados** | NO genera archivos (solo para visualización) |

**¿Cuándo usarlo?**
- Quiero VER la simulación en acción
- Necesito visualizar cómo se mueven los vehículos
- Quiero hacer debugging visual

---

#### **🐛 MODO DEBUG**

#### **📊 RECOLECCIÓN DE DATOS**
| Propiedad | Valor |
|-----------|-------|
| **Localización** | Centro medio |
| **Función** | Carga Mode_Load → Mode_Data |
| **Efecto** | Ejecuta simulación SIN gráficos (modo headless) |
| **Atajo de Teclado** | `A` o Click directo |
| **Tiempo de Carga** | 5-10 segundos (más rápido sin rendering) |
| **FPS Esperado** | 120+ FPS (sin límite visual) |
| **Datos Generados** | **SÍ** - Archivos CSV + JSON + LOG |

**Archivos Generados**:
```
Assets/Output/
├── events.csv          # Todos los eventos de simulación
├── stats.json          # Estadísticas agregadas (promedio FPS, etc)
└── simulation.log      # Log detallado con timestamps
```

**¿Cuándo usarlo?**
- Necesito recopilar 1000+ eventos para análisis
- Quiero ejecutar múltiples simulaciones rápidamente
- Me interesa procesar datos sin visualización
- Debo optimizar CPU/GPU (Mode_Data es más ligero)

---

#### **📷 MODO CAPTURA**
| Propiedad | Valor |
|-----------|-------|
| **Localización** | Centro medio-inferior |
| **Función** | Carga directamente Mode_Capture |
| **Efecto** | Abre modo especializado para capturar fotos de baches |
| **Atajo de Teclado** | `C` o Click directo |
| **Tiempo de Carga** | Instantáneo (sin generación de ciudad) |
| **FPS Esperado** | 60 FPS (optimizado) |
| **Datos Generados** | **SÍ** - Screenshots + metadata JSON |

**Especificaciones de Captura**:
- Resolución: **1270 x 950 píxeles**
- Formato: **PNG de 8 bits**
- Metadata: **JSON con posición, ángulo, severidad**
- Almacenamiento: `Assets/Captures/`

**¿Cuándo usarlo?**
- Necesito capturar imágenes de baches para dataset de ML
- Quiero screenshots de alta calidad de infraestructura dañada
- Debo compilar un dataset de entrenamiento
- Me interesa modo automático (30+ imágenes por sesión)

---

#### **⚙️ CONFIGURACIÓN**
| Propiedad | Valor |
|-----------|-------|
| **Localización** | Esquina inferior izquierda |
| **Función** | Abre panel modal de opciones |
| **Efecto** | Permite ajustar parámetros globales |
| **Atajo de Teclado** | `S` o Click directo |
| **Alcance** | Afecta TODAS las escenas |

**Opciones Disponibles**:
| Opción | Rango | Descripción |
|--------|-------|-------------|
| 🔊 **Volumen** | 0-100% | Control de audio |
| 🎨 **Calidad Gráficos** | Baja/Media/Alta | Resolución y efectos |
| ⚡ **Sensibilidad Cámara** | 0.5x - 2.0x | Velocidad rotación mouse |
| 🌍 **Idioma** | ES/EN/FR | Localización de UI |
| ⏱️ **Modo Tiempo** | Normalizado/Rápido/Lento | Velocidad base de simulación |

---

#### **❌ SALIR**
| Propiedad | Valor |
|-----------|-------|
| **Localización** | Esquina inferior derecha |
| **Función** | Termina la aplicación |
| **Efecto** | Cierra completamente el programa |
| **Atajo de Teclado** | `ESC` o Click directo |
| **Confirmación** | Ninguna (salida inmediata) |

---

### ⌨️ Teclas de Teclado en Mode_Menu

| Tecla | Acción |
|-------|--------|
| `ENTER` | Inicia simulación (igual que botón Play) |
| `D` | Abre Modo Debug |
| `A` | Abre Recolección de Datos |
| `C` | Abre Modo Captura |
| `S` | Abre Configuración |
| `ESC` | Salir de la aplicación |

---

---

## 2️⃣ MODE_LOAD - Pantalla de Carga

### 📌 Descripción General

**Archivo de Escena**: `Assets/Scenes/Mode_Load.unity`

**Propósito**: Mostrar barra de progreso mientras se genera la ciudad y se horrea el NavMesh

**Duración**: 10-15 segundos

### 🎨 Elementos Visuales

```
╔═══════════════════════════════════════════════════════════════════╗
║                        CARGANDO SIMULACIÓN                        ║
║                        [LOGO ANIMADO]                             ║
║                                                                   ║
║                  ⟳ Activando objetos...                           ║
║                                                                   ║
║                  [████████░░░░░░░░░░] 45% COMPLETO                ║
║                                                                   ║
║                  Objetos: 67/150                                  ║
║                                                                   ║
║                  Tiempo estimado: 8 segundos...                   ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

### 📊 Componentes de UI

#### **Barra de Progreso Animada**
- **Rango**: 0% → 100%
- **Color**: Verde (progreso) + Gris fondo
- **Actualización**: Cada 0.5 segundos
- **Movimiento**: Suave (easing exponencial)

#### **Porcentaje de Texto**
- **Formato**: "{XX}% COMPLETO"
- **Actualización en vivo**: Se actualiza con cada incremento
- **Ejemplo**: "0% COMPLETO" → "50% COMPLETO" → "100% LISTO!"

#### **Mensaje de Estado**
Cambia según fase de carga:

| % Rango | Mensaje | Proceso Activo |
|---------|---------|---|
| 0-30% | "Activando objetos..." | Instanciación de GameObjects |
| 30-60% | "Generando ciudad..." | Creación procedural de manzanas |
| 60-90% | "Horneando NavMesh..." | Bake del sistema de navegación |
| 90-100% | "Finalizando..." | Inicialización de sistemas |
| 100% | "¡Listo! Cargando escena..." | Transición a siguiente escena |

#### **Contador de Objetos**
- **Formato**: "Objetos: {actual}/{total}"
- **Ejemplo**: "Objetos: 45/150"
- **Actualización**: Cada objeto activado

#### **Spinner (Indicador de Actividad)**
- **Animación**: Rotación infinita (⟳)
- **Propósito**: Indica que el proceso sigue activo
- **Nota**: NO presionar nada durante esta pantalla

---

### ⚠️ Información Importante

**Durante la carga (Mode_Load)**:
- ❌ NO hay botones interactivos
- ❌ NO presiones nada en el teclado
- ✅ Solo observa la barra de progreso
- ✅ Espera a que llegue a 100% automáticamente

**Si la carga se atasca**:
- Espera 30 segundos más (a veces la generación es lenta)
- Si continúa atascada después de 60s, presiona `ESC` para salir
- Reporta el error en los logs de Unity

---

---

## 3️⃣ MODE_MODEL - Simulación Visual Principal

### 📌 Descripción General

**Archivo de Escena**: `Assets/Scenes/Mode_Model.unity`

**Propósito**: Visualización completa e interactiva de la simulación

**Características**:
- ✅ Gráficos completos (60 FPS)
- ✅ Vehículos y peatones en movimiento
- ✅ Cámara interactiva (3 vistas)
- ✅ Panel de estadísticas en tiempo real
- ✅ Controles de pausa/resume

### 🏗️ Estructura de la Escena

```
Mode_Model (Escena Principal)
├── GeneratedCity (Contenedor de la ciudad)
│   ├── Vehicles (5 vehículos)
│   │   ├── Vehicle_0 (con script CarPatrol)
│   │   ├── Vehicle_1
│   │   ├── Vehicle_2
│   │   ├── Vehicle_3
│   │   └── Vehicle_4
│   ├── Pedestrians (3-5 peatones)
│   │   ├── Pedestrian_0 (con script RectangularPatrol)
│   │   ├── Pedestrian_1
│   │   └── Pedestrian_2
│   ├── Buildings (50-150 casas generadas)
│   ├── Potholes (100-200 baches detectables)
│   ├── Obstacles (muros, árboles, etc)
│   └── NavMesh (pre-horneado)
│
├── Canvas (ScreenSpace - Overlay)
│   ├── Panel_TopRight (Estadísticas)
│   │   ├── FPS Display
│   │   ├── Vehicle Count
│   │   ├── Pedestrian Count
│   │   ├── Pothole Count
│   │   └── Time Display
│   │
│   ├── Panel_BottomLeft (Controles)
│   │   ├── Button_Pause
│   │   ├── Button_Resume
│   │   ├── KeyReference_Text
│   │   └── Time Scale Display
│   │
│   ├── Panel_BottomRight (Event Log)
│   │   ├── Event_Scroll
│   │   └── Event_Items (últimas 10 líneas)
│   │
│   ├── Button_Menu
│   ├── Button_Restart
│   └── Button_Screenshot
│
├── MainCamera (Aerial/FirstPerson/Lateral)
│   └── CameraController script
│
├── Light (iluminación)
└── EventSystem
```

---

### 🖱️ Botones de Canvas - Descripción Detallada

#### **⏸ PAUSA**
```
┌────────────────────────────────────────┐
│ Botón: ⏸ PAUSA                         │
├────────────────────────────────────────┤
│ Localización      │ Panel inferior izq │
│ Visible cuando    │ Simulación activa  │
│ Desaparece cuando │ Ya está pausada    │
│ Al presionar      │ timeScale = 0      │
│ Efecto en UI      │ Muestra "PAUSADA"  │
│ Atajo Teclado     │ SPACE (barra)      │
│ Sonido            │ Click (si audio ON)│
└────────────────────────────────────────┘
```

**Comportamiento Detallado**:
1. Usuario presiona botón o SPACE
2. `Time.timeScale` se establece en 0
3. La simulación congela todos los movimientos
4. Botón desaparece y aparece "▶ REANUDAR"
5. Se detiene generación de eventos
6. Panel de estadísticas sigue actualizado

**¿Qué se congela?**
- ✅ Movimiento de vehículos
- ✅ Movimiento de peatones
- ✅ Animaciones
- ✅ Físicas
- ❌ UI y panel de estadísticas (sigue funcionando)

---

#### **▶ REANUDAR**
```
┌────────────────────────────────────────┐
│ Botón: ▶ REANUDAR                      │
├────────────────────────────────────────┤
│ Localización      │ Panel inferior izq │
│ Visible cuando    │ Simulación pausada │
│ Desaparece cuando │ Se reanuda         │
│ Al presionar      │ timeScale = 1      │
│ Efecto en UI      │ Vuelve "normal"    │
│ Atajo Teclado     │ SPACE (barra)      │
│ Latencia          │ Instantáneo (<1ms) │
└────────────────────────────────────────┘
```

**Comportamiento Detallado**:
1. Usuario presiona botón o SPACE
2. `Time.timeScale` vuelve a 1.0
3. La simulación se reanuda desde donde paró
4. Botón desaparece y aparece "⏸ PAUSA"
5. Se reanudan generación de eventos
6. Vehículos y peatones continúan movimiento

---

#### **🏠 MENÚ**
```
┌────────────────────────────────────────┐
│ Botón: 🏠 MENÚ                         │
├────────────────────────────────────────┤
│ Localización      │ Esquina sup. derec │
│ Siempre Visible   │ Sí                 │
│ Al presionar      │ Regresa a Mode_Menu│
│ Tiempo Transición │ 1-2 segundos       │
│ Datos al salir    │ Se descarga escena │
│ Atajo Teclado     │ ESC                │
│ Confirmación      │ NO (salida directa)│
└────────────────────────────────────────┘
```

**Flujo de Salida**:
```
MENÚ presionado
    ↓
Pausa simulación (timeScale = 0)
    ↓
Envía señal de guardado (si aplica)
    ↓
Descarga escena Mode_Model
    ↓
Libera memoria (vehículos, peatones, ciudad)
    ↓
Carga Mode_Menu
    ↓
Vuelve a interfaz principal
```

---

#### **🔄 REINICIAR**
```
┌────────────────────────────────────────┐
│ Botón: 🔄 REINICIAR                    │
├────────────────────────────────────────┤
│ Localización      │ Esquina sup. derec │
│ Siempre Visible   │ Sí                 │
│ Al presionar      │ Recarga Mode_Model │
│ Tiempo Transición │ 10-15 segundos     │
│ Estado Anterior   │ Se pierde TODO     │
│ Atajo Teclado     │ No hay (solo botón)│
│ Confirmación      │ NO (reinicio direc)│
└────────────────────────────────────────┘
```

**Flujo de Reinicio**:
```
REINICIAR presionado
    ↓
Pausa simulación
    ↓
Descarga Mode_Model actual
    ↓
Genera NUEVA ciudad
    ↓
NUEVOS vehículos y peatones
    ↓
Carga Mode_Load (barra de progreso)
    ↓
Nueva simulación comienza
```

**¿Se pierden datos?** 
- ✅ SÍ, completamente (sin guardado automático en Mode_Model)
- Para salvar datos, usar Mode_Data en su lugar

---

#### **📊 PANEL ESTADÍSTICAS** (No es botón, es mostrador)
```
╔═══════════════════════════════════════╗
║        ESTADÍSTICAS EN VIVO           ║
╠═══════════════════════════════════════╣
║ FPS:             60.0                 ║
║ Vehículos:       5/5                  ║
║ Peatones:        3/3                  ║
║ Baches:          127                  ║
║ Tiempo:          00:05:23             ║
╚═══════════════════════════════════════╝
```

**Descripción de Campos**:

| Campo | Rango | Actualización | Significado |
|-------|-------|---|---|
| **FPS** | 30-60 | Cada frame | Fotogramas por segundo |
| **Vehículos** | 0-5 | En tiempo real | Cantidad activos / Total |
| **Peatones** | 0-5 | En tiempo real | Cantidad activos / Total |
| **Baches** | 0-200 | En tiempo real | Baches detectados en escena |
| **Tiempo** | HH:MM:SS | Cada segundo | Duración de simulación actual |

**Interpretación de Valores**:
- ✅ FPS > 50: Rendimiento excelente
- ⚠️ FPS 30-50: Rendimiento aceptable
- ❌ FPS < 30: Posible lag (reduce gráficos)

---

#### **📋 PANEL DE EVENTOS LOG** (No es botón, es mostrador)
```
╔═══════════════════════════════════════╗
║        ÚLTIMOS EVENTOS (10 líneas)    ║
╠═══════════════════════════════════════╣
║ [00:00] Sistema iniciado              ║
║ [00:02] Vehicle_0 en ruta             ║
║ [00:05] Bache detectado               ║
║ [00:08] Pedestrian_1 activo           ║
║ [00:12] Vehicle_2 cambió rumbo        ║
║ [00:15] Colisión evitada              ║
║ [00:18] Bache inspeccionado           ║
║ [00:20] FPS drop detectado            ║
║ [00:23] Evento crítico: ...           ║
║ [00:25] Simulación estable            ║
╚═══════════════════════════════════════╝
```

**Tipos de Eventos Mostrados**:
- 📍 Eventos de inicialización
- 🚗 Movimiento de vehículos
- 🚶 Movimiento de peatones
- 🔍 Detección de baches
- ⚠️ Colisiones/Evasiones
- ⚡ Cambios de rendimiento
- 🐛 Eventos de debug

**Nota**: Solo muestra últimas 10 líneas (lista scrolleable en Debug)

---

### ⌨️ Controles de Teclado en Mode_Model

#### **Controles de Cámara**

| Tecla | Función | Efecto | Vista Afectada |
|-------|---------|--------|---|
| `V` | Cambiar vista | Cicla: Aérea → 1ª Persona → Lateral → Aérea | Todas |
| `MOUSE WHEEL` | Zoom | Acerca/aleja | Todas |
| `Mouse Botón Derecho` | Rotar vista | Libre rotación de cámara | Debug/Capture |

**Detalles de vistas**:
1. **Aérea**: Cámara arriba (bird's eye view)
2. **Primera Persona**: Desde interior del vehículo principal
3. **Lateral**: Vista de costado de la ciudad

---

#### **Controles de Simulación**

| Tecla | Función | Efecto | Requisito |
|-------|---------|--------|---|
| `SPACE` | Pausa/Resume | Congela o reanuda timeScale | Siempre disponible |
| `P` | Profiler | Abre panel de FPS + Memory | Siempre disponible |
| `ESC` | Volver menú | Regresa a Mode_Menu (ESC = MENÚ botón) | Siempre disponible |

---

### 🔄 Relación entre Botones y Teclas

```
┌─────────────────────────────────────────────────────┐
│           PAUSA (SPACE o Botón ⏸)                  │
├─────────────────────────────────────────────────────┤
│ Opción 1: Presionar SPACE (barra espaciadora)      │
│ Opción 2: Hacer clic en botón "⏸ PAUSA"           │
│                                                     │
│ RESULTADO: timeScale = 0 (todo congelado)          │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│        CAMBIAR CÁMARA (V o panel derecha)          │
├─────────────────────────────────────────────────────┤
│ Opción 1: Presionar V                              │
│ Opción 2: Ciclar con botones (si están disponibles)│
│                                                     │
│ RESULTADO: Cambia entre 3 vistas automáticamente   │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│          VOLVER MENÚ (ESC o botón MENÚ)            │
├─────────────────────────────────────────────────────┤
│ Opción 1: Presionar ESC (escape)                   │
│ Opción 2: Hacer clic en botón "🏠 MENÚ"           │
│                                                     │
│ RESULTADO: Carga Mode_Menu                         │
└─────────────────────────────────────────────────────┘
```

---

---

## 4️⃣ MODE_DATA - Recopilación de Datos Sin Visual

### 📌 Descripción General

**Archivo de Escena**: `Assets/Scenes/Mode_Data.unity`

**Propósito**: Ejecutar simulación con máximo rendimiento para recopilar datos

**Características**:
- ✅ Sin gráficos (120+ FPS)
- ✅ Generación de eventos detallados
- ✅ Exportación automática de datos
- ✅ Ideal para análisis estadístico

### 📊 Diferencias vs Mode_Model

| Aspecto | Mode_Model | Mode_Data |
|--------|-----------|----------|
| **Visual** | Sí (completo) | NO (solo text logging) |
| **FPS** | 60 | 120+ |
| **Velocidad Real** | 1x | Hasta 4x más rápido |
| **Archivos Generados** | NO | SÍ (CSV + JSON) |
| **Cámara** | 3 vistas | NO (irrelevante) |
| **UI** | Completa | Mínima (solo consola) |
| **Uso** | Visualización | Análisis de datos |

---

### 🎮 Interfaz de Mode_Data

```
╔═══════════════════════════════════════════════════════════════╗
║                    SIMULACIÓN EN EJECUCIÓN                   ║
║                  (Sin gráficos - Solo datos)                 ║
║                                                              ║
║  ┌────────────────────────────────────────────────────────┐ ║
║  │ [CONSOLA DE EVENTOS]                                   │ ║
║  │                                                         │ ║
║  │ [00:00] Sistema iniciado                              │ ║
║  │ [00:00] CityGenerator: Creando manzanas...            │ ║
║  │ [00:02] NavMeshBaker: Horneando...                    │ ║
║  │ [00:05] Vehicle_0 iniciado en pos (10, 0, 15)        │ ║
║  │ [00:05] Vehicle_1 iniciado en pos (20, 0, 25)        │ ║
║  │ [00:07] Simulación completa. Ejecutando...           │ ║
║  │ [00:10] Vehicle_0: Pothole detectado en (15, 0, 20)  │ ║
║  │ [00:12] Pedestrian_0: Waypoint alcanzado             │ ║
║  │ [00:15] Vehicle_1: Evasión de colisión               │ ║
║  │ [00:18] Sistema estable. Continuando...              │ ║
║  │ ...                                                    │ ║
║  │                                                         │ ║
║  └────────────────────────────────────────────────────────┘ ║
║                                                              ║
║  Simulación corriendo... Presiona ESC para terminar y        ║
║  guardar datos.                                              ║
║                                                              ║
╚═══════════════════════════════════════════════════════════════╝
```

---

### ⌨️ Controles de Teclado en Mode_Data

| Tecla | Acción | Efecto |
|-------|--------|--------|
| `ESC` | Terminar + Guardar | Detiene simulación y exporta archivos |
| `SPACE` | Pausa/Resume (opcional) | Para debugging si algo falla |
| `P` | Profiler (opcional) | Muestra stats de rendimiento |
| `V` | Cambiar cámara (NO visible) | Tecla sin efecto visual |

---

### 📁 Archivos Generados

Cuando presionas `ESC`, se crean **3 archivos** en `Assets/Output/`:

#### **1. events.csv** - Todos los Eventos

**Formato**: CSV estándar (Excel compatible)

```csv
Timestamp,Event_Type,Agent_ID,Position_X,Position_Y,Position_Z,Severity,Details
00:00.000,SYSTEM_INIT,SYSTEM,0,0,0,1.0,Simulación iniciada
00:00.100,NAVMESH_BAKE,SYSTEM,0,0,0,1.0,NavMesh horneado correctamente
00:05.200,VEHICLE_INIT,Vehicle_0,10.5,0.0,15.3,1.0,Vehículo 0 iniciado
00:05.250,VEHICLE_INIT,Vehicle_1,20.8,0.0,25.1,1.0,Vehículo 1 iniciado
00:10.500,POTHOLE_DETECTED,Vehicle_0,15.2,0.0,20.4,0.75,Bache detectado - Severidad 0.75
00:12.100,WAYPOINT_REACHED,Pedestrian_0,12.0,0.0,12.0,1.0,Peatón llegó a waypoint
00:15.300,COLLISION_AVOIDED,Vehicle_1,21.0,0.0,26.0,0.5,Colisión evitada con Pedestrian_1
00:18.800,POTHOLE_DETECTED,Vehicle_1,25.0,0.0,30.0,0.85,Bache detectado - Severidad 0.85
```

**Columnas**:
- `Timestamp`: Hora en formato MM:SS.mmm
- `Event_Type`: SYSTEM_INIT, POTHOLE_DETECTED, COLLISION_AVOIDED, etc
- `Agent_ID`: Vehicle_0, Pedestrian_1, SYSTEM, etc
- `Position_X/Y/Z`: Coordenadas 3D donde ocurrió el evento
- `Severity`: 0.0 (bajo) a 1.0 (alto)
- `Details`: Descripción textual del evento

---

#### **2. stats.json** - Estadísticas Agregadas

**Formato**: JSON legible

```json
{
  "simulation": {
    "duration_seconds": 300,
    "total_events": 1247,
    "start_time": "2026-05-05T14:30:00Z",
    "end_time": "2026-05-05T14:35:00Z"
  },
  "performance": {
    "avg_fps": 120,
    "min_fps": 95,
    "max_fps": 142,
    "avg_memory_mb": 456.8,
    "peak_memory_mb": 523.2
  },
  "vehicles": {
    "total": 5,
    "potholes_detected": 87,
    "collisions_avoided": 12,
    "avg_speed_ms": 15.3,
    "total_distance_m": 4590
  },
  "pedestrians": {
    "total": 3,
    "waypoints_reached": 45,
    "collisions_avoided": 8,
    "avg_speed_ms": 1.8
  },
  "potholes": {
    "total_in_city": 156,
    "detected_by_vehicles": 87,
    "avg_severity": 0.68,
    "distribution": {
      "low_severity": 32,
      "medium_severity": 38,
      "high_severity": 17
    }
  }
}
```

**Secciones**:
- `simulation`: Información general de la ejecución
- `performance`: Métricas de rendimiento
- `vehicles`: Estadísticas de vehículos
- `pedestrians`: Estadísticas de peatones
- `potholes`: Análisis de baches detectados

---

#### **3. simulation.log** - Log Detallado

**Formato**: Texto plano con timestamps

```
[2026-05-05 14:30:00.000] INFO  - Inicializando simulación...
[2026-05-05 14:30:00.050] DEBUG - CityGenerator: Creando manzana 0 en (0, 0)
[2026-05-05 14:30:00.075] DEBUG - CityGenerator: Creando manzana 1 en (50, 0)
[2026-05-05 14:30:00.100] DEBUG - CityGenerator: Creando manzana 2 en (0, 50)
[2026-05-05 14:30:01.200] INFO  - NavMeshBaker: Iniciando horneado...
[2026-05-05 14:30:02.500] INFO  - NavMeshBaker: Horneado completado (1.3s)
[2026-05-05 14:30:05.100] INFO  - Inicializando vehículos...
[2026-05-05 14:30:05.150] INFO  - Vehicle_0: Iniciado en (10.5, 0, 15.3)
[2026-05-05 14:30:05.200] INFO  - Vehicle_1: Iniciado en (20.8, 0, 25.1)
[2026-05-05 14:30:05.250] DEBUG - Iniciando bucle de simulación...
[2026-05-05 14:30:10.300] DEBUG - Vehicle_0: Pothole detectado en (15.2, 0, 20.4), severidad 0.75
[2026-05-05 14:30:12.100] DEBUG - Pedestrian_0: Waypoint alcanzado en (12.0, 0, 12.0)
[2026-05-05 14:30:15.300] WARNING - Vehicle_1: Colisión potencial con Pedestrian_1, evadiendo...
[2026-05-05 14:30:18.800] DEBUG - Vehicle_1: Pothole detectado en (25.0, 0, 30.0), severidad 0.85
```

**Niveles de Log**:
- `INFO`: Eventos generales importantes
- `DEBUG`: Detalles de ejecución
- `WARNING`: Situaciones anómalas
- `ERROR`: Problemas graves

---

### 🔗 Relación con Otros Modos

```
Mode_Menu
    ↓
[Presiona "RECOLECCIÓN DE DATOS"]
    ↓
Mode_Load (10-15s de carga)
    ↓
Mode_Data (ejecuta sin visual)
    ↓
ESC presionado
    ↓
├─ Genera events.csv
├─ Genera stats.json
└─ Genera simulation.log
    ↓
Automáticamente vuelve a Mode_Menu
```

---

---

---

## 6️⃣ MODE_CAPTURE - Sistema de Captura de Imágenes

### 📌 Descripción General

**Archivo de Escena**: `Assets/Scenes/Mode_Capture.unity`

**Propósito**: Capturar screenshots de baches para dataset de ML

**Características**:
- ✅ Captura manual o automática de imágenes
- ✅ Alta resolución (1270x950)
- ✅ Metadata JSON para cada imagen
- ✅ Control de altura y zoom de cámara
- ✅ Generación de baches nuevos on-demand

### 🎨 Interfaz de Mode_Capture

```
╔═══════════════════════════════════════════════════════════════════╗
║                   MODO CAPTURA DE BACHES                          ║
║                   [VISTA 3D DE BACHES]                            ║
║                                                                   ║
║  ┌────────────────────────────────────────────────────────────┐ ║
║  │ [GEN NUEVOS BACHES]  [CAPTURAR SCREENSHOT]  [MODO AUTO: ✗] │ ║
║  └────────────────────────────────────────────────────────────┘ ║
║                                                                   ║
║                                                                   ║
║              [ÁREA DE VISTA DE BACHES 3D]                        ║
║              (Se ven baches desde arriba)                        ║
║                                                                   ║
║                                                                   ║
║  ┌────────────────────────────────────────────────────────────┐ ║
║  │ ALTURA CÁMARA:  ⬆️  ⬇️   │ [━━━●━━━] 15.2m (0.5-25m)     │ ║
║  │                                                            │ ║
║  │ ESCALA BBOX:    [━━━●━━━] 1.0x (0.5-2.0x)               │ ║
║  │                                                            │ ║
║  │ Baches Capturados: 42/200     Baches Visibles: 87       │ ║
║  │ Altura Actual: 15.2m           Ángulo: 87%               │ ║
║  │ Severidad Promedio: 0.65       FPS: 60                   │ ║
║  └────────────────────────────────────────────────────────────┘ ║
║                                                                   ║
║  [🏠 MENÚ]                                                       ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

---

### 🖱️ Botones Detallados

#### **🔄 GENERAR NUEVOS BACHES**
| Propiedad | Valor |
|-----------|-------|
| **Localización** | Panel superior izquierda |
| **Función** | Destruye baches actuales y crea nuevos |
| **Efecto** | Nueva seed aleatoria para baches |
| **Clics** | Ilimitados |
| **Baches Creados** | 50-200 por click |
| **Tiempo de Generación** | 1-2 segundos |
| **Atajo Teclado** | No hay (solo botón) |

**Flujo**:
```
[GEN NUEVOS BACHES] presionado
    ↓
Busca todos los GameObjects de tipo "Pothole"
    ↓
Los destruye
    ↓
Genera nueva seed aleatoria
    ↓
Crea 50-200 baches nuevos en posiciones aleatorias
    ↓
Actualiza contador en UI
```

---

#### **📸 CAPTURAR SCREENSHOT**
| Propiedad | Valor |
|-----------|-------|
| **Localización** | Panel superior centro |
| **Función** | Toma screenshot de baches actuales |
| **Efecto** | Guarda PNG + JSON metadata |
| **Resolución** | 1270 x 950 píxeles |
| **Formato** | PNG de 8 bits |
| **Almacenamiento** | `Assets/Captures/` |
| **Tiempo** | ~100ms por captura |
| **Nombre Archivo** | `capture_[timestamp]_[numero].png` |

**Metadata JSON adjunto**:
```json
{
  "capture_id": "capture_20260505_142530_001",
  "timestamp": "2026-05-05T14:25:30.123Z",
  "camera": {
    "position": [0, 15.2, 0],
    "rotation": [90, 0, 0],
    "fov": 45,
    "height_m": 15.2
  },
  "potholes_in_frame": 42,
  "potholes_severity": {
    "low": 12,
    "medium": 20,
    "high": 10
  },
  "avg_severity": 0.65,
  "visibility_percentage": 85.3
}
```

**¿Cuándo presionar?**
- Cuando veo baches interesantes en la vista
- Cuando quiero captura manual de una escena específica
- Para dataset de entrenamiento de ML

---

#### **✅ MODO AUTO**
| Propiedad | Valor |
|-----------|-------|
| **Tipo** | Toggle (On/Off) |
| **Localización** | Panel superior derecha |
| **Función** | Captura automática cada N segundos |
| **Intervalo** | 2 segundos entre capturas |
| **Umbral Visibilidad** | >40% baches visibles |
| **Imágenes Generadas** | 30-50 por sesión (depende duración) |
| **Atajo Teclado** | No hay (solo toggle) |

**Algoritmo de Auto-Captura**:
```
[MODO AUTO: ON]
    ↓
Cada 2 segundos:
    ├─ ¿Hay >40% de baches visibles?
    │  ├─ SÍ → Captura automática (sin click)
    │  └─ NO → Espera, ajusta cámara, reintenta
    └─ Repite

Se detiene cuando:
- Presionas [MODO AUTO] de nuevo (OFF)
- Presionas ESC (vuelve a menú)
```

**¿Cuándo activar?**
- Quiero dataset grande sin intervención manual
- Necesito 100+ imágenes rápidamente
- Hago captura desatendida mientras trabajo en otra cosa

---

### 🎛️ Sliders y Controles

#### **📏 ALTURA CÁMARA**
```
Slider: [━━━━●━━━━]
Rango: 0.5m - 25m
Valor Actual: 15.2m
Atajo Teclado: ↑ (arriba) / ↓ (abajo)
```

**Efecto**:
- Controla altura de la cámara drone sobre baches
- Afecta qué baches se ven
- Altura recomendada: 10-20m para balance

---

#### **📐 ESCALA BOUNDING BOX**
```
Slider: [━━━●━━━]
Rango: 0.5x - 2.0x
Valor Actual: 1.0x
Atajo Teclado: No hay
```

**Interpretación**:
- `0.5x`: Bounding box muy pequeño (solo bache)
- `1.0x`: Tamaño perfecto (bache + contexto)
- `2.0x`: Muy grande (mucho contexto)

**Recomendación**: Dejar en `1.0x` para dataset

---

### 📊 Panel de Información

```
Baches Capturados:    42/200      (42 de 200 posibles)
Baches Visibles:      87          (87 están en viewport actual)
Altura Actual:        15.2m       (altura cámara drone)
Ángulo:               87%         (ángulo de inclinación)
Severidad Promedio:   0.65        (0=bajo, 1=alto)
FPS:                  60          (fotogramas por segundo)
```

---

### ⌨️ Controles de Teclado en Mode_Capture

| Tecla | Función | Efecto |
|-------|---------|--------|
| `↑ (Arriba)` | Subir cámara | Aumenta altura (max 25m) |
| `↓ (Abajo)` | Bajar cámara | Disminuye altura (min 0.5m) |
| `W/A/S/D` | Movimiento libre | Mueve cámara horizontal/vertical |
| `Mouse Wheel` | Zoom | Acerca/aleja vista (FOV 30-90°) |
| `Mouse Botón Derecho` | Rotar | Rota cámara libremente |
| `V` | Cambiar vista | Cicla entre vistas (opcional) |
| `ESC` | Volver Menú | Regresa a Mode_Menu |

---

### 📁 Archivos Generados

**Carpeta**: `Assets/Captures/`

**Contenido**:
```
Captures/
├── capture_20260505_142530_001.png
├── capture_20260505_142530_001.json
├── capture_20260505_142532_002.png
├── capture_20260505_142532_002.json
├── capture_20260505_142534_003.png
├── capture_20260505_142534_003.json
├── ...
└── capture_index.csv
```

**capture_index.csv** (generado automáticamente):
```csv
image_file,json_file,timestamp,pothole_count,avg_severity
capture_20260505_142530_001.png,capture_20260505_142530_001.json,2026-05-05T14:25:30Z,42,0.65
capture_20260505_142532_002.png,capture_20260505_142532_002.json,2026-05-05T14:25:32Z,39,0.62
capture_20260505_142534_003.png,capture_20260505_142534_003.json,2026-05-05T14:25:34Z,45,0.68
```

---

---

## ⌨️ Tabla Maestra de Controles por Escena

```
╔════════════════════════════════════════════════════════════╗
║              TECLAS DE TECLADO FÍSICO - REFERENCIA GLOBAL  ║
╠════════════════════════════════════════════════════════════╣
║                                                            ║
║ TECLA      MODE_MODEL   MODE_DATA   MODE_CAPTURE  EFECTO  ║
║ ──────────────────────────────────────────────────────── ║
║                                                            ║
║ V          ✅ Cambiar   ✅ (No UI)  ✅ Cambiar   Camera   ║
║            cámara                   vista       Switch    ║
║                                                            ║
║ ESC        ✅ Menú      ✅ Menú +   ✅ Menú      Menu /   ║
║                        Guardar                    Export   ║
║                                                            ║
║ SPACE      ✅ Pausa     ✅ Pausa    ❌ N/A        Pause    ║
║            Resume      Resume                             ║
║                                                            ║
║ P          ✅ Profiler  ✅ Profiler ❌ N/A        Stats    ║
║                                                            ║
║ ↑          ❌ N/A       ❌ N/A      ✅ Subir cám  Camera   ║
║                                                  Up/Down   ║
║                                                            ║
║ ↓          ❌ N/A       ❌ N/A      ✅ Bajar cám  Camera   ║
║                                                  Up/Down   ║
║                                                            ║
║ W/A/S/D    ❌ N/A       ❌ N/A      ✅ Mover      Move     ║
║                                    cámara                  ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
```

---

## 🔄 Diagrama de Relaciones entre Escenas

```
                        ┌─────────────────┐
                        │   MODE_MENU     │
                        │ (Punto entrada) │
                        └────────┬────────┘
                                 │
                ┌────────────────┼────────────────┐
                │                │                │
           [ENTER]          [D o Botón]      [A o Botón]
                │                │                │
        ┌───────▼─────────┐  ┌───▼────────┐  ┌──▼───────────┐
        │   MODE_LOAD     │  │ MODE_DEBUG │  │  MODE_LOAD   │
        │  (10-15 seg)    │  │ (Directo)  │  │ (10-15 seg)  │
        └────────┬────────┘  └────────────┘  └──────┬───────┘
                 │                                    │
            ┌────▼─────────┐                   ┌─────▼──────┐
            │  MODE_MODEL  │                   │  MODE_DATA │
            │  (Visual)    │                   │ (Sin visual)│
            └─────────────┘                   └────────────┘
                 │                                  │
                 │ ESC/Menú                        │ ESC
                 └─────────────┬────────────────────┘
                               │
                        ┌──────▼───────┐
                        │   MODE_MENU  │
                        │ (Regresa)    │
                        └─────┬─────┬─────┘
                              │     │
                         [ENTER] [A] [C]
                              │     │     │
                    ┌─────────┘     │     └─────────┐
                    │               │               │
            ┌───────▼─────────┐ ┌──▼───────────┐ ┌─▼──────────┐
            │   MODE_LOAD     │ │  MODE_LOAD   │ │MODE_CAPTURE│
            │  (10-15 seg)    │ │ (10-15 seg)  │ │  (Directo) │
            └────────┬────────┘ └──────┬───────┘ └────────────┘
                     │                 │
              ┌──────▼──────┐    ┌─────▼─────┐
              │ MODE_MODEL  │    │ MODE_DATA │
              │  (Visual)   │    │(Sin visual)│
              └──────┬──────┘    └─────┬─────┘
                     │                │
                     │ ESC            │ ESC
                     └────────┬───────┘
                              │
                       ┌──────▼───────┐
                       │   MODE_MENU  │
                       │  (Regresa)   │
                       └──────────────┘
2. Baja calidad gráficos en Configuración
3. Espera 15 segundos (a veces la generación es lenta)

---

### ❓ "El vehículo está atascado en la misma posición"
**Causa**: Posible deadlock en la navegación  
**Solución**:
1. Abre Mode_Debug
2. Selecciona el vehículo en el dropdown
3. Presiona `I` o botón `[Teleport]`
4. Presiona `[ ]` para ralentizar si quieres ver detalles

---

### ❓ "No se generan los archivos CSV"
**Causa**: No presionaste ESC en Mode_Data  
**Solución**:
1. En Mode_Data, presiona `ESC` explícitamente (no botón, tecla)
2. Se guardarán automáticamente en `Assets/Output/`
3. Revisa la consola de Unity para mensajes de error

---

### ❓ "Las capturas de baches están borrosas"
**Causa**: Altura de cámara muy baja o ángulo incorrecto  
**Solución**:
1. Presiona `↑` para subir la cámara (10-20m es ideal)
2. Ajusta escala de BoundingBox a 1.0x
3. Espera a que FPS esté estable (60 FPS)

---

### ❓ "¿Cómo combino 100 capturas en un dataset?"
**Solución**:
1. Copia todos los archivos de `Assets/Captures/`
2. Lee los JSON para metadata de cada imagen
3. USA `capture_index.csv` como guía
4. Puedes escribir script Python para procesar batch

---

### ❓ "¿Cuál es la diferencia entre V (tecla) y botones de cámara?"
**Respuesta**:
- `V` (tecla): Cicla automáticamente entre 3 vistas predefinidas
- Botones (si existen): Acceso directo a vista específica
- Efecto: Ambos cambian la cámara, `V` es más rápido

---

### ❓ "¿Qué significa 'Severity' en baches?"
**Respuesta**:
- `0.0`: Bache muy pequeño, sin importancia
- `0.5`: Bache moderado, visible pero no crítico
- `1.0`: Bache grave, requiere reparación inmediata
- Se calcula por: tamaño + profundidad + visibilidad en pantalla

---

## 🚀 Guía Rápida para Principiantes

### Opción 1: "Solo quiero VER la simulación"
```
1. Abre el juego
2. Presiona "INICIAR SIMULACIÓN"
3. Espera carga (10-15s)
4. Observa los vehículos en movimiento
5. Presiona V para cambiar cámara
6. Presiona SPACE para pausar
7. Presiona ESC para volver
```

### Opción 2: "Necesito recopilar datos"
```
1. Abre el juego
2. Presiona "RECOLECCIÓN DE DATOS"
3. Espera carga (5-10s)
4. Espera a que termine (30-60s)
5. Presiona ESC
6. Archivos guardados en Assets/Output/
```

### Opción 3: "Quiero capturar imágenes de baches"
```
1. Abre el juego
2. Presiona "MODO CAPTURA"
3. Presiona "GENERAR NUEVOS BACHES"
4. Presiona ↑ para subir cámara (15m ideal)
5. Presiona "CAPTURAR SCREENSHOT" (manual)
6. O activa "MODO AUTO" para captura automática
7. Presiona ESC cuando termines
```

---

## 📚 Recursos Adicionales

- **INFORME_COMPLETO_SIMULADOR.md**: Documentación técnica completa
- **GUIA_ULTRA_COMPLETA.md**: Guía exhaustiva de funciones
- **REFERENCIA_RAPIDA_BOTONES_TECLAS.md**: Tabla de controles (original)
- **Assets/Scripts/**: Código fuente de toda la lógica
- **Assets/Output/**: Archivos generados en las simulaciones

---

## 📞 Información de Proyecto

- **Versión**: 3.0 (Mayo 2026)
- **Motor**: Unity 2022 LTS +
- **Plataforma**: Windows/Mac/Linux
- **Requisitos**: 4GB RAM, GPU dedicada recomendada
- **Autor**: Equipo de Simulación
- **Licencia**: Propietaria

---

**Documento generado**: Mayo 5, 2026  
**Última actualización**: Mayo 5, 2026  
**Estado**: ✅ COMPLETO Y FUNCIONANDO
