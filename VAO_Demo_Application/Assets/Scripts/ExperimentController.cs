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



    /// <summary>
    /// Platziert/aktualisiert den Quellmarker basierend auf Azimut/Elevation.
    /// </summary>
    public void PlaceTarget(float azimuthDeg, float elevationDeg)
    {
        if (sourceMarkerPrefab == null)
        {
            Debug.LogError("ExperimentController: sourceMarkerPrefab ist nicht gesetzt.");
            return;
        }

        if (currentSourceMarker == null)
        {
            currentSourceMarker = Instantiate(sourceMarkerPrefab, Vector3.zero, Quaternion.identity);
            currentSourceMarker.name = "CurrentSourceMarker";
        }

        Vector3 dir = SphericalCoords.DirectionFromAzEl(azimuthDeg, elevationDeg);
        currentSourceMarker.transform.position = dir * sphereRadius;
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
    /// Gibt die aktuelle Richtungsvektor-Quelle zurück (normalisiert), falls benötigt.
    /// </summary>
    public Vector3 GetSourceDirection()
    {
        if (currentSourceMarker == null)
        {
            return Vector3.forward;
        }

        return (currentSourceMarker.transform.position - head.position).normalized;
    }

    /// <summary>
    /// Wählt einen neuen zufälligen Zielwinkel (Azimut/Elevation)
    /// und platziert den Marker auf der Sphäre.
    /// Hier erstmal simple Zufallsverteilung; Balancing kommt später.
    /// </summary>
    public void PlaceRandomTarget()
    {
        // Azimut: komplett zufällig 0–360°
        float azimuth = Random.Range(0f, 360f);

        // Elevation: hier Beispiel 0° oder +30°
        float elevation = Random.value < 0.5f ? 0f : 30f;

        PlaceTarget(azimuth, elevation);
        Debug.Log($"Neues Target gesetzt: Az = {azimuth:F1}°, El = {elevation:F1}°");
    }

    private void Start()
    {
        // Erstes Target beim Szenenstart setzen
        PlaceRandomTarget();
    }

}
