using System.Collections.Generic;
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

    // Masque par case, ou null sans masque (Fractal). `rng` place les continents et les îles ;
    // `landFraction` donne la taille des îles.
    public static float[,] Mask(MapType type, int width, int height, int polarRows, bool wrapX, System.Random rng = null,
                                float landFraction = 0.35f)
    {
        float playable = height - 2 * polarRows;
        var bumps = new List<(Vector2 centre, Vector2 radius)>();
        if (type == MapType.Pangaea)
            // Assez étroite pour laisser de l'océan tout autour, couture comprise (0,45 < 0,5 de la largeur).
            bumps.Add((new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f), new Vector2(width * 0.45f, playable * 0.4f)));
        else if (type == MapType.Continents)
        {
            float spacing = (float)width / ContinentCount(width);
            foreach (Vector2 c in ContinentCentres(width, height, polarRows, wrapX, rng ?? new System.Random(0)))
                // Rayon de 35 % de l'intervalle : 30 % d'océan entre deux continents voisins. À 40 %, les bosses
                // couvraient plus que la part des terres : la mer tombait à leur bord et le bruit reliait les continents.
                bumps.Add((c, new Vector2(spacing * 0.35f, playable * 0.4f)));
        }
        else if (type == MapType.Archipelago)
            bumps.AddRange(IslandBumps(width, height, polarRows, wrapX, landFraction, rng ?? new System.Random(0)));
        if (bumps.Count == 0) return null;

        var mask = new float[width, height];
        float wrapWidth = wrapX ? width : 0f;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                foreach (var (centre, radius) in bumps)
                    mask[x, y] = Mathf.Max(mask[x, y], Bump(x, y, centre, radius, wrapWidth));
        return mask;
    }

    // Colonnes du chenal d'océan, à mi-chemin entre deux continents voisins (couture comprise en monde qui
    // boucle), sur 2 × halfWidth + 1 colonnes. Le masque seul laissait parfois le bruit relier les continents.
    public static bool[] OceanChannels(int width, List<Vector2> centres, bool wrapX, int halfWidth = 1)
    {
        var channels = new bool[width];
        var xs = new List<float>();
        foreach (Vector2 c in centres) xs.Add(c.x);
        xs.Sort();
        int pairs = wrapX ? xs.Count : xs.Count - 1;
        for (int i = 0; i < pairs; i++)
        {
            float a = xs[i], b = i + 1 < xs.Count ? xs[i + 1] : xs[0] + width;
            int mid = Mathf.RoundToInt((a + b) * 0.5f);
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                int x = mid + dx;
                if (wrapX) x = (x % width + width) % width;
                if (x >= 0 && x < width) channels[x] = true;
            }
        }
        return channels;
    }

    // Archipel : une île pour 80 cases jouables, au moins 4.
    public static int IslandCount(int width, int height, int polarRows) =>
        Mathf.Max(4, Mathf.RoundToInt(width * (height - 2 * polarRows) / 80f));

    // Îles de tailles variées (0,7 à 1,3 fois le rayon moyen), placées au hasard dans la bande jouable,
    // chacune à au moins deux rayons moyens des autres si un des 50 essais le permet (sinon le plus éloigné).
    // Rayon moyen : les îles couvrent à peu près la part des terres. Un archipel de petites îles d'une case
    // (bruit plus fin) ne laissait aucune masse assez grande pour un départ.
    public static List<(Vector2 centre, Vector2 radius)> IslandBumps(int width, int height, int polarRows, bool wrapX,
                                                                      float landFraction, System.Random rng)
    {
        int count = IslandCount(width, height, polarRows);
        float playable = height - 2 * polarRows;
        float rx = Mathf.Sqrt(width * playable * landFraction / (count * Mathf.PI));
        float ry = rx * RowsPerColumn;
        float wrapWidth = wrapX ? width : 0f;
        var islands = new List<(Vector2 centre, Vector2 radius)>();
        for (int i = 0; i < count; i++)
        {
            Vector2 best = default;
            float bestGap = -1f;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                // Bords fixes : une demi-île de marge à l'ouest et à l'est ; toujours au nord et au sud.
                float x = wrapX ? (float)rng.NextDouble() * width
                                : rx * 0.5f + (float)rng.NextDouble() * Mathf.Max(0f, width - 1 - rx);
                float y = polarRows + ry * 0.5f + (float)rng.NextDouble() * Mathf.Max(0f, playable - 1 - ry);
                var p = new Vector2(x, y);
                float gap = float.MaxValue;
                foreach (var (c, _) in islands) gap = Mathf.Min(gap, Gap(p, c, wrapWidth));
                if (gap > bestGap) { best = p; bestGap = gap; }
                if (gap >= 2f * rx) break;
            }
            float scale = 0.7f + 0.6f * (float)rng.NextDouble();
            islands.Add((best, new Vector2(rx * scale, ry * scale)));
        }
        return islands;
    }

    // Une colonne fait 1,73 de large, une rangée 1,5 de haut : un rayon de n colonnes vaut ~1,15 n rangées.
    private const float RowsPerColumn = 1.15f;

    // Écart entre deux centres, en colonnes (y ramené à l'échelle de x), à travers la couture si wrapWidth > 0.
    public static float Gap(Vector2 a, Vector2 b, float wrapWidth = 0f)
    {
        float dx = a.x - b.x;
        if (wrapWidth > 0f) dx -= Mathf.Round(dx / wrapWidth) * wrapWidth;
        float dy = (a.y - b.y) / RowsPerColumn;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // 2 continents, 3 sur une carte large.
    public static int ContinentCount(int width) => width >= 60 ? 3 : 2;

    // Centres répartis en x à intervalles réguliers, un peu décalés au hasard, et vers le milieu en y.
    // Bords fixes : le premier à un demi-intervalle du bord ouest (océan aux deux bords) ; monde qui boucle :
    // départ au hasard, l'intervalle se referme à travers la couture.
    public static List<Vector2> ContinentCentres(int width, int height, int polarRows, bool wrapX, System.Random rng)
    {
        int count = ContinentCount(width);
        float spacing = (float)width / count, playable = height - 2 * polarRows;
        // −0,5 : les colonnes vont de 0 à width − 1, la carte est centrée sur (width − 1) / 2.
        float start = wrapX ? (float)rng.NextDouble() * spacing : spacing * 0.5f - 0.5f;
        var centres = new List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            // ± 5 % de l'intervalle : avec un rayon de 35 %, l'océan reste garanti aux bords et entre les continents.
            float jitterX = ((float)rng.NextDouble() - 0.5f) * 0.1f * spacing;
            float jitterY = ((float)rng.NextDouble() - 0.5f) * 0.2f * playable;    // ± 10 % de la hauteur jouable
            float x = start + i * spacing + jitterX;
            if (wrapX) x = Mathf.Repeat(x, width);
            centres.Add(new Vector2(x, (height - 1) * 0.5f + jitterY));
        }
        return centres;
    }
}
