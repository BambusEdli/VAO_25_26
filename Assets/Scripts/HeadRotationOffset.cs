using UnityEngine;

/// <summary>
/// Erzwingt einen festen lokalen Rotations-Offset für das Head-Objekt
/// (Child des Vive-Trackers), z.B. um 90° nach vorne zu kippen.
/// Die Rotationen werden explizit in der Reihenfolge Y -> X -> Z angewendet.
/// </summary>
public class HeadRotationOffset : MonoBehaviour
{
    [Tooltip("Lokaler Rotations-Offset in Grad (Euler) relativ zum Tracker: (X = Pitch, Y = Yaw, Z = Roll).")]
    public Vector3 rotationOffsetEuler = new Vector3(90f, 0f, 0f); // im Inspector anpassen

    private Quaternion rotationOffset;

    private void OnValidate()
    {
        RecomputeRotationOffset();
    }

    private void Start()
    {
        RecomputeRotationOffset();
    }

    private void RecomputeRotationOffset()
    {
        // X = Pitch (rechts/links kippen um lokale X-Achse)
        // Y = Yaw   (links/rechts drehen um lokale Y-Achse)
        // Z = Roll  (um Vorwärtsachse drehen)
        float pitchX = rotationOffsetEuler.x;
        float yawY = rotationOffsetEuler.y;
        float rollZ = rotationOffsetEuler.z;

        Quaternion qYaw = Quaternion.AngleAxis(yawY, Vector3.up);      // Y
        Quaternion qPitch = Quaternion.AngleAxis(pitchX, Vector3.right);   // X
        Quaternion qRoll = Quaternion.AngleAxis(rollZ, Vector3.forward); // Z

        // Reihenfolge der Anwendung: Yaw -> Pitch -> Roll
        // (Operationen wirken von rechts nach links)
        rotationOffset = qRoll * qPitch * qYaw;
    }

    private void LateUpdate()
    {
        // Nur sinnvoll, wenn dieses Objekt ein Child des Trackers ist.
        var parent = transform.parent;
        if (parent == null)
            return;

        // Tracker liefert Weltrotation; Head hängt als Child dran.
        // Wir setzen die lokale Rotation jedes Frame auf den gewünschten Offset:
        // Weltrotation = Tracker.rotation * rotationOffset
        transform.localRotation = rotationOffset;
    }
}
