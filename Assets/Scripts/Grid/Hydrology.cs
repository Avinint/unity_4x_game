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
    // Jamais sur le bord de la carte : l'eau ne peut pas en sortir, le bord collecte le débit de toute une région.
    // Les rivières plus petites sont sur les arêtes (RiverNetwork).
    public static bool[,] Navigable(float[,] flow, Relief[,] relief, MapRulesSettings s)
    {
        int w = flow.GetLength(0), h = flow.GetLength(1);
        bool OnEdge(int x, int y) => x == 0 || y == 0 || x == w - 1 || y == h - 1;
        var landFlows = new List<float>();
        foreach (var (x, y) in Cells(w, h)) if (relief[x, y] != Relief.Water && !OnEdge(x, y)) landFlows.Add(flow[x, y]);
        float navigableAbove = MapRules.Percentile(landFlows, 1f - s.NavigableFraction);

        var navigable = new bool[w, h];
        foreach (var (x, y) in Cells(w, h))
        {
            if (relief[x, y] == Relief.Water || OnEdge(x, y) || landFlows.Count == 0) continue;
            navigable[x, y] = flow[x, y] >= navigableAbove && relief[x, y] != Relief.Mountain;
        }
        return navigable;
    }

    // Fleuves d'une case de large : de l'aval vers l'amont (débit décroissant), une case est gardée si son aval
    // est de l'eau ou gardé, et si elle ne touche aucune case gardée hors de son aval et de l'aval de celui-ci
    // (virage serré toléré). Un affluent qui touche le fleuve sur deux cases est retiré avec son amont.
    public static bool[,] SingleWidth(bool[,] navigable, float[,] flow, Vector2Int[,] downstream,
        Func<int, int, IEnumerable<Vector2Int>> neighbours)
    {
        int w = navigable.GetLength(0), h = navigable.GetLength(1);
        var order = new List<Vector2Int>();
        foreach (var (x, y) in Cells(w, h)) if (navigable[x, y]) order.Add(new Vector2Int(x, y));
        order.Sort((a, b) => flow[b.x, b.y].CompareTo(flow[a.x, a.y]));   // l'aval a toujours un débit au moins égal

        var kept = new bool[w, h];
        foreach (var c in order)
        {
            var d = downstream[c.x, c.y];
            if (d == NoDownstream) continue;
            if (navigable[d.x, d.y] && !kept[d.x, d.y]) continue;   // aval navigable retiré : l'amont est coupé
            var dd = navigable[d.x, d.y] ? downstream[d.x, d.y] : NoDownstream;
            bool touchesOther = false;
            foreach (var n in neighbours(c.x, c.y))
            {
                if (n.x < 0 || n.x >= w || n.y < 0 || n.y >= h || !kept[n.x, n.y]) continue;
                if (n != d && n != dd) { touchesOther = true; break; }
            }
            kept[c.x, c.y] = !touchesOther;
        }
        return kept;
    }

    // Retire les affluents trop courts : à chaque confluent, la branche la plus longue (puis au plus fort débit)
    // continue le fleuve ; une autre branche de moins de minLength cases, de sa source à la jonction,
    // redevient de la terre avec tout son amont (évite les moignons d'une case collés au fleuve).
    public static bool[,] PruneShortTributaries(bool[,] navigable, float[,] flow, Vector2Int[,] downstream, int minLength)
    {
        int w = navigable.GetLength(0), h = navigable.GetLength(1);
        var children = new Dictionary<Vector2Int, List<Vector2Int>>();   // case → cases navigables qui s'y jettent
        foreach (var (x, y) in Cells(w, h))
        {
            if (!navigable[x, y] || !IsNavigable(navigable, downstream[x, y])) continue;
            var d = downstream[x, y];
            if (!children.TryGetValue(d, out var list)) children[d] = list = new List<Vector2Int>();
            list.Add(new Vector2Int(x, y));
        }

        var depth = new Dictionary<Vector2Int, int>();   // plus long trajet en cases depuis une source jusqu'à la case
        int Depth(Vector2Int c)
        {
            if (depth.TryGetValue(c, out int known)) return known;
            int longest = 0;
            if (children.TryGetValue(c, out var ch)) foreach (var k in ch) longest = Math.Max(longest, Depth(k));
            return depth[c] = 1 + longest;
        }

        var kept = (bool[,])navigable.Clone();
        void Remove(Vector2Int c)
        {
            kept[c.x, c.y] = false;
            if (children.TryGetValue(c, out var ch)) foreach (var k in ch) Remove(k);
        }

        foreach (var branches in children.Values)
        {
            if (branches.Count < 2) continue;
            Vector2Int main = branches[0];
            foreach (var b in branches)
                if (Depth(b) > Depth(main) || (Depth(b) == Depth(main) && flow[b.x, b.y] > flow[main.x, main.y])) main = b;
            foreach (var b in branches)
                if (b != main && Depth(b) < minLength) Remove(b);
        }
        return kept;
    }

    // Retire les fleuves navigables trop courts ou qui finissent dans un lac (ils ne mènent pas à la mer).
    // Un fleuve = toutes les cases qui descendent vers la même embouchure (dernière case navigable avant l'eau) ;
    // sa longueur = le plus long trajet jusqu'à elle, donc un petit affluent d'un grand fleuve est gardé.
    public static bool[,] KeepLongRivers(bool[,] navigable, Vector2Int[,] downstream, bool[,] lake, int minLength)
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
        {
            if (!navigable[x, y]) continue;
            var mouth = mouthOf[x, y];
            var outlet = downstream[mouth.x, mouth.y];
            bool intoLake = outlet != NoDownstream && lake[outlet.x, outlet.y];
            kept[x, y] = longest[mouth] >= minLength && !intoLake;
        }
        return kept;
    }

    private static bool IsNavigable(bool[,] navigable, Vector2Int c) =>
        c != NoDownstream && navigable[c.x, c.y];

    private static IEnumerable<(int, int)> Cells(int w, int h)
    {
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) yield return (x, y);
    }
}
