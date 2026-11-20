using System;
using System.Collections.Generic;
using UnityEngine;

// NavigableRiver : fleuve navigable, eau pour le déplacement, la case garde son biome (rives, rendements).
public enum Relief { Water, Flat, Hill, Mountain, NavigableRiver }

// Sol de la case (comme Civ 7) ; la végétation, le marais et la neige sont des caractéristiques.
// Nouveaux biomes à la fin : la liste BiomeTerrains de la scène est enregistrée par numéro.
public enum Biome { Ocean, Coast, Lake, Tundra, Grassland, Plains, Desert, Tropical }

// Caractéristique posée sur le sol : végétation (Forest, Taiga, Rainforest), zone humide (Marsh), neige (Snow),
// banquise (Ice, sur l'eau). Nouvelles valeurs à la fin (liste FeatureTerrains de la scène).
public enum Feature { None, Forest, Taiga, Rainforest, Marsh, Snow, Ice }

// Seuils de génération (réglables dans l'Inspector de MapGenerator).
[Serializable]
public class MapRulesSettings
{
    [Range(0f, 1f)] public float WaterFraction = 0.55f;       // part de la carte sous l'eau (continents)
    [Range(0f, 1f)] public float HillFraction = 0.25f;        // part des terres en colline
    [Range(0f, 1f)] public float MountainFraction = 0.08f;    // part des terres en montagne
    [Range(0f, 1f)] public float RidgeWeight = 0.7f;         // part des crêtes dans l'altitude des terres : chaînes plutôt que blocs
    [Range(0f, 1f)] public float LatitudeWeight = 0.8f;       // part de la latitude dans la température (le reste : bruit)
    [Range(0f, 1f)] public float AltitudeCooling = 0.2f;      // refroidissement au sommet des terres

    [Header("Climat (proportions des terres)")]
    [Range(0f, 1f)] public float FrozenFraction = 0.05f;      // les plus froides : neige
    [Range(0f, 1f)] public float TundraFraction = 0.08f;      // ensuite : toundra
    [Range(0f, 1f)] public float ColdFraction = 0.12f;        // ensuite : taïga (toundra si sec)
    [Range(0f, 1f)] public float HotFraction = 0.20f;         // les plus chaudes : désert / plaine / jungle
    [Range(0f, 1f)] public float DryFraction = 0.25f;         // les plus sèches
    [Range(0f, 1f)] public float WetFraction = 0.25f;         // les plus humides
    [Range(0f, 1f)] public float MarshFraction = 0.08f;       // les plus humides de toutes : marais possible
    [Range(0f, 1f)] public float IceEdgeFraction = 0.5f;      // part de la mer qui gèle dans la rangée qui touche la banquise

    // Seuils absolus, calculés à chaque génération par MapRules.WithClimateThresholds.
    [HideInInspector] public float FrozenBelow = 0.15f, TundraBelow = 0.25f, ColdBelow = 0.35f, HotAbove = 0.7f;
    [HideInInspector] public float DryBelow = 0.33f, WetAbove = 0.66f, MarshAbove = 0.8f;
    [Range(0f, 1f)] public float LowlandFraction = 0.2f;     // terres plates les plus basses où un marais est possible

    [Header("Fleuves (proportions des terres, par débit)")]
    [Range(0f, 1f)] public float RiverFraction = 0.15f;       // part des sommets de terre d'où part une rivière (plus forts débits)
    [Range(0f, 1f)] public float NavigableFraction = 0.10f;  // plus forts débits des cases : fleuve navigable
    [Min(1)] public int RiverMinLength = 3;                    // rivières plus courtes (en arêtes) supprimées
    [Min(1)] public int NavigableMinLength = 3;                // fleuves navigables plus courts (en cases) rendus à la terre
    [Min(1)] public int TributaryMinLength = 2;                // affluents navigables plus courts (en cases) rendus à la terre
    [Min(0)] public int LakeMaxSize = 6;                       // étendues d'eau de cette taille au plus (en cases) : lacs d'eau douce

    public MapRulesSettings Clone() => (MapRulesSettings)MemberwiseClone();
}

