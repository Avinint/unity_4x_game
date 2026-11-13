using System.Collections.Generic;
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
    private readonly List<GameObject> created = new List<GameObject>();
    private GameObject unitObject;   // dernière unité créée

    private MeshRenderer NewUnit()
    {
        unitObject = new GameObject("TestUnit");
        created.Add(unitObject);
        unitObject.AddComponent<Unit>();
        var model = new GameObject("Model");
        model.transform.SetParent(unitObject.transform);
        return model.AddComponent<MeshRenderer>();
    }

    private Player NewPlayer()
    {
        var go = new GameObject("TestPlayer");
        created.Add(go);
        return go.AddComponent<Player>();
    }

    [TearDown]
    public void DestroyUnit()
    {
        foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
        created.Clear();
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

    [Test]
    public void GetFirstUnit_PrefersOwnersUnit()
    {
        var tile = NewTile();
        var me = NewPlayer();
        var other = NewPlayer();
        NewUnit();
        var otherUnit = unitObject.GetComponent<Unit>();
        otherUnit.Owner = other;
        tile.AddUnit(unitObject);
        NewUnit();
        var myUnit = unitObject.GetComponent<Unit>();
        myUnit.Owner = me;
        tile.AddUnit(unitObject);

        Assert.AreSame(myUnit, tile.GetFirstUnit(me));
    }

    [Test]
    public void GetFirstUnit_NoOwnUnit_ReturnsFirst()
    {
        var tile = NewTile();
        var me = NewPlayer();
        var other = NewPlayer();
        NewUnit();
        var firstUnit = unitObject.GetComponent<Unit>();
        firstUnit.Owner = other;
        tile.AddUnit(unitObject);
        NewUnit();
        unitObject.GetComponent<Unit>().Owner = other;
        tile.AddUnit(unitObject);

        Assert.AreSame(firstUnit, tile.GetFirstUnit(me));
    }
}
