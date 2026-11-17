using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RiverNetworkTests
{
    // Disque de rayon 3 ; mer à droite (q ≥ 2), terres qui descendent vers elle.
    private static readonly List<Vector2Int> Disk = HexMetrics.AxialRange(Vector2Int.zero, 3);
    private static readonly HashSet<Vector2Int> InDisk = new HashSet<Vector2Int>(Disk);

    private static HashSet<RiverEdge> Rivers(bool withSea, float fraction, int minLength) =>
        RiverNetwork.Rivers(Disk, InDisk.Contains,
            c => withSea && c.x >= 2,
            c => (3f - c.x) / 6f,
            c => 1f,
            fraction, minLength);

    [Test]
    public void Slope_MakesRiversBetweenLandNeighbours()
    {
        var rivers = Rivers(true, 0.3f, 1);

        Assert.IsNotEmpty(rivers);
        foreach (var e in rivers)
        {
            Assert.AreEqual(1, HexMetrics.AxialDistance(e.A, e.B), "arête entre deux cases voisines");
            Assert.Less(e.A.x, 2, "rive en terre");
            Assert.Less(e.B.x, 2, "rive en terre");
        }
    }

    [Test]
    public void Edge_IsOrderIndependent()
    {
        var a = new Vector2Int(0, 0);
        var b = new Vector2Int(1, 0);

        Assert.AreEqual(new RiverEdge(a, b), new RiverEdge(b, a));
    }

    [Test]
    public void TooShortNetworks_AreDropped()
    {
        Assert.IsEmpty(Rivers(true, 0.3f, 1000));
    }

    [Test]
    public void NoWater_NoRiver()
    {
        Assert.IsEmpty(Rivers(false, 0.3f, 1));
    }
}
