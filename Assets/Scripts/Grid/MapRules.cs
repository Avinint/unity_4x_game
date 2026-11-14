using System;
using System.Collections.Generic;
using UnityEngine;

public enum Relief { Water, Flat, Hill, Mountain }

public enum Biome { Ocean, Coast, Grassland, Plain, Desert, Forest, Taiga, Jungle, Tundra, Snow, Marsh }

// Seuils de génération (réglables dans l'Inspector de MapGenerator).
[Serializable]
public class MapRulesSettings
{
    [Range(0f, 1f)] public float WaterFraction = 0.65f;       // part de la carte sous l'eau (continents)
    [Range(0f, 1f)] public float HillFraction = 0.25f;        // part des terres en colline
    [Range(0f, 1f)] public float MountainFraction = 0.10f;    // part des terres en montagne
    [Range(0f, 1f)] public float LatitudeWeight = 0.8f;       // part de la latitude dans la température (le reste : bruit)
    [Range(0f, 1f)] public float AltitudeCooling = 0.5f;      // refroidissement au sommet des terres
    public float FrozenBelow = 0.15f, ColdBelow = 0.35f, HotAbove = 0.7f;   // seuils de température
    public float DryBelow = 0.33f, WetAbove = 0.66f;                        // seuils d'humidité
    public float MarshAbove = 0.8f;                                         // humidité d'un marais
    [Range(0f, 1f)] public float LowlandFraction = 0.2f;     // terres plates les plus basses où un marais est possible
}

// Règles pures : altitude, température, humidité → relief et biome.
public static class MapRules
{
    // Valeur sous laquelle se trouve `fraction` des valeurs (ex. 0.65 → niveau de la mer pour 65 % d'eau).
    public static float Percentile(float[,] values, float fraction)
    {
        var sorted = new List<float>(values.Length);
        foreach (float v in values) sorted.Add(v);
        if (sorted.Count == 0) return 0f;
        sorted.Sort();
        int index = Mathf.Clamp(Mathf.FloorToInt(fraction * sorted.Count), 0, sorted.Count - 1);
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
        if (relief == Relief.Flat && lowland && humidity > s.MarshAbove && !cold) return Biome.Marsh;
        bool dry = humidity < s.DryBelow, wet = humidity > s.WetAbove;
        if (cold) return dry ? Biome.Tundra : Biome.Taiga;
        if (temperature > s.HotAbove) return dry ? Biome.Desert : wet ? Biome.Jungle : Biome.Plain;
        return dry ? Biome.Plain : wet ? Biome.Forest : Biome.Grassland;
    }

    public static Biome WaterBiome(bool touchesLand) => touchesLand ? Biome.Coast : Biome.Ocean;
}
