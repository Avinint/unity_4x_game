using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class UnitTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    private Unit NewUnit()
    {
        var go = new GameObject("TestUnit");
        created.Add(go);
        return go.AddComponent<Unit>();
    }

    private Player NewPlayer(Color color)
    {
        var go = new GameObject("TestPlayer");
        created.Add(go);
        var player = go.AddComponent<Player>();
        player.Color = color;
        return player;
    }

    [TearDown]
    public void DestroyAll()
    {
        foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    private static Renderer OwnerBase(Unit unit) => unit.transform.Find("OwnerBase")?.GetComponent<Renderer>();

    [Test]
    public void SetOwner_CreatesBaseWithOwnersMaterial()
    {
        var unit = NewUnit();
        var player = NewPlayer(Color.red);

        unit.Owner = player;

        var ownerBase = OwnerBase(unit);
        Assert.IsNotNull(ownerBase);
        Assert.IsTrue(ownerBase.enabled);
        Assert.AreSame(player.PlayerColorMaterial, ownerBase.sharedMaterial);
        Assert.IsNull(ownerBase.GetComponent<Collider>());
    }

    [Test]
    public void SetOwner_OnHiddenUnit_BaseIsHidden()
    {
        var unit = NewUnit();
        unit.SetVisible(false);

        unit.Owner = NewPlayer(Color.blue);

        Assert.IsFalse(OwnerBase(unit).enabled);
    }

    private Unit NewUnitWithPoints()
    {
        var unit = NewUnit();
        unit.type = ScriptableObject.CreateInstance<UnitType>();   // 2 points par défaut
        unit.ResetMovementPoints();
        unit.Owner = NewPlayer(Color.red);
        return unit;
    }

    private static HexTile Tile(Relief relief) =>
        new HexTile(Vector2Int.zero, HexOrientation.PointyTop) { Relief = relief };

    [Test]
    public void CanEnter_Land_IsTrue()
    {
        Assert.IsTrue(NewUnitWithPoints().CanEnter(Tile(Relief.Flat)));
    }

    [Test]
    public void CanEnter_WaterRevealedOnTheWay_IsFalse()
    {
        Assert.IsFalse(NewUnitWithPoints().CanEnter(Tile(Relief.Water)));
    }

    [Test]
    public void CanEnter_ForeignUnit_IsFalse()
    {
        var unit = NewUnitWithPoints();
        var tile = Tile(Relief.Flat);
        var enemy = NewUnit();
        enemy.Owner = NewPlayer(Color.blue);
        tile.AddUnit(enemy.gameObject);

        Assert.IsFalse(unit.CanEnter(tile));
    }

    [Test]
    public void CanEnter_NoPointsLeft_IsFalse()
    {
        var unit = NewUnit();   // pas de type : 0 point
        unit.Owner = NewPlayer(Color.red);

        Assert.IsFalse(unit.CanEnter(Tile(Relief.Flat)));
    }

    [Test]
    public void AddUnit_SetsTile_RemoveUnit_ClearsIt()
    {
        var unit = NewUnit();
        var tile = Tile(Relief.Flat);

        tile.AddUnit(unit.gameObject);
        Assert.AreSame(tile, unit.Tile);

        tile.RemoveUnit(unit.gameObject);
        Assert.IsNull(unit.Tile);
    }
}
