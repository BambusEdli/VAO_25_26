using System.Collections;
using UnityEngine;
using extOSC;

public class ReaperOscSender : MonoBehaviour
{
    [Header("REAPER OSC Target")]
    public string RemoteHost = "127.0.0.1";
    public int RemotePort = 8000;

    private OSCTransmitter _transmitter;

    private void Awake()
    {
        _transmitter = gameObject.GetComponent<OSCTransmitter>();
        if (_transmitter == null)
            _transmitter = gameObject.AddComponent<OSCTransmitter>();

        _transmitter.RemoteHost = RemoteHost;
        _transmitter.RemotePort = RemotePort;
    }

    // ----------------- Öffentliche API -----------------

    public enum SignalType
    {
        Voice,
        Noise,
        Music
    }

    public enum RepresentationType
    {
        HOA3,
        HOA4,
        Binaural
    }

    /// <summary>
    /// Setzt das Routing für eine Kombination aus Signal und Repräsentation:
    ///  - genau ein Signal-Track (1=Voice, 2=Noise, 3=Music)
    ///  - genau ein Bus-Track (4=HOA3, 5=HOA4, 6=Binaural)
    /// </summary>
    public void ConfigureRouting(SignalType signal, RepresentationType representation)
    {
        // Alles zunächst muten
        SetAllTracksMute(true);

        // Signal-Track bestimmen
        int signalTrack = 1;
        switch (signal)
        {
            case SignalType.Voice:
                signalTrack = 1;
                break;
            case SignalType.Noise:
                signalTrack = 2;
                break;
            case SignalType.Music:
                signalTrack = 3;
                break;
        }

        // Repräsentations-Track bestimmen
        int repTrack = 4;
        switch (representation)
        {
            case RepresentationType.HOA3:
                repTrack = 4;
                break;
            case RepresentationType.HOA4:
                repTrack = 5;
                break;
            case RepresentationType.Binaural:
                repTrack = 6;
                break;
        }

        // Nur diese beiden unmute
        SetTrackMute(signalTrack, false);
        SetTrackMute(repTrack, false);

        Debug.Log($"ReaperOscSender: Routing gesetzt -> Signal {signal} (Track {signalTrack}), " +
                  $"Rep {representation} (Track {repTrack})");
    }

    /// <summary>
    /// Führt den gesamten Ablauf aus:
    /// - Routing setzen
    /// - zum Start springen
    /// - Play
    /// - nach 'durationSec' wieder Stop
    /// </summary>
    public void PlayStimulus(SignalType signal,
                             RepresentationType representation,
                             float durationSec)
    {
        StartCoroutine(PlayStimulusRoutine(signal, representation, durationSec));
    }

    // ----------------- Bisherige Basisfunktionen -----------------

    public void TogglePlay()
    {
        if (_transmitter == null) return;
        var msg = new OSCMessage("/play");
        _transmitter.Send(msg);
    }

    public void ToggleStop()
    {
        if (_transmitter == null) return;
        var msg = new OSCMessage("/stop");
        _transmitter.Send(msg);
    }

    public void JumpToStart()
    {
        if (_transmitter == null) return;
        var msg = new OSCMessage("/time");
        msg.AddValue(OSCValue.Float(0f));
        _transmitter.Send(msg);
    }

    // Generische Mute-Funktion
    public void SetTrackMute(int trackIndex, bool mute)
    {
        if (_transmitter == null) return;
        var msg = new OSCMessage($"/track/{trackIndex}/mute");
        msg.AddValue(OSCValue.Int(mute ? 1 : 0));
        _transmitter.Send(msg);
    }

    public void SetAllTracksMute(bool mute)
    {
        for (int i = 1; i <= 6; i++)
        {
            SetTrackMute(i, mute);
        }
    }

    // ----------------- Interne Coroutine -----------------

    private IEnumerator PlayStimulusRoutine(SignalType signal,
                                            RepresentationType representation,
                                            float durationSec)
    {
        // Schritt 1: Routing setzen (entspricht den Mute/Unmute-Blöcken in der PDF)
        ConfigureRouting(signal, representation);

        // Schritt 2: (optional) Source-Position per OSC schicken
        // -> hier könnt ihr später "Move Source for OSC" einhängen

        // Schritt 3: an den Anfang springen
        JumpToStart();

        // Schritt 4: Play
        TogglePlay();

        // Schritt 5: Warten (Stimulusdauer)
        yield return new WaitForSeconds(durationSec);

        // Schritt 6: Stop
        ToggleStop();
    }
}