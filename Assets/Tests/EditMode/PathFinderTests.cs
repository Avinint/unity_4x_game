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
}