// Règles pures : altitude, température, humidité → relief et biome.
public static class MapRules
{
    // Chaînes de montagnes : sur les terres, l'altitude est mêlée à des crêtes (1 − |2n − 1| : fortes le long
    // des lignes où le bruit vaut 0,5), donc les plus hauts points suivent des lignes et non des taches rondes.
    // L'eau ne change pas ; une terre reste entre le niveau de la mer et le sommet d'origine (côtes intactes).
    public static float[,] AddRidges(float[,] altitude, float[,] noise, float sea, float weight)
    {
        int w = altitude.GetLength(0), h = altitude.GetLength(1);
        float top = sea;
        foreach (float a in altitude) top = Mathf.Max(top, a);

        var result = new float[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = altitude[x, y];
                if (a < sea) { result[x, y] = a; continue; }
                float ridge = 1f - Mathf.Abs(2f * noise[x, y] - 1f);
                result[x, y] = Mathf.Lerp(sea, top, Mathf.Lerp(Mathf.InverseLerp(sea, top, a), ridge, weight));
            }
        return result;
    }

    // Valeur sous laquelle se trouve `fraction` des valeurs (ex. 0.65 → niveau de la mer pour 65 % d'eau).
    public static float Percentile(float[,] values, float fraction)
    {
        var list = new List<float>(values.Length);
        foreach (float v in values) list.Add(v);
        return Percentile(list, fraction);
    }

    public static float Percentile(List<float> values, float fraction)
    {
        var sorted = new List<float>(values);
        if (sorted.Count == 0) return 0f;
        sorted.Sort();
        // Petite tolérance : 0.08 + 0.12 vaut 0.19999… en flottant, il ne faut pas perdre une case à l'arrondi.
        int index = Mathf.Clamp(Mathf.FloorToInt(fraction * sorted.Count + 1e-4f), 0, sorted.Count - 1);
        return sorted[index];
    }

    // Rangées de banquise ajoutées à chaque pôle.
    public static bool IsPolar(int y, int height, int rows) => y < rows || y >= height - rows;

    // Rangée qui touche la banquise : elle y gèle par endroits (limite irrégulière).
    public static bool IsIceEdge(int y, int height, int rows) =>
        rows > 0 && (y == rows || y == height - 1 - rows);

    public static Relief ReliefAt(float altitude, float seaLevel, float hillLevel, float mountainLevel) =>
        altitude < seaLevel ? Relief.Water
        : altitude >= mountainLevel ? Relief.Mountain
        : altitude >= hillLevel ? Relief.Hill
        : Relief.Flat;

    // latitude : 0 au pôle sud, 1 au pôle nord ; altitudeAboveSea : 0 au rivage, 1 au plus haut sommet.
    public static float Temperature(float latitude, float noise, float altitudeAboveSea, MapRulesSettings s)
    {
        float equator = 1f - Mathf.Abs(2f * latitude - 1f);          // 1 à l'équateur, 0 aux pôles
        float t = Mathf.Lerp(noise, equator, s.LatitudeWeight);
        // Pas de borne : les seuils de climat sont des centiles ; borner à 0 créait des ex aequo aux pôles
        // (plus de 5 % des terres à 0) et plus aucune case n'était sous le seuil de la neige.
        return t - s.AltitudeCooling * Mathf.Clamp01(altitudeAboveSea);
    }

    // Sol et caractéristique d'une terre (l'eau est traitée à part : Ocean / Coast / Lake).
    public static (Biome biome, Feature feature) LandBiome(float temperature, float humidity, Relief relief, bool lowland, MapRulesSettings s)
    {
        bool cold = temperature < s.ColdBelow;
        if (temperature < s.FrozenBelow) return (Biome.Tundra, Feature.Snow);
        if (temperature < s.TundraBelow) return (Biome.Tundra, Feature.None);
        if (relief == Relief.Flat && lowland && humidity > s.MarshAbove && !cold) return (Biome.Grassland, Feature.Marsh);
        bool dry = humidity < s.DryBelow, wet = humidity > s.WetAbove;
        if (cold) return (Biome.Tundra, dry ? Feature.None : Feature.Taiga);
        if (temperature > s.HotAbove)
            return dry ? (Biome.Desert, Feature.None) : (Biome.Tropical, wet ? Feature.Rainforest : Feature.None);
        return dry ? (Biome.Plains, Feature.None) : (Biome.Grassland, wet ? Feature.Forest : Feature.None);
    }

    // Eau pour le déplacement et l'hydrologie (mer, côte, lac, fleuve navigable).
    public static bool IsWater(Relief relief) => relief == Relief.Water || relief == Relief.NavigableRiver;

