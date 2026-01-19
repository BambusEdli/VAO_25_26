using UnityEngine;
using extOSC;

public class ReaperOscSender : MonoBehaviour
{
    [Header("REAPER OSC Target")]
    public string RemoteHost = "127.0.0.1";
    public int RemotePort = 8000;

    private OSCTransmitter _transmitter;

    void Awake()
    {
        // Fügt automatisch einen OSCTransmitter an dieses GameObject an
        _transmitter = gameObject.GetComponent<OSCTransmitter>();
        if (_transmitter == null)
            _transmitter = gameObject.AddComponent<OSCTransmitter>();

        _transmitter.RemoteHost = RemoteHost;
        _transmitter.RemotePort = RemotePort;
    }

   

// Toggle Play (sendet /play ohne Argument -> Toggle)
public void TogglePlay()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/play");
    _transmitter.Send(msg);
}

// Toggle Stop (sendet /stop ohne Argument -> Toggle)
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


public void MuteTrack1()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/1/mute");
    msg.AddValue(OSCValue.Int(1)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void UnmuteTrack1()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/1/mute");
    msg.AddValue(OSCValue.Int(0)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}


public void MuteTrack2()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/2/mute");
    msg.AddValue(OSCValue.Int(1)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void UnmuteTrack2()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/2/mute");
    msg.AddValue(OSCValue.Int(0)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void MuteTrack3()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/3/mute");
    msg.AddValue(OSCValue.Int(1)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void UnmuteTrack3()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/3/mute");
    msg.AddValue(OSCValue.Int(0)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void MuteTrack4()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/4/mute");
    msg.AddValue(OSCValue.Int(1)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void UnmuteTrack4()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/4/mute");
    msg.AddValue(OSCValue.Int(0)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}


public void MuteTrack5()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/5/mute");
    msg.AddValue(OSCValue.Int(1)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void UnmuteTrack5()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/5/mute");
    msg.AddValue(OSCValue.Int(0)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void MuteTrack6()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/6/mute");
    msg.AddValue(OSCValue.Int(1)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}

public void UnmuteTrack6()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/6/mute");
    msg.AddValue(OSCValue.Int(0)); // 1 zum Stummschalten, 0 zum Aufheben der Stummschaltung
    _transmitter.Send(msg);
}






/*
public void BypassHOA3()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/5/fx/1/bypass", OSCValue.Int(0));
    _transmitter.Send(msg); 
}

public void UnbypassHOA3()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/5/fx/1/bypass", OSCValue.Int(1));
    _transmitter.Send(msg); 
}

public void BypassHOA4()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/5/fx/2/bypass", OSCValue.Int(0));
    _transmitter.Send(msg); 
}

public void UnbypassHOA4()
{
    if (_transmitter == null) return;
    var msg = new OSCMessage("/track/5/fx/2/bypass", OSCValue.Int(1));
    _transmitter.Send(msg);
}
*/


}