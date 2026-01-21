using UnityEngine;

/// <summary>
/// Erzwingt einen festen lokalen Rotations-Offset für das Head-Objekt
/// (Child des Vive-Trackers), z.B. um 90° nach vorne zu kippen.
/// </summary>
public class HeadRotationOffset : MonoBehaviour
{
    [Tooltip("Lokaler Rotations-Offset in Grad (Euler) relativ zum Tracker.")]
    public Vector3 rotationOffsetEuler = new Vector3(90f, 0f, 0f); // Wert ggf. im Inspector anpassen

    private Quaternion rotationOffset;

    private void OnValidate()
    {
        rotationOffset = Quaternion.Euler(rotationOffsetEuler);
    }

    private void Start()
    {
        rotationOffset = Quaternion.Euler(rotationOffsetEuler);
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
