using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ExperimentController : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Head-Objekt mit Kamera / Tracker-Rotation")]
    public Transform head;

    [Tooltip("Optional: Transform, dessen Vorwärtsrichtung als Blickrichtung verwendet wird (z.B. Kamera). Wenn null, wird 'head' verwendet.")]
    public Transform gazeTransform;

    [Tooltip("Prefab für den visuellen Marker der Schallquelle")]
    public GameObject sourceMarkerPrefab;

    [Header("Sphärenparameter")]
    [Tooltip("Radius der virtuellen Sphäre um den Kopf")]
    public float sphereRadius = 10f;

    private GameObject currentSourceMarker;

    [Header("Audio / ASIO")]
    [Tooltip("Persistente Asio-Quelle in der Szene, wird an die Target-Position verschoben.")]
    public Transform asioSourceTransform;

    [Header("Elevation-Bereich")]
    [Tooltip("Minimale Elevation in Grad (z. B. -40)")]
    public float minElevationDeg = -40f;

    [Tooltip("Maximale Elevation in Grad (z. B. +40)")]
    public float maxElevationDeg = 40f;

    [Header("Reaper / OSC")]
    [Tooltip("Optional: OSC-Sender nach Reaper für das Abspielen der Stimuli")]
    public ReaperOscSender reaperOscSender;


    // Configures the routing in REAPER for the current trial:
    // one signal track active (1=Voice, 2=Tone/Noise, 3=Music)
    // one representation track active (4=HOA3, 5=HOA4, 6=Binaural)

    private void ConfigureReaperRoutingForCurrentTrial()
    {
        if (reaperOscSender == null || currentTrial == null)
        {
            return;
        }

        // 1: Experiment signal type -> Reaper track
        ReaperOscSender.SignalType reaperSignalType;
        switch (currentTrial.signalType)
        {
            case SignalType.Voice:
                reaperSignalType = ReaperOscSender.SignalType.Voice;
                break;
            case SignalType.Tone:
                reaperSignalType = ReaperOscSender.SignalType.Noise;
                break;
            case SignalType.Music:
                reaperSignalType = ReaperOscSender.SignalType.Music;
                break;
            default:
                reaperSignalType = ReaperOscSender.SignalType.Noise;
                break;
        }

        // 2: Experiment representation -> Reaper track
        ReaperOscSender.RepresentationType reaperRepType;
        switch (currentTrial.representation)
        {
            case RepresentationType.HOA3rdOrder:
                reaperRepType = ReaperOscSender.RepresentationType.HOA3;
                break;
            case RepresentationType.HOA4thOrder:
                reaperRepType = ReaperOscSender.RepresentationType.HOA4;
                break;
            case RepresentationType.Binaural:
            default:
                reaperRepType = ReaperOscSender.RepresentationType.Binaural;
                break;
        }

        // 3: setting routing in Reaper (mute/unmute all tracks)
        reaperOscSender.ConfigureRouting(reaperSignalType, reaperRepType);

        Debug.Log(
            $"Reaper-Routing: Signal={currentTrial.signalType} (Reaper {reaperSignalType}), " +
            $"Rep={currentTrial.representation} (Reaper {reaperRepType})"
        );
    }

    // Starts stimulus in Reaper for current trial

    public void StartStimulusForCurrentTrial(float durationSeconds)
    {
        // Optional for debugging
        PlayCurrentSourceAudio();

        if (reaperOscSender == null)
        {
            Debug.LogWarning("ExperimentController: Kein ReaperOscSender gesetzt – es wird nur Unity-Audio gespielt.");
            return;
        }

        if (currentTrial == null)
        {
            Debug.LogWarning("ExperimentController: Kein aktuelles Trial – kein Stimulus in Reaper gestartet.");
            return;
        }

        StartCoroutine(ReaperStimulusRoutine(durationSeconds));
    }

    private IEnumerator ReaperStimulusRoutine(float durationSeconds)
    {
        // Set routing based on current trial
        ConfigureReaperRoutingForCurrentTrial();

        // Jump to start of timeline
        reaperOscSender.JumpToStart();

        reaperOscSender.TogglePlay();

        yield return new WaitForSeconds(durationSeconds);

        reaperOscSender.ToggleStop();
    }



    public enum RepresentationType
    {
        Binaural,
        HOA3rdOrder,
        HOA4thOrder
    }

    public enum SignalType
    {
        Tone,
        Voice,
        Music
    }

    [Serializable]
    public class TrialDefinition
    {
        public int trialIndex;
        public RepresentationType representation;
        public SignalType signalType;
        public int quadrantIndex; // 0..3 (0: 0–90, 1: 90–180, 2: 180–270, 3: 270–360)
        public float targetAzimuthDeg;
        public float targetElevationDeg;
    }

    private List<TrialDefinition> trials;
    private int currentTrialIndex = -1;
    private TrialDefinition currentTrial;
    private bool experimentFinished = false;

    // Places/updates source marker based on azimuth/elevation
    public void PlaceTarget(float azimuthDeg, float elevationDeg)
    {
        if (sourceMarkerPrefab == null)
        {
            Debug.LogError("ExperimentController: sourceMarkerPrefab is not assigned " +
                           "(should point to the SoundSource object in the scene).");
            return;
        }

        if (currentSourceMarker == null)
        {
            currentSourceMarker = sourceMarkerPrefab;
        }

        Vector3 dir = SphericalCoords.DirectionFromAzEl(azimuthDeg, elevationDeg);
        currentSourceMarker.transform.position = dir * sphereRadius;

        // Move ASIO source to same position as visual marker
        if (asioSourceTransform != null)
        {
            asioSourceTransform.position = currentSourceMarker.transform.position;
        }
    }

    // Returns the current gaze direction in world coordinates
    public Vector3 GetGazeDirection()
    {
        Transform t = gazeTransform != null ? gazeTransform : head;
        if (t == null) return Vector3.forward;
        return t.forward;
    }

    // Returns position from which the angle to the source is computed
    public Vector3 GetGazePosition()
    {
        Transform t = gazeTransform != null ? gazeTransform : head;
        if (t == null) return Vector3.zero;
        return t.position;
    }



    // Computes angle between head forward direction and source in degrees
    public float ComputeAngularError()
    {
        if (currentSourceMarker == null || (head == null && gazeTransform == null))
        {
            Debug.LogWarning("ExperimentController: head/gazeTransform or currentSourceMarker is missing.");
            return 0f;
        }

        Vector3 headDir = GetGazeDirection();
        Vector3 headPos = GetGazePosition();
        Vector3 toSource = (currentSourceMarker.transform.position - headPos).normalized;

        float angle = Vector3.Angle(headDir, toSource);
        return angle;
    }


    // Returns current direction vector to the source (normalized) if needed
    public Vector3 GetSourceDirection()
    {
        if (currentSourceMarker == null)
        {
            return Vector3.forward;
        }

        Vector3 headPos = GetGazePosition();
        return (currentSourceMarker.transform.position - headPos).normalized;
    }


    /// <summary>
    /// Plays the sound of the current source:
    /// Unity AudioSource on the marker (if present)
    /// ASIO source in the scene (if present)
    /// </summary>
    public void PlayCurrentSourceAudio()
    {
        if (currentSourceMarker == null && asioSourceTransform == null)
        {
            Debug.LogWarning("ExperimentController: No source available to play.");
            return;
        }

        // Unity AudioSource on the marker
        if (currentSourceMarker != null)
        {
            currentSourceMarker.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        }

        // Persistent ASIO source
        if (asioSourceTransform != null)
        {
            asioSourceTransform.gameObject.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        }
    }

    // Trial Design & Management

    private void Awake()
    {
        GenerateBalancedTrials();
    }

    private void Start()
    {
        AdvanceToNextTrial();
    }

    // Indicates whether the experiment (all trials) has already finished
    public bool IsExperimentFinished
    {
        get { return experimentFinished; }
    }

    /// <summary>
    /// Generates a trial list with:
    /// 3 representations x 3 signal types = 9 combinations
    /// Each combination equally often in 4 quadrants = 36 trials
    /// Azimuth per quadrant randomized within the quadrant
    /// Elevation randomized within the inspector-defined range
    /// </summary>
    private void GenerateBalancedTrials()
    {
        trials = new List<TrialDefinition>();

        var reps = (RepresentationType[])Enum.GetValues(typeof(RepresentationType));
        var sigs = (SignalType[])Enum.GetValues(typeof(SignalType));

        int idx = 0;
        foreach (var rep in reps)
        {
            foreach (var sig in sigs)
            {
                for (int quadrant = 0; quadrant < 4; quadrant++)
                {
                    float minAz = quadrant * 90f;
                    float maxAz = (quadrant + 1) * 90f;

                    float az = UnityEngine.Random.Range(minAz, maxAz);

                    float el;
                    if (maxElevationDeg > minElevationDeg)
                    {
                        el = UnityEngine.Random.Range(minElevationDeg, maxElevationDeg);
                    }
                    else
                    {
                        // Fallback: if min/max are swapped, just use minElevationDeg
                        el = minElevationDeg;
                    }


                    var trial = new TrialDefinition
                    {
                        trialIndex = idx,
                        representation = rep,
                        signalType = sig,
                        quadrantIndex = quadrant,
                        targetAzimuthDeg = az,
                        targetElevationDeg = el
                    };

                    trials.Add(trial);
                    idx++;
                }
            }
        }

        // Shuffle trials so the order is randomized
        for (int i = 0; i < trials.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, trials.Count);
            var tmp = trials[i];
            trials[i] = trials[j];
            trials[j] = tmp;
        }

        currentTrialIndex = -1;
        currentTrial = null;
        experimentFinished = false;

        Debug.Log($"ExperimentController: {trials.Count} trials generated (9 combinations x 4 quadrants).");
    }

    // Advances to next trial, places source accordingly, returns whether another trial exists
    public bool AdvanceToNextTrial()
    {
        if (trials == null || trials.Count == 0)
        {
            Debug.LogWarning("ExperimentController: No trials defined.");
            experimentFinished = true;
            currentTrial = null;
            return false;
        }

        currentTrialIndex++;

        if (currentTrialIndex >= trials.Count)
        {
            Debug.Log("ExperimentController: All trials have been performed. Experiment is finished.");
            experimentFinished = true;
            currentTrial = null;
            return false;
        }

        currentTrial = trials[currentTrialIndex];
        experimentFinished = false;

        PlaceTarget(currentTrial.targetAzimuthDeg, currentTrial.targetElevationDeg);

        Debug.Log(
            $"Neuer Trial #{currentTrialIndex + 1}/{trials.Count}: " +
            $"Rep={currentTrial.representation}, Signal={currentTrial.signalType}, " +
            $"Quadrant={(((currentTrial.quadrantIndex + 2) % 4) + 1)}, " +   // +180° for logging
            $"Az={currentTrial.targetAzimuthDeg:F1}°, El={currentTrial.targetElevationDeg:F1}°");

        return true;
    }

    // Getters for Logging

    public int GetCurrentTrialNumber()
    {
        return currentTrialIndex + 1;
    }

    public int GetTotalTrialCount()
    {
        return (trials != null) ? trials.Count : 0;
    }

    public RepresentationType GetCurrentRepresentation()
    {
        return currentTrial != null ? currentTrial.representation : RepresentationType.Binaural;
    }

    public SignalType GetCurrentSignalType()
    {
        return currentTrial != null ? currentTrial.signalType : SignalType.Tone;
    }

    public float GetCurrentTargetAzimuth()
    {
        return currentTrial != null ? currentTrial.targetAzimuthDeg : 0f;
    }

    public float GetCurrentTargetElevation()
    {
        return currentTrial != null ? currentTrial.targetElevationDeg : 0f;
    }

    public int GetCurrentQuadrantIndex()
    {
        return currentTrial != null ? currentTrial.quadrantIndex : -1;
    }
}
