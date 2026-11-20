using System.Linq;
using NUnit.Framework;

public class TileInfoTests
{
    private static string[] Keys(Relief relief, Biome biome, int cost, bool river, bool freshWater) =>
        TileInfo.Describe(relief, biome, cost, river, freshWater).Select(l => l.key).ToArray();

    [Test]
    public void FlatGrassland_NameAndCost()
    {
        var lines = TileInfo.Describe(Relief.Flat, Biome.Grassland, 1, false, false);

        CollectionAssert.AreEqual(new[] { "biome.grassland", "tooltip.movement_cost" }, lines.Select(l => l.key).ToArray());
        Assert.AreEqual(1, lines[1].args[0]);
    }

    [Test]
    public void ForestHill_WithRiver_AllLines()
    {
        CollectionAssert.AreEqual(
            new[] { "biome.forest", "relief.hill", "tooltip.movement_cost", "tooltip.river", "tooltip.fresh_water" },
            Keys(Relief.Hill, Biome.Forest, 3, true, true));
    }

    [Test]
    public void Mountain_ReliefOnly_Impassable() =>
        CollectionAssert.AreEqual(new[] { "relief.mountain", "tooltip.impassable" },
            Keys(Relief.Mountain, Biome.Grassland, 1, false, false));

    [Test]
    public void Ocean_Impassable() =>
        CollectionAssert.AreEqual(new[] { "biome.ocean", "tooltip.impassable" },
            Keys(Relief.Water, Biome.Ocean, 1, false, false));
}
