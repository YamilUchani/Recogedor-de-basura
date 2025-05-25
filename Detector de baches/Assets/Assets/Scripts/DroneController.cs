using UnityEngine;

public class DroneController : MonoBehaviour
{
    [Header("Configuración Altura")]
    public float targetHeight = 5f;
    public float maxAscendSpeed = 10f;
    public float heightPID_Kp = 50f;
    public float heightPID_Ki = 5f;
    public float heightPID_Kd = 10f;

    [Header("Configuración Movimiento")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 90f;
    public float tiltAngle = 15f;
    public float stabilizationSpeed = 5f;
    public float maxStabilizationTorque = 100f;

    [Header("Configuración Motores")]
    public float motorRotationSpeed = 500f;
    public bool alternateRotation = true; // Si true, los motores alternan direcciones

    [Header("Referencias")]
    public Transform[] motors;
    public Transform modelRoot;

    private Rigidbody rb;
    private PIDController heightPID;
    private float verticalInput;
    private float currentRotation;
    private Vector2 movementInput;
    private float[] motorRotationAngles;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        heightPID = new PIDController(heightPID_Kp, heightPID_Ki, heightPID_Kd);
        
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
        
        motorRotationAngles = new float[motors.Length];
    }

    void Update()
    {
        HandleInput();
        ApplyMotorRotation();
    }

    void FixedUpdate()
    {
        HandleAltitude();
        HandleMovement();
        HandleRotation();
        ForceStabilization();
    }

    private void HandleInput()
    {
        verticalInput = Input.GetKey(KeyCode.Space) ? 1 : Input.GetKey(KeyCode.LeftControl) ? -1 : 0;
        movementInput.x = Input.GetKey(KeyCode.Q) ? -1 : Input.GetKey(KeyCode.E) ? 1 : 0;
        movementInput.y = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        currentRotation = Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;
    }

    private void HandleAltitude()
    {
        float currentHeight = transform.position.y;
        float heightError = targetHeight - currentHeight;
        float pidOutput = heightPID.Update(heightError, Time.fixedDeltaTime);
        
        Vector3 verticalForce = Vector3.up * (pidOutput + verticalInput * maxAscendSpeed);
        rb.AddForce(verticalForce, ForceMode.Acceleration);
    }

    private void HandleMovement()
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

        for(int i = 0; i < motors.Length; i++)
        {
            // Determinar dirección de rotación (alternando si alternateRotation=true)
            float rotationDirection = alternateRotation ? (i % 2 == 0 ? 1 : -1) : 1;
            
            // Actualizar ángulo de rotación en Y
            motorRotationAngles[i] += motorRotationSpeed * rotationDirection * Time.deltaTime;
            
            // Normalizar ángulo (opcional, pero útil para depuración)
            if(motorRotationAngles[i] > 360f) motorRotationAngles[i] -= 360f;
            if(motorRotationAngles[i] < -360f) motorRotationAngles[i] += 360f;

            // Aplicar rotación combinada (tilt + rotación Y)
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
        rotationDiffEuler.y = (rotationDiffEuler.y > 180) ? rotationDiffEuler.y - 360 : rotationDiffEuler.y;
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
}

public class PIDController
{
    private float Kp, Ki, Kd;
    private float integral;
    private float lastError;

    public PIDController(float Kp, float Ki, float Kd)
    {
        this.Kp = Kp;
        this.Ki = Ki;
        this.Kd = Kd;
    }

    public float Update(float error, float deltaTime)
    {
        integral += error * deltaTime;
        float derivative = (error - lastError) / deltaTime;
        lastError = error;
        return Kp * error + Ki * integral + Kd * derivative;
    }
}