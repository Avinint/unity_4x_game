using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LineOfSightTests
{
    private static readonly List<Vector2Int> Range = HexMetrics.AxialRange(Vector2Int.zero, 2);

    // obstacles : hauteur comme obstacle ; surfaces : hauteur du sommet de la case (relief). 0 par défaut.
    private static List<Vector2Int> See(Dictionary<Vector2Int, int> obstacles, int eye,
                                        Dictionary<Vector2Int, int> surfaces = null) =>
        LineOfSight.Visible(Vector2Int.zero, Range,
            c => obstacles.TryGetValue(c, out int h) ? h : 0,
            c => surfaces != null && surfaces.TryGetValue(c, out int h) ? h : 0,
            eye);

    [Test]
    public void OpenGround_SeesEverything()
    {
        Assert.AreEqual(Range.Count, See(new Dictionary<Vector2Int, int>(), 0).Count);
    }

    [Test]
    public void Forest_HidesWhatIsBehind_ButIsSeen()
    {
        var seen = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 1 }, 0);

        CollectionAssert.Contains(seen, new Vector2Int(1, 0));
        CollectionAssert.DoesNotContain(seen, new Vector2Int(2, 0));
    }

    [Test]
    public void FromHill_SeesOverForestAndHill_NotOverMountain()
    {
        var forest = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 1 }, 2);
        CollectionAssert.Contains(forest, new Vector2Int(2, 0));

        var hill = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 2 }, 2);
        CollectionAssert.Contains(hill, new Vector2Int(2, 0));

        var mountain = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 4 }, 2);
        CollectionAssert.DoesNotContain(mountain, new Vector2Int(2, 0));
    }

    [Test]
    public void MountainBehindForest_IsSeen()
    {
        var target = new Vector2Int(2, 0);
        var seen = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 1, [target] = 4 }, 0,
                       new Dictionary<Vector2Int, int> { [target] = 4 });

        CollectionAssert.Contains(seen, target);
    }

    [Test]
    public void HillBehindForest_IsSeen()
    {
        var target = new Vector2Int(2, 0);
        var seen = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 1, [target] = 2 }, 0,
                       new Dictionary<Vector2Int, int> { [target] = 2 });

        CollectionAssert.Contains(seen, target);
    }

    [Test]
    public void MountainBehindMountain_IsHidden()
    {
        var target = new Vector2Int(2, 0);
        var seen = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 4, [target] = 4 }, 0,
                       new Dictionary<Vector2Int, int> { [target] = 4 });

        CollectionAssert.DoesNotContain(seen, target);
    }

    [Test]
    public void AlongEdge_OneSideClear_IsSeen()
    {
        // (1,1) : la ligne passe entre (1,0) et (0,1).
        var seen = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 1 }, 0);

        CollectionAssert.Contains(seen, new Vector2Int(1, 1));
    }

    [Test]
    public void AlongEdge_BothSidesBlocked_IsHidden()
    {
        var seen = See(new Dictionary<Vector2Int, int> { [new Vector2Int(1, 0)] = 1, [new Vector2Int(0, 1)] = 1 }, 0);

        CollectionAssert.DoesNotContain(seen, new Vector2Int(1, 1));
    }
}
