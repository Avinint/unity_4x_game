using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapRulesTests
{
    private static readonly MapRulesSettings S = new MapRulesSettings();

    // Températures et humidités choisies au milieu de chaque tranche des réglages par défaut.
    private const float Frozen = 0.05f, VeryCold = 0.2f, Cold = 0.3f, Temperate = 0.5f, Hot = 0.85f;
    private const float Dry = 0.1f, Medium = 0.5f, Wet = 0.75f, Soaked = 0.95f;

    [Test]
    public void Percentile_ReturnsValueBelowWhichFractionLies()
    {
        var values = new float[10, 10];
        for (int i = 0; i < 100; i++) values[i % 10, i / 10] = i;

        float level = MapRules.Percentile(values, 0.65f);

        int below = 0;
        foreach (float v in values) if (v < level) below++;
        Assert.AreEqual(65, below);
    }

    [Test]
    public void ReliefAt_Thresholds()
    {
        Assert.AreEqual(Relief.Water, MapRules.ReliefAt(0.2f, 0.5f, 0.7f, 0.9f));
        Assert.AreEqual(Relief.Flat, MapRules.ReliefAt(0.5f, 0.5f, 0.7f, 0.9f));
        Assert.AreEqual(Relief.Hill, MapRules.ReliefAt(0.7f, 0.5f, 0.7f, 0.9f));
        Assert.AreEqual(Relief.Mountain, MapRules.ReliefAt(0.95f, 0.5f, 0.7f, 0.9f));
    }

    [Test]
    public void Temperature_PolesColdEquatorHot()
    {
        float pole = MapRules.Temperature(0f, 0.5f, 0f, S);
        float equator = MapRules.Temperature(0.5f, 0.5f, 0f, S);
        float northPole = MapRules.Temperature(1f, 0.5f, 0f, S);

        Assert.Less(pole, S.FrozenBelow);
        Assert.Less(northPole, S.FrozenBelow);
        Assert.Greater(equator, S.HotAbove);
    }

    [Test]
    public void Temperature_CoolsWithAltitude()
    {
        float shore = MapRules.Temperature(0.5f, 0.5f, 0f, S);
        float summit = MapRules.Temperature(0.5f, 0.5f, 1f, S);

        Assert.Less(summit, shore);
    }

    [TestCase(Frozen, Dry, Biome.Snow)]
    [TestCase(Frozen, Medium, Biome.Snow)]
    [TestCase(Frozen, Wet, Biome.Snow)]
    [TestCase(VeryCold, Dry, Biome.Tundra)]
    [TestCase(VeryCold, Medium, Biome.Tundra)]
    [TestCase(VeryCold, Wet, Biome.Tundra)]
    [TestCase(Cold, Dry, Biome.Tundra)]
    [TestCase(Cold, Wet, Biome.Taiga)]
    [TestCase(Temperate, Dry, Biome.Plain)]
    [TestCase(Temperate, Medium, Biome.Grassland)]
    [TestCase(Temperate, Wet, Biome.Forest)]
    [TestCase(Hot, Dry, Biome.Desert)]
    [TestCase(Hot, Medium, Biome.Plain)]
    [TestCase(Hot, Wet, Biome.Jungle)]
    public void LandBiome_WhittakerTable(float temperature, float humidity, Biome expected)
    {
        Assert.AreEqual(expected, MapRules.LandBiome(temperature, humidity, Relief.Flat, false, S));
    }

    [Test]
    public void LandBiome_ColdMediumHumidity_IsTaiga()
    {
        Assert.AreEqual(Biome.Taiga, MapRules.LandBiome(Cold, Medium, Relief.Flat, false, S));
    }

    [Test]
    public void LandBiome_Marsh_OnlyFlatLowWetNotCold()
    {
        Assert.AreEqual(Biome.Marsh, MapRules.LandBiome(Temperate, Soaked, Relief.Flat, true, S));
        Assert.AreNotEqual(Biome.Marsh, MapRules.LandBiome(Temperate, Soaked, Relief.Hill, true, S));    // pas plat
        Assert.AreNotEqual(Biome.Marsh, MapRules.LandBiome(Temperate, Soaked, Relief.Flat, false, S));   // pas bas
        Assert.AreNotEqual(Biome.Marsh, MapRules.LandBiome(Temperate, Wet, Relief.Flat, true, S));       // pas assez humide
        Assert.AreNotEqual(Biome.Marsh, MapRules.LandBiome(Cold, Soaked, Relief.Flat, true, S));         // trop froid
    }

    [Test]
    public void Percentile_List_SameAsArray()
    {
        var array = new float[4, 5];
        var list = new System.Collections.Generic.List<float>();
        for (int i = 0; i < 20; i++) { array[i % 4, i / 4] = (i * 7) % 20; list.Add((i * 7) % 20); }

        Assert.AreEqual(MapRules.Percentile(array, 0.3f), MapRules.Percentile(list, 0.3f));
    }

    [Test]
    public void CoastMask_WaterNextToLandIsCoast()
    {
        // 3 × 3 d'eau, une terre au centre-gauche ; voisins : haut, bas, gauche, droite.
        var relief = new Relief[3, 3];
        relief[0, 1] = Relief.Flat;
        System.Collections.Generic.IEnumerable<UnityEngine.Vector2Int> Four(int x, int y)
        {
            yield return new UnityEngine.Vector2Int(x + 1, y);
            yield return new UnityEngine.Vector2Int(x - 1, y);
            yield return new UnityEngine.Vector2Int(x, y + 1);
            yield return new UnityEngine.Vector2Int(x, y - 1);
        }

        bool[,] coast = MapRules.CoastMask(relief, Four);

        Assert.IsTrue(coast[1, 1]);    // à droite de la terre
        Assert.IsTrue(coast[0, 0]);    // en dessous
        Assert.IsTrue(coast[0, 2]);    // au-dessus
        Assert.IsFalse(coast[2, 1]);   // trop loin
        Assert.IsFalse(coast[0, 1]);   // la terre n'est pas une côte
    }

    private static System.Collections.Generic.List<float> Uniform100()
    {
        var list = new System.Collections.Generic.List<float>();
        for (int i = 0; i < 100; i++) list.Add(i / 100f);
        return list;
    }

    private static int CountBelow(System.Collections.Generic.List<float> values, float level)
    {
        int n = 0;
        foreach (float v in values) if (v < level) n++;
        return n;
    }

    [Test]
    public void WithClimateThresholds_SplitsByFractions()
    {
        var values = Uniform100();
        var s = new MapRulesSettings
        {
            FrozenFraction = 0.08f, TundraFraction = 0.12f, ColdFraction = 0.15f, HotFraction = 0.25f,
            DryFraction = 0.25f, WetFraction = 0.25f, MarshFraction = 0.08f
        };

        var r = MapRules.WithClimateThresholds(s, values, values);

        Assert.AreEqual(8, CountBelow(values, r.FrozenBelow));
        Assert.AreEqual(20, CountBelow(values, r.TundraBelow));
        Assert.AreEqual(35, CountBelow(values, r.ColdBelow));
        Assert.AreEqual(75, CountBelow(values, r.HotAbove));    // 25 % au-dessus
        Assert.AreEqual(25, CountBelow(values, r.DryBelow));
        Assert.AreEqual(75, CountBelow(values, r.WetAbove));
        Assert.AreEqual(92, CountBelow(values, r.MarshAbove));
    }

    [Test]
    public void WithClimateThresholds_DoesNotModifyOriginal()
    {
        var original = new MapRulesSettings();
        float frozen = original.FrozenBelow;

        var r = MapRules.WithClimateThresholds(original, Uniform100(), Uniform100());

        Assert.AreNotSame(original, r);
        Assert.AreEqual(frozen, original.FrozenBelow);
    }

    [Test]
    public void Elevation_ByRelief()
    {
        Assert.AreEqual(0f, MapRules.Elevation(Relief.Water, 1.5f, 4f));
        Assert.AreEqual(0f, MapRules.Elevation(Relief.Flat, 1.5f, 4f));
        Assert.AreEqual(1.5f, MapRules.Elevation(Relief.Hill, 1.5f, 4f));
        Assert.AreEqual(4f, MapRules.Elevation(Relief.Mountain, 1.5f, 4f));
    }

    [Test]
    public void WaterBiome_CoastWhenTouchingLand()
    {
        Assert.AreEqual(Biome.Coast, MapRules.WaterBiome(true));
        Assert.AreEqual(Biome.Ocean, MapRules.WaterBiome(false));
    }

    [Test]
    public void ObstacleHeight_ReliefPlusDenseVegetation()
    {
        Assert.AreEqual(0, MapRules.ObstacleHeight(Relief.Flat, Biome.Plain));
        Assert.AreEqual(1, MapRules.ObstacleHeight(Relief.Flat, Biome.Forest));
        Assert.AreEqual(2, MapRules.ObstacleHeight(Relief.Hill, Biome.Plain));
        Assert.AreEqual(3, MapRules.ObstacleHeight(Relief.Hill, Biome.Taiga));
        Assert.AreEqual(4, MapRules.ObstacleHeight(Relief.Mountain, Biome.Plain));
    }

    [Test]
    public void AddRidges_WaterUnchanged_LandStaysAboveSea()
    {
        var altitude = new float[,] { { 0.1f }, { 0.6f }, { 1f } };
        var noise = new float[,] { { 0.5f }, { 0f }, { 0f } };
        var result = MapRules.AddRidges(altitude, noise, 0.5f, 1f);

        Assert.AreEqual(0.1f, result[0, 0]);               // eau
        Assert.GreaterOrEqual(result[1, 0], 0.5f);         // terre sans crête : au niveau de la mer, pas dessous
        Assert.GreaterOrEqual(result[2, 0], 0.5f);
    }

    [Test]
    public void AddRidges_WeightZero_Unchanged()
    {
        var altitude = new float[,] { { 0.2f }, { 0.7f }, { 0.9f } };
        var result = MapRules.AddRidges(altitude, new float[,] { { 0.5f }, { 0.5f }, { 0.1f } }, 0.5f, 0f);

        Assert.AreEqual(0.7f, result[1, 0], 1e-5f);
        Assert.AreEqual(0.9f, result[2, 0], 1e-5f);
    }

    [Test]
    public void AddRidges_RidgeLineRaisesLand()
    {
        // Deux terres de même altitude : celle sur la ligne de crête (bruit 0,5) monte au-dessus de l'autre.
        var altitude = new float[,] { { 0.6f }, { 0.6f }, { 1f } };
        var noise = new float[,] { { 0.5f }, { 0f }, { 0f } };
        var result = MapRules.AddRidges(altitude, noise, 0.5f, 0.5f);

        Assert.Greater(result[0, 0], result[1, 0]);
    }

    // Carte d'une seule rangée : voisins = gauche et droite.
    private static IEnumerable<Vector2Int> Line(int x, int y)
    {
        yield return new Vector2Int(x - 1, y);
        yield return new Vector2Int(x + 1, y);
    }

    private static Relief[,] Row(params Relief[] v)
    {
        var a = new Relief[v.Length, 1];
        for (int i = 0; i < v.Length; i++) a[i, 0] = v[i];
        return a;
    }

    [Test]
    public void LakeMask_SmallWaterBody_IsLake()
    {
        var lake = MapRules.LakeMask(Row(Relief.Flat, Relief.Water, Relief.Flat), Line, 6);

        Assert.IsTrue(lake[1, 0]);
        Assert.IsFalse(lake[0, 0]);   // terre
    }

    [Test]
    public void LakeMask_LargeWaterBody_IsSea()
    {
        var w = Relief.Water;
        var lake = MapRules.LakeMask(Row(Relief.Flat, w, w, w, Relief.Flat), Line, 2);

        Assert.IsFalse(lake[1, 0] || lake[2, 0] || lake[3, 0]);
    }

    [Test]
    public void LakeMask_EachBodyMeasuredSeparately()
    {
        // Une case d'eau isolée (lac) et une étendue de 3 cases (mer, max 2).
        var w = Relief.Water;
        var lake = MapRules.LakeMask(Row(w, Relief.Flat, w, w, w), Line, 2);

        Assert.IsTrue(lake[0, 0]);
        Assert.IsFalse(lake[2, 0]);
    }
}
