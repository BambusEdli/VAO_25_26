using System.Collections;
using UnityEngine;

public class TrackerCentering : MonoBehaviour
{
    [Header("Origin-Suche")]
    [SerializeField] private string originTag = "TrackerOrigin";
    [SerializeField] private float searchTimeoutSeconds = 5f;

    [Header("Positions-Offset")]
    [SerializeField] private float zOffset = -2.56f;

    [Header("Rotations-Offset")]
    [Tooltip("Rotationskorrektur in Grad (Euler), z.B. (90, 0, 0) zum 90°-Nach-vorne-Kippen.")]
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(90f, 0f, 0f);

    private Transform trackingOrigin;

    private IEnumerator Start()
    {
        float t0 = Time.time;

        // 1) Auf Origin mit bestimmtem Tag warten
        while (!trackingOrigin && Time.time - t0 < searchTimeoutSeconds)
        {
            var go = GameObject.FindGameObjectWithTag(originTag);
            if (go)
                trackingOrigin = go.transform;

            if (!trackingOrigin)
                yield return null;
        }

        if (!trackingOrigin)
        {
            Debug.LogError($"Origin not found with tag '{originTag}'");
            yield break;
        }

        // 2) Origin verschieben (verschiebt den gesamten Tracking-Space)
        Vector3 p = trackingOrigin.position;   // world space
        p.z += zOffset;
        trackingOrigin.position = p;

        // 3) Origin rotieren (kippt den gesamten Tracking-Space)
        //    -> Tracker wird um rotationOffsetEuler gedreht, z.B. 90° nach vorne
        Quaternion rotOffset = Quaternion.Euler(rotationOffsetEuler);
        trackingOrigin.rotation = trackingOrigin.rotation * rotOffset;

        Debug.Log($"TrackerCentering: Origin verschoben (z += {zOffset}) und rotiert um {rotationOffsetEuler} Grad.");
    }
}
