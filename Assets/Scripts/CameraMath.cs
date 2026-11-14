using UnityEngine;

public static class CameraMath
{
    // Inclinaison interpolée entre pitchNear (à minDistance) et pitchFar (à maxDistance).
    public static float Pitch(float distance, float minDistance, float maxDistance, float pitchNear, float pitchFar)
        => Mathf.Lerp(pitchNear, pitchFar, Mathf.InverseLerp(minDistance, maxDistance, distance));

    // Décalage caméra → cible : à `distance`, côté sud, inclinée selon Pitch.
    public static Vector3 TiltedOffset(float distance, float minDistance, float maxDistance, float pitchNear, float pitchFar)
    {
        float rad = Pitch(distance, minDistance, maxDistance, pitchNear, pitchFar) * Mathf.Deg2Rad;
        return new Vector3(0f, distance * Mathf.Sin(rad), -distance * Mathf.Cos(rad));
    }
}
