using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PathFinderTests
{
    // Grille rectangulaire de coordonnées axiales, moins les obstacles.
    private static Func<Vector2Int, bool> Grid(int width, int height, params Vector2Int[] blocked)
    {
        var walls = new HashSet<Vector2Int>(blocked);
        return c => c.x >= 0 && c.x < width && c.y >= 0 && c.y < height && !walls.Contains(c);
    }

    private static void AssertContiguous(Vector2Int origin, List<Vector2Int> path)
    {
        Vector2Int previous = origin;
        foreach (var step in path)
        {
            Assert.AreEqual(1, HexMetrics.AxialDistance(previous, step), $"Pas non adjacent : {previous} -> {step}");
            previous = step;
        }
    }

    [Test]
    public void FindPath_StraightLine_ReturnsEachTileInOrder()
    {
        var path = PathFinder.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 0), Grid(10, 1));

        CollectionAssert.AreEqual(
            new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) },
            path);
    }

    [Test]
    public void FindPath_SameTile_ReturnsEmptyPath()
    {
        var path = PathFinder.FindPath(new Vector2Int(2, 2), new Vector2Int(2, 2), Grid(5, 5));

        Assert.IsNotNull(path);
        Assert.IsEmpty(path);
    }

    [Test]
    public void FindPath_DestinationNotPassable_ReturnsNull()
    {
        var destination = new Vector2Int(3, 3);

        Assert.IsNull(PathFinder.FindPath(new Vector2Int(0, 0), destination, Grid(5, 5, destination)));
    }

    [Test]
    public void FindPath_DestinationSurrounded_ReturnsNull()
    {
        var destination = new Vector2Int(5, 5);
        var ring = new[]
        {
            new Vector2Int(6, 5), new Vector2Int(6, 4), new Vector2Int(5, 4),
            new Vector2Int(4, 5), new Vector2Int(4, 6), new Vector2Int(5, 6)
        };

        Assert.IsNull(PathFinder.FindPath(new Vector2Int(0, 0), destination, Grid(10, 10, ring)));
    }

    [Test]
    public void FindPath_WallWithGap_GoesThroughGap()
    {
        // Mur en q = 3 sur toute la hauteur sauf r = 7.
        var wall = new List<Vector2Int>();
        for (int r = 0; r < 10; r++)
            if (r != 7) wall.Add(new Vector2Int(3, r));

        var origin = new Vector2Int(0, 0);
        var path = PathFinder.FindPath(origin, new Vector2Int(6, 0), Grid(10, 10, wall.ToArray()));

        Assert.IsNotNull(path);
        CollectionAssert.Contains(path, new Vector2Int(3, 7));
        AssertContiguous(origin, path);
    }

    [Test]
    public void FindPath_NoObstacle_LengthEqualsDistance()
    {
        var origin = new Vector2Int(1, 8);
        var destination = new Vector2Int(8, 1);

        var path = PathFinder.FindPath(origin, destination, Grid(10, 10));

        Assert.AreEqual(HexMetrics.AxialDistance(origin, destination), path.Count);
        AssertContiguous(origin, path);
        Assert.AreEqual(destination, path[path.Count - 1]);
    }

    [Test]
    public void FindPath_PrefersCheaperDetour()
    {
        // 10 points, un seul tour : direct, (1,0) coûte 5 puis (2,0) → 6 ; détour par (0,1), (1,1), (2,0) → 3,
        // même tour mais plus de points restants.
        var forest = new Vector2Int(1, 0);
        var origin = new Vector2Int(0, 0);
        var path = PathFinder.FindPath(origin, new Vector2Int(2, 0), Grid(5, 2),
            (_, to) => to == forest ? 5 : 1, 10, 10);

        CollectionAssert.DoesNotContain(path, forest);
        Assert.AreEqual(3, path.Count);
        AssertContiguous(origin, path);
    }

    [Test]
    public void FindPath_FewestTurns_CrossesRiverRatherThanDetour()
    {
        // 1 point par tour. Traverser coûte 10 mais se fait en 1 tour (on entre avec 1 point) ;
        // le détour par (1,0) ne coûte que 2 points mais prend 2 tours.
        var origin = new Vector2Int(0, 1);
        var target = new Vector2Int(1, 1);
        var river = new RiverEdge(origin, target);
        var path = PathFinder.FindPath(origin, target, Grid(5, 5),
            (from, to) => new RiverEdge(from, to).Equals(river) ? 10 : 1, 1, 1);

        CollectionAssert.AreEqual(new[] { target }, path);
    }

    [Test]
    public void FindPath_FewestTurns_NoObstacle_LengthEqualsDistance()
    {
        var origin = new Vector2Int(1, 8);
        var destination = new Vector2Int(8, 1);

        var path = PathFinder.FindPath(origin, destination, Grid(10, 10), (_, _) => 1, 2, 2);

        Assert.AreEqual(HexMetrics.AxialDistance(origin, destination), path.Count);
        AssertContiguous(origin, path);
    }

    [Test]
    public void FindPath_Wrap_CrossesSeam()
    {
        // Rangée de 10 cases (q = 0..9, r = 0) qui fait le tour : de 0 à 9, un seul pas par la couture.
        Func<Vector2Int, Vector2Int> wrap = v => new Vector2Int(((v.x % 10) + 10) % 10, v.y);
        Func<Vector2Int, Vector2Int, int> distance = (a, b) =>
            Math.Min(HexMetrics.AxialDistance(a, b),
                Math.Min(HexMetrics.AxialDistance(a, b + new Vector2Int(10, 0)), HexMetrics.AxialDistance(a, b - new Vector2Int(10, 0))));

        var path = PathFinder.FindPath(new Vector2Int(0, 0), new Vector2Int(9, 0), Grid(10, 1), wrap, distance);

        CollectionAssert.AreEqual(new[] { new Vector2Int(9, 0) }, path);
    }

    [Test]
    public void Reachable_TwoPoints_TwoStepsEachWay()
    {
        var zone = PathFinder.Reachable(new Vector2Int(5, 0), Grid(10, 1), (_, _) => 1, 2);

        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(5, 0), new Vector2Int(6, 0), new Vector2Int(7, 0) },
            zone);
    }

    [Test]
    public void Reachable_ExpensiveStep_EntersThenStops()
    {
        // Coût 3 avec 2 points : on entre dans la voisine (il reste un point), pas plus loin.
        var zone = PathFinder.Reachable(new Vector2Int(0, 0), Grid(10, 1), (_, _) => 3, 2);

        CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, zone);
    }

    [Test]
    public void TurnEnds_TwoTurns() =>
        CollectionAssert.AreEqual(new[] { (1, 1), (3, 2) }, PathFinder.TurnEnds(new[] { 1, 1, 1, 1 }, 2, 2));

    [Test]
    public void TurnEnds_ExpensiveStepUsesLastPoints() =>
        CollectionAssert.AreEqual(new[] { (0, 1), (1, 2) }, PathFinder.TurnEnds(new[] { 3, 1 }, 2, 2));

    [Test]
    public void TurnEnds_NoPointsLeft_FirstBadgeIsTwo() =>
        CollectionAssert.AreEqual(new[] { (1, 2) }, PathFinder.TurnEnds(new[] { 1, 1 }, 0, 2));

    [Test]
    public void TurnEnds_ReachedThisTurn_SingleBadge() =>
        CollectionAssert.AreEqual(new[] { (0, 1) }, PathFinder.TurnEnds(new[] { 1 }, 2, 2));

    [Test]
    public void ReachableCount_StopsWhenPointsRunOut()
    {
        Assert.AreEqual(2, PathFinder.ReachableCount(new[] { 1, 1, 1 }, 2));
    }

    [Test]
    public void ReachableCount_ExpensiveTileUsesLastPoints()
    {
        Assert.AreEqual(2, PathFinder.ReachableCount(new[] { 1, 2, 1 }, 2));
    }

    [Test]
    public void ReachableCount_NoPoints_IsZero()
    {
        Assert.AreEqual(0, PathFinder.ReachableCount(new[] { 1, 1 }, 0));
    }

    [Test]
    public void FindPath_StepCost_AvoidsExpensiveCrossing()
    {
        // 20 points, un seul tour. Rivière entre (0,1) et (1,1) : 10 pour la franchir ;
        // le détour (3 pas) arrive au même tour avec plus de points.
        var origin = new Vector2Int(0, 1);
        var river = new RiverEdge(origin, new Vector2Int(1, 1));
        var path = PathFinder.FindPath(origin, new Vector2Int(2, 1), Grid(5, 5),
            (from, to) => new RiverEdge(from, to).Equals(river) ? 10 : 1, 20, 20);

        Assert.AreNotEqual(new Vector2Int(1, 1), path[0]);
        AssertContiguous(origin, path);
    }

    [Test]
    public void LastStop_AllFree_Unchanged() =>
        Assert.AreEqual(3, PathFinder.LastStop(3, i => true));

    [Test]
    public void LastStop_LastTileFull_StepsBack() =>
        Assert.AreEqual(1, PathFinder.LastStop(3, i => i == 0));   // cases 2 et 3 pleines : arrêt sur la 1re

    [Test]
    public void LastStop_NoFreeTile_IsZero() =>
        Assert.AreEqual(0, PathFinder.LastStop(2, i => false));
}
