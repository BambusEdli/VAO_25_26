using System.IO;
using UnityEngine;

/// <summary>
/// Steuert den Ablauf eines einzelnen Trials über die Tasten 1-2-3:
/// 1: Ausgangsblickrichtung (Baseline) loggen
/// 2: Stimulus starten (Audio + Stimulusphase)
/// 3: Antwort einloggen und Fehler berechnen
/// Zusätzlich: Kopfbewegungs-Constraint und Logging in eine CSV-Datei.
/// </summary>
public class TrialStateController : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Head-Transform (Kamera/Tracker)")]
    public Transform head;

    [Tooltip("Controller für Quelle, Trial-Design und Fehlerberechnung")]
    public ExperimentController experiment;

    [Header("Stimulus-Einstellungen (Simulation)")]
    [Tooltip("Dauer des Stimulus in Sekunden (für ersten Prototyp)")]
    public float simulatedStimulusDuration = 2.0f;

    [Header("Head-Movement-Constraint")]
    [Tooltip("Maximale erlaubte Abweichung vom Baseline-Heading in Grad während Baseline + Stimulus")]
    public float maxAllowedHeadDeviation = 25f;

    [Header("Logging")]
    [Tooltip("ID der Versuchsperson (für CSV-Log, z.B. S01, VP03)")]
    public string subjectId = "S01";

    [Tooltip("Name der Versuchsperson (optional, kann auch leer bleiben)")]
    public string subjectName = "";

    [Tooltip("Wenn true: Logs nach Assets/ExperimentLogs, sonst in persistentDataPath/ExperimentLogs.")]
    public bool logUnderAssetsFolder = false;

    private enum TrialState
    {
        Idle,
        BaselineLogged,
        StimulusPlaying,
        WaitingForResponse
    }

    private TrialState state = TrialState.Idle;

    private Quaternion baselineRotation;
    private float stimulusOffsetTime;   // Zeitpunkt, wenn Stimulus fertig ist

    // Tracking der Kopfbewegung pro Trial
    private float maxHeadDeviationThisTrial = 0f;
    private bool headDeviationExceededThisTrial = false;

    // Logging
    private string logFilePath;

    private void Start()
    {
        InitLogFile();
    }

    private void Update()
    {
        // Wenn Experiment schon beendet ist, Eingaben ignorieren
        if (experiment != null && experiment.IsExperimentFinished && state == TrialState.Idle)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) ||
                Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) ||
                Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                Debug.Log("TrialStateController: Experiment ist abgeschlossen. Keine weiteren Trials.");
            }

            return;
        }

        // Taste 1: Baseline loggen
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            OnKey1();
        }

        // Taste 2: Stimulus starten
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            OnKey2();
        }

        // Taste 3: Antwort loggen
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            OnKey3();
        }

        // Stimulus-Status überwachen (Wechsel StimulusPlaying -> WaitingForResponse)
        UpdateStimulusState();

        // Kopfbewegung während Baseline/Stimulus tracken
        TrackHeadMovement();
    }

    #region Logging-Initialisierung

    private void InitLogFile()
    {
        string baseDir = logUnderAssetsFolder
            ? Application.dataPath
            : Application.persistentDataPath;

        string logDir = Path.Combine(baseDir, "ExperimentLogs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        string safeSubjectId = string.IsNullOrWhiteSpace(subjectId) ? "unknown" : subjectId.Trim();
        string fileName = $"localization_{safeSubjectId}.csv";

        logFilePath = Path.Combine(logDir, fileName);

        if (!File.Exists(logFilePath))
        {
            string header =
                "timestamp;subjectId;subjectName;designTrialIndex;representation;signalType;quadrant;targetAzDeg;targetElDeg;errorDeg;responseTimeSec;maxHeadDeviationDeg;headDeviationExceeded";
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
        float errorAngle,
        float responseTimeSec,
        float maxHeadDeviationDeg,
        bool headDeviationExceeded)
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        int exceededFlag = headDeviationExceeded ? 1 : 0;

        string sanitizedName = subjectName?.Replace(";", ",") ?? "";
        int quadrantHuman = quadrantIndex + 1; // 1..4

        string line = string.Format(
            "{0};{1};{2};{3};{4};{5};{6};{7:F1};{8:F1};{9:F3};{10:F3};{11:F1};{12}",
            timestamp,
            subjectId,
            sanitizedName,
            designTrialIndex,
            representation,
            signalType,
            quadrantHuman,
            targetAzDeg,
            targetElDeg,
            errorAngle,
            responseTimeSec,
            maxHeadDeviationDeg,
            exceededFlag
        );

        File.AppendAllText(logFilePath, line + "\n");
    }

    #endregion

    #region State-Callbacks für 1-2-3

    private void OnKey1()
    {
        if (state != TrialState.Idle)
        {
            Debug.LogWarning("Key 1 gedrückt, aber TrialState ist nicht Idle.");
            return;
        }

        if (head == null)
        {
            Debug.LogError("TrialStateController: head ist nicht gesetzt.");
            return;
        }

        baselineRotation = head.rotation;
        maxHeadDeviationThisTrial = 0f;
        headDeviationExceededThisTrial = false;

        state = TrialState.BaselineLogged;
        Debug.Log("Baseline logged: " + baselineRotation.eulerAngles);
    }

    private void OnKey2()
    {
        if (state != TrialState.BaselineLogged)
        {
            Debug.LogWarning("Key 2 nur nach Key 1 (BaselineLogged) erlaubt.");
            return;
        }

        if (experiment == null)
        {
            Debug.LogError("TrialStateController: experiment-Referenz fehlt.");
            return;
        }

        float stimulusDuration = simulatedStimulusDuration;

        // Hier jetzt wirklich den Stimulus starten:
        //  - Routing in Reaper nach aktuellem Trial
        //  - JumpToStart + Play + nach Dauer Stop
        //  - optional zusätzlich Unity-Audio (in StartStimulusForCurrentTrial)
        experiment.StartStimulusForCurrentTrial(stimulusDuration);

        // Lokalen Timer für die Antwortphase setzen
        stimulusOffsetTime = Time.time + stimulusDuration;
        state = TrialState.StimulusPlaying;

        Debug.Log("Stimulus gestartet, Dauer: " + stimulusDuration + " s");
    }


    private void OnKey3()
    {
        if (state != TrialState.WaitingForResponse)
        {
            Debug.LogWarning("Key 3 gedrückt, aber TrialState ist nicht WaitingForResponse.");
            return;
        }

        if (head == null || experiment == null)
        {
            Debug.LogError("TrialStateController: head oder experiment fehlt.");
            return;
        }

        float responseTime = Time.time - stimulusOffsetTime;

        Vector3 headDir = head.forward;
        Vector3 sourceDir = experiment.GetSourceDirection();

        float errorAngle = Vector3.Angle(headDir, sourceDir);

        Debug.Log($"Antwort geloggt. ResponseTime = {responseTime:F3} s, Error = {errorAngle:F1}°");
        Debug.Log($"headDir = {headDir}, sourceDir = {sourceDir}");

        // Trial-Metadaten aus ExperimentController holen
        int designTrialIndex = experiment.GetCurrentTrialNumber();
        var representation = experiment.GetCurrentRepresentation();
        var signalType = experiment.GetCurrentSignalType();
        int quadrantIndex = experiment.GetCurrentQuadrantIndex();
        float targetAz = experiment.GetCurrentTargetAzimuth();
        float targetEl = experiment.GetCurrentTargetElevation();

        Debug.Log(
            $"Trial-Metadaten: Trial #{designTrialIndex}, Rep={representation}, Signal={signalType}, " +
            $"Quadrant={quadrantIndex + 1}, TargetAz={targetAz:F1}°, TargetEl={targetEl:F1}°");

        // In CSV-Datei schreiben
        AppendTrialLog(
            designTrialIndex,
            representation,
            signalType,
            quadrantIndex,
            targetAz,
            targetEl,
            errorAngle,
            responseTime,
            maxHeadDeviationThisTrial,
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
        if (state == TrialState.StimulusPlaying)
        {
            if (Time.time >= stimulusOffsetTime)
            {
                state = TrialState.WaitingForResponse;

                if (headDeviationExceededThisTrial)
                {
                    Debug.Log(
                        $"Achtung: Kopfbewegung hat die erlaubte Abweichung von {maxAllowedHeadDeviation:F1}° " +
                        $"überschritten. Maximale Abweichung: {maxHeadDeviationThisTrial:F1}°"
                    );
                }
                else
                {
                    Debug.Log(
                        $"Kopfbewegung innerhalb des Limits. " +
                        $"Maximale Abweichung in diesem Trial: {maxHeadDeviationThisTrial:F1}°"
                    );
                }

                Debug.Log("Stimulus fertig. Warte auf Antwort (Key 3)...");
            }
        }
    }

    /// <summary>
    /// Trackt die Kopfbewegung relativ zur Baseline während BaselineLogged + StimulusPlaying.
    /// </summary>
    private void TrackHeadMovement()
    {
        if (head == null)
            return;

        if (state != TrialState.BaselineLogged && state != TrialState.StimulusPlaying)
            return;

        float deviation = Quaternion.Angle(baselineRotation, head.rotation);

        if (deviation > maxHeadDeviationThisTrial)
        {
            maxHeadDeviationThisTrial = deviation;
        }

        if (deviation > maxAllowedHeadDeviation)
        {
            headDeviationExceededThisTrial = true;
        }
    }

    #endregion
}
