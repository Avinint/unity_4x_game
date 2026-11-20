using UnityEngine;

// Fractal = génération sans masque. Nouvelles valeurs à la fin (réglage enregistré par numéro).
public enum MapType { Fractal, Continents, Pangaea, Archipelago }

// Forme des terres selon le type de carte : un masque (0 à 1) mêlé à l'altitude avant le calcul du
// niveau de la mer. La mer restant un centile, la part de terres ne change pas : seule leur répartition.
public static class MapShape
{
    // Bosse elliptique : 1 au centre, 0 au-delà des rayons, adoucie.
    // wrapWidth > 0 : l'écart en x prend le plus court, à travers la couture (monde qui fait le tour).
    public static float Bump(float x, float y, Vector2 centre, Vector2 radius, float wrapWidth = 0f)
    {
        float dx = x - centre.x;
        if (wrapWidth > 0f) dx -= Mathf.Round(dx / wrapWidth) * wrapWidth;
        float dy = y - centre.y;
        float d = Mathf.Sqrt(dx * dx / (radius.x * radius.x) + dy * dy / (radius.y * radius.y));
        return 1f - Mathf.SmoothStep(0f, 1f, d);
    }

    // Masque par case, ou null sans masque (Fractal ; Continents et Archipel viendront ensuite).
    public static float[,] Mask(MapType type, int width, int height, int polarRows, bool wrapX)
    {
        if (type != MapType.Pangaea) return null;
        var mask = new float[width, height];
        float playable = height - 2 * polarRows;
        var centre = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        // Assez étroite pour laisser de l'océan tout autour, couture comprise (0,45 < 0,5 de la largeur).
        var radius = new Vector2(width * 0.45f, playable * 0.4f);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                mask[x, y] = Bump(x, y, centre, radius, wrapX ? width : 0f);
        return mask;
    }
}
