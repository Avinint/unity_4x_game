using System;
using System.Collections.Generic;
using UnityEngine;

// A* sur coordonnées axiales : le plus court en cases, ou le moins de tours selon un coût par pas.
public static class PathFinder
{
    private static readonly Vector2Int[] Directions =
    {
        new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)
    };

    // Chemin de origin (exclue) à destination (incluse). Liste vide si origin == destination,
    // null si destination inaccessible. isPassable dit si une case existe et peut être traversée.
    // Le plus court en nombre de cases.
    public static List<Vector2Int> FindPath(Vector2Int origin, Vector2Int destination, Func<Vector2Int, bool> isPassable,
        Func<Vector2Int, Vector2Int> wrap = null, Func<Vector2Int, Vector2Int, int> distance = null)
        => Search(origin, destination, isPassable, 0, (score, _, _) => score + 1, wrap, distance);

    // Chemin qui prend le moins de tours, puis, à égalité, qui laisse le plus de points.
    // stepCost(depuis, vers) : coût du pas (≥ 1), par exemple plus cher à travers une rivière.
    // Même règle que le déplacement : on entre dans une case s'il reste au moins un point,
    // les points ne descendent pas sous 0, et un nouveau tour rend maxPoints.
    public static List<Vector2Int> FindPath(Vector2Int origin, Vector2Int destination,
        Func<Vector2Int, bool> isPassable, Func<Vector2Int, Vector2Int, int> stepCost,
        int startPoints, int maxPoints,
        Func<Vector2Int, Vector2Int> wrap = null, Func<Vector2Int, Vector2Int, int> distance = null)
    {
        maxPoints = Mathf.Max(1, maxPoints);
        // État (tour, points restants) résumé en un nombre, plus petit = meilleur :
        // tour × (max + 1) + points déjà dépensés dans ce tour.
        int PathScore(int turn, int points) => turn * (maxPoints + 1) + (maxPoints - points);
        return Search(origin, destination, isPassable, PathScore(0, Mathf.Clamp(startPoints, 0, maxPoints)),
            (score, from, to) =>
            {
                int turn = score / (maxPoints + 1);
                int points = maxPoints - score % (maxPoints + 1);
                if (points <= 0) { turn++; points = maxPoints; }   // plus de point : on attend le tour suivant
                return PathScore(turn, Mathf.Max(0, points - stepCost(from, to)));
            }, wrap, distance);
    }

    // A* : advance(score, depuis, vers) donne le score après un pas (il augmente d'au moins 1,
    // donc la distance reste une bonne estimation).
    // wrap ramène une case dans la carte (monde qui fait le tour), distance sert d'estimation ; par défaut : sans bouclage.
    private static List<Vector2Int> Search(Vector2Int origin, Vector2Int destination,
        Func<Vector2Int, bool> isPassable, int startScore, Func<int, Vector2Int, Vector2Int, int> advance,
        Func<Vector2Int, Vector2Int> wrap, Func<Vector2Int, Vector2Int, int> distance)
    {
        wrap ??= v => v;
        distance ??= HexMetrics.AxialDistance;
        if (origin == destination) return new List<Vector2Int>();
        if (!isPassable(destination)) return null;

        var open = new List<Vector2Int> { origin };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costFromOrigin = new Dictionary<Vector2Int, int> { [origin] = startScore };
        var closed = new HashSet<Vector2Int>();

        while (open.Count > 0)
        {
            Vector2Int current = Cheapest(open, costFromOrigin, destination, distance);
            if (current == destination) return Rebuild(cameFrom, origin, destination);

            open.Remove(current);
            closed.Add(current);

            foreach (var dir in Directions)
            {
                Vector2Int next = wrap(current + dir);
                if (closed.Contains(next) || !isPassable(next)) continue;

                int cost = advance(costFromOrigin[current], current, next);
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
    private static Vector2Int Cheapest(List<Vector2Int> open, Dictionary<Vector2Int, int> costFromOrigin, Vector2Int destination,
        Func<Vector2Int, Vector2Int, int> distance)
    {
        Vector2Int best = open[0];
        int bestH = distance(best, destination);
        int bestF = costFromOrigin[best] + bestH;
        for (int i = 1; i < open.Count; i++)
        {
            int h = distance(open[i], destination);
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
