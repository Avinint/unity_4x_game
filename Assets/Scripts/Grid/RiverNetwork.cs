using System;
using System.Collections.Generic;
using UnityEngine;

// Arête entre deux cases voisines (les deux rives), indépendante de l'ordre.
public readonly struct RiverEdge : IEquatable<RiverEdge>
{
    public readonly Vector2Int A, B;
    public RiverEdge(Vector2Int a, Vector2Int b) { bool s = Less(a, b); A = s ? a : b; B = s ? b : a; }
    internal static bool Less(Vector2Int a, Vector2Int b) => a.x < b.x || (a.x == b.x && a.y < b.y);
    public bool Equals(RiverEdge o) => A == o.A && B == o.B;
    public override bool Equals(object o) => o is RiverEdge e && Equals(e);
    public override int GetHashCode() => A.GetHashCode() * 397 ^ B.GetHashCode();
}

// Rivières sur les arêtes : l'eau coule de sommet en sommet (coin commun à trois cases) jusqu'à l'eau.
// Classe pure, coordonnées axiales ; la carte est décrite par les fonctions passées en paramètre.
public static class RiverNetwork
{
    // Voisins dans l'ordre du tour de l'hexagone : deux directions consécutives sont voisines entre elles.
    private static readonly Vector2Int[] Dirs =
        { new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1) };

    // Sommet = les trois cases qui s'y touchent, triées.
    private readonly struct Corner : IEquatable<Corner>
    {
        public readonly Vector2Int A, B, C;
        public Corner(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            if (RiverEdge.Less(b, a)) (a, b) = (b, a);
            if (RiverEdge.Less(c, b)) (b, c) = (c, b);
            if (RiverEdge.Less(b, a)) (a, b) = (b, a);
            A = a; B = b; C = c;
        }
        public bool Equals(Corner o) => A == o.A && B == o.B && C == o.C;
        public override bool Equals(object o) => o is Corner c && Equals(c);
        public override int GetHashCode() => (A.GetHashCode() * 397 ^ B.GetHashCode()) * 397 ^ C.GetHashCode();
    }

    // Arêtes portant une rivière. Seuls les sommets dont les trois cases existent comptent.
    public static HashSet<RiverEdge> Rivers(IEnumerable<Vector2Int> cells, Func<Vector2Int, bool> exists,
        Func<Vector2Int, bool> isWater, Func<Vector2Int, float> altitude, Func<Vector2Int, float> rain,
        float fraction, int minLength)
    {
        // 1. Sommets : altitude et pluie moyennes ; « eau » si une de leurs cases est de l'eau.
        var corners = new List<Corner>();
        var index = new Dictionary<Corner, int>();
        foreach (var cell in cells)
            for (int i = 0; i < 6; i++)
            {
                var c = new Corner(cell, cell + Dirs[i], cell + Dirs[(i + 1) % 6]);
                if (!exists(c.A) || !exists(c.B) || !exists(c.C) || index.ContainsKey(c)) continue;
                index[c] = corners.Count;
                corners.Add(c);
            }
        int n = corners.Count;
        var level = new float[n];
        var flow = new float[n];
        var water = new bool[n];
        var down = new int[n];
        for (int i = 0; i < n; i++)
        {
            var c = corners[i];
            level[i] = (altitude(c.A) + altitude(c.B) + altitude(c.C)) / 3f;
            water[i] = isWater(c.A) || isWater(c.B) || isWater(c.C);
            flow[i] = water[i] ? 0f : (rain(c.A) + rain(c.B) + rain(c.C)) / 3f;
            down[i] = -1;
        }

        // 2. Remplissage par priorité depuis les sommets d'eau, comme Hydrology.Flow.
        var done = new bool[n];
        var queue = new SortedSet<(float, int)>();
        var order = new List<int>();
        for (int i = 0; i < n; i++)
            if (water[i]) { done[i] = true; queue.Add((level[i], i)); }
        while (queue.Count > 0)
        {
            var (lv, i) = queue.Min;
            queue.Remove(queue.Min);
            foreach (int j in Next(corners[i], index))
            {
                if (done[j]) continue;
                done[j] = true;
                down[j] = i;
                level[j] = Mathf.Max(level[j], lv + 1e-5f);   // cuvette : remplie jusqu'au col
                order.Add(j);
                queue.Add((level[j], j));
            }
        }
        for (int k = order.Count - 1; k >= 0; k--)
        {
            int i = order[k];
            if (!water[down[i]]) flow[down[i]] += flow[i];
        }

        // 3. Plus forts débits des sommets de terre → arête vers le sommet aval.
        var landFlows = new List<float>();
        for (int i = 0; i < n; i++) if (!water[i] && down[i] >= 0) landFlows.Add(flow[i]);
        var links = new List<(int from, int to, RiverEdge edge)>();
        if (landFlows.Count > 0)
        {
            float above = MapRules.Percentile(landFlows, 1f - fraction);
            for (int i = 0; i < n; i++)
                if (!water[i] && down[i] >= 0 && flow[i] >= above)
                    links.Add((i, down[i], EdgeBetween(corners[i], corners[down[i]])));
        }

        // 4. Réseaux trop courts supprimés (composantes reliées par leurs sommets).
        return DropShort(links, n, minLength);
    }

    // Sommets voisins (le long des trois arêtes) qui existent.
    private static IEnumerable<int> Next(Corner c, Dictionary<Corner, int> index)
    {
        foreach (var (a, b, other) in new[] { (c.A, c.B, c.C), (c.B, c.C, c.A), (c.A, c.C, c.B) })
            if (index.TryGetValue(new Corner(a, b, OtherCommonNeighbour(a, b, other)), out int j))
                yield return j;
    }

    // a et b voisins ont deux voisins communs : renvoie celui qui n'est pas `known`.
    private static Vector2Int OtherCommonNeighbour(Vector2Int a, Vector2Int b, Vector2Int known)
    {
        int k = Array.IndexOf(Dirs, b - a);
        var p = a + Dirs[(k + 5) % 6];
        var q = a + Dirs[(k + 1) % 6];
        return p == known ? q : p;
    }

    // L'arête entre deux sommets voisins : les deux cases qu'ils ont en commun.
    private static RiverEdge EdgeBetween(Corner u, Corner v)
    {
        var shared = new List<Vector2Int>(2);
        foreach (var c in new[] { u.A, u.B, u.C })
            if (c == v.A || c == v.B || c == v.C) shared.Add(c);
        return new RiverEdge(shared[0], shared[1]);
    }

    private static HashSet<RiverEdge> DropShort(List<(int from, int to, RiverEdge edge)> links, int n, int minLength)
    {
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;
        int Find(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
        foreach (var l in links) parent[Find(l.from)] = Find(l.to);
        var size = new Dictionary<int, int>();
        foreach (var l in links)
        {
            int r = Find(l.from);
            size[r] = size.TryGetValue(r, out int s) ? s + 1 : 1;
        }
        var result = new HashSet<RiverEdge>();
        foreach (var l in links)
            if (size[Find(l.from)] >= minLength) result.Add(l.edge);
        return result;
    }
}
