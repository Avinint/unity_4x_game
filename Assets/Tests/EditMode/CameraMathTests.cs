using NUnit.Framework;
using UnityEngine;

public class CameraMathTests
{
    private const float Min = 40f, Max = 250f, Near = 40f, Far = 70f;

    [Test]
    public void Pitch_AtMinAndMax_ReturnsBounds()
    {
        Assert.AreEqual(Near, CameraMath.Pitch(Min, Min, Max, Near, Far), 0.0001f);
        Assert.AreEqual(Far, CameraMath.Pitch(Max, Min, Max, Near, Far), 0.0001f);
    }

    [Test]
    public void Pitch_IsClampedOutsideRange()
    {
        Assert.AreEqual(Near, CameraMath.Pitch(10f, Min, Max, Near, Far), 0.0001f);
        Assert.AreEqual(Far, CameraMath.Pitch(500f, Min, Max, Near, Far), 0.0001f);
    }

    [TestCase(40f)]
    [TestCase(120f)]
    [TestCase(250f)]
    public void TiltedOffset_HasRequestedLength(float distance)
    {
        Assert.AreEqual(distance, CameraMath.TiltedOffset(distance, Min, Max, Near, Far).magnitude, 0.001f);
    }

    private static readonly Vector2 Screen = new Vector2(1000f, 600f);
    private const float Margin = 15f;

    [Test]
    public void EdgeScroll_Center_IsZero()
    {
        Assert.AreEqual(Vector2.zero, CameraMath.EdgeScrollDirection(new Vector2(500f, 300f), Screen, Margin));
    }

    [Test]
    public void EdgeScroll_LeftEdge_PointsLeft()
    {
        Assert.AreEqual(new Vector2(-1f, 0f), CameraMath.EdgeScrollDirection(new Vector2(5f, 300f), Screen, Margin));
    }

    [Test]
    public void EdgeScroll_TopRightCorner_IsNormalizedDiagonal()
    {
        Vector2 direction = CameraMath.EdgeScrollDirection(new Vector2(995f, 595f), Screen, Margin);

        Assert.AreEqual(1f, direction.magnitude, 0.0001f);
        Assert.Greater(direction.x, 0f);
        Assert.Greater(direction.y, 0f);
    }

    [Test]
    public void EdgeScroll_OutsideWindow_IsZero()
    {
        Assert.AreEqual(Vector2.zero, CameraMath.EdgeScrollDirection(new Vector2(-10f, 300f), Screen, Margin));
        Assert.AreEqual(Vector2.zero, CameraMath.EdgeScrollDirection(new Vector2(500f, 700f), Screen, Margin));
    }

    [Test]
    public void FitDistance_GrowsWithMapSize()
    {
        float small = CameraMath.FitDistance(new Vector2(100f, 60f), 30f, 16f / 9f, 70f);
        float large = CameraMath.FitDistance(new Vector2(200f, 120f), 30f, 16f / 9f, 70f);

        Assert.AreEqual(2f * small, large, 0.01f);
    }

    [Test]
    public void FitDistance_WideMapLimitedByWidth()
    {
        // Carte très large et peu profonde : seule la largeur compte, la profondeur ne change rien.
        float a = CameraMath.FitDistance(new Vector2(400f, 10f), 30f, 16f / 9f, 70f);
        float b = CameraMath.FitDistance(new Vector2(400f, 20f), 30f, 16f / 9f, 70f);

        Assert.AreEqual(a, b, 0.0001f);
    }

    [Test]
    public void FitDistance_ZeroSize_IsZero()
    {
        Assert.AreEqual(0f, CameraMath.FitDistance(Vector2.zero, 30f, 16f / 9f, 70f), 0.0001f);
    }

    [Test]
    public void GroundExtent_TopDown_Symmetric()
    {
        var (south, north) = CameraMath.GroundExtent(100f, 90f, 30f);

        float expected = 100f * Mathf.Tan(15f * Mathf.Deg2Rad);
        Assert.AreEqual(expected, north, 0.01f);
        Assert.AreEqual(expected, south, 0.01f);
    }

    [Test]
    public void GroundExtent_Tilted_SeesFartherNorth()
    {
        var (south, north) = CameraMath.GroundExtent(100f, 40f, 30f);

        Assert.Greater(south, 0f);
        Assert.Greater(north, south);
    }

    [Test]
    public void ClampSpan_KeepsViewInside()
    {
        Assert.AreEqual(15f, CameraMath.ClampSpan(0f, 0f, 100f, 15f, 30f), 0.0001f);     // bord sud
        Assert.AreEqual(70f, CameraMath.ClampSpan(95f, 0f, 100f, 15f, 30f), 0.0001f);    // bord nord
        Assert.AreEqual(40f, CameraMath.ClampSpan(40f, 0f, 100f, 15f, 30f), 0.0001f);    // libre
    }

    [Test]
    public void ClampSpan_ViewLargerThanMap_Centred()
    {
        // Vue de 60 au sud et 80 au nord sur une carte de 100 : chaque côté dépasse de 20.
        Assert.AreEqual(40f, CameraMath.ClampSpan(10f, 0f, 100f, 60f, 80f), 0.0001f);
    }

    [Test]
    public void TiltedOffset_IsSouthAndAbove()
    {
        Vector3 offset = CameraMath.TiltedOffset(120f, Min, Max, Near, Far);

        Assert.AreEqual(0f, offset.x, 0.0001f);
        Assert.Greater(offset.y, 0f);
        Assert.Less(offset.z, 0f);
    }
}
