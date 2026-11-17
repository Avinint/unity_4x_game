using System;
using System.Collections.Generic;
using UnityEngine;

public enum Relief { Water, Flat, Hill, Mountain }

public enum Biome { Ocean, Coast, Grassland, Plain, Desert, Forest, Taiga, Jungle, Tundra, Snow, Marsh, River }   // nouveaux biomes à la fin (numéros enregistrés)

// Seuils de génération (réglables dans l'Inspector de MapGenerator).
[Serializable]
public class MapRulesSettings
{
    [Range(0f, 1f)] public float WaterFraction = 0.65f;       // part de la carte sous l'eau (continents)
    [Range(0f, 1f)] public float HillFraction = 0.25f;        // part des terres en colline
    [Range(0f, 1f)] public float MountainFraction = 0.10f;    // part des terres en montagne
    [Range(0f, 1f)] public float LatitudeWeight = 0.8f;       // part de la latitude dans la température (le reste : bruit)
    [Range(0f, 1f)] public float AltitudeCooling = 0.2f;      // refroidissement au sommet des terres

    [Header("Climat (proportions des terres)")]
    [Range(0f, 1f)] public float FrozenFraction = 0.08f;      // les plus froides : neige
    [Range(0f, 1f)] public float TundraFraction = 0.12f;      // ensuite : toundra
    [Range(0f, 1f)] public float ColdFraction = 0.15f;        // ensuite : taïga (toundra si sec)
    [Range(0f, 1f)] public float HotFraction = 0.25f;         // les plus chaudes : désert / plaine / jungle
    [Range(0f, 1f)] public float DryFraction = 0.25f;         // les plus sèches
    [Range(0f, 1f)] public float WetFraction = 0.25f;         // les plus humides
    [Range(0f, 1f)] public float MarshFraction = 0.08f;       // les plus humides de toutes : marais possible

    // Seuils absolus, calculés à chaque génération par MapRules.WithClimateThresholds.
    [HideInInspector] public float FrozenBelow = 0.15f, TundraBelow = 0.25f, ColdBelow = 0.35f, HotAbove = 0.7f;
    [HideInInspector] public float DryBelow = 0.33f, WetAbove = 0.66f, MarshAbove = 0.8f;
    [Range(0f, 1f)] public float LowlandFraction = 0.2f;     // terres plates les plus basses où un marais est possible

    [Header("Fleuves (proportions des terres, par débit)")]
    [Range(0f, 1f)] public float RiverFraction = 0.15f;       // part des sommets de terre d'où part une rivière (plus forts débits)
    [Range(0f, 1f)] public float NavigableFraction = 0.03f;   // plus forts débits des cases : fleuve navigable
    [Min(1)] public int RiverMinLength = 3;                    // rivières plus courtes (en arêtes) supprimées

    public MapRulesSettings Clone() => (MapRulesSettings)MemberwiseClone();
}

// Règles pures : altitude, température, humidité → relief et biome.
public static class MapRules
{
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
        return Mathf.Clamp01(t - s.AltitudeCooling * Mathf.Clamp01(altitudeAboveSea));
    }

    // Biome d'une terre (le relief eau est traité à part : Ocean / Coast).
    public static Biome LandBiome(float temperature, float humidity, Relief relief, bool lowland, MapRulesSettings s)
    {
        bool cold = temperature < s.ColdBelow;
        if (temperature < s.FrozenBelow) return Biome.Snow;
        if (temperature < s.TundraBelow) return Biome.Tundra;
        if (relief == Relief.Flat && lowland && humidity > s.MarshAbove && !cold) return Biome.Marsh;
        bool dry = humidity < s.DryBelow, wet = humidity > s.WetAbove;
        if (cold) return dry ? Biome.Tundra : Biome.Taiga;
        if (temperature > s.HotAbove) return dry ? Biome.Desert : wet ? Biome.Jungle : Biome.Plain;
        return dry ? Biome.Plain : wet ? Biome.Forest : Biome.Grassland;
    }

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
    public static bool BlocksSight(Biome biome) =>
        biome == Biome.Forest || biome == Biome.Taiga || biome == Biome.Jungle;

    // Hauteur d'une case comme obstacle à la vue.
    public static int ObstacleHeight(Relief relief, Biome biome) =>
        SightHeight(relief) + (BlocksSight(biome) ? 1 : 0);

    public static Biome WaterBiome(bool touchesLand) => touchesLand ? Biome.Coast : Biome.Ocean;

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
