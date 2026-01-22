using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verantwortlich für das Platzieren der Zielquelle auf der Sphäre
/// und das Berechnen des Winkelfehlers zwischen Kopf-Forward und Quelle.
/// </summary>
public class ExperimentController : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Head-Objekt mit Kamera / Tracker-Rotation")]
    public Transform head;

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



    /// <summary>
    /// Konfiguriert das Routing in REAPER für das aktuelle Trial:
    /// - genau ein Signaltrack aktiv (1=Voice, 2=Tone/Noise, 3=Music)
    /// - genau ein Repräsentations-Track aktiv (4=HOA3, 5=HOA4, 6=Binaural)
    /// </summary>
    private void ConfigureReaperRoutingForCurrentTrial()
    {
        if (reaperOscSender == null || currentTrial == null)
        {
            return;
        }

        // 1) Experiment-Signaltyp auf Reaper-Signaltyp abbilden
        ReaperOscSender.SignalType reaperSignalType;
        switch (currentTrial.signalType)
        {
            case SignalType.Voice:
                reaperSignalType = ReaperOscSender.SignalType.Voice;
                break;
            case SignalType.Tone:   // dein Noise/Tone-Track -> Reaper "Noise"
                reaperSignalType = ReaperOscSender.SignalType.Noise;
                break;
            case SignalType.Music:
                reaperSignalType = ReaperOscSender.SignalType.Music;
                break;
            default:
                reaperSignalType = ReaperOscSender.SignalType.Noise;
                break;
        }

        // 2) Experiment-Repräsentation auf Reaper-Repräsentation abbilden
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

        // 3) Routing in Reaper setzen (Mute/Unmute aller Tracks)
        reaperOscSender.ConfigureRouting(reaperSignalType, reaperRepType);

        Debug.Log(
            $"Reaper-Routing: Signal={currentTrial.signalType} (Reaper {reaperSignalType}), " +
            $"Rep={currentTrial.representation} (Reaper {reaperRepType})"
        );
    }


    /// <summary>
    /// Startet für das aktuelle Trial den Stimulus in Reaper
    /// (Routing setzen, JumpToStart, Play, nach Dauer Stop).
    /// Optional: zusätzlich Unity-Audio abspielen.
    /// </summary>
    public void StartStimulusForCurrentTrial(float durationSeconds)
    {
        // Optional: lokale Audioquelle parallel spielen lassen (zum Debuggen)
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
        // 1) Routing je nach aktuellem Trial setzen
        ConfigureReaperRoutingForCurrentTrial();

        // 2) TODO: hier später 'Move Source for OSC' ergänzen, wenn Adresse klar ist

        // 3) An den Anfang der Timeline springen
        reaperOscSender.JumpToStart();

        // 4) Play
        reaperOscSender.TogglePlay();

        // 5) Warten (Stimulusdauer)
        yield return new WaitForSeconds(durationSeconds);

        // 6) Stop
        reaperOscSender.ToggleStop();
    }



    // ---------- Faktorielles Design ----------

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
        public int trialIndex;                // 0-basiert intern, Logging kann +1 nehmen
        public RepresentationType representation;
        public SignalType signalType;
        public int quadrantIndex;            // 0..3 (0: 0–90, 1: 90–180, 2: 180–270, 3: 270–360)
        public float targetAzimuthDeg;
        public float targetElevationDeg;
    }

    private List<TrialDefinition> trials;
    private int currentTrialIndex = -1;
    private TrialDefinition currentTrial;
    private bool experimentFinished = false;

    // ---------- Platzierung & Geometrie ----------

    /// <summary>
    /// Platziert/aktualisiert den Quellmarker basierend auf Azimut/Elevation.
    /// </summary>
    public void PlaceTarget(float azimuthDeg, float elevationDeg)
    {
        if (sourceMarkerPrefab == null)
        {
            Debug.LogError("ExperimentController: sourceMarkerPrefab ist nicht gesetzt " +
                           "(sollte jetzt auf das SoundSource-Objekt in der Szene zeigen).");
            return;
        }

        // Wir benutzen das bereits in der Szene vorhandene Objekt als aktuellen Marker
        if (currentSourceMarker == null)
        {
            currentSourceMarker = sourceMarkerPrefab;
        }

        Vector3 dir = SphericalCoords.DirectionFromAzEl(azimuthDeg, elevationDeg);
        currentSourceMarker.transform.position = dir * sphereRadius;

        // Asio-Quelle an die gleiche Position wie den visuellen Marker setzen
        if (asioSourceTransform != null)
        {
            asioSourceTransform.position = currentSourceMarker.transform.position;
        }
    }


    /// <summary>
    /// Berechnet den Winkel zwischen Kopf-Vorwärtsrichtung und Quelle in Grad.
    /// </summary>
    public float ComputeAngularError()
    {
        if (head == null || currentSourceMarker == null)
        {
            Debug.LogWarning("ExperimentController: head oder currentSourceMarker fehlt.");
            return 0f;
        }

        Vector3 headDir = head.forward;
        Vector3 toSource = (currentSourceMarker.transform.position - head.position).normalized;

        float angle = Vector3.Angle(headDir, toSource);
        return angle;
    }

    /// <summary>
    /// Gibt den aktuellen Richtungsvektor zur Quelle zurück (normalisiert), falls benötigt.
    /// </summary>
    public Vector3 GetSourceDirection()
    {
        if (currentSourceMarker == null || head == null)
        {
            return Vector3.forward;
        }

        return (currentSourceMarker.transform.position - head.position).normalized;
    }

    /// <summary>
    /// Spielt den Sound der aktuellen Quelle ab:
    /// - Unity-AudioSource auf dem Marker (falls vorhanden)
    /// - Asio-Quelle in der Szene (falls vorhanden)
    /// </summary>
    public void PlayCurrentSourceAudio()
    {
        if (currentSourceMarker == null && asioSourceTransform == null)
        {
            Debug.LogWarning("ExperimentController: Keine Quelle zum Abspielen vorhanden.");
            return;
        }

        // Unity-AudioSource auf dem Marker (z.B. Test-Clip/Kopfhörer)
        if (currentSourceMarker != null)
        {
            currentSourceMarker.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        }

        // Persistente Asio-Quelle
        if (asioSourceTransform != null)
        {
            asioSourceTransform.gameObject.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        }
    }

    // ---------- Trial-Design & -Verwaltung ----------

    private void Awake()
    {
        GenerateBalancedTrials();
    }

    private void Start()
    {
        // Erstes Trial vorbereiten
        AdvanceToNextTrial();
    }

    /// <summary>
    /// Gibt an, ob das Experiment (alle Trials) bereits abgeschlossen ist.
    /// </summary>
    public bool IsExperimentFinished
    {
        get { return experimentFinished; }
    }

    /// <summary>
    /// Erzeugt eine Trial-Liste mit:
    /// - 3 Repräsentationen x 3 Signaltypen = 9 Kombinationen
    /// - Jede Kombination gleich oft in 4 Quadranten = 36 Trials
    /// - Azimut pro Quadrant zufällig innerhalb des Quadranten
    /// - Elevation zunächst 0° oder 30° zufällig (noch nicht gebalanced)
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

                    // Elevation aus dem im Inspector gesetzten Bereich
                    float el;
                    if (maxElevationDeg > minElevationDeg)
                    {
                        el = UnityEngine.Random.Range(minElevationDeg, maxElevationDeg);
                    }
                    else
                    {
                        // Fallback: falls jemand min/max vertauscht, nimm einfach minElevationDeg
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

        // Shuffle der Trials, damit Reihenfolge randomisiert ist
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

        Debug.Log($"ExperimentController: {trials.Count} Trials generiert (9 Kombinationen x 4 Quadranten).");
    }

    /// <summary>
    /// Springt auf das nächste Trial, platziert die Quelle entsprechend
    /// und gibt zurück, ob ein weiteres Trial existiert.
    /// </summary>
    public bool AdvanceToNextTrial()
    {
        if (trials == null || trials.Count == 0)
        {
            Debug.LogWarning("ExperimentController: Keine Trials definiert.");
            experimentFinished = true;
            currentTrial = null;
            return false;
        }

        currentTrialIndex++;

        if (currentTrialIndex >= trials.Count)
        {
            Debug.Log("ExperimentController: Alle Trials wurden durchgeführt. Experiment ist beendet.");
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
            $"Quadrant={currentTrial.quadrantIndex + 1}, " +
            $"Az={currentTrial.targetAzimuthDeg:F1}°, El={currentTrial.targetElevationDeg:F1}°");

        return true;
    }

    // ---------- Getter für Logging ----------

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
