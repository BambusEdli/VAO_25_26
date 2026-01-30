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

    // ----------------- Public API -----------------

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
    /// Sets the routing for a combination of signal and representation:
    /// exactly one signal track (1 = Voice, 2 = Noise, 3 = Music)
    /// exactly one bus track   (4 = Binaural, 5 = HOA3, 6 = HOA4)  <-- matches your REAPER project
    /// </summary>
    public void ConfigureRouting(SignalType signal, RepresentationType representation)
    {
        SetAllTracksMute(true);

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

        // IMPORTANT: Representation track order in REAPER:
        // Track 4 = Binaural, Track 5 = HOA3, Track 6 = HOA4
        int repTrack = 4;
        switch (representation)
        {
            case RepresentationType.Binaural:
                repTrack = 4;
                break;
            case RepresentationType.HOA3:
                repTrack = 5;
                break;
            case RepresentationType.HOA4:
                repTrack = 6;
                break;
        }

        SetTrackMute(signalTrack, false);
        SetTrackMute(repTrack, false);

        Debug.Log(
            $"ReaperOscSender: Routing set -> Signal {signal} (Track {signalTrack}), " +
            $"Rep {representation} (Track {repTrack})"
        );
    }

    public void PlayStimulus(SignalType signal,
                             RepresentationType representation,
                             float durationSec)
    {
        StartCoroutine(PlayStimulusRoutine(signal, representation, durationSec));
    }

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

    private IEnumerator PlayStimulusRoutine(SignalType signal, RepresentationType representation, float durationSec)
    {
        ConfigureRouting(signal, representation);

        JumpToStart();
        TogglePlay();

        yield return new WaitForSeconds(durationSec);

        ToggleStop();
    }
}
