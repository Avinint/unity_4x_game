using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class StartPositionPickerTests
{
    // Grille 10 x 10 de coordonnées axiales.
    private static List<Vector2Int> Grid10x10()
    {
        var candidates = new List<Vector2Int>();
        for (int q = 0; q < 10; q++)
            for (int r = 0; r < 10; r++)
                candidates.Add(new Vector2Int(q, r));
        return candidates;
    }

    // Ligne de 30 cases : q = 0..29, r = 0.
    private static List<Vector2Int> Line30()
    {
        return Enumerable.Range(0, 30).Select(q => new Vector2Int(q, 0)).ToList();
    }

    [Test]
    public void Pick_ReturnsRequestedNumberOfPlayers()
    {
        var picks = StartPositionPicker.Pick(Grid10x10(), 5, new System.Random(1));

        Assert.AreEqual(5, picks.Count);
    }

    [Test]
    public void Pick_ReturnsDistinctTiles()
    {
        var picks = StartPositionPicker.Pick(Grid10x10(), 20, new System.Random(2));

        Assert.AreEqual(picks.Count, picks.Select(p => p.Index).Distinct().Count());
    }

    [Test]
    public void Pick_WithFewerCandidatesThanPlayers_ReturnsOnePerCandidate()
    {
        var candidates = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(5, 5), new Vector2Int(9, 1) };

        var picks = StartPositionPicker.Pick(candidates, 8, new System.Random(3));

        Assert.AreEqual(3, picks.Count);
        Assert.AreEqual(3, picks.Select(p => p.Index).Distinct().Count());
    }

    [Test]
    public void Pick_WithoutCandidates_ReturnsEmpty()
    {
        var picks = StartPositionPicker.Pick(new List<Vector2Int>(), 4, new System.Random(4));

        Assert.IsEmpty(picks);
    }

    [Test]
    public void Pick_SecondPlayerIsAmongTheFarthestFromTheFirst()
    {
        var candidates = Line30();

        var picks = StartPositionPicker.Pick(candidates, 2, new System.Random(5));

        Vector2Int first = candidates[picks[0].Index];
        Vector2Int second = candidates[picks[1].Index];
        // 10 % de 30 candidats = les 3 plus éloignés : le 2e doit être au moins aussi loin que le 3e plus éloigné.
        int thirdFarthest = candidates.Select(c => HexMetrics.AxialDistance(c, first))
            .OrderByDescending(d => d)
            .ElementAt(2);

        Assert.GreaterOrEqual(HexMetrics.AxialDistance(first, second), thirdFarthest);
    }

    [Test]
    public void Pick_SameSeed_GivesSameResult()
    {
        var a = StartPositionPicker.Pick(Grid10x10(), 5, new System.Random(42)).Select(p => p.Index).ToList();
        var b = StartPositionPicker.Pick(Grid10x10(), 5, new System.Random(42)).Select(p => p.Index).ToList();

        CollectionAssert.AreEqual(a, b);
    }

    [Test]
    public void Pick_ReportsTrueDistanceToNearestPreviousPick()
    {
        var candidates = Grid10x10();

        var picks = StartPositionPicker.Pick(candidates, 6, new System.Random(7));

        for (int p = 1; p < picks.Count; p++)
        {
            int expected = Enumerable.Range(0, p)
                .Min(q => HexMetrics.AxialDistance(candidates[picks[p].Index], candidates[picks[q].Index]));
            Assert.AreEqual(expected, picks[p].DistanceToNearest, $"joueur {p}");
        }
    }

    [Test]
    public void SuggestedMinDistance_140LandFor5Players_Is6()
    {
        Assert.AreEqual(6, StartPositionPicker.SuggestedMinDistance(140, 5));
    }

    [Test]
    public void SuggestedMinDistance_SinglePlayer_IsZero()
    {
        Assert.AreEqual(0, StartPositionPicker.SuggestedMinDistance(140, 1));
    }

    [Test]
    public void SuggestedMinDistance_GrowsWithLand()
    {
        int small = StartPositionPicker.SuggestedMinDistance(300, 3);
        int large = StartPositionPicker.SuggestedMinDistance(1200, 3);

        Assert.That(large, Is.InRange(2 * small - 1, 2 * small + 1));
    }
}
