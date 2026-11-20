using NUnit.Framework;

public class PathPreviewTests
{
    [Test]
    public void StateFor_NoPath_IsForbidden() =>
        Assert.AreEqual(DestinationState.Forbidden, PathPreview.StateFor(false, true));

    [Test]
    public void StateFor_PathToUnseenTile_IsOutOfSight() =>
        Assert.AreEqual(DestinationState.OutOfSight, PathPreview.StateFor(true, false));

    [Test]
    public void StateFor_PathToSeenTile_IsReachable() =>
        Assert.AreEqual(DestinationState.Reachable, PathPreview.StateFor(true, true));
}
