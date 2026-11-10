using NUnit.Framework;
using UnityEngine;

public class HexTileTests
{
    private static HexTile NewTile() => new HexTile(new Vector2Int(2, 3), HexOrientation.PointyTop);

    [Test]
    public void NewTile_IsNeitherDiscoveredNorHovered()
    {
        var tile = NewTile();

        Assert.IsFalse(tile.IsDiscovered);
        Assert.IsFalse(tile.IsHovered);
    }

    [Test]
    public void NewTile_AxialCoordinatesMatchOffset()
    {
        var tile = NewTile();

        Assert.AreEqual(HexMetrics.OffsetToAxial(2, 3, HexOrientation.PointyTop), tile.AxialCoordinates);
    }

    [Test]
    public void Discover_SetsIsDiscovered_AndIsIdempotent()
    {
        var tile = NewTile();

        tile.Discover();
        tile.Discover();

        Assert.IsTrue(tile.IsDiscovered);
    }

    [Test]
    public void Hover_IsIgnoredWhileHidden()
    {
        var tile = NewTile();

        tile.OnMouseEnter();

        Assert.IsFalse(tile.IsHovered);
    }

    [Test]
    public void Hover_WorksOnceDiscovered_AndExitClearsIt()
    {
        var tile = NewTile();
        tile.Discover();

        tile.OnMouseEnter();
        Assert.IsTrue(tile.IsHovered);

        tile.OnMouseExit();
        Assert.IsFalse(tile.IsHovered);
    }
}
