using System.IO;
using UnityEngine;

/// <summary>
/// Bedienung:
/// - SPACE (1. Druck): Baseline loggen + Stimulus starten (Schritt 1 & 2 zusammen)
/// - SPACE (nach Stimulus-Ende): Antwort loggen (Schritt 3)
/// 
/// Zusätzlich:
/// - Kopfbewegungs-Tracking während der Hörphase
/// - CSV-Export
/// - Fehlerausgabe: Gesamtwinkel + Azimut-/Elevationsfehler
/// </summary>
public class TrialStateController : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Head-Transform (Tracker-/Rig-Transform). Wird für Still-Sitzen (Quaternion.Angle) verwendet.")]
    public Transform head;

    [Tooltip("Optional: Transform, dessen forward als Blickrichtung verwendet wird (z.B. die Kamera). Wenn leer, wird head.forward verwendet.")]
    public Transform gazeTransform;

    [Tooltip("Controller für Quelle, Trial-Design und Fehlerberechnung")]
    public ExperimentController experiment;

    [Header("Stimulus")]
    [Tooltip("Dauer des Stimulus in Sekunden (für Timing/Antwortzeit)")]
    public float simulatedStimulusDuration = 2.0f;

    [Header("Head-Movement-Constraint")]
    [Tooltip("Maximale erlaubte Abweichung vom Baseline-Heading in Grad während der Hörphase")]
    public float maxAllowedHeadDeviation = 25f;

    [Header("Logging")]
    [Tooltip("ID der Versuchsperson (für CSV-Log, z.B. S01, VP03)")]
    public string subjectId = "S01";

    [Tooltip("Wenn true: Logs nach Assets/ExperimentLogs, sonst in persistentDataPath/ExperimentLogs.")]
    public bool logUnderAssetsFolder = false;

    private enum TrialState
    {
        Idle,
        StimulusPlaying,
        WaitingForResponse
    }

    private TrialState state = TrialState.Idle;

    private Quaternion baselineRotation;
    private float stimulusOffsetTime; // Zeitpunkt, wenn Stimulus fertig ist
    private bool stimulusFinishedLogged = false;

    // Baseline Az/El (für Yaw/Pitch-Abweichungen)
    private float baselineAzDeg = 0f;
    private float baselineElDeg = 0f;

    // Movement Tracking pro Trial
    private float maxHeadDeviationThisTrial = 0f;    // Gesamtwinkel
    private float maxYawDeviationThisTrial = 0f;     // |ΔAz| zur Baseline
    private float maxPitchDeviationThisTrial = 0f;   // |ΔEl| zur Baseline
    private bool headDeviationExceededThisTrial = false;

    // Logging
    private string logFilePath;

    private void Start()
    {
        InitLogFile();
    }

    private void Update()
    {
        // Experiment beendet?
        if (experiment != null && experiment.IsExperimentFinished && state == TrialState.Idle)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("TrialStateController: Experiment ist abgeschlossen. Keine weiteren Trials.");
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleSpace();
        }

        UpdateStimulusState();
        TrackHeadMovement();
    }

    #region Helpers: Gaze + Az/El

    private Vector3 GetCurrentGazeDirection()
    {
        Transform t = gazeTransform != null ? gazeTransform : head;
        if (t == null) return Vector3.forward;
        return t.forward;
    }

    // Konvention passend zu SphericalCoords:
    // Azimut: 0° = +Z (vorne), +90° = +X (rechts), Bereich [-180, 180]
    // Elevation: 0° = Horizont, +90° = oben
    private static void DirectionToAzEl(Vector3 dir, out float azDeg, out float elDeg)
    {
        if (dir.sqrMagnitude < 1e-8f)
        {
            azDeg = 0f;
            elDeg = 0f;
            return;
        }

        dir.Normalize();
        azDeg = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        float horiz = Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z);
        elDeg = Mathf.Atan2(dir.y, horiz) * Mathf.Rad2Deg;
    }

    #endregion

    #region Input Handling

    private void HandleSpace()
    {
        switch (state)
        {
            case TrialState.Idle:
                StartBaselineAndStimulus();
                break;

            case TrialState.StimulusPlaying:
                // Falls der Stimulus bereits vorbei ist, aber UpdateStimulusState noch nicht umgeschaltet hat:
                if (Time.time >= stimulusOffsetTime)
                {
                    FinishStimulusPhaseIfNeeded();
                    LogResponseAndAdvance();
                }
                else
                {
                    Debug.Log("Noch nicht: Stimulus läuft noch. Warte bis er fertig ist, dann SPACE für Antwort.");
                }
                break;

            case TrialState.WaitingForResponse:
                LogResponseAndAdvance();
                break;
        }
    }

    #endregion

    #region Trial Flow

    private void StartBaselineAndStimulus()
    {
        if (head == null)
        {
            Debug.LogError("TrialStateController: head ist nicht gesetzt.");
            return;
        }

        if (experiment == null)
        {
            Debug.LogError("TrialStateController: experiment-Referenz fehlt.");
            return;
        }

        // Baseline setzen
        baselineRotation = head.rotation;

        Vector3 gazeDir = GetCurrentGazeDirection();
        DirectionToAzEl(gazeDir, out baselineAzDeg, out baselineElDeg);

        // Trial-Tracking resetten
        maxHeadDeviationThisTrial = 0f;
        maxYawDeviationThisTrial = 0f;
        maxPitchDeviationThisTrial = 0f;
        headDeviationExceededThisTrial = false;
        stimulusFinishedLogged = false;

        // Stimulus starten (inkl. Reaper/OSC-Routing – bleibt in ExperimentController)
        float dur = simulatedStimulusDuration;

        experiment.StartStimulusForCurrentTrial(dur);

        stimulusOffsetTime = Time.time + dur;
        state = TrialState.StimulusPlaying;

        Debug.Log($"Baseline+Stimulus gestartet (SPACE). BaselineAz={baselineAzDeg:F1}°, BaselineEl={baselineElDeg:F1}°, Dauer={dur:F2}s");
    }

    private void FinishStimulusPhaseIfNeeded()
    {
        if (state != TrialState.StimulusPlaying)
            return;

        state = TrialState.WaitingForResponse;

        if (!stimulusFinishedLogged)
        {
            stimulusFinishedLogged = true;

            // Kleiner Log direkt nach Stimulus-Ende (hilft beim Debugging/Timing)
            if (experiment != null)
            {
                int tn = experiment.GetCurrentTrialNumber();
                int total = experiment.GetTotalTrialCount();
                Debug.Log($"Stimulus-Ende (Trial {tn}/{total}). Wechsel in Antwortphase.");
            }
            else
            {
                Debug.Log("Stimulus-Ende. Wechsel in Antwortphase.");
            }

            // Ausgabe zur maximalen Kopfbewegung (inkl. möglicher Überschreitung) genau an diesem Punkt
            if (headDeviationExceededThisTrial)
            {
                Debug.Log(
                    $"Achtung: Kopfbewegung > {maxAllowedHeadDeviation:F1}°. " +
                    $"MaxGesamt={maxHeadDeviationThisTrial:F1}°, MaxYaw={maxYawDeviationThisTrial:F1}°, MaxPitch={maxPitchDeviationThisTrial:F1}°"
                );
            }
            else
            {
                Debug.Log(
                    $"Kopfbewegung ok. " +
                    $"MaxGesamt={maxHeadDeviationThisTrial:F1}°, MaxYaw={maxYawDeviationThisTrial:F1}°, MaxPitch={maxPitchDeviationThisTrial:F1}°"
                );
            }

            Debug.Log("Stimulus fertig. SPACE zum Einloggen der Antwort.");
        }
    }


    private void LogResponseAndAdvance()
    {
        if (experiment == null)
        {
            Debug.LogError("TrialStateController: experiment fehlt.");
            return;
        }

        // Antwortzeit (Ende Stimulus -> Tastendruck)
        float responseTime = Time.time - stimulusOffsetTime;

        // Blickrichtung & Zielrichtung
        Vector3 headDir = GetCurrentGazeDirection();
        Vector3 sourceDir = experiment.GetSourceDirection();

        // Gesamtfehler
        float errorAngle = Vector3.Angle(headDir, sourceDir);

        // Fehler in Az/El
        DirectionToAzEl(headDir, out float headAz, out float headEl);
        DirectionToAzEl(sourceDir, out float tgtAz, out float tgtEl);

        float errorAz = Mathf.DeltaAngle(tgtAz, headAz);  // [-180..180]
        float errorEl = headEl - tgtEl;

        Debug.Log(
            $"Antwort geloggt (SPACE). RT={responseTime:F3}s, Error={errorAngle:F1}° (AzErr={errorAz:F1}°, ElErr={errorEl:F1}°)"
        );

        // Trial-Metadaten aus ExperimentController
        int designTrialIndex = experiment.GetCurrentTrialNumber();
        var representation = experiment.GetCurrentRepresentation();
        var signalType = experiment.GetCurrentSignalType();
        int quadrantIndex = experiment.GetCurrentQuadrantIndex();
        float targetAz = experiment.GetCurrentTargetAzimuth();
        float targetEl = experiment.GetCurrentTargetElevation();

        AppendTrialLog(
            designTrialIndex,
            representation,
            signalType,
            quadrantIndex,
            targetAz,
            targetEl,
            errorAngle,
            errorAz,
            errorEl,
            responseTime,
            maxHeadDeviationThisTrial,
            maxYawDeviationThisTrial,
            maxPitchDeviationThisTrial,
            headDeviationExceededThisTrial
        );

        // Nächstes Trial vorbereiten
        bool hasNext = experiment.AdvanceToNextTrial();
        if (!hasNext)
        {
            Debug.Log("TrialStateController: Alle Trials abgeschlossen. Experiment beendet.");
        }

        state = TrialState.Idle;
    }

    #endregion

    #region Stimulus-Update & Head-Tracking

    private void UpdateStimulusState()
    {
        if (state == TrialState.StimulusPlaying && Time.time >= stimulusOffsetTime)
        {
            FinishStimulusPhaseIfNeeded();
        }
    }

    /// <summary>
    /// Trackt Kopfbewegung während der Hörphase (StimulusPlaying).
    /// - Gesamtwinkel weiterhin über Quaternion.Angle
    /// - Zusätzlich Yaw/Pitch-Abweichung über Az/El der Blickrichtung
    /// </summary>
    private void TrackHeadMovement()
    {
        if (head == null)
            return;

        if (state != TrialState.StimulusPlaying)
            return;

        // 1) Gesamtwinkel-Abweichung (wie vorher)
        float deviation = Quaternion.Angle(baselineRotation, head.rotation);
        if (deviation > maxHeadDeviationThisTrial)
            maxHeadDeviationThisTrial = deviation;

        if (deviation > maxAllowedHeadDeviation)
            headDeviationExceededThisTrial = true;

        // 2) Yaw/Pitch-Abweichung über Blickrichtung
        Vector3 gazeDir = GetCurrentGazeDirection();
        DirectionToAzEl(gazeDir, out float az, out float el);

        float yawDev = Mathf.Abs(Mathf.DeltaAngle(baselineAzDeg, az));
        float pitchDev = Mathf.Abs(el - baselineElDeg);

        if (yawDev > maxYawDeviationThisTrial) maxYawDeviationThisTrial = yawDev;
        if (pitchDev > maxPitchDeviationThisTrial) maxPitchDeviationThisTrial = pitchDev;
    }

    #endregion

    #region CSV Logging

    private void InitLogFile()
    {
        string baseDir = logUnderAssetsFolder
            ? Application.dataPath
            : Application.persistentDataPath;

        string logDir = Path.Combine(baseDir, "ExperimentLogs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        string safeSubjectId = string.IsNullOrWhiteSpace(subjectId) ? "unknown" : subjectId.Trim();
        string fileName = $"localization_{safeSubjectId}.csv";
        logFilePath = Path.Combine(logDir, fileName);

        if (!File.Exists(logFilePath))
        {
            string header =
                "timestamp;subjectId;designTrialIndex;representation;signalType;quadrant;targetAzDeg;targetElDeg;" +
                "errorDeg;errorAzDeg;errorElDeg;responseTimeSec;" +
                "maxHeadDeviationDeg;maxYawDeviationDeg;maxPitchDeviationDeg;headDeviationExceeded";
            File.WriteAllText(logFilePath, header + "\n");
        }

        Debug.Log($"Logging initialisiert. Log-Datei: {logFilePath}");
    }

    private void AppendTrialLog(
        int designTrialIndex,
        ExperimentController.RepresentationType representation,
        ExperimentController.SignalType signalType,
        int quadrantIndex,
        float targetAzDeg,
        float targetElDeg,
        float errorAngleDeg,
        float errorAzDeg,
        float errorElDeg,
        float responseTimeSec,
        float maxHeadDeviationDeg,
        float maxYawDeviationDeg,
        float maxPitchDeviationDeg,
        bool headDeviationExceeded)
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        int exceededFlag = headDeviationExceeded ? 1 : 0;
        int quadrantHuman = quadrantIndex + 1;

        string line = string.Format(
            "{0};{1};{2};{3};{4};{5};{6:F1};{7:F1};{8:F3};{9:F3};{10:F3};{11:F3};{12:F1};{13:F1};{14:F1};{15}",
            timestamp,
            subjectId,
            designTrialIndex,
            representation,
            signalType,
            quadrantHuman,
            targetAzDeg,
            targetElDeg,
            errorAngleDeg,
            errorAzDeg,
            errorElDeg,
            responseTimeSec,
            maxHeadDeviationDeg,
            maxYawDeviationDeg,
            maxPitchDeviationDeg,
            exceededFlag
        );

        File.AppendAllText(logFilePath, line + "\n");
    }

    #endregion
}
