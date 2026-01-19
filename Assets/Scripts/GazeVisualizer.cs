using UnityEngine;

/// <summary>
/// Zeichnet einen "Laser" in Blickrichtung des Kopfes mit einem LineRenderer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GazeVisualizer : MonoBehaviour
{
    [Tooltip("Head-Transform (Kamera/Tracker)")]
    public Transform head;

    [Tooltip("Länge des Gaze-Lasers")]
    public float gazeLength = 10f;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }

    private void Update()
    {
        if (head == null)
        {
            return;
        }

        Vector3 start = head.position;
        Vector3 end = start + head.forward * gazeLength;

        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
}

