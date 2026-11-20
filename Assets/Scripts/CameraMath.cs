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

    // Étendue au sol de la vue (vers le sud, vers le nord), mesurée depuis la cible, pour une caméra
    // inclinée de `pitchDeg` à `distance` de la cible : rayons du bas et du haut de l'écran jusqu'au sol.
    public static (float south, float north) GroundExtent(float distance, float pitchDeg, float verticalFovDeg)
    {
        float pitch = pitchDeg * Mathf.Deg2Rad, half = verticalFovDeg * 0.5f * Mathf.Deg2Rad;
        float height = distance * Mathf.Sin(pitch), back = distance * Mathf.Cos(pitch);
        // Distance horizontale caméra → sol pour un rayon incliné de `a` sous l'horizontale (négative au-delà de 90°).
        float Reach(float a) => height * Mathf.Cos(a) / Mathf.Max(Mathf.Sin(a), 1e-4f);
        float north = Mathf.Min(Reach(pitch - half), 10000f) - back;   // rayon du haut proche de l'horizon : borné
        float south = back - Reach(pitch + half);
        return (south, north);
    }

    // Valeur gardée pour que [value − below, value + above] reste dans [min, max] ; centrée si la vue est plus grande.
    public static float ClampSpan(float value, float min, float max, float below, float above)
    {
        float lo = min + below, hi = max - above;
        return lo <= hi ? Mathf.Clamp(value, lo, hi) : (lo + hi) * 0.5f;
    }

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
