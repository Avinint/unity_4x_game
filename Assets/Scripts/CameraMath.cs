using UnityEngine;

public static class CameraMath
{
    // Inclinaison interpolée entre pitchNear (à minDistance) et pitchFar (à maxDistance).
    public static float Pitch(float distance, float minDistance, float maxDistance, float pitchNear, float pitchFar)
        => Mathf.Lerp(pitchNear, pitchFar, Mathf.InverseLerp(minDistance, maxDistance, distance));

    // Direction de défilement quand la souris est à moins de `margin` pixels d'un bord de l'écran.
    // Zéro au centre ou si la souris est hors de la fenêtre ; normalisée dans les coins.
    public static Vector2 EdgeScrollDirection(Vector2 mouse, Vector2 screen, float margin)
    {
        if (mouse.x < 0f || mouse.y < 0f || mouse.x > screen.x || mouse.y > screen.y) return Vector2.zero;
        float x = mouse.x <= margin ? -1f : mouse.x >= screen.x - margin ? 1f : 0f;
        float y = mouse.y <= margin ? -1f : mouse.y >= screen.y - margin ? 1f : 0f;
        var direction = new Vector2(x, y);
        return direction == Vector2.zero ? direction : direction.normalized;
    }

    // Décalage caméra → cible : à `distance`, côté sud, inclinée selon Pitch.
    public static Vector3 TiltedOffset(float distance, float minDistance, float maxDistance, float pitchNear, float pitchFar)
    {
        float rad = Pitch(distance, minDistance, maxDistance, pitchNear, pitchFar) * Mathf.Deg2Rad;
        return new Vector3(0f, distance * Mathf.Sin(rad), -distance * Mathf.Cos(rad));
    }
}
