using System.Collections.Generic;
using UnityEngine;

// Affiche les rivières d'arêtes : une ligne par arête, visible dès qu'une de ses deux rives est découverte.
public class RiverView : MonoBehaviour
{
    [SerializeField] private float width = 0.5f;
    [SerializeField] private float heightOffset = 0.1f;
    [SerializeField] private Color color = new Color(0.30f, 0.55f, 0.90f, 1f);   // comme River.mat

    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private readonly Dictionary<Vector2Int, List<LineRenderer>> byTile = new Dictionary<Vector2Int, List<LineRenderer>>();
    private Material material;

    public void Build(HexGrid grid, IEnumerable<RiverEdge> edges)
    {
        Clear();
        if (material == null) material = new Material(Shader.Find("Sprites/Default"));
        foreach (var edge in edges)
        {
            if (!grid.TryGetTileAxial(edge.A, out HexTile a) || !grid.TryGetTileAxial(edge.B, out HexTile b)) continue;
            var (p, q) = HexMetrics.SharedEdge(a.CentrePosition, b.CentrePosition, grid.HexSize, grid.Orientation);
            p.y = q.y = Mathf.Min(a.CentrePosition.y, b.CentrePosition.y) + heightOffset;   // rive la plus basse

            var line = new GameObject("River").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 2;
            line.SetPosition(0, p);
            line.SetPosition(1, q);
            line.enabled = a.IsDiscovered || b.IsDiscovered;   // rives déjà découvertes (zones de départ)

            lines.Add(line);
            Register(edge.A, line);
            Register(edge.B, line);
        }
    }

    // Une rive vient d'être découverte : ses rivières apparaissent.
    public void Reveal(HexTile tile)
    {
        if (byTile.TryGetValue(tile.AxialCoordinates, out var list))
            foreach (var line in list) line.enabled = true;
    }

    private void Register(Vector2Int cell, LineRenderer line)
    {
        if (!byTile.TryGetValue(cell, out var list)) byTile[cell] = list = new List<LineRenderer>();
        list.Add(line);
    }

    private void Clear()
    {
        foreach (var line in lines) if (line != null) Destroy(line.gameObject);
        lines.Clear();
        byTile.Clear();
    }
}
