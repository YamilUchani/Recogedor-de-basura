using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace DigitalTwin
{
    public class DigitalTwinManager : MonoBehaviour
    {
        public static DigitalTwinManager Instance { get; private set; }

        [Header("1. Estado Global del DT (D_t)")]
        [Tooltip("Viaje o entrenamiento actual")]
        public int currentEpisode = 0;
        
        [Tooltip("¿El episodio de entrenamiento está en curso?")]
        public bool isEpisodeActive = false;

        [Tooltip("Paso de tiempo discreto (t)")]
        public int timeStep = 0; 

        [Header("2. Segmentos de Carretera (S)")]
        public List<RoadSegment> roadNetwork = new List<RoadSegment>();
        public RoadSegment currentTarget; // s*

        [Header("3. Estado del UAV (x_t)")]
        public UAVState currentUAVState;

        [Header("4. Agentes Dinámicos (A_t)")]
        public List<TrafficAgent> activeAgents = new List<TrafficAgent>();

        [Header("5. Visibilidad y Memoria (M_t)")]
        [Tooltip("Umbral de activación (\tau_o)")]
        public float visibilityThreshold = 0.7f; 
        public Dictionary<RoadSegment, SegmentStatus> inspectionMemory = new Dictionary<RoadSegment, SegmentStatus>();
        public List<RoadSegment> revisitQueue = new List<RoadSegment>();

        [Header("Referencias a Sistemas Reales")]
        [Tooltip("El controlador de movimiento físico real")]
        private DroneNavMeshController droneController;
        private TrafficDensityController densityController;
        private float currentVisibilityScore = 1.0f; 
        [Tooltip("La interfaz encargada de los raycasts de visibilidad")]
        public MovementInterface movementInterface;

        // Lista para recopilar los datos del viaje
        private List<string> episodeLog = new List<string>();
        
        // --- Registro secundario para A_t (Tráfico) ---
        private List<string> trafficLog = new List<string>();
        
        // --- Cálculo de Velocidad universal ---
        private Dictionary<int, Vector3> previousAgentPositions = new Dictionary<int, Vector3>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!isEpisodeActive) return; // Solo avanza si hay un viaje en curso

            // 1. Avanzar tiempo discreto t (como exige el paper)
            timeStep++;

            // 2. Traffic/Env (A_t)
            UpdateAgentsState();

            // 3. UAV State (x_t) - Mapea variables reales a la formalización
            UpdateUAVState();

            // 4 y 5. Visibilidad (o_{s,t}) y Memoria (M_t)
            UpdateVisibilityState();

            // 6. Action (\Pi) - Leer la política de DroneController.cs
            SyncRecoveryPolicy();

            // 7. Recopilar datos para el archivo .txt
            RecordLogStep();
        }

        private void RecordLogStep()
        {
            // Formato CSV alineado matemáticamente con el paper:
            // t: Tiempo discreto
            // p_t_x, p_t_y: Posición plana en R^2
            // h_t: Altitud
            // v_t: Velocidad escalar
            // e_t: Batería
            // |A_t|: Cardinalidad del conjunto de agentes dinámicos
            // |M_t|: Tamaño de la memoria de inspección (baches documentados)
            // o_s_t: Score de visibilidad [0, 1]
            // Pi: Política de recuperación actual
            // D_t: Densidad de tráfico actual (Low=1, Medium=2, High=3)
            // dt_wait: Delta t_wait (Tiempo de espera en estación para Hover)
            // dt_x, dt_y: Componentes del vector delta_t para Micro
            int densityValue = densityController != null ? (int)densityController.currentDensity : 1;
            
            float dt_wait = 0f;
            float dt_x = 0f;
            float dt_y = 0f;

            if (droneController != null)
            {
                if (currentPolicy == RecoveryPolicy.Hover)
                    dt_wait = droneController.currentHoverWaitTime;
                else if (currentPolicy == RecoveryPolicy.Micro)
                {
                    dt_x = droneController.currentMicroDelta.x;
                    dt_y = droneController.currentMicroDelta.y;
                }
            }
            
            string logLine = $"{timeStep},{currentUAVState.p.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{currentUAVState.p.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{currentUAVState.h.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{currentUAVState.v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{currentUAVState.e.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)},{activeAgents.Count},{inspectionMemory.Count},{currentVisibilityScore.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{currentPolicy},{densityValue},{dt_wait.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{dt_x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{dt_y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}";
            episodeLog.Add(logLine);

            // Guardar detalles de A_t para este instante
            for (int i = 0; i < activeAgents.Count; i++)
            {
                var a = activeAgents[i];
                // Formato: t, id_relativo, y_x, y_z, nu (velocidad escalar), kappa (clase)
                string tLine = $"{timeStep},{i},{a.position.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{a.position.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{a.velocity.magnitude.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},{a.type}";
                trafficLog.Add(tLine);
            }
        }

        private void UpdateUAVState()
        {
            droneController = FindFirstObjectByType<DroneNavMeshController>();
            densityController = FindFirstObjectByType<TrafficDensityController>();
            if (droneController == null) return;

            Vector3 pos = droneController.transform.position;
            currentUAVState.p = new Vector2(pos.x, pos.z);
            
            float groundHeight = Terrain.activeTerrain != null ? Terrain.activeTerrain.SampleHeight(pos) : 0f;
            currentUAVState.h = pos.y - groundHeight;

            // La velocidad física en un dron controlado por NavMesh no está en el Rigidbody, está en el NavMeshAgent
            UnityEngine.AI.NavMeshAgent nav = droneController.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nav != null)
                currentUAVState.v = nav.velocity.magnitude;
            
            // Usamos GetComponent por si el droneController.energyController no se asignó en el Inspector
            EnergyController eController = droneController.energyController != null ? droneController.energyController : droneController.GetComponent<EnergyController>();
            if (eController != null)
                currentUAVState.e = eController.energia / 100f;
        }

        private void UpdateAgentsState()
        {
            activeAgents.Clear();

            GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");
            GameObject[] persons = GameObject.FindGameObjectsWithTag("Person");

            List<GameObject> allWorldAgents = new List<GameObject>();
            allWorldAgents.AddRange(cars);
            allWorldAgents.AddRange(persons);

            // Lista temporal para limpiar el diccionario de memoria muerta
            List<int> currentFrameIds = new List<int>();

            foreach (GameObject obj in allWorldAgents)
            {
                TrafficAgent agent = new TrafficAgent();
                agent.position = obj.transform.position;
                
                int id = obj.GetInstanceID();
                currentFrameIds.Add(id);

                // Cálculo universal de velocidad (delta posición / delta tiempo)
                if (previousAgentPositions.TryGetValue(id, out Vector3 prevPos))
                {
                    // Solo calculamos velocidad horizontal en plano (X, Z) que es lo relevante
                    Vector3 delta = new Vector3(agent.position.x - prevPos.x, 0f, agent.position.z - prevPos.z);
                    agent.velocity = delta / Time.fixedDeltaTime;
                }
                else
                {
                    agent.velocity = Vector3.zero;
                }
                
                previousAgentPositions[id] = agent.position;
                agent.type = obj.CompareTag("Car") ? AgentType.Vehicle : AgentType.Pedestrian;
                
                activeAgents.Add(agent);
            }

            // Limpiar agentes que fueron destruidos (garbage collection del diccionario)
            var keys = new List<int>(previousAgentPositions.Keys);
            foreach (int k in keys)
            {
                if (!currentFrameIds.Contains(k))
                    previousAgentPositions.Remove(k);
            }
        }

        private void UpdateVisibilityState()
        {
            if (droneController == null) return;
            
            // Fórmula o_{s,t} del paper: 1.0 = totalmente visible, 0.0 = totalmente ocluido
            // Usamos 5 raycasts hacia abajo (centro + 4 bordes)
            int totalRays = 5;
            int hitsCount = 0;
            float spread = 1.0f; // Dispersión de los rayos en metros

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
                Vector3 origin = droneController.transform.position + offset;
                // Si NO choca con Person o Car, el rayo llega al suelo y está visible
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f))
                {
                    if (!hit.collider.CompareTag("Person") && !hit.collider.CompareTag("Car"))
                    {
                        hitsCount++;
                    }
                }
                else
                {
                    // Si no choca con nada asume visible
                    hitsCount++;
                }
            }

            currentVisibilityScore = (float)hitsCount / totalRays;
            currentVisibilityScore = Mathf.Clamp01(currentVisibilityScore);
        }

        private void SyncRecoveryPolicy()
        {
            if (droneController == null) return;

            RecoveryPolicy currentPolicyLocal = RecoveryPolicy.None;
            
            if (droneController.navigationMode == NavigationMode.Hover) 
                currentPolicyLocal = RecoveryPolicy.Hover;
            else if (droneController.navigationMode == NavigationMode.Micro) 
                currentPolicyLocal = RecoveryPolicy.Micro;
            else if (droneController.navigationMode == NavigationMode.Skip) 
                currentPolicyLocal = RecoveryPolicy.Skip;

            this.currentPolicy = currentPolicyLocal;
        }

        // Se declara la variable para guardar en memoria
        public RecoveryPolicy currentPolicy = RecoveryPolicy.None;

        public void StartNewEpisode()
        {
            currentEpisode++;
            timeStep = 0; 
            isEpisodeActive = true;
            
            inspectionMemory.Clear();
            revisitQueue.Clear();
            previousAgentPositions.Clear();
            
            episodeLog.Clear();
            // Encabezados usando la nomenclatura matemática estricta del paper:
            // |A_t| = Cardinalidad de agentes, |M_t| = Tamaño de la memoria de inspección
            // o_s_t = Visibility Score, D_t = Traffic Density
            // dt_wait = Delta t_wait (Hover), dt_x y dt_y = componentes de delta_t (Micro)
            episodeLog.Add("t,p_t_x,p_t_y,h_t,v_t,e_t,|A_t|,|M_t|,o_s_t,Pi,D_t,dt_wait,dt_x,dt_y");

            trafficLog.Clear();
            // Encabezados para el log de agentes: tupla (y_i^t, \nu_i^t, \kappa_i)
            trafficLog.Add("t,agent_id,y_t_x,y_t_y,nu_t,kappa_i");

            Debug.Log($"[Digital Twin] Iniciando Viaje / Episodio de Entrenamiento #{currentEpisode}");
        }

        public void EndEpisode()
        {
            isEpisodeActive = false;
            
            // Guardar el log a un archivo .txt
            string folderPath = Path.Combine(Application.dataPath, "DigitalTwin_Logs");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            // Cambiado a .csv para que Excel o Python/Pandas lo lean como tabla nativa
            string fileName = $"Episode_{currentEpisode}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllLines(filePath, episodeLog);

            // Guardar el archivo secundario de tráfico
            string trafficFileName = $"Episode_{currentEpisode}_{System.DateTime.Now:yyyyMMdd_HHmmss}_TrafficData.csv";
            string trafficFilePath = Path.Combine(folderPath, trafficFileName);
            File.WriteAllLines(trafficFilePath, trafficLog);
            
            Debug.Log($"[Digital Twin] Viaje/Episodio finalizado. Logs guardados en: {folderPath}");
        }
    }
}
