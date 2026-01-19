using UnityEngine;

public static class SphericalCoords
{
    // azimuthDeg: 0° = +Z (vorne), 90° = +X (rechts)
    // elevationDeg: 0° = Horizont, +90° = Zenit
    public static Vector3 DirectionFromAzEl(float azimuthDeg, float elevationDeg)
    {
        float az = azimuthDeg * Mathf.Deg2Rad;
        float el = elevationDeg * Mathf.Deg2Rad;

        float x = Mathf.Sin(az) * Mathf.Cos(el);
        float y = Mathf.Sin(el);
        float z = Mathf.Cos(az) * Mathf.Cos(el);

        return new Vector3(x, y, z).normalized;
    }
}
