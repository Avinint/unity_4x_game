using NUnit.Framework;

public class MapRulesTests
{
    private static readonly MapRulesSettings S = new MapRulesSettings();

    // Températures et humidités choisies au milieu de chaque tranche des réglages par défaut.
    private const float Frozen = 0.05f, Cold = 0.25f, Temperate = 0.5f, Hot = 0.85f;
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
    public void WaterBiome_CoastWhenTouchingLand()
    {
        Assert.AreEqual(Biome.Coast, MapRules.WaterBiome(true));
        Assert.AreEqual(Biome.Ocean, MapRules.WaterBiome(false));
    }
}
