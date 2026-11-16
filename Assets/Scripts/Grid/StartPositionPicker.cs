using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public readonly struct StartPick
{
    public readonly int Index;               // indice du candidat choisi
    public readonly int DistanceToNearest;   // distance au joueur déjà placé le plus proche (int.MaxValue pour le 1er)

    public StartPick(int index, int distance)
    {
        Index = index;
        DistanceToNearest = distance;
    }
}

public static class StartPositionPicker
{
    // Distance conseillée entre joueurs : chacun dispose d'environ landCount / playerCount cases,
    // soit un disque hexagonal de rayon r ≈ √(cases / 3) ; deux joueurs sont à environ 2r.
    public static int SuggestedMinDistance(int landCount, int playerCount)
    {
        if (playerCount <= 1 || landCount <= 0) return 0;
        float radius = Mathf.Sqrt(landCount / (3f * playerCount));
        return Mathf.Max(1, Mathf.FloorToInt(2f * radius));
    }

    // 1er joueur au hasard, puis chacun parmi les 10 % de candidats les plus éloignés des joueurs déjà placés.
    public static List<StartPick> Pick(IReadOnlyList<Vector2Int> candidates, int playerCount, System.Random rng)
    {
        var result = new List<StartPick>();
        int count = Math.Min(playerCount, candidates.Count);
        if (count <= 0) return result;

        int[] minDistance = Enumerable.Repeat(int.MaxValue, candidates.Count).ToArray();
        bool[] taken = new bool[candidates.Count];
        int topCount = Math.Max(1, candidates.Count / 10);

        for (int p = 0; p < count; p++)
        {
            int chosen = p == 0 ? rng.Next(candidates.Count) : PickAmongFarthest(minDistance, taken, topCount, rng);
            result.Add(new StartPick(chosen, minDistance[chosen]));
            taken[chosen] = true;

            for (int i = 0; i < candidates.Count; i++)
            {
                int d = HexMetrics.AxialDistance(candidates[i], candidates[chosen]);
                if (d < minDistance[i]) minDistance[i] = d;
            }
        }

        return result;
    }

    private static int PickAmongFarthest(int[] minDistance, bool[] taken, int topCount, System.Random rng)
    {
        var top = Enumerable.Range(0, minDistance.Length)
            .Where(i => !taken[i])
            .OrderByDescending(i => minDistance[i])
            .Take(topCount)
            .ToList();

        return top[rng.Next(top.Count)];
    }
}
