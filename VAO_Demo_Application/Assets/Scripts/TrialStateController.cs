using UnityEngine;

/// <summary>
/// Steuert den Ablauf eines einzelnen Trials über die Tasten 1-2-3:
/// 1: Ausgangsblickrichtung (Baseline) loggen
/// 2: Stimulus starten (hier zunächst simuliert)
/// 3: Antwort einloggen und Fehler berechnen
/// </summary>
public class TrialStateController : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Head-Transform (Kamera/Tracker)")]
    public Transform head;

    [Tooltip("Controller für Quelle und Fehlerberechnung")]
    public ExperimentController experiment;

    [Header("Stimulus-Einstellungen (Simulation)")]
    [Tooltip("Dauer des Stimulus in Sekunden (für ersten Prototyp)")]
    public float simulatedStimulusDuration = 2.0f;

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

    private void Update()
    {
        // Taste 1: Baseline loggen
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OnKey1();
        }

        // Taste 2: Stimulus starten
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OnKey2();
        }

        // Taste 3: Antwort loggen
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            OnKey3();
        }

        // Stimulus-Status überwachen
        UpdateStimulusState();
    }

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

        // Hier: Reaper triggern (Stimulusstart). Für Prototyp simulieren wir nur die Dauer.
        float stimulusDuration = simulatedStimulusDuration;

        stimulusOffsetTime = Time.time + stimulusDuration;
        state = TrialState.StimulusPlaying;

        Debug.Log("Stimulus gestartet, simulierte Dauer: " + stimulusDuration + " s");
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

        // Antwortzeit (Ende Stimulus -> Tastendruck)
        float responseTime = Time.time - stimulusOffsetTime;

        // Richtungsvektoren
        Vector3 headDir = head.forward;
        Vector3 sourceDir = experiment.GetSourceDirection();

        // Winkelfehler (in Grad)
        float errorAngle = Vector3.Angle(headDir, sourceDir);

        Debug.Log($"Antwort geloggt. ResponseTime = {responseTime:F3} s, Error = {errorAngle:F1}°");
        Debug.Log($"headDir = {headDir}, sourceDir = {sourceDir}");

        experiment.PlaceRandomTarget();
        // TODO: Hier später in Datei/CSV loggen

        // Trial zurücksetzen oder nächsten Trial vorbereiten
        state = TrialState.Idle;
    }


    private void UpdateStimulusState()
    {
        if (state == TrialState.StimulusPlaying)
        {
            // Hier könntest du auch Kopfbewegung während Stimulus loggen.
            if (Time.time >= stimulusOffsetTime)
            {
                state = TrialState.WaitingForResponse;
                Debug.Log("Stimulus fertig. Warte auf Antwort (Key 3)...");
            }
        }
    }
}
