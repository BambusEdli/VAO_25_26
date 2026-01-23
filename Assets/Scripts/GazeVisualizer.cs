using UnityEngine;

/// Zeichnet Laser in Blickrichtung des Kopfes mit einem LineRenderer

[RequireComponent(typeof(LineRenderer))]
public class GazeVisualizer : MonoBehaviour
{
    [Tooltip("Head-Transform (Kamera/Tracker)")]
    public Transform head;

    [Tooltip("Länge des Gaze-Lasers")]
    public float gazeLength = 10f;

    private LineRenderer line;

    [Tooltip("ExperimentController, von dem die bereinigte Blickrichtung bezogen wird (optional).")]
    public ExperimentController experiment;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }

    private void Update()
    {
        if (line == null)
        {
            return;
        }

        // Wenn ein ExperimentController gesetzt ist: dessenGaze-Definition nutzen
        if (experiment != null)
        {
            Vector3 start = experiment.GetGazePosition();
            Vector3 dir = experiment.GetGazeDirection();
            Vector3 end = start + dir * gazeLength;

            line.SetPosition(0, start);
            line.SetPosition(1, end);
            return;
        }

        // Fallback: direkt head.forward
        if (head == null)
        {
            return;
        }

        Vector3 s = head.position;
        Vector3 e = s + head.forward * gazeLength;

        line.SetPosition(0, s);
        line.SetPosition(1, e);
    }

}

