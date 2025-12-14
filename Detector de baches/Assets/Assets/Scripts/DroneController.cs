using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Refuel Position")]
    public Vector3 repostajePosition = new Vector3(0.5f, 0f, 0.5f);

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
        ApplyMotorRotation();

        if (!manualControl && hasRoute && Time.time - lastPathUpdateTime > updatePathInterval && !isReturningToBase)
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
                            GenerateReturnPath();
                        }
                        else if (isReturningToBase)
                        {
                            // Reached refuel position
                            isReturningToBase = false;
                            hasRoute = false;
                            searchWaypoints.Clear();
                            Debug.Log("Mission completed and drone returned to base");
                        }
                    }
                }
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
        return Vector3.Distance(transform.position, repostajePosition) < 0.5f;
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