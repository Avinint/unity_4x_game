using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class FogOfWarTests
{
    private static readonly Vector2Int A = new Vector2Int(0, 0);
    private static readonly Vector2Int B = new Vector2Int(1, 0);
    private static Vector2Int[] Cells(params Vector2Int[] cells) => cells;

    [Test]
    public void Explore_MarksExplored_NotVisible()
    {
        var fog = new FogOfWar<string>();

        fog.Explore("P1", Cells(A));

        Assert.IsTrue(fog.IsExplored("P1", A));
        Assert.IsFalse(fog.IsVisible("P1", A));
    }

    [Test]
    public void AddWatcher_MakesVisibleAndExplored()
    {
        var fog = new FogOfWar<string>();

        fog.AddWatcher("P1", Cells(A));

        Assert.IsTrue(fog.IsVisible("P1", A));
        Assert.IsTrue(fog.IsExplored("P1", A));
    }

    [Test]
    public void TwoWatchers_OneRemoved_StillVisible()
    {
        var fog = new FogOfWar<string>();
        fog.AddWatcher("P1", Cells(A, B));
        fog.AddWatcher("P1", Cells(A));

        fog.RemoveWatcher("P1", Cells(A, B));

        Assert.IsTrue(fog.IsVisible("P1", A));
        Assert.IsFalse(fog.IsVisible("P1", B));
    }

    [Test]
    public void RemoveWatcher_KeepsExplored_NoLongerVisible()
    {
        var fog = new FogOfWar<string>();
        fog.AddWatcher("P1", Cells(A));

        fog.RemoveWatcher("P1", Cells(A));

        Assert.IsFalse(fog.IsVisible("P1", A));
        Assert.IsTrue(fog.IsExplored("P1", A));
    }

    [Test]
    public void Players_AreIndependent()
    {
        var fog = new FogOfWar<string>();

        fog.AddWatcher("P1", Cells(A));

        Assert.IsFalse(fog.IsExplored("P2", A));
        Assert.IsFalse(fog.IsVisible("P2", A));
    }

    [Test]
    public void TileExplored_RaisedOnlyOnce()
    {
        var fog = new FogOfWar<string>();
        var raised = new List<Vector2Int>();
        fog.TileExplored += (p, c) => raised.Add(c);

        fog.Explore("P1", Cells(A));
        fog.AddWatcher("P1", Cells(A));
        fog.AddWatcher("P1", Cells(A));

        CollectionAssert.AreEqual(new[] { A }, raised);
    }

    [Test]
    public void VisibilityChanged_RaisedOnlyOnTransitions()
    {
        var fog = new FogOfWar<string>();
        var raised = new List<bool>();
        fog.VisibilityChanged += (p, c, visible) => raised.Add(visible);

        fog.AddWatcher("P1", Cells(A));     // 0 → 1 : vue
        fog.AddWatcher("P1", Cells(A));     // 1 → 2 : rien
        fog.RemoveWatcher("P1", Cells(A));  // 2 → 1 : rien
        fog.RemoveWatcher("P1", Cells(A));  // 1 → 0 : plus vue

        CollectionAssert.AreEqual(new[] { true, false }, raised);
    }

    [Test]
    public void RemoveWatcher_UnknownCell_DoesNothing()
    {
        var fog = new FogOfWar<string>();
        fog.AddWatcher("P1", Cells(A));

        Assert.DoesNotThrow(() => fog.RemoveWatcher("P1", Cells(B)));
        Assert.DoesNotThrow(() => fog.RemoveWatcher("P2", Cells(A)));
        Assert.IsTrue(fog.IsVisible("P1", A));
    }
}
