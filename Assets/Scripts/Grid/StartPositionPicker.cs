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
    // Qualité d'un départ : plat ou accidenté, sur prairie, plaines ou plaine tropicale, hors marais.
    public static bool IsGoodStart(Relief relief, Biome biome, Feature feature) =>
        MapRules.IsPassable(relief) && feature != Feature.Marsh &&
        (biome == Biome.Grassland || biome == Biome.Plains || biome == Biome.Tropical);

    public const int FreshWaterBonus = 4;

    // Valeur d'une case autour d'un départ : terres cultivables d'abord, un peu d'eau utile, montagne et neige gênent.
    public static int CellValue(Relief relief, Biome biome, Feature feature)
    {
        if (relief == Relief.Mountain) return -1;
        if (relief == Relief.NavigableRiver) return 1;   // eau douce
        int value;
        switch (biome)
        {
            case Biome.Grassland: case Biome.Plains: case Biome.Tropical:
                value = feature == Feature.None ? 2 : feature == Feature.Marsh ? 0 : 1; break;
            case Biome.Coast: case Biome.Lake: value = 1; break;
            case Biome.Tundra: value = feature == Feature.Snow ? -1 : 0; break;
            default: value = 0; break;   // désert, océan
        }
        return relief == Relief.Hill ? Mathf.Min(value, 1) : value;   // accidenté : +1 au plus
    }

    // Taille de la masse de terre de chaque case : cases de `land` reliées à pied par leurs voisines hexagonales
    // (la mer, les lacs, les fleuves et les montagnes, absents de `land`, coupent la marche).
    public static Dictionary<Vector2Int, int> LandmassSizes(IEnumerable<Vector2Int> land, Func<Vector2Int, Vector2Int> wrap = null)
    {
        wrap ??= v => v;
        var isLand = new HashSet<Vector2Int>(land);
        var size = new Dictionary<Vector2Int, int>();
        foreach (var start in isLand)
        {
            if (size.ContainsKey(start)) continue;
            var mass = new List<Vector2Int> { start };
            var seen = new HashSet<Vector2Int> { start };
            var stack = new Stack<Vector2Int>();
            stack.Push(start);
            while (stack.Count > 0)
                foreach (var raw in HexMetrics.GetNeighbourCoordinatesList(stack.Pop()))
                {
                    var n = wrap(raw);
                    if (isLand.Contains(n) && seen.Add(n)) { mass.Add(n); stack.Push(n); }
                }
            foreach (var c in mass) size[c] = mass.Count;
        }
        return size;
    }

    // Masse de terre minimale conseillée pour un départ : un quart de la part de terre d'un joueur
    // (exclut les petites îles sans place pour s'étendre).
    public static int SuggestedMinLandmass(int landCount, int playerCount) =>
        playerCount <= 0 ? 0 : landCount / (4 * playerCount);

    // Note d'un départ : valeur des cases autour, plus un bonus si la case de départ touche de l'eau douce.
    public static int Score(IEnumerable<(Relief relief, Biome biome, Feature feature)> around, bool freshWater) =>
        around.Sum(c => CellValue(c.relief, c.biome, c.feature)) + (freshWater ? FreshWaterBonus : 0);

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
    public static List<StartPick> Pick(IReadOnlyList<Vector2Int> candidates, int playerCount, System.Random rng, int minDistance = 0,
        Func<Vector2Int, Vector2Int, int> distance = null)
    {
        distance ??= HexMetrics.AxialDistance;
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
                int d = distance(candidates[i], candidates[chosen]);
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
