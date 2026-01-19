using UnityEngine;

/// <summary>
/// Sehr einfaches Mouse-Look-Script für das Head-Objekt,
/// nur für erste Tests, bis der Vive-Tracker angebunden ist.
/// </summary>
public class SimpleHeadMouseLook : MonoBehaviour
{
    public float sensitivityX = 3f;
    public float sensitivityY = 3f;

    public float minY = -80f;
    public float maxY = 80f;

    private float rotationY = 0f;

    private void Update()
    {
        float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

        rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
    }
}
