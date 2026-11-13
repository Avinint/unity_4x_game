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
}
