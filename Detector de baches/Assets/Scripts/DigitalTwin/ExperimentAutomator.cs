using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DigitalTwin
{
    /// <summary>
    /// Automatizador del experimento batch del Digital Twin.
    ///
    /// ESTRUCTURA CORRECTA:
    ///   Un EPISODIO = una CALLE completa (todos sus segmentos en orden).
    ///   Traffic(3) × Altitude(3) × Strategy(4) × Calle(N) = episodios totales.
    ///
    /// Por episodio (calle):
    ///   1. StartNewEpisode()              ← UNA VEZ por calle
    ///   2. suppressAutoEndEpisode = true  ← el dron NO cierra el episodio entre segmentos
    ///   3. Para cada segmento (1..6):
    ///      a. StartSegment()              ← tracking por segmento
    ///      b. SetSearchArea()             ← dron sale (tránsito → ACDC ON → escaneo → ACDC OFF → base)
    ///      c. Esperar segmentDone         ← dron llegó a base y recargó
    ///      d. EndSegment()               ← guarda SegmentResult
    ///   4. suppressAutoEndEpisode = false
    ///   5. EndEpisode()                   ← UNA VEZ al final → guarda CSV del episodio
    ///   6. SaveStreetCsv(segResults)      ← CSV por calle para el paper
    /// </summary>
    public class ExperimentAutomator : MonoBehaviour
    {
        [Header("Controllers a Ciclar")]
        public TrafficDensityController densityController;
        public DroneHeightController heightController;
        public DroneNavMeshController droneController;

        [Header("Configuración")]
        [Tooltip("Tiempo máximo (segundos) por segmento antes de forzar el siguiente.")]
        public float maxTimeoutSeconds = 300f;

        [Header("Botón de Inicio (opcional)")]
        public Button startButton;

        // ── Estado interno ─────────────────────────────────────────────────────
        private List<string> globalTableData = new List<string>();
        private List<string> paperTableData = new List<string>(); // ← Novedad: Tabla exacta para el paper
        private string progressCsvPath;
        private MostrarSoloAlPasarMouse[] allZones;
        private Dictionary<Transform, List<MostrarSoloAlPasarMouse>> callesMap;

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(StartExperimentBatch);
        }

        // ── Punto de entrada ──────────────────────────────────────────────────
        [ContextMenu("▶ Start Experiment Batch")]
        public void StartExperimentBatch()
        {
            if (DigitalTwinManager.Instance == null)
            {
                Debug.LogError("[Automator] DigitalTwinManager no encontrado en la escena.");
                return;
            }

            allZones = FindObjectsByType<MostrarSoloAlPasarMouse>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (allZones == null || allZones.Length == 0)
            {
                Debug.LogError("[Automator] No se encontró ningún MostrarSoloAlPasarMouse en la escena.");
                return;
            }

            callesMap = GroupByParent(allZones);

            Debug.Log($"[Automator] {callesMap.Count} calle(s) | {allZones.Length} segmentos totales:");
            foreach (var kv in callesMap)
                Debug.Log($"  • {kv.Key.name}: {kv.Value.Count} segmento(s)");

            StartCoroutine(RunAllPermutations());
        }

        // ── Agrupación de segmentos por calle padre ───────────────────────────
        private Dictionary<Transform, List<MostrarSoloAlPasarMouse>> GroupByParent(
            MostrarSoloAlPasarMouse[] zones)
        {
            var map = new Dictionary<Transform, List<MostrarSoloAlPasarMouse>>();
            foreach (var zone in zones)
            {
                Transform parent = zone.transform.parent ?? zone.transform;
                if (!map.ContainsKey(parent))
                    map[parent] = new List<MostrarSoloAlPasarMouse>();
                map[parent].Add(zone);
            }
            // Ordenar segmentos por nombre dentro de cada calle
            foreach (var kv in map)
                kv.Value.Sort((a, b) => string.Compare(
                    a.gameObject.name, b.gameObject.name,
                    System.StringComparison.OrdinalIgnoreCase));
            return map;
        }

        // ── Corrutina principal ───────────────────────────────────────────────
        private IEnumerator RunAllPermutations()
        {
            Debug.Log("[Automator] ══════════════════════════════════════════");
            Debug.Log("[Automator]  Batch iniciado — 1 EPISODIO = 1 CALLE");
            Debug.Log("[Automator] ══════════════════════════════════════════");

            globalTableData.Clear();
            paperTableData.Clear();

            string dirPath = Path.Combine(Application.dataPath, "DigitalTwin_Logs");
            Directory.CreateDirectory(dirPath);
            progressCsvPath = Path.Combine(dirPath, "Progress_Results.csv");

            string globalHeader = BuildGlobalHeader();
            File.WriteAllText(progressCsvPath, globalHeader + System.Environment.NewLine);
            globalTableData.Add(globalHeader);
            
            // Header para la tabla resumida del paper (coincide exactamente con tu LaTeX)
            paperTableData.Add("Traffic,Altitude,Strategy,Coverage,Time (s),Energy,Revisit Ratio");

            // ── Fase 1: Todo excepto Micro (Estrategias 1=Baseline, 2=Hover, 4=Skip) ──
            int[] phase1Strategies = { 1, 2, 4 };
            for (int t = 1; t <= 3; t++)
            {
                for (int a = 0; a <= 2; a++)
                {
                    foreach (int s in phase1Strategies)
                    {
                        // Saltos de lo que ya completaste exitosamente en el CSV
                        if (t == 1 && a == 0)
                        {
                            Debug.Log($"[Automator] Saltando T:{t}/A:{a}/S:{s} (Low/Low 1,2,4 ya completados).");
                            continue;
                        }
                        if (t == 1 && a == 1 && (s == 1 || s == 2))
                        {
                            Debug.Log($"[Automator] Saltando T:{t}/A:{a}/S:{s} (Low/Medium 1,2 ya completados).");
                            continue;
                        }

                        SetDensity(t);
                        SetAltitude(a);
                        SetNavigation(s);
                        yield return new WaitForSeconds(0.5f);

                        string trafficStr = densityController.currentDensity.ToString();
                        string altStr     = heightController.currentLevel.ToString();
                        string navStr     = droneController.navigationMode.ToString();

                        Debug.Log($"[Automator] ▶▶▶ FASE 1 | {trafficStr} / {altStr} / {navStr}");

                        List<Transform> keys = new List<Transform>(callesMap.Keys);
                        Transform randomStreet = keys[UnityEngine.Random.Range(0, keys.Count)];
                        List<MostrarSoloAlPasarMouse> segments = callesMap[randomStreet];

                        StreetSummary res = new StreetSummary();
                        yield return StartCoroutine(RunStreetEpisode(
                            trafficStr, altStr, navStr,
                            randomStreet.name, segments, 
                            summary => res = summary));

                        var ic = System.Globalization.CultureInfo.InvariantCulture;
                        string paperRow = $"{trafficStr},{altStr},{navStr}," +
                                          $"{res.avgCoverage.ToString("F2", ic)}," +
                                          $"{res.totalTime.ToString("F2", ic)}," +
                                          $"{res.totalEnergy.ToString("F2", ic)}," +
                                          $"{res.avgRecovery.ToString("F2", ic)}";
                        paperTableData.Add(paperRow);
                    }
                }
            }

            // ── Fase 2: SOLO Micro (Estrategia 3) ──
            // Se correrá para absolutamente todas las combinaciones de Tráfico y Altura
            for (int t = 1; t <= 3; t++)
            {
                for (int a = 0; a <= 2; a++)
                {
                    int s = 3; // Micro

                    SetDensity(t);
                    SetAltitude(a);
                    SetNavigation(s);
                    yield return new WaitForSeconds(0.5f);

                    string trafficStr = densityController.currentDensity.ToString();
                    string altStr     = heightController.currentLevel.ToString();
                    string navStr     = droneController.navigationMode.ToString();

                    Debug.Log($"[Automator] ▶▶▶ FASE 2 (SOLO MICRO) | {trafficStr} / {altStr} / {navStr}");

                    List<Transform> keys = new List<Transform>(callesMap.Keys);
                    Transform randomStreet = keys[UnityEngine.Random.Range(0, keys.Count)];
                    List<MostrarSoloAlPasarMouse> segments = callesMap[randomStreet];

                    StreetSummary res = new StreetSummary();
                    yield return StartCoroutine(RunStreetEpisode(
                        trafficStr, altStr, navStr,
                        randomStreet.name, segments, 
                        summary => res = summary));

                    var ic = System.Globalization.CultureInfo.InvariantCulture;
                    string paperRow = $"{trafficStr},{altStr},{navStr}," +
                                      $"{res.avgCoverage.ToString("F2", ic)}," +
                                      $"{res.totalTime.ToString("F2", ic)}," +
                                      $"{res.totalEnergy.ToString("F2", ic)}," +
                                      $"{res.avgRecovery.ToString("F2", ic)}";
                    paperTableData.Add(paperRow);
                }
            }

            // ── CSVs finales consolidados ─────────────────────────────────────────
            string finalPath = Path.Combine(
                Application.dataPath, "DigitalTwin_Logs", "All_Streets_Results.csv");
            File.WriteAllLines(finalPath, globalTableData);

            string paperPath = Path.Combine(
                Application.dataPath, "DigitalTwin_Logs", "Paper_Table_Results.csv");
            File.WriteAllLines(paperPath, paperTableData);

            Debug.Log("[Automator] ══════════════════════════════════════════");
            Debug.Log("[Automator]  ✅ BATCH TERMINADO");
            Debug.Log($"[Automator]  📊 CSV DETALLE CALLES: {finalPath}");
            Debug.Log($"[Automator]  🏆 CSV PARA LATEX: {paperPath}");
            Debug.Log("[Automator] ══════════════════════════════════════════");
        }

        private struct StreetSummary
        {
            public float avgCoverage;
            public float avgRecovery;
            public float totalTime;
            public float totalEnergy;
        }

        // ── Episodio completo de una calle (todos sus segmentos) ──────────────
        private IEnumerator RunStreetEpisode(
            string trafficStr, string altStr, string navStr,
            string calleName, List<MostrarSoloAlPasarMouse> segments,
            System.Action<StreetSummary> onComplete)
        {
            Debug.Log($"[Automator] ═══ EPISODIO: {calleName} ({segments.Count} segmentos) ═══");

            var mi = DigitalTwinManager.Instance.movementInterface;

            // Limpiar contadores del episodio anterior
            if (mi != null) mi.ResetDetectedPotholes();

            // ─── INICIO DEL EPISODIO (una vez por calle) ─────────────────────
            DigitalTwinManager.Instance.suppressAutoEndEpisode = true;
            DigitalTwinManager.Instance.StartNewEpisode();
            // StartNewEpisode llama ResetDetectedPotholes internamente — OK

            var segResults = new List<MovementInterface.SegmentResult>();

            // ── Por cada segmento de la calle ─────────────────────────────────
            for (int si = 0; si < segments.Count; si++)
            {
                MostrarSoloAlPasarMouse seg = segments[si];
                string segName = seg.gameObject.name;

                float startEnergy = droneController.energyController != null
                    ? droneController.energyController.energia : 100f;

                // Notificar inicio de segmento para tracking granular
                if (mi != null) mi.StartSegment(segName, startEnergy);

                Debug.Log($"[Automator]   → Seg {si + 1}/{segments.Count}: {segName}");

                // Lanzar el dron
                // ACDC: DroneController lo activa al llegar al área y lo desactiva
                //       cuando terminan los waypoints. No se toca aquí.
                droneController.segmentDone  = false;
                droneController.apagado       = false;
                droneController.manualControl = false;
                droneController.SetSearchArea(seg.primeraPosicion, seg.posicionFinal);

                float segStart = Time.time;
                yield return new WaitForSeconds(1f);  // pequeña pausa de arranque

                // ── Esperar que el dron termine el segmento ───────────────────
                // segmentDone se activa cuando el dron llega a base tras el escaneo
                // (incluye la revisita completa en modo Skip)
                while (!droneController.segmentDone)
                {
                    if (Time.time - segStart >= maxTimeoutSeconds)
                    {
                        Debug.LogWarning($"[Automator] ⚠ Timeout segmento '{segName}'. Forzando cierre.");
                        droneController.segmentDone = true;
                        break;
                    }
                    yield return null;
                }

                // VUELO CONTINUO: No esperamos recarga aquí.
                // El dron pasa directamente a su siguiente tarea (según lo que le ordene este bucle).

                // ── Cerrar segmento → obtener métricas ────────────────────────
                float endEnergy = droneController.energyController != null
                    ? droneController.energyController.energia : 0f;

                MovementInterface.SegmentResult result = null;
                if (mi != null) result = mi.EndSegment(endEnergy);

                if (result != null)
                {
                    segResults.Add(result);
                    Debug.Log($"[Automator]   ✓ {segName}: " +
                              $"Coverage={result.Coverage:F1}% " +
                              $"({result.detectedByModel}/{result.detectedByRaycast}) | " +
                              $"Recovery={result.RecoveryRatio:F1}% | " +
                              $"Obstáculos={result.hadObstacles}");
                }

                // No reseteamos a base aquí. Se reseteará al terminar la calle completa.
                yield return new WaitForSeconds(0.3f);
            }

            // ─── FIN DE LOS SEGMENTOS: RETORNAR A BASE Y RECARGAR ────────────
            Debug.Log($"[Automator] ⚡ Calle '{calleName}' escaneada. Retornando a base para recargar al 100%...");
            
            droneController.ReturnToBase();
            
            float rechargeFinalStart = Time.time;
            while (true)
            {
                if (droneController.energyController != null &&
                    droneController.energyController.recargaCompleta)
                {
                    droneController.energyController.recargaCompleta = false;
                    break;
                }
                if (Time.time - rechargeFinalStart >= maxTimeoutSeconds)
                {
                    Debug.LogWarning($"[Automator] ⚠ Timeout recarga final de episodio. Continuando.");
                    break;
                }
                yield return null;
            }

            // ─── FIN DEL EPISODIO ─────────────
            DigitalTwinManager.Instance.suppressAutoEndEpisode = false;
            DigitalTwinManager.Instance.EndEpisode();  // ← guarda Episode_N_*.csv

            // ── Guardar métricas de la calle (1 fila por episodio) ───────────────────
            string row = BuildStreetRow(trafficStr, altStr, navStr, calleName, segResults, out StreetSummary summary);
            globalTableData.Add(row);
            AppendProgressCsv(row);

            Debug.Log($"[Automator] ✅ Episodio '{calleName}' completado." +
                      $" Segs={segResults.Count} | " +
                      $"Avg Coverage={(segResults.Count > 0 ? segResults[0].Coverage : 0):F1}%");

            if (onComplete != null)
                onComplete(summary);
        }

        // ── CSV: Encabezado global (Simplificado a 1 fila por episodio) ────────
        private string BuildGlobalHeader()
        {
            var cols = new List<string> 
            { 
                "Traffic", "Altitude", "Strategy", "Street",
                "Coverage(%)", "Recovery(%)",
                "Segs_with_obstacles", "Total_segs",
                "Total_Time(s)", "Total_Energy(%)"
            };
            return string.Join(",", cols);
        }

        // ── CSV: Fila por calle (1 fila por episodio) ─────────────────────────
        private string BuildStreetRow(
            string traffic, string alt, string nav, string street,
            List<MovementInterface.SegmentResult> segs, out StreetSummary summary)
        {
            var ic   = System.Globalization.CultureInfo.InvariantCulture;
            var cols = new List<string> { traffic, alt, nav, street };

            float totalCov = 0f, totalRec = 0f, totalTime = 0f, totalEnergy = 0f;
            int segsWithObst = 0;

            foreach (var seg in segs)
            {
                totalCov    += seg.Coverage;
                totalRec    += seg.RecoveryRatio;
                totalTime   += seg.timeTaken;
                totalEnergy += seg.energyConsumed;
                if (seg.hadObstacles) segsWithObst++;
            }

            int n = segs.Count;
            float avgCov = n > 0 ? totalCov / n : 0f;
            float avgRec = n > 0 ? totalRec / n : 0f;

            // Cobertura y recuperación promediada de todo el episodio
            cols.Add(avgCov.ToString("F2", ic));
            cols.Add(avgRec.ToString("F2", ic));
            
            // Total de obstáculos detectados en los segmentos y metadata
            cols.Add(segsWithObst.ToString());
            cols.Add(n.ToString());
            cols.Add(totalTime.ToString("F2",   ic));
            cols.Add(totalEnergy.ToString("F2", ic));

            summary = new StreetSummary
            {
                avgCoverage = avgCov,
                avgRecovery = avgRec,
                totalTime   = totalTime,
                totalEnergy = totalEnergy
            };

            return string.Join(",", cols);
        }



        private void AppendProgressCsv(string row)
        {
            try { File.AppendAllText(progressCsvPath, row + System.Environment.NewLine); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[Automator] Error CSV progreso: {ex.Message}"); }
        }

        // ── Reset entre segmentos ─────────────────────────────────────────────
        private void ResetDrone()
        {
            if (droneController == null) return;
            droneController.manualControl      = true;
            droneController.apagado            = false;
            droneController.transform.position = droneController.repostajePosition;
            droneController.energyController?.IniciarRecarga();
        }

        // ── Helpers de ciclar modos ───────────────────────────────────────────
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
