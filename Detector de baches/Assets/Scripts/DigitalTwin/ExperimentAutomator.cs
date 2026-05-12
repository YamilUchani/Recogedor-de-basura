using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DigitalTwin
{
    /// <summary>
    /// Automatizador para generar la tabla de resultados de todas las combinaciones
    /// (Traffic x Altitude x Strategy) requeridas en el Digital Twin.
    ///
    /// Para cada permutación replica exactamente lo que hace el usuario al hacer clic
    /// en una zona del mapa: toma primeraPosicion y posicionFinal del componente
    /// MostrarSoloAlPasarMouse y los pasa a DroneNavMeshController.SetSearchArea().
    /// </summary>
    public class ExperimentAutomator : MonoBehaviour
    {
        [Header("Controllers a Ciclar")]
        public TrafficDensityController densityController;
        public DroneHeightController heightController;
        public DroneNavMeshController droneController;

        [Header("Configuración")]
        [Tooltip("Tiempo máximo (segundos) por ronda antes de forzar la siguiente.")]
        public float maxTimeoutSeconds = 300f;

        [Header("Botón de Inicio (opcional)")]
        [Tooltip("Arrastra aquí el botón que debe lanzar el batch. Se conecta automáticamente.")]
        public Button startButton;

        private List<string> tableData = new List<string>();
        // Todas las zonas disponibles en la escena
        private MostrarSoloAlPasarMouse[] allZones;

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(StartExperimentBatch);
        }

        // ── Punto de entrada ────────────────────────────────────────────────────
        [ContextMenu("▶ Start Experiment Batch")]
        public void StartExperimentBatch()
        {
            if (DigitalTwinManager.Instance == null)
            {
                Debug.LogError("[Automator] DigitalTwinManager no encontrado en la escena.");
                return;
            }

            // Recopilar TODAS las zonas disponibles en la escena (incluyendo desactivadas)
            allZones = FindObjectsByType<MostrarSoloAlPasarMouse>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allZones == null || allZones.Length == 0)
            {
                Debug.LogError("[Automator] No se encontró ningún MostrarSoloAlPasarMouse en la escena.");
                return;
            }

            Debug.Log($"[Automator] {allZones.Length} zona(s) detectadas: " +
                      string.Join(", ", System.Array.ConvertAll(allZones, z => z.gameObject.name)));

            StartCoroutine(RunAllPermutations());
        }

        // ── Corrutina principal ──────────────────────────────────────────────────
        private IEnumerator RunAllPermutations()
        {
            Debug.Log("[Automator] Iniciando Batch...");
            tableData.Clear();
            tableData.Add("Traffic,Altitude,Strategy,Zone,Coverage(%),Time(s),EnergyCons(%),RevisitRatio");

            // Traffic: Low=1, Medium=2, High=3
            for (int t = 1; t <= 3; t++)
            {
                // Altitude: Low=0, Medium=1, High=2
                for (int a = 0; a <= 2; a++)
                {
                    // Strategy: Baseline=1, Hover=2, Micro=3, Skip=4
                    for (int s = 1; s <= 4; s++)
                    {
                        // ── Configurar escenario ───────────────────────────────
                        SetDensity(t);
                        SetAltitude(a);
                        SetNavigation(s);

                        yield return new WaitForSeconds(0.5f);

                        string trafficStr = densityController.currentDensity.ToString();
                        string altStr     = heightController.currentLevel.ToString();
                        string navStr     = droneController.navigationMode.ToString();

                        // ── Elegir zona al azar para este episodio ─────────────
                        MostrarSoloAlPasarMouse chosenZone = allZones[Random.Range(0, allZones.Length)];
                        Vector3 posInicio = chosenZone.primeraPosicion;
                        Vector3 posFinal  = chosenZone.posicionFinal;
                        string zoneName   = chosenZone.gameObject.name;

                        Debug.Log($"[Automator] ▶ {trafficStr} | {altStr} | {navStr} | Zona: {zoneName}");

                        // ── Iniciar simulación (= clic en la zona del minimapa) ──
                        float startEnergy = (droneController.energyController != null)
                            ? droneController.energyController.energia : 100f;
                        float startTime = Time.time;

                        // Replicar OnPointerClick de MostrarSoloAlPasarMouse
                        droneController.apagado       = false;
                        droneController.manualControl  = false;
                        droneController.SetSearchArea(posInicio, posFinal);
                        DigitalTwinManager.Instance.StartNewEpisode();

                        yield return new WaitForSeconds(1f);

                        // ── Esperar fin del episodio ────────────────────────────
                        bool done = false;
                        while (!done)
                        {
                            if (!DigitalTwinManager.Instance.isEpisodeActive)
                            {
                                done = true;
                            }
                            else if (Time.time - startTime >= maxTimeoutSeconds)
                            {
                                Debug.LogWarning($"[Automator] Timeout en {trafficStr}-{altStr}-{navStr}.");
                                DigitalTwinManager.Instance.EndEpisode();
                                done = true;
                            }
                            yield return null;
                        }

                        // ── Recopilar métricas ──────────────────────────────────
                        float timeTaken      = Time.time - startTime;
                        float endEnergy      = (droneController.energyController != null)
                            ? droneController.energyController.energia : 0f;
                        float energyConsumed = Mathf.Max(0f, startEnergy - endEnergy);

                        int totalRoads  = DigitalTwinManager.Instance.roadNetwork.Count;
                        int inspected   = DigitalTwinManager.Instance.inspectionMemory.Count;
                        float coverage  = (totalRoads > 0)
                            ? ((float)inspected / totalRoads) * 100f : 0f;

                        int revisits        = DigitalTwinManager.Instance.revisitQueue.Count;
                        float revisitRatio  = (inspected + revisits > 0)
                            ? (float)revisits / (inspected + revisits) : 0f;

                        // ── Guardar fila (incluye nombre de zona) ───────────────
                        string row = $"{trafficStr},{altStr},{navStr},{zoneName}," +
                                     $"{coverage:F2},{timeTaken:F2},{energyConsumed:F2},{revisitRatio:F3}";
                        tableData.Add(row);
                        Debug.Log($"[Automator] ✓ {row}");

                        ResetDrone();
                        yield return new WaitForSeconds(3f);
                    }
                }
            }

            // ── Guardar CSV ──────────────────────────────────────────────────────
            string path = Path.Combine(Application.dataPath,
                                       "DigitalTwin_Logs",
                                       "Latex_Experiment_Results.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, tableData);
            Debug.Log($"[Automator] ¡BATCH TERMINADO! Guardado en:\n{path}");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private void ResetDrone()
        {
            if (droneController == null) return;
            droneController.manualControl      = true;
            droneController.apagado            = false;
            droneController.transform.position = droneController.repostajePosition;
            droneController.energyController?.IniciarRecarga();
        }

        private void SetDensity(int target)
        {
            if (densityController == null) return;
            for (int i = 0; i < 6 && (int)densityController.currentDensity != target; i++)
                densityController.CycleDensityMode();
        }

        private void SetAltitude(int target)
        {
            if (heightController == null) return;
            for (int i = 0; i < 6 && (int)heightController.currentLevel != target; i++)
                heightController.CycleHeightMode();
        }

        private void SetNavigation(int target)
        {
            if (droneController == null) return;
            for (int i = 0; i < 6 && (int)droneController.navigationMode != target; i++)
                droneController.CycleNavigationMode();
        }
    }
}
