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

    [Test]
    public void TiltedOffset_IsSouthAndAbove()
    {
        Vector3 offset = CameraMath.TiltedOffset(120f, Min, Max, Near, Far);

        Assert.AreEqual(0f, offset.x, 0.0001f);
        Assert.Greater(offset.y, 0f);
        Assert.Less(offset.z, 0f);
    }
}
