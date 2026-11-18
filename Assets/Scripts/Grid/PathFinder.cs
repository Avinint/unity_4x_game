using System;
using System.Collections.Generic;
using UnityEngine;

// A* sur coordonnées axiales, coût 1 par case par défaut, ou coût par pas.
public static class PathFinder
{
    private static readonly Vector2Int[] Directions =
    {
        new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)
    };

    // Chemin de origin (exclue) à destination (incluse). Liste vide si origin == destination,
    // null si destination inaccessible. isPassable dit si une case existe et peut être traversée.
    // Coût 1 par case.
    public static List<Vector2Int> FindPath(Vector2Int origin, Vector2Int destination, Func<Vector2Int, bool> isPassable)
        => FindPath(origin, destination, isPassable, _ => 1);

    // stepCost(case) : coût pour entrer dans la case (≥ 1).
    public static List<Vector2Int> FindPath(Vector2Int origin, Vector2Int destination,
        Func<Vector2Int, bool> isPassable, Func<Vector2Int, int> stepCost)
        => FindPath(origin, destination, isPassable, (_, to) => stepCost(to));

    // stepCost(depuis, vers) : coût du pas (≥ 1), par exemple plus cher à travers une rivière.
    // Renvoie le chemin le moins coûteux.
    public static List<Vector2Int> FindPath(Vector2Int origin, Vector2Int destination,
        Func<Vector2Int, bool> isPassable, Func<Vector2Int, Vector2Int, int> stepCost)
    {
        if (origin == destination) return new List<Vector2Int>();
        if (!isPassable(destination)) return null;

        var open = new List<Vector2Int> { origin };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costFromOrigin = new Dictionary<Vector2Int, int> { [origin] = 0 };
        var closed = new HashSet<Vector2Int>();

        while (open.Count > 0)
        {
            Vector2Int current = Cheapest(open, costFromOrigin, destination);
            if (current == destination) return Rebuild(cameFrom, origin, destination);

            open.Remove(current);
            closed.Add(current);

            foreach (var dir in Directions)
            {
                Vector2Int next = current + dir;
                if (closed.Contains(next) || !isPassable(next)) continue;

                int cost = costFromOrigin[current] + stepCost(current, next);
                if (costFromOrigin.TryGetValue(next, out int known) && cost >= known) continue;

                costFromOrigin[next] = cost;
                cameFrom[next] = current;
                if (!open.Contains(next)) open.Add(next);
            }
        }
        return null;
    }

    // Nombre de cases du chemin parcourues avec `points` : on peut entrer dans une case
    // tant qu'il reste au moins un point ; une case plus chère que les points restants les consomme tous.
    public static int ReachableCount(IReadOnlyList<int> stepCosts, int points)
    {
        int count = 0;
        foreach (int cost in stepCosts)
        {
            if (points <= 0) break;
            points -= cost;
            count++;
        }
        return count;
    }

    // Nombre de cases à parcourir en reculant depuis `count` jusqu'à une case où l'on peut s'arrêter
    // (canStopAt(i) : arrêt possible sur la i-ème case du chemin). 0 si aucune.
    public static int LastStop(int count, Func<int, bool> canStopAt)
    {
        while (count > 0 && !canStopAt(count - 1)) count--;
        return count;
    }

    // Plus petit coût estimé ; à égalité, la case la plus proche de la destination.
    private static Vector2Int Cheapest(List<Vector2Int> open, Dictionary<Vector2Int, int> costFromOrigin, Vector2Int destination)
    {
        Vector2Int best = open[0];
        int bestH = HexMetrics.AxialDistance(best, destination);
        int bestF = costFromOrigin[best] + bestH;
        for (int i = 1; i < open.Count; i++)
        {
            int h = HexMetrics.AxialDistance(open[i], destination);
            int f = costFromOrigin[open[i]] + h;
            if (f < bestF || (f == bestF && h < bestH)) { best = open[i]; bestF = f; bestH = h; }
        }
        return best;
    }

    private static List<Vector2Int> Rebuild(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int origin, Vector2Int destination)
    {
        var path = new List<Vector2Int>();
        for (Vector2Int c = destination; c != origin; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        return path;
    }
}
