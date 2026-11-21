using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class MapShapeTests
{
    [Test]
    public void Bump_OneAtCentre_ZeroBeyondRadius()
    {
        var centre = new Vector2(10f, 10f); var radius = new Vector2(5f, 4f);
        Assert.AreEqual(1f, MapShape.Bump(10f, 10f, centre, radius), 1e-4f);
        Assert.AreEqual(0f, MapShape.Bump(16f, 10f, centre, radius), 1e-4f);
        Assert.AreEqual(0f, MapShape.Bump(10f, 15f, centre, radius), 1e-4f);
    }

    [Test]
    public void Bump_Wrap_CloseAcrossSeam()
    {
        var centre = new Vector2(0f, 5f); var radius = new Vector2(5f, 5f);
        Assert.AreEqual(0f, MapShape.Bump(39f, 5f, centre, radius), 1e-4f);              // bords fixes : 39 cases
        Assert.Greater(MapShape.Bump(39f, 5f, centre, radius, 40f), 0.8f);               // à travers la couture : 1 case
    }

    [Test]
    public void Mask_Fractal_IsNull() =>
        Assert.IsNull(MapShape.Mask(MapType.Fractal, 40, 29, 2, false));

    [Test]
    public void Mask_Pangaea_HighInCentre_ZeroAtEdges([Values(false, true)] bool wrap)
    {
        var mask = MapShape.Mask(MapType.Pangaea, 40, 29, 2, wrap);
        Assert.Greater(mask[20, 14], 0.95f);
        Assert.AreEqual(0f, mask[0, 14], 1e-4f);     // ouest, et la couture en monde qui boucle
        Assert.AreEqual(0f, mask[20, 2], 1e-4f);     // sud, au bord de la banquise
    }

    [Test]
    public void ContinentCount_TwoThenThreeOnWideMaps()
    {
        Assert.AreEqual(2, MapShape.ContinentCount(40));
        Assert.AreEqual(3, MapShape.ContinentCount(60));
    }

    [Test]
    public void Mask_Continents_LandAtCentres_OceanBetween([Values(false, true)] bool wrap)
    {
        var centres = MapShape.ContinentCentres(40, 29, 2, wrap, new System.Random(3));
        var mask = MapShape.Mask(MapType.Continents, 40, 29, 2, wrap, new System.Random(3));

        foreach (var c in centres)
            Assert.Greater(mask[Mathf.RoundToInt(c.x) % 40, Mathf.RoundToInt(c.y)], 0.9f);
        // À mi-chemin entre les deux centres : océan.
        int between = Mathf.RoundToInt((centres[0].x + centres[1].x) * 0.5f);
        Assert.AreEqual(0f, mask[between, 14], 1e-4f);
    }

    [Test]
    public void Mask_Continents_FixedEdges_OceanAtWestAndEast()
    {
        var mask = MapShape.Mask(MapType.Continents, 40, 29, 2, false, new System.Random(5));
        Assert.AreEqual(0f, mask[0, 14], 1e-4f);
        Assert.AreEqual(0f, mask[39, 14], 1e-4f);
    }

    [Test]
    public void OceanChannels_FixedEdges_OnlyBetweenContinents()
    {
        var centres = new List<Vector2> { new Vector2(30f, 10f), new Vector2(10f, 10f) };
        bool[] channels = MapShape.OceanChannels(40, centres, false);

        CollectionAssert.AreEqual(new[] { 19, 20, 21 }, Enumerable.Range(0, 40).Where(x => channels[x]).ToArray());
    }

    [Test]
    public void OceanChannels_Wrap_AlsoAcrossSeam()
    {
        var centres = new List<Vector2> { new Vector2(10f, 10f), new Vector2(30f, 10f) };
        bool[] channels = MapShape.OceanChannels(40, centres, true);

        // Entre 10 et 30 : 19-21 ; entre 30 et 50 (= 10 de l'autre côté) : 39, 0, 1.
        CollectionAssert.AreEqual(new[] { 0, 1, 19, 20, 21, 39 }, Enumerable.Range(0, 40).Where(x => channels[x]).ToArray());
    }

    [Test]
    public void IslandCount_OnePer80Cells_AtLeastFour()
    {
        Assert.AreEqual(12, MapShape.IslandCount(40, 29, 2));   // 40 × 25 = 1000 cases jouables
        Assert.AreEqual(4, MapShape.IslandCount(10, 10, 2));
    }

    [Test]
    public void IslandBumps_CountAndInsidePlayableBand([Values(false, true)] bool wrap)
    {
        var islands = MapShape.IslandBumps(40, 29, 2, wrap, 0.35f, new System.Random(1));

        Assert.AreEqual(12, islands.Count);
        foreach (var (c, _) in islands)
        {
            Assert.That(c.x, Is.InRange(0f, 40f));
            Assert.That(c.y, Is.InRange(2f, 26f));
        }
    }

    [Test]
    public void IslandBumps_SpreadApart()
    {
        var islands = MapShape.IslandBumps(40, 29, 2, true, 0.35f, new System.Random(1));
        float rx = Mathf.Sqrt(40 * 25 * 0.35f / (12 * Mathf.PI));
        for (int a = 0; a < islands.Count; a++)
            for (int b = a + 1; b < islands.Count; b++)
                Assert.GreaterOrEqual(MapShape.Gap(islands[a].centre, islands[b].centre, 40f), 2f * rx, $"îles {a} et {b}");
    }

    [Test]
    public void Gap_Wrap_ShortestAcrossSeam() =>
        Assert.AreEqual(2f, MapShape.Gap(new Vector2(39f, 5f), new Vector2(1f, 5f), 40f), 1e-4f);

    [Test]
    public void Mask_Continents_SameRandom_SameMask()
    {
        var a = MapShape.Mask(MapType.Continents, 40, 29, 2, true, new System.Random(9));
        var b = MapShape.Mask(MapType.Continents, 40, 29, 2, true, new System.Random(9));
        CollectionAssert.AreEqual(a, b);
    }
}
