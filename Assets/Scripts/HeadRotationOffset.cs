using UnityEngine;

/// <summary>
/// Hält einen festen lokalen Rotations-Offset für das Head-Objekt
/// (Child des Vive-Trackers).
/// Die Rotation wird exakt so verwendet, wie du sie im Transform-Inspector einstellst.
/// </summary>
public class HeadRotationOffset : MonoBehaviour
{
    [Tooltip("Gespeicherte lokale Euler-Winkel (X,Y,Z) relativ zum Tracker. "
           + "Wird im Editor automatisch aus der aktuellen Transform-Rotation übernommen.")]
    public Vector3 localEulerOffset = new Vector3(0f, 0f, 0f);

    private void OnValidate()
    {
        // Im Edit-Mode: immer wenn du das Objekt drehst,
        // übernehmen wir diese Rotation als gewünschten Offset.
        if (!Application.isPlaying)
        {
            localEulerOffset = transform.localEulerAngles;
        }
    }

    private void LateUpdate()
    {
        // Nur sinnvoll, wenn das Head-Objekt ein Child des Trackers ist.
        if (transform.parent == null)
            return;

        // Tracker setzt seine Rotation, wir erzwingen danach
        // diesen lokalen Offset relativ zum Tracker:
        transform.localEulerAngles = localEulerOffset;
    }
}
