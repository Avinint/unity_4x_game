using System;
using System.Collections.Generic;
using UnityEngine;

// Écoulement de l'eau à la génération. Ne connaît pas la carte : voisins fournis par l'appelant.
public static class Hydrology
{
    public static readonly Vector2Int NoDownstream = new Vector2Int(-1, -1);

    // Case aval de chaque terre (vers la mer, cuvettes comprises) et débit cumulé (pluie = `rain`).
    // Remplissage par priorité : depuis l'eau, on remonte toujours par la case la plus basse non traitée.
    public static (Vector2Int[,] downstream, float[,] flow) Flow(
        float[,] altitude, Relief[,] relief, float[,] rain, Func<int, int, IEnumerable<Vector2Int>> neighbours)
    {
        int w = altitude.GetLength(0), h = altitude.GetLength(1);
        var downstream = new Vector2Int[w, h];
        var flow = new float[w, h];
        var filled = new float[w, h];
        var done = new bool[w, h];
        var queue = new SortedSet<(float level, int index)>();
        var order = new List<Vector2Int>();   // terres dans l'ordre de traitement (de l'aval vers l'amont)

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                downstream[x, y] = NoDownstream;
                if (relief[x, y] != Relief.Water) { flow[x, y] = rain[x, y]; continue; }
                done[x, y] = true;
                filled[x, y] = altitude[x, y];
                queue.Add((altitude[x, y], y * w + x));
            }

        while (queue.Count > 0)
        {
            var current = queue.Min;
            queue.Remove(current);
            int cx = current.index % w, cy = current.index / w;
            foreach (var n in neighbours(cx, cy))
            {
                if (n.x < 0 || n.x >= w || n.y < 0 || n.y >= h || done[n.x, n.y]) continue;
                done[n.x, n.y] = true;
                filled[n.x, n.y] = Mathf.Max(altitude[n.x, n.y], filled[cx, cy] + 1e-5f);   // cuvette : remplie jusqu'au col
                downstream[n.x, n.y] = new Vector2Int(cx, cy);
                order.Add(n);
                queue.Add((filled[n.x, n.y], n.y * w + n.x));
            }
        }

        // De l'amont vers l'aval : chaque terre verse son débit à sa case aval.
        for (int i = order.Count - 1; i >= 0; i--)
        {
            var c = order[i];
            var d = downstream[c.x, c.y];
            if (relief[d.x, d.y] != Relief.Water) flow[d.x, d.y] += flow[c.x, c.y];
        }
        return (downstream, flow);
    }

    // Fleuve navigable : les plus forts débits, en proportion des terres (jamais en montagne).
    // Les rivières plus petites sont sur les arêtes (RiverNetwork).
    public static bool[,] Navigable(float[,] flow, Relief[,] relief, MapRulesSettings s)
    {
        int w = flow.GetLength(0), h = flow.GetLength(1);
        var landFlows = new List<float>();
        foreach (var (x, y) in Cells(w, h)) if (relief[x, y] != Relief.Water) landFlows.Add(flow[x, y]);
        float navigableAbove = MapRules.Percentile(landFlows, 1f - s.NavigableFraction);

        var navigable = new bool[w, h];
        foreach (var (x, y) in Cells(w, h))
        {
            if (relief[x, y] == Relief.Water || landFlows.Count == 0) continue;
            navigable[x, y] = flow[x, y] >= navigableAbove && relief[x, y] != Relief.Mountain;
        }
        return navigable;
    }

    // Retire les fleuves navigables trop courts. Un fleuve = toutes les cases qui descendent vers la même
    // embouchure (dernière case navigable avant la mer) ; sa longueur = le plus long trajet jusqu'à elle,
    // donc un petit affluent d'un grand fleuve est gardé.
    public static bool[,] KeepLongRivers(bool[,] navigable, Vector2Int[,] downstream, int minLength)
    {
        int w = navigable.GetLength(0), h = navigable.GetLength(1);
        var mouthOf = new Vector2Int[w, h];
        var longest = new Dictionary<Vector2Int, int>();   // embouchure → plus long trajet en cases
        foreach (var (x, y) in Cells(w, h))
        {
            if (!navigable[x, y]) continue;
            var c = new Vector2Int(x, y);
            int length = 1;
            while (IsNavigable(navigable, downstream[c.x, c.y])) { c = downstream[c.x, c.y]; length++; }
            mouthOf[x, y] = c;
            longest[c] = Mathf.Max(length, longest.TryGetValue(c, out int l) ? l : 0);
        }

        var kept = new bool[w, h];
        foreach (var (x, y) in Cells(w, h))
            kept[x, y] = navigable[x, y] && longest[mouthOf[x, y]] >= minLength;
        return kept;
    }

    private static bool IsNavigable(bool[,] navigable, Vector2Int c) =>
        c != NoDownstream && navigable[c.x, c.y];

    private static IEnumerable<(int, int)> Cells(int w, int h)
    {
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) yield return (x, y);
    }
}
