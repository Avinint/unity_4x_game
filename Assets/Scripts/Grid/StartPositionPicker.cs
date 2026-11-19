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
    // Qualité d'un départ. La case de départ elle-même : plate ou en colline, dans un climat où l'on peut vivre.
    public static bool IsGoodStart(Relief relief, Biome biome) =>
        (relief == Relief.Flat || relief == Relief.Hill) &&
        (biome == Biome.Grassland || biome == Biome.Plain || biome == Biome.Forest || biome == Biome.Jungle);

    public const int FreshWaterBonus = 4;

    // Valeur d'une case autour d'un départ : terres cultivables d'abord, un peu d'eau utile, montagne et neige gênent.
    public static int CellValue(Relief relief, Biome biome)
    {
        if (relief == Relief.Mountain) return -1;
        int value;
        switch (biome)
        {
            case Biome.Grassland: case Biome.Plain: value = 2; break;
            case Biome.Forest: case Biome.Jungle: case Biome.Coast: case Biome.Lake: case Biome.River: value = 1; break;
            case Biome.Snow: value = -1; break;
            default: value = 0; break;   // désert, toundra, taïga, marais, océan
        }
        return relief == Relief.Hill ? Mathf.Min(value, 1) : value;   // colline : +1 au plus
    }

    // Note d'un départ : valeur des cases autour, plus un bonus si la case de départ touche de l'eau douce.
    public static int Score(IEnumerable<(Relief relief, Biome biome)> around, bool freshWater) =>
        around.Sum(c => CellValue(c.relief, c.biome)) + (freshWater ? FreshWaterBonus : 0);

    // Distance conseillée entre joueurs : chacun dispose d'environ landCount / playerCount cases,
    // soit un disque hexagonal de rayon r ≈ √(cases / 3) ; deux joueurs sont à environ 2r.
    // Plancher : 2 × startRadius + 2, pour que les zones de départ ne se touchent pas (une case libre entre elles).
    public static int SuggestedMinDistance(int landCount, int playerCount, int startRadius = 0)
    {
        if (playerCount <= 1 || landCount <= 0) return 0;
        float radius = Mathf.Sqrt(landCount / (3f * playerCount));
        return Mathf.Max(Mathf.Max(1, Mathf.FloorToInt(2f * radius)), 2 * startRadius + 2);
    }

    // 1er joueur au hasard, puis chacun au hasard parmi les candidats à au moins `minDistance` des joueurs déjà placés ;
    // s'il n'y en a aucun (ou si minDistance ≤ 0), parmi les 10 % de candidats les plus éloignés.
    public static List<StartPick> Pick(IReadOnlyList<Vector2Int> candidates, int playerCount, System.Random rng, int minDistance = 0)
    {
        var result = new List<StartPick>();
        int count = Math.Min(playerCount, candidates.Count);
        if (count <= 0) return result;

        int[] nearest = Enumerable.Repeat(int.MaxValue, candidates.Count).ToArray();   // distance au joueur placé le plus proche
        bool[] taken = new bool[candidates.Count];
        int topCount = Math.Max(1, candidates.Count / 10);

        for (int p = 0; p < count; p++)
        {
            int chosen = p == 0 ? rng.Next(candidates.Count)
                : PickFarEnough(nearest, taken, minDistance, rng) ?? PickAmongFarthest(nearest, taken, topCount, rng);
            result.Add(new StartPick(chosen, nearest[chosen]));
            taken[chosen] = true;

            for (int i = 0; i < candidates.Count; i++)
            {
                int d = HexMetrics.AxialDistance(candidates[i], candidates[chosen]);
                if (d < nearest[i]) nearest[i] = d;
            }
        }

        return result;
    }

    // Un candidat libre à au moins minDistance de tous les joueurs placés, au hasard ; null s'il n'y en a pas.
    private static int? PickFarEnough(int[] nearest, bool[] taken, int minDistance, System.Random rng)
    {
        if (minDistance <= 0) return null;
        var allowed = Enumerable.Range(0, nearest.Length).Where(i => !taken[i] && nearest[i] >= minDistance).ToList();
        return allowed.Count > 0 ? allowed[rng.Next(allowed.Count)] : (int?)null;
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
