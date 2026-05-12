using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Modo de navegación autónoma del drone.
/// Cambiar con teclas 1-2 durante el Play Mode.
/// </summary>
public enum NavigationMode
{
    Baseline = 1,   // Zigzag + detección de baches + retorno (comportamiento original)
    Hover    = 2,   // Igual que Baseline + pausa si hay Person/Car debajo
    Micro    = 3,   // Zigzag errático dentro del área para mejor ángulo de captura
    Skip     = 4    // Primera pasada rápida anotando obstaculizados; revisita tras recarga
    // 5 reservado para modos futuros
}

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class DroneNavMeshController : MonoBehaviour
{
    public bool apagado = false;
private bool apagando = false;
private float tiempoApagado = 0f;
private float tiempoParaApagarMotores = 2f;
    // private float velocidadBajada = 2f; // Unused
private float targetHeightInicial;
private float minHeightInicial;



    [Header("Height Configuration")]
    public float targetHeight = 5f;
    public float minHeight = 3f;
    public float maxAscendSpeed = 10f;
    public float heightPID_Kp = 50f;
    public float heightPID_Ki = 5f;
    public float heightPID_Kd = 10f;
    public float heightDeadZone = 0.1f;

    [Header("Movement Configuration")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 90f;
    public float tiltAngle = 15f;
    public float stabilizationSpeed = 5f;
    public float maxStabilizationTorque = 100f;

    [Header("Motor Configuration")]
    public float motorRotationSpeed = 500f;
    public bool alternateRotation = true;

    [Header("NavMesh Configuration")]
    public float stoppingDistance = 0.1f;
    public float updatePathInterval = 0.5f;

    [Header("References")]
    public Transform[] motors;
    public Transform modelRoot;
    /// <summary>Referencia al EnergyController para que Skip mode consulte el nivel de energía.</summary>
    public EnergyController energyController;

    [Header("Navigation Mode Button (opcional)")]
    [Tooltip("Botón UI que cicla entre los modos de navegación. Se oculta en modo manual. Asignar en Inspector.")]
    public Button navModeButton;
    [Tooltip("Texto TMP del botón de modo. Si es null se intenta obtener del botón automáticamente.")]
    public TMP_Text navModeButtonText;

    [Header("Refuel Position")]
    public Vector3 repostajePosition = new Vector3(0.5f, 0f, 0.5f);

    [Header("Navigation Mode")]
    [Tooltip("Modo activo. Cambiar en Inspector o con teclas 1-2 en Play.")]
    public NavigationMode navigationMode = NavigationMode.Baseline;

    [Header("Hover Mode Settings")]
    [Tooltip("Umbral de visibilidad matemática (tau_o). 1.0=total, 0.0=ocluido. Si la visibilidad es menor, se activa la política.")]
    [Range(0f, 1f)]
    public float visibilityThreshold = 0.5f;
    [Tooltip("Profundidad máxima del rayo de visibilidad")]
    public float obstacleDetectionHeight = 6f;

    /// <summary>Evento disparado cuando un obstáculo se despeja; pasa la posición XZ donde estaba.</summary>
    public Action<Vector3> onObstacleCleared;

    [Header("Micro Mode Settings")]
    [Tooltip("Segundos entre cada micro-maniobra (0 = maniobra continua)")]
    public float microManeuverInterval = 2f;
    [Tooltip("Magnitud máxima del desplazamiento delta_t (d_max en el paper)")]
    public float microManeuverRadius = 1.0f;
    [Tooltip("Duración de cada micro-maniobra antes de retomar el waypoint real (segundos)")]
    public float microManeuverDuration = 1.5f;

    [Header("Skip Mode Settings")]
    [Tooltip("Umbral de energía (%) para considerar recarga completa antes de revisitar")]
    public float skipRechargeThreshold = 95f;
    [Tooltip("Radio de deduplicación espacial: posiciones más próximas que esto se ignoran")]
    public float skipDeduplicationRadius = 1.5f;
    [Tooltip("Segundos mínimos entre registros consecutivos de posición skipped")]
    public float skipRecordCooldown = 2f;
    [Tooltip("Segundos que el drone permanece sobre cada zona anotada durante la revisita")]
    public float revisitHoverTime = 2.5f;

    // Private variables
    private Rigidbody rb;
    private NavMeshAgent agent;
    private PIDController heightPID;
    private float verticalInput;
    private float currentRotation;
    private Vector2 movementInput;
    private float[] motorRotationAngles;
    private float lastPathUpdateTime;
    public bool manualControl = true;
    private bool hasRoute = false;
    private bool isReturningToBase = false;
    private bool missionComplete = false;

    // Waypoint system
    private List<Vector3> searchWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    // private float droneWidth = 0.3f; // Unused
    // private float droneLength = 0.3f; // Unused
    public float searchSpacing = 0.25f;

    // --- Hover Mode state ---
    private bool isHoveringForObstacle = false;
    private Vector3 hoveredObstaclePosition;
    private float lastObstacleCheckTime = 0f;
    private const float obstacleCheckInterval = 0.3f;

    // --- Micro Mode state ---
    private bool isMicroManeuvering = false;
    private float lastMicroManeuverTime = 0f;
    private float microManeuverStartTime = 0f;
    private Vector3 searchAreaMin;   // límite inferior del área de búsqueda (XZ)
    private Vector3 searchAreaMax;   // límite superior del área de búsqueda (XZ)

    // --- Skip Mode state ---
    private enum SkipPhase { Idle, FirstPass, ReturningAfterFirstPass, Recharging, Revisiting, Done }
    private SkipPhase skipPhase = SkipPhase.Idle;
    private List<Vector3> skippedPositions = new List<Vector3>();
    private int currentSkipIndex = 0;
    private float lastSkipRecordTime = -999f;
    private bool isRevisitHovering = false;
    private float revisitHoverStartTime = 0f;

    // --- Digital Twin Mathematical Variables ---
    public float currentHoverWaitTime = 0f;
    public Vector2 currentMicroDelta = Vector2.zero;

    void Start()
    {
        targetHeightInicial = targetHeight;
minHeightInicial = minHeight;

        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        heightPID = new PIDController(heightPID_Kp, heightPID_Ki, heightPID_Kd);

        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;

        motorRotationAngles = new float[motors.Length];
        transform.position = repostajePosition;

        ConfigureNavAgent(false);
        UpdateNavModeButtonUI();
    }

    void Update()
    {
        if (apagado)
{
if (Input.GetKeyDown(KeyCode.Space))
{
    apagado = false;
    manualControl = true;

    // Restaurar valores de altura
    targetHeight = targetHeightInicial;
    minHeight = minHeightInicial;

    ConfigureNavAgent(false);
    Debug.Log("Drone reactivado en modo manual");
}


    return;
}

if (apagando)
{
    tiempoApagado += Time.deltaTime;

    // Suavizar motores
    for (int i = 0; i < motors.Length; i++)
    {
        float t = Mathf.Clamp01(tiempoApagado / tiempoParaApagarMotores);
        float speedFactor = Mathf.Lerp(1f, 0f, t);

        float rotationDirection = alternateRotation ? (i % 2 == 0 ? 1 : -1) : 1;
        motorRotationAngles[i] += motorRotationSpeed * speedFactor * rotationDirection * Time.deltaTime;

        motors[i].localRotation = Quaternion.Euler(0f, motorRotationAngles[i], 0f);
    }

    // Bajar altura objetivo
    targetHeight = Mathf.MoveTowards(targetHeight, 0.04f, Time.deltaTime * 1.5f);

    // También reducir el minHeight para permitir el descenso total
    minHeight = Mathf.MoveTowards(minHeight, 0f, Time.deltaTime * 2f);

    // Detectar si ya tocó el suelo
    Ray ray = new Ray(transform.position, Vector3.down);
    if (Physics.Raycast(ray, out RaycastHit hit, 5f))
    {
        float distanceToGround = hit.distance;
        if (distanceToGround <= 0.08f && targetHeight <= 0.05f)
        {
            FinalizarApagado();
        }
    }

    return;
}




        HandleInput();
        HandleModeInput();
        ApplyMotorRotation();

        // Ejecutar lógicas especiales SOLO si el dron ya tiene una ruta activa (evita que arranque solo al cambiar modos)
        if (hasRoute && !manualControl && !isReturningToBase)
        {
            // [Hover] Detectar obstáculos debajo antes de avanzar waypoints
            if (navigationMode == NavigationMode.Hover)
                CheckObstaclesBelow();

            // [Micro] Ejecutar micro-maniobras de búsqueda dentro del área
            if (navigationMode == NavigationMode.Micro)
                CheckMicroManeuver();

            // [Skip] Máquina de estados de dos pasadas
            if (navigationMode == NavigationMode.Skip)
                UpdateSkipMode();
        }

        // No avanzar waypoints mientras el drone está pausado (Hover), maniobra (Micro),
        // o en fases de Skip que gestionan su propio agente
        if (!manualControl && hasRoute && Time.time - lastPathUpdateTime > updatePathInterval
            && !isReturningToBase && !isHoveringForObstacle && !isMicroManeuvering
            && skipPhase != SkipPhase.Recharging
            && skipPhase != SkipPhase.Revisiting
            && skipPhase != SkipPhase.Done)
        {
            if (searchWaypoints.Count > 0 && currentWaypointIndex < searchWaypoints.Count)
            {
                agent.SetDestination(searchWaypoints[currentWaypointIndex]);
                lastPathUpdateTime = Time.time;

                // Check if reached current waypoint
                if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
                {
                    currentWaypointIndex++;

                    // If completed all waypoints
                    if (currentWaypointIndex >= searchWaypoints.Count)
                    {
                        if (!missionComplete)
                        {
                            missionComplete = true;
                            isReturningToBase = true;
                            // Skip: marcar que el retorno es para recargar, no terminar
                            if (navigationMode == NavigationMode.Skip)
                                skipPhase = SkipPhase.ReturningAfterFirstPass;
                            GenerateReturnPath();
                        }
                        else if (isReturningToBase)
                        {
                            if (navigationMode == NavigationMode.Skip
                                && skipPhase == SkipPhase.ReturningAfterFirstPass)
                            {
                                // Llegó a base tras primera pasada → iniciar recarga
                                isReturningToBase = false;
                                hasRoute = false;
                                searchWaypoints.Clear();
                                skipPhase = SkipPhase.Recharging;
                                energyController?.IniciarRecarga();
                                Debug.Log($"[Skip] En base. Recargando... {skippedPositions.Count} zona(s) anotadas para revisita.");
                            }
                            else
                            {
                                // Comportamiento normal (Baseline / Hover / Micro)
                                isReturningToBase = false;
                                hasRoute = false;
                                searchWaypoints.Clear();
                                Debug.Log("Mission completed and drone returned to base");
                                
                                // ¡Avisamos al Gemelo Digital que terminó el viaje!
                                if (DigitalTwin.DigitalTwinManager.Instance != null)
                                {
                                    DigitalTwin.DigitalTwinManager.Instance.EndEpisode();
                                }
                            }
                        }
                    }
                }
            }
        }

        // --- INICIO DIAGNÓSTICO ---
        if (missionComplete || isReturningToBase)
        {
            float dist = Vector3.Distance(transform.position, repostajePosition);
            Debug.Log($"[Diagnóstico Fin] isReturning={isReturningToBase} | missionComplete={missionComplete} | Distancia a Base={dist:F2}m | IsAtBase={IsAtBase()} | hasRoute={hasRoute}");
        }
        // --- FIN DIAGNÓSTICO ---

        // Parche: Validación absoluta de llegada a base (el bucle de waypoints ignoraba el final por el !isReturningToBase)
        if (IsAtBase() && hasRoute)
        {
            if (isReturningToBase && navigationMode != NavigationMode.Skip)
            {
                isReturningToBase = false;
                hasRoute = false;
                searchWaypoints.Clear();
                Debug.Log("Mission completed and drone returned to base [Unified Check]");
                if (DigitalTwin.DigitalTwinManager.Instance != null && DigitalTwin.DigitalTwinManager.Instance.isEpisodeActive)
                    DigitalTwin.DigitalTwinManager.Instance.EndEpisode();
            }
            else if (navigationMode == NavigationMode.Skip && skipPhase == SkipPhase.Done)
            {
                hasRoute = false;
                Debug.Log("Skip Mission fully completed and drone returned to base [Unified Check]");
                if (DigitalTwin.DigitalTwinManager.Instance != null && DigitalTwin.DigitalTwinManager.Instance.isEpisodeActive)
                    DigitalTwin.DigitalTwinManager.Instance.EndEpisode();
            }
        }
    }

    void FixedUpdate()
    {
        HandleAltitude();

        if (manualControl)
        {
            HandleManualMovement();
            HandleRotation();
            ForceStabilization();
        }
    }

    public void SetSearchArea(Vector3 startPos, Vector3 endPos)
    {
        startPos.y = targetHeight;
        endPos.y = targetHeight;

        missionComplete = false;
        isReturningToBase = false;
        hasRoute = true;

        // Guardar límites del área para acotar el modo Micro
        searchAreaMin = new Vector3(Mathf.Min(startPos.x, endPos.x), 0f, Mathf.Min(startPos.z, endPos.z));
        searchAreaMax = new Vector3(Mathf.Max(startPos.x, endPos.x), 0f, Mathf.Max(startPos.z, endPos.z));

        // Inicializar estado del modo Skip
        if (navigationMode == NavigationMode.Skip)
        {
            skippedPositions.Clear();
            currentSkipIndex = 0;
            isRevisitHovering = false;
            lastSkipRecordTime = -999f;
            skipPhase = SkipPhase.FirstPass;
        }

        GenerateSearchPattern(startPos, endPos);

        if (!manualControl && searchWaypoints.Count > 0)
        {
            currentWaypointIndex = 0;
            agent.SetDestination(searchWaypoints[currentWaypointIndex]);
        }
    }

    private void GenerateSearchPattern(Vector3 start, Vector3 end)
    {
        searchWaypoints.Clear();
        currentWaypointIndex = 0;

        // Calcular el área real que debe cubrir (incluyendo el tamaño del drone)
        Vector3 realStart = new Vector3(
            Mathf.Min(start.x, end.x),
            targetHeight,
            Mathf.Min(start.z, end.z));

        Vector3 realEnd = new Vector3(
            Mathf.Max(start.x, end.x),
            targetHeight,
            Mathf.Max(start.z, end.z));

        // Dimensiones reales del área a cubrir
        float width = realEnd.x - realStart.x;
        float length = realEnd.z - realStart.z;

        // Determinar dirección principal (mejor cobertura)
        bool searchAlongWidth = width >= length;
        float mainAxisLength = searchAlongWidth ? width : length;
        float crossAxisLength = searchAlongWidth ? length : width;

        // Calcular número de pasadas necesarias para cobertura completa
        // Asegurar al menos 1 pasada incluso para áreas pequeñas
        int passes = Mathf.Max(1, Mathf.CeilToInt(crossAxisLength / (searchSpacing * 0.8f)));

        // Generar waypoints en patrón de zigzag
        for (int i = 0; i <= passes; i++)
        {
            float crossAxisPos = Mathf.Lerp(0, crossAxisLength, (float)i / passes);

            // Alternar direcciones para el patrón de zigzag
            if (i % 2 == 0)
            {
                // Ida
                if (searchAlongWidth)
                {
                    searchWaypoints.Add(new Vector3(realStart.x, targetHeight, realStart.z + crossAxisPos));
                    searchWaypoints.Add(new Vector3(realEnd.x, targetHeight, realStart.z + crossAxisPos));
                }
                else
                {
                    searchWaypoints.Add(new Vector3(realStart.x + crossAxisPos, targetHeight, realStart.z));
                    searchWaypoints.Add(new Vector3(realStart.x + crossAxisPos, targetHeight, realEnd.z));
                }
            }
            else
            {
                // Vuelta
                if (searchAlongWidth)
                {
                    searchWaypoints.Add(new Vector3(realEnd.x, targetHeight, realStart.z + crossAxisPos));
                    searchWaypoints.Add(new Vector3(realStart.x, targetHeight, realStart.z + crossAxisPos));
                }
                else
                {
                    searchWaypoints.Add(new Vector3(realStart.x + crossAxisPos, targetHeight, realEnd.z));
                    searchWaypoints.Add(new Vector3(realStart.x + crossAxisPos, targetHeight, realStart.z));
                }
            }
        }

        // Eliminar waypoints duplicados consecutivos
        for (int i = searchWaypoints.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(searchWaypoints[i], searchWaypoints[i - 1]) < 0.1f)
            {
                searchWaypoints.RemoveAt(i);
            }
        }

        Debug.Log($"Patrón generado con {searchWaypoints.Count} waypoints. Área: {width}x{length}m. Pasadas: {passes}");
    }
    private void GenerateReturnPath()
    {
        searchWaypoints.Clear();
        currentWaypointIndex = 0;
        searchWaypoints.Add(repostajePosition);
        agent.SetDestination(searchWaypoints[currentWaypointIndex]);
        Debug.Log("Generated return path to base");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CAMBIO DE MODO  — Teclas numéricas (fila + teclado num.) + botón UI
    // ──────────────────────────────────────────────────────────────────────────
    private void HandleModeInput()
    {
        // Fila numérica superior
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SwitchMode(NavigationMode.Baseline);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SwitchMode(NavigationMode.Hover);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SwitchMode(NavigationMode.Micro);
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SwitchMode(NavigationMode.Skip);
        // Alpha5 / Keypad5 reservados para modo futuro
    }

    private void SwitchMode(NavigationMode newMode)
    {
        if (navigationMode == newMode) return;
        navigationMode = newMode;

        // Limpiar estado Hover si estaba activo
        if (isHoveringForObstacle)
        {
            isHoveringForObstacle = false;
            if (agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;
        }

        // Limpiar estado Micro si estaba activo
        isMicroManeuvering = false;

        // Limpiar estado Skip
        skipPhase = SkipPhase.Idle;
        skippedPositions.Clear();
        currentSkipIndex = 0;
        isRevisitHovering = false;

        Debug.Log($"[NavMode] Modo activo: {newMode} (tecla {(int)newMode})");
        UpdateNavModeButtonUI();
    }

    /// <summary>
    /// Cicla al siguiente modo de navegación en orden: Baseline→Hover→Micro→Skip→Baseline.
    /// Llamar desde el botón UI de OnClick().
    /// </summary>
    public void CycleNavigationMode()
    {
        // Los valores del enum son 1..4; hacemos módulo 4 y sumamos 1
        int next = (int)navigationMode % 4 + 1;
        SwitchMode((NavigationMode)next);
    }

    /// <summary>
    /// Actualiza la visibilidad y el texto del botón de navegación.
    /// El botón solo aparece cuando el drone está en modo autónomo (manualControl == false).
    /// </summary>
    private void UpdateNavModeButtonUI()
    {
        if (navModeButton == null) return;

        // Mostrar solo en modo navegación
        navModeButton.gameObject.SetActive(!manualControl);

        // Resolver texto: campo propio o GetComponentInChildren como fallback
        TMP_Text label = navModeButtonText
            ?? navModeButton.GetComponentInChildren<TMP_Text>();

        if (label != null)
            label.text = navigationMode.ToString();   // "Baseline", "Hover", "Micro", "Skip"
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HOVER MODE & VISIBILITY — detección matemática del paper
    // ──────────────────────────────────────────────────────────────────────────
    
    public float CalculateVisibilityScore()
    {
        // Fórmula o_{s,t} del paper: 5 raycasts
        int totalRays = 5;
        int hitsCount = 0;
        float spread = 1.0f; 

        Vector3[] rayOffsets = new Vector3[]
        {
            Vector3.zero,
            new Vector3(spread, 0, 0),
            new Vector3(-spread, 0, 0),
            new Vector3(0, 0, spread),
            new Vector3(0, 0, -spread)
        };

        foreach (Vector3 offset in rayOffsets)
        {
            Vector3 origin = transform.position + offset;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, obstacleDetectionHeight))
            {
                if (!hit.collider.CompareTag("Person") && !hit.collider.CompareTag("Car"))
                    hitsCount++;
            }
            else
            {
                // Sin impacto = área libre
                hitsCount++;
            }
        }

        return Mathf.Clamp01((float)hitsCount / totalRays);
    }

    private void CheckObstaclesBelow()
    {
        if (Time.time - lastObstacleCheckTime < obstacleCheckInterval) return;
        lastObstacleCheckTime = Time.time;

        float o_st = CalculateVisibilityScore();
        bool obstacleFound = o_st < visibilityThreshold; // o_{s*,t} < tau_o
        Vector3 groundPos = new Vector3(transform.position.x, 0f, transform.position.z);

        if (obstacleFound && !isHoveringForObstacle)
        {
            // Oclusión detectada -> Activar política Hover
            isHoveringForObstacle = true;
            currentHoverWaitTime = 0f; // Reiniciar timer
            hoveredObstaclePosition = groundPos;
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            Debug.Log($"[Hover] Visibilidad ({o_st:F2}) < umbral ({visibilityThreshold:F2}) — Drone en estación (wait)...");
        }
        else if (obstacleFound && isHoveringForObstacle)
        {
            // Sumar tiempo a Delta t_wait mientras sigue estacionario
            currentHoverWaitTime += Time.deltaTime;
        }
        else if (!obstacleFound && isHoveringForObstacle)
        {
            // Zona despejada -> Reanudar
            isHoveringForObstacle = false;
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            Debug.Log($"[Hover] Visibilidad recuperada ({o_st:F2}) — Reanudando. Verificando bache...");
            onObstacleCleared?.Invoke(hoveredObstaclePosition);
        }
        // Sin obstáculo y sin hover previo → marcha normal (no-op)
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  SKIP MODE — dos pasadas con recarga de energía real
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Dispatcher de fases del modo Skip. Se llama en Update() independientemente de manualControl.</summary>
    private void UpdateSkipMode()
    {
        switch (skipPhase)
        {
            case SkipPhase.FirstPass:
                // No escanear mientras está volviendo a base al final de la 1ª pasada
                if (!isReturningToBase) ScanForSkipPositions();
                break;

            case SkipPhase.ReturningAfterFirstPass:
                // El waypoint-loop no puede detectar la llegada porque tiene &&!isReturningToBase.
                // Usamos IsAtBase() para hacer el polling de llegada.
                if (IsAtBase())
                {
                    isReturningToBase = false;
                    hasRoute = false;
                    searchWaypoints.Clear();
                    skipPhase = SkipPhase.Recharging;
                    energyController?.IniciarRecarga();
                    Debug.Log($"[Skip] En base. Recargando energía... {skippedPositions.Count} zona(s) anotadas para revisita.");
                }
                break;

            case SkipPhase.Recharging:
                UpdateRecharging();
                break;

            case SkipPhase.Revisiting:
                UpdateRevisiting();
                break;
        }
    }

    /// <summary>1ª pasada: anota posiciones que no cumplen con tau_o.</summary>
    private void ScanForSkipPositions()
    {
        if (Time.time - lastSkipRecordTime < skipRecordCooldown) return;

        float o_st = CalculateVisibilityScore();
        if (o_st >= visibilityThreshold) return; // Si o_st >= tau_o, el segmento es "inspected" en tiempo real

        Vector3 pos = new Vector3(transform.position.x, 0f, transform.position.z);

        // Deduplicación espacial: ignorar si ya hay una posición anotada cercana
        foreach (var sp in skippedPositions)
            if (Vector3.Distance(new Vector3(sp.x, 0, sp.z), pos) < skipDeduplicationRadius)
                return;

        skippedPositions.Add(pos);
        lastSkipRecordTime = Time.time;
        Debug.Log($"[Skip] Posición #{skippedPositions.Count} marcada como Pending: {pos} (o_st={o_st:F2} < {visibilityThreshold:F2})");
    }

    /// <summary>Espera a que EnergyController indique que la recarga está completa (~95%).</summary>
    private void UpdateRecharging()
    {
        bool cargada = (energyController != null)
            ? energyController.EstaCompleta(skipRechargeThreshold)
            : true;  // si no hay referencia, pasar directamente

        if (!cargada) return;

        if (skippedPositions.Count == 0)
        {
            Debug.Log("[Skip] Sin zonas que revisar. Misión completa.");
            skipPhase = SkipPhase.Done;
            return;
        }

        Debug.Log($"[Skip] Recarga completa ({energyController?.energia:F1}%). Iniciando revisita de {skippedPositions.Count} zona(s)...");
        skipPhase = SkipPhase.Revisiting;
        currentSkipIndex = 0;
        isRevisitHovering = false;

        // Enviar al primer punto de revisita
        if (agent.enabled && agent.isOnNavMesh)
        {
            Vector3 first = new Vector3(skippedPositions[0].x, targetHeight, skippedPositions[0].z);
            agent.SetDestination(first);
        }
    }

    /// <summary>Visita cada posición anotada, verifica baches y retorna a base al terminar.</summary>
    private void UpdateRevisiting()
    {
        // ¿Ya terminamos todas?
        if (currentSkipIndex >= skippedPositions.Count)
        {
            skipPhase = SkipPhase.Done;
            Debug.Log("[Skip] Todas las zonas revisitadas. Retornando a base.");
            if (agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(repostajePosition);
            return;
        }

        // Hovering sobre la posición actual
        if (isRevisitHovering)
        {
            if (Time.time - revisitHoverStartTime >= revisitHoverTime)
            {
                isRevisitHovering = false;
                currentSkipIndex++;
                Debug.Log($"[Skip] Revisita {currentSkipIndex}/{skippedPositions.Count} completada.");

                // Navegar al siguiente punto (si hay)
                if (currentSkipIndex < skippedPositions.Count && agent.enabled && agent.isOnNavMesh)
                {
                    Vector3 next = new Vector3(
                        skippedPositions[currentSkipIndex].x, targetHeight,
                        skippedPositions[currentSkipIndex].z);
                    agent.SetDestination(next);
                }
            }
            return;
        }

        // Navegando hacia el punto: ¿llegamos?
        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            // Llegamos: iniciar hover y verificar bache
            isRevisitHovering = true;
            revisitHoverStartTime = Time.time;
            Debug.Log($"[Skip] Llegado a zona #{currentSkipIndex + 1}. Verificando bache ({revisitHoverTime}s)...");
            onObstacleCleared?.Invoke(skippedPositions[currentSkipIndex]);
        }
        else if (Time.time - lastPathUpdateTime > updatePathInterval)
        {
            // Refrescar destino periódicamente
            Vector3 target = new Vector3(
                skippedPositions[currentSkipIndex].x, targetHeight,
                skippedPositions[currentSkipIndex].z);
            if (agent.enabled && agent.isOnNavMesh) agent.SetDestination(target);
            lastPathUpdateTime = Time.time;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  MICRO MODE — movimiento errático acotado al área de búsqueda
    // ──────────────────────────────────────────────────────────────────────────
    private void CheckMicroManeuver()
    {
        if (isMicroManeuvering)
        {
            // ¿Terminó la duración de la maniobra actual?
            if (Time.time - microManeuverStartTime >= microManeuverDuration)
            {
                isMicroManeuvering = false;
                // Retomar el waypoint real de la ruta zigzag
                if (agent.enabled && agent.isOnNavMesh
                    && searchWaypoints.Count > 0 && currentWaypointIndex < searchWaypoints.Count)
                {
                    agent.SetDestination(searchWaypoints[currentWaypointIndex]);
                    Debug.Log($"[Micro] Maniobra terminada — Retomando waypoint {currentWaypointIndex}");
                }
            }
            return; // El agente ya tiene su micro-destino asignado
        }

        // ¿Es hora de iniciar otra micro-maniobra?
        if (Time.time - lastMicroManeuverTime < microManeuverInterval) return;
        lastMicroManeuverTime  = Time.time;
        microManeuverStartTime = Time.time;
        isMicroManeuvering     = true;

        // Ángulo y radio aleatorios para el desvío
        float angleRad = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float radius   = UnityEngine.Random.Range(microManeuverRadius * 0.3f, microManeuverRadius);
        
        // Matemática estricta: delta_t en R^2 sujeto a ||delta_t|| <= d_max
        Vector2 delta_t = new Vector2(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius);
        delta_t = Vector2.ClampMagnitude(delta_t, microManeuverRadius); // d_max = microManeuverRadius
        
        currentMicroDelta = delta_t; // Guardar para DigitalTwinManager
        Vector3 offset = new Vector3(delta_t.x, 0f, delta_t.y);

        // Clampo al bounding box del área de búsqueda para no salirse
        Vector3 microTarget = new Vector3(
            Mathf.Clamp(transform.position.x + offset.x, searchAreaMin.x, searchAreaMax.x),
            targetHeight,
            Mathf.Clamp(transform.position.z + offset.z, searchAreaMin.z, searchAreaMax.z)
        );

        agent.SetDestination(microTarget);
        Debug.Log($"[Micro] Micro-maniobra → radio={radius:F2}m, ángulo={angleRad * Mathf.Rad2Deg:F0}°");
    }

    private void SmoothWaypoints()
    {
        if (searchWaypoints.Count < 3) return;

        List<Vector3> smoothed = new List<Vector3>();
        smoothed.Add(searchWaypoints[0]);

        for (int i = 1; i < searchWaypoints.Count - 1; i++)
        {
            // Average between previous, current and next waypoint
            Vector3 smoothedPoint = (searchWaypoints[i - 1] + searchWaypoints[i] + searchWaypoints[i + 1]) / 3f;
            smoothedPoint.y = targetHeight;
            smoothed.Add(smoothedPoint);
        }

        smoothed.Add(searchWaypoints[searchWaypoints.Count - 1]);
        searchWaypoints = smoothed;
    }

    public void ToggleControlMode()
    {
        manualControl = !manualControl;
        ConfigureNavAgent(!manualControl);

        if (!manualControl && hasRoute && searchWaypoints.Count > 0)
        {
            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, searchWaypoints.Count - 1);
            agent.Warp(transform.position);
            agent.SetDestination(searchWaypoints[currentWaypointIndex]);
        }
        else if (manualControl)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        UpdateNavModeButtonUI();
    }

    private void ConfigureNavAgent(bool active)
    {
        if (active)
        {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.stoppingDistance = stoppingDistance;
            agent.speed = moveSpeed;
            agent.angularSpeed = rotationSpeed;
            agent.acceleration = moveSpeed * 5;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
        }
        else
        {
            // Reset path only if agent is active and on a NavMesh to avoid errors
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            
            agent.enabled = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    [Header("Mobile Controls")]
    public VirtualJoystick virtualJoystick;
    public VirtualJoystick heightJoystick; // Optional second joystick for height/rot

    private void HandleInput()
    {
        // ── Ajuste de altura: I (subir) / K (bajar) — funciona en AMBOS modos ──
        if (Input.GetKey(KeyCode.I))
            targetHeight += 5f * Time.deltaTime;
        if (Input.GetKey(KeyCode.K))
        {
            targetHeight -= 5f * Time.deltaTime;
            targetHeight = Mathf.Max(targetHeight, minHeight);
        }

        if (manualControl)
        {
            // Keyboard Input
            float vKeyboard = Input.GetKey(KeyCode.Space) ? 1 : Input.GetKey(KeyCode.LeftControl) ? -1 : 0;
            Vector2 mKeyboard = new Vector2(
                Input.GetKey(KeyCode.Q) ? -1 : Input.GetKey(KeyCode.E) ? 1 : 0,
                Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
            );
            float rKeyboard = Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;

            // Touch Input (Joystick)
            Vector3 joyInput = (virtualJoystick != null) ? virtualJoystick.InputDirection : Vector3.zero;
            Vector3 heightJoyInput = (heightJoystick != null) ? heightJoystick.InputDirection : Vector3.zero;

            // Combine inputs (Keyboard + Joystick)
            verticalInput = vKeyboard + heightJoyInput.y;
            movementInput.x = mKeyboard.x + joyInput.x;
            movementInput.y = mKeyboard.y + joyInput.y;
            currentRotation = rKeyboard + heightJoyInput.x;
            
            // Clamp to ensure we don't exceed 1.0 force if using both
            verticalInput = Mathf.Clamp(verticalInput, -1f, 1f);
            movementInput = Vector2.ClampMagnitude(movementInput, 1f);
            currentRotation = Mathf.Clamp(currentRotation, -1f, 1f);
        }
        else
        {
            verticalInput = 0;
            movementInput = Vector2.zero;
            currentRotation = 0;
        }
    }

    private void HandleAltitude()
    {
        float currentHeight = transform.position.y;

        if (currentHeight < minHeight)
        {
            rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(0, rb.linearVelocity.y), rb.linearVelocity.z);
            currentHeight = minHeight;
        }

        float heightError = targetHeight - currentHeight;

        if (Mathf.Abs(heightError) < heightDeadZone)
        {
            heightPID.ResetIntegral();
        }

        float pidOutput = heightPID.Update(heightError, Time.fixedDeltaTime);

        if (pidOutput < 0 && currentHeight >= targetHeight)
        {
            pidOutput *= 0.3f;
        }

        if (heightError > 0.5f)
        {
            pidOutput *= 1.5f;
        }

        Vector3 verticalForce = Vector3.up * (pidOutput + verticalInput * maxAscendSpeed);
        rb.AddForce(verticalForce, ForceMode.Acceleration);
    }

    private void HandleManualMovement()
    {
        if (movementInput.magnitude > 0)
        {
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);
            moveDirection = transform.TransformDirection(moveDirection);
            rb.AddForce(moveDirection * moveSpeed, ForceMode.Acceleration);
        }
    }

    private void HandleRotation()
    {
        if (currentRotation != 0)
        {
            float rotationAmount = currentRotation * rotationSpeed * Time.fixedDeltaTime;
            rb.AddTorque(Vector3.up * rotationAmount, ForceMode.VelocityChange);
        }
    }

    private void ApplyMotorRotation()
    {
        float targetTiltX = movementInput.y * tiltAngle;
        float targetTiltZ = -movementInput.x * tiltAngle;

        for (int i = 0; i < motors.Length; i++)
        {
            float rotationDirection = alternateRotation ? (i % 2 == 0 ? 1 : -1) : 1;
            motorRotationAngles[i] += motorRotationSpeed * rotationDirection * Time.deltaTime;

            if (motorRotationAngles[i] > 360f) motorRotationAngles[i] -= 360f;
            if (motorRotationAngles[i] < -360f) motorRotationAngles[i] += 360f;

            motors[i].localRotation = Quaternion.Euler(targetTiltX, motorRotationAngles[i], targetTiltZ);
        }
    }

    private void ForceStabilization()
    {
        Quaternion currentRot = transform.rotation;
        float currentYRotation = currentRot.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, currentYRotation, 0);

        Quaternion rotationDiff = targetRotation * Quaternion.Inverse(currentRot);
        Vector3 rotationDiffEuler = rotationDiff.eulerAngles;

        rotationDiffEuler.x = (rotationDiffEuler.x > 180) ? rotationDiffEuler.x - 360 : rotationDiffEuler.x;
        rotationDiffEuler.z = (rotationDiffEuler.z > 180) ? rotationDiffEuler.z - 360 : rotationDiffEuler.z;

        Vector3 stabilizationTorque = new Vector3(
            rotationDiffEuler.x * stabilizationSpeed,
            0,
            rotationDiffEuler.z * stabilizationSpeed
        );

        stabilizationTorque = Vector3.ClampMagnitude(stabilizationTorque, maxStabilizationTorque);
        rb.AddTorque(stabilizationTorque, ForceMode.Acceleration);

        Vector3 angularVelocity = rb.angularVelocity;
        if (currentRotation == 0)
        {
            angularVelocity.y = Mathf.Lerp(angularVelocity.y, 0, Time.fixedDeltaTime * stabilizationSpeed);
        }
        angularVelocity.x = Mathf.Lerp(angularVelocity.x, 0, Time.fixedDeltaTime * stabilizationSpeed * 2);
        angularVelocity.z = Mathf.Lerp(angularVelocity.z, 0, Time.fixedDeltaTime * stabilizationSpeed * 2);

        rb.angularVelocity = angularVelocity;
    }

    // Visualize waypoints in editor
    void OnDrawGizmosSelected()
    {
        if (searchWaypoints != null && searchWaypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < searchWaypoints.Count - 1; i++)
            {
                Gizmos.DrawLine(searchWaypoints[i], searchWaypoints[i + 1]);
                Gizmos.DrawSphere(searchWaypoints[i], 0.1f);
            }
            Gizmos.DrawSphere(searchWaypoints[searchWaypoints.Count - 1], 0.1f);

            // Draw current target
            if (currentWaypointIndex < searchWaypoints.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(searchWaypoints[currentWaypointIndex], 0.2f);
            }
        }
    }
        public void ReturnToBase()
{
    missionComplete = true;
    isReturningToBase = true;
    hasRoute = true;

    searchWaypoints.Clear();
    currentWaypointIndex = 0;

    if (!manualControl && agent.isOnNavMesh)
    {
        agent.ResetPath();
        agent.SetDestination(repostajePosition);
    }

    Debug.Log("Retorno forzado a base por energía baja");
}

public bool IsManualControl()
{
    return manualControl;
}

public bool IsReturningToBase()
{
    return isReturningToBase;
}

    public bool IsAtBase()
    {
        // Ignorar la altura (Y) para calcular la distancia, ya que el dron vuela a varios metros del suelo.
        Vector3 pos2D = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 base2D = new Vector3(repostajePosition.x, 0f, repostajePosition.z);
        
        return Vector3.Distance(pos2D, base2D) < 2.5f;
    }
    public void ApagarDrone()
    {
        if (apagado || apagando) return;

        Debug.Log("Iniciando apagado del dron...");

        apagando = true;
        manualControl = false;
        hasRoute = false;
        isReturningToBase = false;

        agent.enabled = false;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        tiempoApagado = 0f;
    }
private void FinalizarApagado()
{
    apagado = true;
    apagando = false;

    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
    rb.isKinematic = true;

    Debug.Log("Dron apagado completamente.");
}


public bool EstaApagado()
{
    return apagado;
}
public bool IsFullyShutdown()
{
    return apagado;
}
}

public class PIDController
{
    private float Kp, Ki, Kd;
    private float integral;
    private float lastError;
    private float maxIntegral = 10f;

    public PIDController(float Kp, float Ki, float Kd)
    {
        this.Kp = Kp;
        this.Ki = Ki;
        this.Kd = Kd;
    }

    public float Update(float error, float deltaTime)
    {
        integral += error * deltaTime;
        integral = Mathf.Clamp(integral, -maxIntegral, maxIntegral);

        float derivative = (error - lastError) / deltaTime;
        lastError = error;

        return Kp * error + Ki * integral + Kd * derivative;
    }

    public void ResetIntegral()
    {
        integral = 0;
    }


}