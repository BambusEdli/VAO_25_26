using System.IO;
using UnityEngine;

/// <summary>
/// Instructions:
/// - SPACE (1. press): Baseline logged + started stimulus
/// - SPACE (2. press, after end of stimulus): log answer + advance to next trial
/// 
/// Additional:
/// - Tracks head movement during stimulus phase
/// - CSV-export
/// - Error output: absolute angle + azimuth/elevation components
/// </summary>
public class TrialStateController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Head-Transform (Tracker-/Rig-Transform). Used for still sitting (Quaternion.Angle).")]
    public Transform head;

    [Tooltip("Optional: Forward of Transform used as gaze direction (camera). If null head.forward is used.")]
    public Transform gazeTransform;

    [Tooltip("Controller for source, trial design and error calculation")]
    public ExperimentController experiment;

    [Header("Stimulus")]
    [Tooltip("Duration of stimulus in seconds (for timing/response time).")]
    public float simulatedStimulusDuration = 2.0f;

    [Header("Head-Movement-Constraint")]
    [Tooltip("Maximum allowed deviation from baseline heading in degrees during stimulus phase.")]
    public float maxAllowedHeadDeviation = 25f;

    [Header("Logging")]
    [Tooltip("ID of the subject (for CSV log, e.g., 'S01', 'S02').")]
    public string subjectId = "S01";

    [Tooltip("If true: Logs to Assets/ExperimentLogs, otherwise to persistentDataPath/ExperimentLogs.")]
    public bool logUnderAssetsFolder = false;

    private enum TrialState
    {
        Idle,
        StimulusPlaying,
        WaitingForResponse
    }

    private TrialState state = TrialState.Idle;

    private Quaternion baselineRotation;
    private float stimulusOffsetTime;
    private bool stimulusFinishedLogged = false;

    // Baseline az/el (for yaw/pitch deviations)
    private float baselineAzDeg = 0f;
    private float baselineElDeg = 0f;

    // Movement tracking per Trial
    private float maxHeadDeviationThisTrial = 0f;
    private float maxYawDeviationThisTrial = 0f;
    private float maxPitchDeviationThisTrial = 0f;
    private bool headDeviationExceededThisTrial = false;

    // Logging
    private string logFilePath;

    private void Start()
    {
        InitLogFile();
    }

    private void Update()
    {
        // Experiment finished?
        if (experiment != null && experiment.IsExperimentFinished && state == TrialState.Idle)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("TrialStateController: Experiment is finished. No further trials.");
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

    // Convention matching SphericalCoords:
    // Azimuth: 0° = +Z (front), +90° = +X (right), range [-180, 180]
    // Elevation: 0° = horizon, +90° = up
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
                // // If the stimulus is already over but UpdateStimulusState hasnt switched yet
                if (Time.time >= stimulusOffsetTime)
                {
                    FinishStimulusPhaseIfNeeded();
                    LogResponseAndAdvance();
                }
                else
                {
                    Debug.Log("Not yet: the stimulus is still playing. Wait until it finishes, then press SPACE to log the response.");
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
            Debug.LogError("TrialStateController: head is not assigned.");
            return;
        }

        if (experiment == null)
        {
            Debug.LogError("TrialStateController: experiment reference is missing.");
            return;
        }

        // Set baseline
        baselineRotation = head.rotation;

        Vector3 gazeDir = GetCurrentGazeDirection();
        DirectionToAzEl(gazeDir, out baselineAzDeg, out baselineElDeg);

        // Reset trial tracking
        maxHeadDeviationThisTrial = 0f;
        maxYawDeviationThisTrial = 0f;
        maxPitchDeviationThisTrial = 0f;
        headDeviationExceededThisTrial = false;
        stimulusFinishedLogged = false;

        // Start stimulus (incl. Reaper/OSC routing, remains in ExperimentController)
        float dur = simulatedStimulusDuration;

        experiment.StartStimulusForCurrentTrial(dur);

        stimulusOffsetTime = Time.time + dur;
        state = TrialState.StimulusPlaying;

        Debug.Log($"Baseline+Stimulus started (SPACE). BaselineAz={baselineAzDeg:F1}°, BaselineEl={baselineElDeg:F1}°, Duration={dur:F2}s");
    }

    private void FinishStimulusPhaseIfNeeded()
    {
        if (state != TrialState.StimulusPlaying)
            return;

        state = TrialState.WaitingForResponse;

        if (!stimulusFinishedLogged)
        {
            stimulusFinishedLogged = true;

            if (experiment != null)
            {
                int tn = experiment.GetCurrentTrialNumber();
                int total = experiment.GetTotalTrialCount();
                Debug.Log($"Stimulus ended (Trial {tn}/{total}). Switching to response phase.");
            }
            else
            {
                Debug.Log("Stimulus ended. Switching to response phase.");
            }

            // Output of the maximum head movement (including possible exceedance) at this exact point
            if (headDeviationExceededThisTrial)
            {
                Debug.Log(
                    $"Warning: head movement > {maxAllowedHeadDeviation:F1}°. " +
                    $"MaxTotal={maxHeadDeviationThisTrial:F1}°, MaxYaw={maxYawDeviationThisTrial:F1}°, MaxPitch={maxPitchDeviationThisTrial:F1}°"
                );
            }
            else
            {
                Debug.Log(
                    $"Head movement OK. " +
                    $"MaxTotal={maxHeadDeviationThisTrial:F1}°, MaxYaw={maxYawDeviationThisTrial:F1}°, MaxPitch={maxPitchDeviationThisTrial:F1}°"
                );
            }

            Debug.Log("Stimulus finished. Press SPACE to log the response.");
        }
    }


    private void LogResponseAndAdvance()
    {
        if (experiment == null)
        {
            Debug.LogError("TrialStateController: experiment is missing.");
            return;
        }

        // Response time (stimulus end -> key press)
        float responseTime = Time.time - stimulusOffsetTime;

        // Gaze direction & target direction
        Vector3 headDir = GetCurrentGazeDirection();
        Vector3 sourceDir = experiment.GetSourceDirection();

        // Overall error
        float errorAngle = Vector3.Angle(headDir, sourceDir);

        // Error in az/el
        DirectionToAzEl(headDir, out float headAz, out float headEl);
        DirectionToAzEl(sourceDir, out float tgtAz, out float tgtEl);

        float errorAz = Mathf.DeltaAngle(tgtAz, headAz);
        float errorEl = headEl - tgtEl;

        Debug.Log(
            $"Response logged (SPACE). RT={responseTime:F3}s, Error={errorAngle:F1}° (AzErr={errorAz:F1}°, ElErr={errorEl:F1}°)"
        );

        // Trial metadata from ExperimentController
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

        // Prepare next trial
        bool hasNext = experiment.AdvanceToNextTrial();
        if (!hasNext)
        {
            Debug.Log("TrialStateController: All trials completed. Experiment finished.");
        }

        state = TrialState.Idle;
    }

    #endregion

    #region Stimulus update & Head tracking

    private void UpdateStimulusState()
    {
        if (state == TrialState.StimulusPlaying && Time.time >= stimulusOffsetTime)
        {
            FinishStimulusPhaseIfNeeded();
        }
    }

    /// <summary>
    /// Tracks head movement during the listening phase (StimulusPlaying).
    /// Overall angle still via Quaternion.Angle.
    /// Additionally yaw/pitch deviation via az/el of the gaze direction.
    /// </summary>
    private void TrackHeadMovement()
    {
        if (head == null)
            return;

        if (state != TrialState.StimulusPlaying)
            return;

        // Overall angle deviation
        float deviation = Quaternion.Angle(baselineRotation, head.rotation);
        if (deviation > maxHeadDeviationThisTrial)
            maxHeadDeviationThisTrial = deviation;

        if (deviation > maxAllowedHeadDeviation)
            headDeviationExceededThisTrial = true;

        // Yaw/pitch deviation via gaze direction
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

        Debug.Log($"Logging initialized. Log file: {logFilePath}");
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
        int quadrantHuman = quadrantIndex < 0 ? -1 : (((quadrantIndex + 2) % 4) + 1);  // +180° for logging

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
