using System;
using System.Collections.Generic;
using UnityEngine;

// Connaissance de la carte par joueur : cases explorées (définitivement) et cases vues en ce moment
// (nombre d'unités qui les voient). Classe pure, testable sans scène.
public class FogOfWar<TPlayer> where TPlayer : class
{
    private class Vision
    {
        public readonly HashSet<Vector2Int> Explored = new HashSet<Vector2Int>();
        public readonly Dictionary<Vector2Int, int> Watchers = new Dictionary<Vector2Int, int>();
    }

    private readonly Dictionary<TPlayer, Vision> visions = new Dictionary<TPlayer, Vision>();

    public event Action<TPlayer, Vector2Int> TileExplored;              // première fois que le joueur la voit
    public event Action<TPlayer, Vector2Int, bool> VisibilityChanged;   // vue (true) / plus vue (false)

    public bool IsExplored(TPlayer player, Vector2Int cell) =>
        visions.TryGetValue(player, out var v) && v.Explored.Contains(cell);

    public bool IsVisible(TPlayer player, Vector2Int cell) =>
        visions.TryGetValue(player, out var v) && v.Watchers.TryGetValue(cell, out int n) && n > 0;

    // Explore sans voir (zone de départ).
    public void Explore(TPlayer player, IEnumerable<Vector2Int> cells)
    {
        var v = VisionOf(player);
        foreach (var cell in cells) MarkExplored(player, v, cell);
    }

    // Une source de vision (une unité) commence à voir ces cases.
    public void AddWatcher(TPlayer player, IEnumerable<Vector2Int> cells)
    {
        var v = VisionOf(player);
        foreach (var cell in cells)
        {
            v.Watchers.TryGetValue(cell, out int n);
            v.Watchers[cell] = n + 1;
            MarkExplored(player, v, cell);
            if (n == 0) VisibilityChanged?.Invoke(player, cell, true);
        }
    }

    // Une source de vision cesse de voir ces cases ; elles restent explorées.
    public void RemoveWatcher(TPlayer player, IEnumerable<Vector2Int> cells)
    {
        if (!visions.TryGetValue(player, out var v)) return;
        foreach (var cell in cells)
        {
            if (!v.Watchers.TryGetValue(cell, out int n) || n <= 0) continue;
            if (n == 1)
            {
                v.Watchers.Remove(cell);
                VisibilityChanged?.Invoke(player, cell, false);
            }
            else v.Watchers[cell] = n - 1;
        }
    }

    private Vision VisionOf(TPlayer player)
    {
        if (!visions.TryGetValue(player, out var v)) visions[player] = v = new Vision();
        return v;
    }

    private void MarkExplored(TPlayer player, Vision v, Vector2Int cell)
    {
        if (v.Explored.Add(cell)) TileExplored?.Invoke(player, cell);
    }
}
