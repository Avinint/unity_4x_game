using System.Collections.Generic;
using NUnit.Framework;

public class TurnCycleTests
{
    private static readonly List<string> ABC = new List<string> { "A", "B", "C" };

    [Test]
    public void Order_FirstPlayerFirst_ThenOthersInListOrder()
    {
        var cycle = new TurnCycle<string>(new List<string> { "A", "B", "C", "D" }, "D");

        CollectionAssert.AreEqual(new[] { "D", "A", "B", "C" }, cycle.Order);
        Assert.AreEqual("D", cycle.Current);
    }

    [Test]
    public void Order_NoFirstPlayer_KeepsListOrder()
    {
        var cycle = new TurnCycle<string>(ABC, null);

        CollectionAssert.AreEqual(ABC, cycle.Order);
    }

    [Test]
    public void Order_IgnoresNullEntries()
    {
        var cycle = new TurnCycle<string>(new List<string> { "A", null, "B" }, null);

        CollectionAssert.AreEqual(new[] { "A", "B" }, cycle.Order);
    }

    [Test]
    public void Advance_MovesToNextPlayer_InSameRound()
    {
        var cycle = new TurnCycle<string>(ABC, null);

        bool newRound = cycle.Advance();

        Assert.IsFalse(newRound);
        Assert.AreEqual("B", cycle.Current);
        Assert.AreEqual(1, cycle.Round);
    }

    [Test]
    public void Advance_AfterLastPlayer_StartsNewRound()
    {
        var cycle = new TurnCycle<string>(ABC, null);
        cycle.Advance();
        cycle.Advance();

        bool newRound = cycle.Advance();

        Assert.IsTrue(newRound);
        Assert.AreEqual("A", cycle.Current);
        Assert.AreEqual(2, cycle.Round);
    }

    [Test]
    public void Advance_SinglePlayer_EachAdvanceIsNewRound()
    {
        var cycle = new TurnCycle<string>(new List<string> { "A" }, null);

        Assert.IsTrue(cycle.Advance());
        Assert.IsTrue(cycle.Advance());
        Assert.AreEqual("A", cycle.Current);
        Assert.AreEqual(3, cycle.Round);
    }

    [Test]
    public void Advance_EmptyOrder_NoCurrentPlayer()
    {
        var cycle = new TurnCycle<string>(new List<string>(), null);

        Assert.IsFalse(cycle.Advance());
        Assert.IsNull(cycle.Current);
        Assert.AreEqual(1, cycle.Round);
    }
}
