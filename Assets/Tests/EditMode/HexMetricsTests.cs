using NUnit.Framework;
using UnityEngine;

public class HexMetricsTests
{
    private const float HexSize = 5f;

    [Test]
    public void AxialDistance_SameTile_IsZero()
    {
        var tile = new Vector2Int(3, 4);
        Assert.AreEqual(0, HexMetrics.AxialDistance(tile, tile));
    }

    [Test]
    public void Neighbours_AreSixTilesAtDistanceOne()
    {
        var tile = new Vector2Int(3, 4);
        var neighbours = HexMetrics.GetNeighbourCoordinatesList(tile);

        Assert.AreEqual(6, neighbours.Count);
        foreach (var neighbour in neighbours)
            Assert.AreEqual(1, HexMetrics.AxialDistance(tile, neighbour), $"voisin {neighbour}");
    }

    [TestCase(HexOrientation.PointyTop)]
    [TestCase(HexOrientation.FlatTop)]
    public void Offset_ToAxialToCubeToOffset_RoundTrips(HexOrientation orientation)
    {
        for (int col = 0; col < 10; col++)
        {
            for (int row = 0; row < 10; row++)
            {
                Vector2Int axial = HexMetrics.OffsetToAxial(col, row, orientation);
                Vector3Int cube = HexMetrics.AxialToCube(axial);
                Vector2Int back = HexMetrics.CubeToOffset(cube, orientation);

                Assert.AreEqual(new Vector2Int(col, row), back, $"{orientation} offset ({col},{row}) -> axial {axial}");
            }
        }
    }

    [TestCase(HexOrientation.PointyTop)]
    [TestCase(HexOrientation.FlatTop)]
    public void Center_ThenCoordinateToAxial_ReturnsSameTile(HexOrientation orientation)
    {
        for (int col = 0; col < 10; col++)
        {
            for (int row = 0; row < 10; row++)
            {
                Vector3 centre = HexMetrics.Center(HexSize, col, row, orientation);
                Vector2Int axial = HexMetrics.CoordinateToAxial(centre.x, centre.z, HexSize, orientation);
                Vector2Int expected = HexMetrics.OffsetToAxial(col, row, orientation);

                Assert.AreEqual(expected, axial, $"{orientation} case ({col},{row}) centre {centre}");
            }
        }
    }

    [TestCase(HexOrientation.PointyTop)]
    [TestCase(HexOrientation.FlatTop)]
    public void Corners_AreSixPointsAtOuterRadius(HexOrientation orientation)
    {
        Vector3[] corners = HexMetrics.Corners(HexSize, orientation);

        Assert.AreEqual(6, corners.Length);
        foreach (var corner in corners)
            Assert.AreEqual(HexMetrics.OuterRadius(HexSize), corner.magnitude, 0.0001f);
    }

    [TestCase(HexOrientation.PointyTop)]
    [TestCase(HexOrientation.FlatTop)]
    public void GridBounds_SingleTile_IsPointAtFirstCentre(HexOrientation orientation)
    {
        Rect bounds = HexMetrics.GridBounds(1, 1, HexSize, orientation);
        Vector3 c = HexMetrics.Center(HexSize, 0, 0, orientation);

        Assert.AreEqual(Vector2.zero, bounds.size);
        Assert.AreEqual(new Vector2(c.x, c.z), bounds.position);
    }

    [TestCase(HexOrientation.PointyTop)]
    [TestCase(HexOrientation.FlatTop)]
    public void GridBounds_ContainsEveryCentre(HexOrientation orientation)
    {
        Rect bounds = HexMetrics.GridBounds(7, 5, HexSize, orientation);

        for (int z = 0; z < 5; z++)
            for (int x = 0; x < 7; x++)
            {
                Vector3 c = HexMetrics.Center(HexSize, x, z, orientation);
                Assert.IsTrue(c.x >= bounds.xMin && c.x <= bounds.xMax && c.z >= bounds.yMin && c.z <= bounds.yMax,
                    $"{orientation} ({x},{z}) hors de {bounds}");
            }
    }

    [TestCase(HexOrientation.PointyTop)]
    [TestCase(HexOrientation.FlatTop)]
    public void GridBounds_TouchesExtremeCentres(HexOrientation orientation)
    {
        Rect bounds = HexMetrics.GridBounds(7, 5, HexSize, orientation);
        bool minX = false, maxX = false, minZ = false, maxZ = false;

        for (int z = 0; z < 5; z++)
            for (int x = 0; x < 7; x++)
            {
                Vector3 c = HexMetrics.Center(HexSize, x, z, orientation);
                minX |= Mathf.Approximately(c.x, bounds.xMin);
                maxX |= Mathf.Approximately(c.x, bounds.xMax);
                minZ |= Mathf.Approximately(c.z, bounds.yMin);
                maxZ |= Mathf.Approximately(c.z, bounds.yMax);
            }

        Assert.IsTrue(minX && maxX && minZ && maxZ, $"{orientation} : un bord ne touche aucun centre");
    }

    [Test]
    public void AxialRange_RadiusZero_IsCenterOnly()
    {
        var center = new Vector2Int(4, -2);

        CollectionAssert.AreEqual(new[] { center }, HexMetrics.AxialRange(center, 0));
    }

    [TestCase(1, 7)]
    [TestCase(2, 19)]
    [TestCase(3, 37)]
    public void AxialRange_HasExpectedCount(int radius, int expected)
    {
        Assert.AreEqual(expected, HexMetrics.AxialRange(new Vector2Int(3, 4), radius).Count);
    }

    [Test]
    public void AxialRange_AllWithinRadius_NoDuplicates()
    {
        var center = new Vector2Int(3, 4);
        var range = HexMetrics.AxialRange(center, 3);

        CollectionAssert.AllItemsAreUnique(range);
        foreach (var c in range)
            Assert.LessOrEqual(HexMetrics.AxialDistance(center, c), 3, $"{c}");
    }

    [Test]
    public void PlinthMesh_SixSidesFromZeroToOne()
    {
        Mesh mesh = HexMetrics.BuildPlinthMesh(HexSize);

        Assert.AreEqual(24, mesh.vertexCount);
        Assert.AreEqual(36, mesh.triangles.Length);
        Assert.AreEqual(0f, mesh.bounds.min.y, 0.0001f);
        Assert.AreEqual(1f, mesh.bounds.max.y, 0.0001f);
        Object.DestroyImmediate(mesh);
    }

    [Test]
    public void GridBounds_EmptyGrid_IsZero()
    {
        Assert.AreEqual(Rect.zero, HexMetrics.GridBounds(0, 5, HexSize, HexOrientation.PointyTop));
    }

    [Test]
    public void AxialLine_Straight_ListsEveryCell()
    {
        var line = HexMetrics.AxialLine(Vector2Int.zero, new Vector2Int(3, 0));

        CollectionAssert.AreEqual(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }, line);
    }

    [Test]
    public void AxialLine_StepsAreNeighbours()
    {
        var a = new Vector2Int(-2, 1);
        var b = new Vector2Int(3, -4);
        var line = HexMetrics.AxialLine(a, b);

        Assert.AreEqual(HexMetrics.AxialDistance(a, b) + 1, line.Count);
        for (int i = 1; i < line.Count; i++)
            Assert.AreEqual(1, HexMetrics.AxialDistance(line[i - 1], line[i]));
    }
}