    // Praticable à pied : plat ou accidenté (ni eau, ni fleuve, ni montagne).
    public static bool IsPassable(Relief relief) => relief == Relief.Flat || relief == Relief.Hill;

    // Coût pour entrer dans une case praticable : 1, +1 pour une caractéristique (végétation, marais, neige),
    // +1 en relief accidenté. Mêmes valeurs qu'avant (forêt 2, neige 2, forêt accidentée 3).
    public static int MovementCost(Relief relief, Feature feature) =>
        1 + (feature != Feature.None ? 1 : 0) + (relief == Relief.Hill ? 1 : 0);

    // Seuils absolus tirés des proportions, sur les températures et humidités des terres. Ne modifie pas `s`.
    public static MapRulesSettings WithClimateThresholds(MapRulesSettings s, List<float> temperatures, List<float> humidities)
    {
        var r = s.Clone();
        r.FrozenBelow = Percentile(temperatures, s.FrozenFraction);
        r.TundraBelow = Percentile(temperatures, s.FrozenFraction + s.TundraFraction);
        r.ColdBelow = Percentile(temperatures, s.FrozenFraction + s.TundraFraction + s.ColdFraction);
        r.HotAbove = Percentile(temperatures, 1f - s.HotFraction);
        r.DryBelow = Percentile(humidities, s.DryFraction);
        r.WetAbove = Percentile(humidities, 1f - s.WetFraction);
        r.MarshAbove = Percentile(humidities, 1f - s.MarshFraction);
        return r;
    }

    public static float Elevation(Relief relief, float hillHeight, float mountainHeight) =>
        relief == Relief.Hill ? hillHeight : relief == Relief.Mountain ? mountainHeight : 0f;

    // Ligne de vue : hauteur du relief (plat et eau 0, colline 2, montagne 4 ; la végétation dense ajoute 1).
    public static int SightHeight(Relief relief) =>
        relief == Relief.Hill ? 2 : relief == Relief.Mountain ? 4 : 0;

    // Végétation dense : cache ce qui est derrière.
    public static bool BlocksSight(Feature feature) =>
        feature == Feature.Forest || feature == Feature.Taiga || feature == Feature.Rainforest;

    // Hauteur d'une case comme obstacle à la vue.
    public static int ObstacleHeight(Relief relief, Feature feature) =>
        SightHeight(relief) + (BlocksSight(feature) ? 1 : 0);

    public static Biome WaterBiome(bool touchesLand) => touchesLand ? Biome.Coast : Biome.Ocean;

    // Lacs : étendues d'eau (cases d'eau voisines entre elles) de maxSize cases au plus.
    // Évite les « mers » d'une case au milieu des terres ; voisins fournis par l'appelant.
    public static bool[,] LakeMask(Relief[,] relief, Func<int, int, IEnumerable<Vector2Int>> neighbours, int maxSize)
    {
        int w = relief.GetLength(0), h = relief.GetLength(1);
        var lake = new bool[w, h];
        var seen = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (seen[x, y] || relief[x, y] != Relief.Water) continue;
                // Parcours de l'étendue d'eau qui contient (x, y).
                var body = new List<Vector2Int>();
                var stack = new Stack<Vector2Int>();
                stack.Push(new Vector2Int(x, y));
                seen[x, y] = true;
                while (stack.Count > 0)
                {
                    var c = stack.Pop();
                    body.Add(c);
                    foreach (var n in neighbours(c.x, c.y))
                    {
                        if (n.x < 0 || n.x >= w || n.y < 0 || n.y >= h) continue;
                        if (seen[n.x, n.y] || relief[n.x, n.y] != Relief.Water) continue;
                        seen[n.x, n.y] = true;
                        stack.Push(n);
                    }
                }
                if (body.Count <= maxSize)
                    foreach (var c in body) lake[c.x, c.y] = true;
            }
        return lake;
    }

    // Cases d'eau qui touchent au moins une terre (voisins fournis par l'appelant, hors carte ignorés).
    public static bool[,] CoastMask(Relief[,] relief, Func<int, int, IEnumerable<Vector2Int>> neighbours)
    {
        int w = relief.GetLength(0), h = relief.GetLength(1);
        var coast = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (relief[x, y] != Relief.Water) continue;
                foreach (var n in neighbours(x, y))
                    if (n.x >= 0 && n.x < w && n.y >= 0 && n.y < h && relief[n.x, n.y] != Relief.Water)
                    {
                        coast[x, y] = true;
                        break;
                    }
            }
        return coast;
    }
}
