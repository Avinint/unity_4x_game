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

    // Unité de test : un objet avec Unit et un enfant avec un MeshRenderer.
    private GameObject unitObject;

    private MeshRenderer NewUnit()
    {
        unitObject = new GameObject("TestUnit");
        unitObject.AddComponent<Unit>();
        var model = new GameObject("Model");
        model.transform.SetParent(unitObject.transform);
        return model.AddComponent<MeshRenderer>();
    }

    [TearDown]
    public void DestroyUnit()
    {
        if (unitObject != null) Object.DestroyImmediate(unitObject);
    }

    [Test]
    public void AddUnit_OnHiddenTile_HidesUnit()
    {
        var tile = NewTile();
        var renderer = NewUnit();

        tile.AddUnit(unitObject);

        Assert.IsFalse(renderer.enabled);
    }

    [Test]
    public void AddUnit_OnDiscoveredTile_ShowsUnit()
    {
        var tile = NewTile();
        tile.Discover();
        var renderer = NewUnit();
        renderer.enabled = false;

        tile.AddUnit(unitObject);

        Assert.IsTrue(renderer.enabled);
    }

    [Test]
    public void Discover_ShowsUnitsAlreadyOnTile()
    {
        var tile = NewTile();
        var renderer = NewUnit();
        tile.AddUnit(unitObject);

        tile.Discover();

        Assert.IsTrue(renderer.enabled);
    }
}
