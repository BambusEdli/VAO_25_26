using System.Collections;
using UnityEngine;

public class TrackerCentering : MonoBehaviour
{
    [SerializeField] private string originTag = "TrackerOrigin";
    [SerializeField] private float zOffset = -2.56f;
    [SerializeField] private float searchTimeoutSeconds = 5f;

    private Transform trackingOrigin;

    private IEnumerator Start()
    {
        float t0 = Time.time;

        // 1) Wait until the Origin ("O") exists
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

        // 2) MOVE THE ORIGIN HERE
        // This shifts the entire tracking space because the tracker is its child
        Vector3 p = trackingOrigin.position;   // world space
        p.z += zOffset;                         // -2.56
        trackingOrigin.position = p;
    }
}