using UnityEngine;

public static class CameraMath
{
    // Inclinaison interpolée entre pitchNear (à minDistance) et pitchFar (à maxDistance).
    public static float Pitch(float distance, float minDistance, float maxDistance, float pitchNear, float pitchFar)
        => Mathf.Lerp(pitchNear, pitchFar, Mathf.InverseLerp(minDistance, maxDistance, distance));

    // Distance pour que toute la carte (taille x, z) tienne à l'écran, la caméra visant son centre.
    // Largeur : champ horizontal ; profondeur : raccourcie par l'inclinaison (≈ sin pitch). margin > 1 laisse un bord.
    public static float FitDistance(Vector2 mapSize, float verticalFovDeg, float aspect, float pitchDeg, float margin = 1.1f)
    {
        float halfV = verticalFovDeg * 0.5f * Mathf.Deg2Rad;
        float halfH = Mathf.Atan(Mathf.Tan(halfV) * aspect);
        float dx = mapSize.x * 0.5f / Mathf.Tan(halfH);
        float dz = mapSize.y * 0.5f * Mathf.Sin(pitchDeg * Mathf.Deg2Rad) / Mathf.Tan(halfV);
        return Mathf.Max(dx, dz) * margin;
    }

    // Rectangle réduit autour de son centre : factor 1 = inchangé, 0 = réduit au centre.
    public static Rect ShrinkRect(Rect rect, float factor)
    {
        factor = Mathf.Clamp01(factor);
        Vector2 size = rect.size * factor;
        return new Rect(rect.center - size * 0.5f, size);
    }

    // Part de la carte où la vue peut aller : 1 jusqu'à startFraction × max, puis jusqu'à 0 à la distance max.
    public static float PanFreedom(float distance, float maxDistance, float startFraction) =>
        1f - Mathf.InverseLerp(startFraction * maxDistance, maxDistance, distance);

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
