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
    public void Hover_WorksOnHiddenTile()
    {
        var tile = NewTile();

        tile.OnMouseEnter();

        Assert.IsTrue(tile.IsHovered);
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

    [Test]
    public void MovementCost_HillAddsOne()
    {
        var tile = NewTile();
        tile.Relief = Relief.Flat;
        int flat = tile.MovementCost;

        tile.Relief = Relief.Hill;

        Assert.AreEqual(flat + 1, tile.MovementCost);
    }

    [Test]
    public void IsLand_UsesRelief()
    {
        var tile = NewTile();

        tile.Relief = Relief.Flat;
        Assert.IsTrue(tile.IsLand());
        tile.Relief = Relief.Hill;
        Assert.IsTrue(tile.IsLand());
        tile.Relief = Relief.Water;
        Assert.IsFalse(tile.IsLand());
        tile.Relief = Relief.Mountain;
        Assert.IsFalse(tile.IsLand());
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
        HexTile.Viewer = null;
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
        tile.SetVisibleNow(true);
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
        tile.SetVisibleNow(true);   // vue mais pas encore découverte : toujours cachée
        Assert.IsFalse(renderer.enabled);

        tile.Discover();

        Assert.IsTrue(renderer.enabled);
    }

    [Test]
    public void ForeignUnit_OnExploredNotVisibleTile_IsHidden()
    {
        var tile = NewTile();
        tile.Discover();
        HexTile.Viewer = NewPlayer();
        var renderer = NewUnit();
        unitObject.GetComponent<Unit>().Owner = NewPlayer();

        tile.AddUnit(unitObject);

        Assert.IsFalse(renderer.enabled);
    }

    [Test]
    public void OwnUnit_OnExploredNotVisibleTile_IsShown()
    {
        var tile = NewTile();
        tile.Discover();
        var me = NewPlayer();
        HexTile.Viewer = me;
        var renderer = NewUnit();
        unitObject.GetComponent<Unit>().Owner = me;

        tile.AddUnit(unitObject);

        Assert.IsTrue(renderer.enabled);
    }

    [Test]
    public void SetVisibleNow_False_HidesForeignUnit()
    {
        var tile = NewTile();
        tile.Discover();
        tile.SetVisibleNow(true);
        HexTile.Viewer = NewPlayer();
        var renderer = NewUnit();
        unitObject.GetComponent<Unit>().Owner = NewPlayer();
        tile.AddUnit(unitObject);
        Assert.IsTrue(renderer.enabled);

        tile.SetVisibleNow(false);

        Assert.IsFalse(renderer.enabled);
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

    [Test]
    public void HasUnitOfOtherPlayer_OwnUnitOnly_IsFalse()
    {
        var tile = NewTile();
        var me = NewPlayer();
        NewUnit();
        unitObject.GetComponent<Unit>().Owner = me;
        tile.AddUnit(unitObject);

        Assert.IsFalse(tile.HasUnitOfOtherPlayer(me));
    }

    [Test]
    public void HasUnitOfOtherPlayer_ForeignUnit_IsTrue()
    {
        var tile = NewTile();
        var me = NewPlayer();
        var other = NewPlayer();
        NewUnit();
        unitObject.GetComponent<Unit>().Owner = other;
        tile.AddUnit(unitObject);

        Assert.IsTrue(tile.HasUnitOfOtherPlayer(me));
    }

    [Test]
    public void HasUnitOfOtherPlayer_EmptyTile_IsFalse()
    {
        Assert.IsFalse(NewTile().HasUnitOfOtherPlayer(NewPlayer()));
    }

    private Unit NewUnitOf(Player owner, UnitCategory category)
    {
        NewUnit();
        var unit = unitObject.GetComponent<Unit>();
        var type = ScriptableObject.CreateInstance<UnitType>();
        typeof(UnitType).GetField("<Category>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(type, category);
        unit.type = type;
        unit.Owner = owner;
        return unit;
    }

    [Test]
    public void CanStop_EmptyTile_IsTrue()
    {
        Assert.IsTrue(NewTile().CanStop(NewUnitOf(NewPlayer(), UnitCategory.Melee)));
    }

    [Test]
    public void CanStop_OwnMilitaryAlreadyThere_IsFalse()
    {
        var tile = NewTile();
        var me = NewPlayer();
        tile.AddUnit(NewUnitOf(me, UnitCategory.Melee).gameObject);

        Assert.IsFalse(tile.CanStop(NewUnitOf(me, UnitCategory.Ranged)));
    }

    [Test]
    public void CanStop_OwnCivilianThere_MilitaryCanStop()
    {
        var tile = NewTile();
        var me = NewPlayer();
        tile.AddUnit(NewUnitOf(me, UnitCategory.Civilian).gameObject);

        Assert.IsTrue(tile.CanStop(NewUnitOf(me, UnitCategory.Melee)));
    }

    [Test]
    public void CanStop_UnitAlreadyOnTile_IgnoresItself()
    {
        var tile = NewTile();
        var unit = NewUnitOf(NewPlayer(), UnitCategory.Melee);
        tile.AddUnit(unit.gameObject);

        Assert.IsTrue(tile.CanStop(unit));
    }

    [Test]
    public void IsWalkableFor_Unexplored_IsTrueEvenOnWater()
    {
        var tile = NewTile();
        tile.Relief = Relief.Water;

        Assert.IsTrue(tile.IsWalkableFor(NewPlayer(), explored: false));
    }

    [Test]
    public void IsWalkableFor_ExploredWater_IsFalse()
    {
        var tile = NewTile();
        tile.Relief = Relief.Water;

        Assert.IsFalse(tile.IsWalkableFor(NewPlayer(), explored: true));
    }

    [Test]
    public void IsWalkableFor_ExploredForeignUnit_IsFalse()
    {
        var tile = NewTile();
        tile.Relief = Relief.Flat;
        NewUnit();
        unitObject.GetComponent<Unit>().Owner = NewPlayer();
        tile.AddUnit(unitObject);

        Assert.IsFalse(tile.IsWalkableFor(NewPlayer(), explored: true));
    }

    [Test]
    public void MovementCostFor_Unexplored_IsOne()
    {
        var tile = NewTile();
        tile.Relief = Relief.Hill;

        Assert.AreEqual(1, tile.MovementCostFor(explored: false));
        Assert.AreEqual(tile.MovementCost, tile.MovementCostFor(explored: true));
    }
}
