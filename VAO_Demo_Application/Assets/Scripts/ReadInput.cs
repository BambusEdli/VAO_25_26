using System;
using UnityEngine;

public class ReadInput : MonoBehaviour
{
    public GameObject tracker;
    void Start()
    {
        
    }

    public void SetTracker(GameObject newTracker)
    {
        this.tracker = newTracker;
        // GameObject cam = Camera.main.gameObject;
        //cam.transform.parent = tracker.transform;
        // cam.transform.localPosition = Vector3.zero;
    }

    void Update()
    {
        
        // Debug.Log(tracker.transform.position); 
        // Debug.Log(tracker.transform.forward);        
    }

    private void OnDrawGizmos()
    {
        if(tracker == null) return; 
        Gizmos.color = Color.red;
        Gizmos.DrawRay(tracker.transform.position,tracker.transform.forward);
    }
}
