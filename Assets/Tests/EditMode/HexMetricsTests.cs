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
}
