using NUnit.Framework;
using UnityEngine;

public class HexMetricsTests
{
    [Test]
    public void WrapAxial_BringsColumnBackIntoMap()
    {
        // Ligne paire puis impaire : la colonne -1 devient 39, la ligne ne change pas.
        foreach (int row in new[] { 4, 5 })
        {
            var outside = HexMetrics.OffsetToAxial(-1, row, HexOrientation.PointyTop);
            Assert.AreEqual(HexMetrics.OffsetToAxial(39, row, HexOrientation.PointyTop), HexMetrics.WrapAxial(outside, 40));
        }
    }

    [Test]
    public void WrapAxial_InsideMap_Unchanged()
    {
        var inside = HexMetrics.OffsetToAxial(12, 7, HexOrientation.PointyTop);

        Assert.AreEqual(inside, HexMetrics.WrapAxial(inside, 40));
    }

    [Test]
    public void BuildRingMesh_RadiiWithinRing()
    {
        var mesh = HexMetrics.BuildRingMesh(16, (4f, 1f));
        foreach (var v in mesh.vertices)
        {
            float r = new Vector2(v.x, v.z).magnitude;
            Assert.That(r, Is.InRange(2.999f, 4.001f));
        }
    }

    [Test]
    public void BuildRingMesh_FacesUp()
    {
        var mesh = HexMetrics.BuildRingMesh(16, (4f, 1f), (2f, 0.5f));
        var v = mesh.vertices; var t = mesh.triangles;
        for (int i = 0; i < t.Length; i += 3)
            Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
    }

    [Test]
    public void BoundaryEdges_SingleCell_SixEdges() =>
        Assert.AreEqual(6, HexMetrics.BoundaryEdges(new[] { new Vector2Int(3, 3) }).Count);

    [Test]
    public void BoundaryEdges_TwoNeighbours_TenEdges() =>
        Assert.AreEqual(10, HexMetrics.BoundaryEdges(new[] { new Vector2Int(3, 3), new Vector2Int(4, 3) }).Count);

    [Test]
    public void WrapShift_NearColumnStays() => Assert.AreEqual(0f, HexMetrics.WrapShift(10f, 12f, 100f));

    [Test]
    public void WrapShift_FarColumnComesAround()
    {
        // Caméra au bord est (95) : la colonne 2 est affichée juste après, à 102.
        Assert.AreEqual(100f, HexMetrics.WrapShift(2f, 95f, 100f));
        Assert.AreEqual(-100f, HexMetrics.WrapShift(98f, 5f, 100f));
    }

    [Test]
    public void WrappedDistance_AcrossSeam_IsOne()
    {
        var west = HexMetrics.OffsetToAxial(0, 6, HexOrientation.PointyTop);
        var east = HexMetrics.OffsetToAxial(39, 6, HexOrientation.PointyTop);

        Assert.AreEqual(1, HexMetrics.WrappedDistance(west, east, 40));
        Assert.AreEqual(39, HexMetrics.AxialDistance(west, east));
    }

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
    public void SharedEdge_IsTheCommonSideOfTwoNeighbours()
    {
        Vector3 a = HexMetrics.Center(HexSize, 2, 2, HexOrientation.PointyTop);
        Vector3 b = HexMetrics.Center(HexSize, 3, 2, HexOrientation.PointyTop);   // voisine à l'est
        var (p, q) = HexMetrics.SharedEdge(a, b, HexSize, HexOrientation.PointyTop);

        Assert.AreEqual(HexSize, Vector3.Distance(p, q), 0.001f);                   // une arête = un côté
        Assert.AreEqual(0f, ((a + b) / 2f - (p + q) / 2f).magnitude, 0.001f);     // au milieu des deux centres
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
