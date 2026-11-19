using System.Collections.Generic;
using UnityEngine;

// Affiche les rivières d'arêtes : une ligne par arête, visible dès qu'une de ses deux rives est découverte.
public class RiverView : MonoBehaviour
{
    [SerializeField] private float heightOffset = 0.1f;
    [SerializeField] private Color color = new Color(0.30f, 0.55f, 0.90f, 1f);   // comme River.mat

    private readonly List<(LineRenderer line, HexTile a, HexTile b)> lines = new();
    private readonly Dictionary<Vector2Int, List<LineRenderer>> byTile = new Dictionary<Vector2Int, List<LineRenderer>>();
    private Material material;
    private HexGrid grid;

    public void Build(HexGrid grid, IEnumerable<RiverEdge> edges, float width)
    {
        Clear();
        this.grid = grid;
        if (material == null) material = new Material(Shader.Find("Sprites/Default"));
        foreach (var edge in edges)
        {
            if (!grid.TryGetTileAxial(edge.A, out HexTile a) || !grid.TryGetTileAxial(edge.B, out HexTile b)) continue;

            var line = new GameObject("River").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 2;
            Place(line, a, b);
            line.enabled = a.IsDiscovered || b.IsDiscovered;   // rives déjà découvertes (zones de départ)

            lines.Add((line, a, b));
            Register(edge.A, line);
            Register(edge.B, line);
        }
    }

    // Les colonnes ont changé de copie (monde qui fait le tour) : les lignes suivent leurs rives.
    public void Refresh()
    {
        foreach (var (line, a, b) in lines) Place(line, a, b);
    }

    // Ligne sur l'arête commune, à la hauteur de la rive la plus basse ; b pris à la copie la plus proche de a.
    private void Place(LineRenderer line, HexTile a, HexTile b)
    {
        Vector3 pa = a.CentrePosition;
        Vector3 pb = grid.NearestCopy(b.CentrePosition, pa);
        var (p, q) = HexMetrics.SharedEdge(pa, pb, grid.HexSize, grid.Orientation);
        p.y = q.y = Mathf.Min(pa.y, pb.y) + heightOffset;   // rive la plus basse
        line.SetPosition(0, p);
        line.SetPosition(1, q);
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
        foreach (var (line, _, _) in lines) if (line != null) Destroy(line.gameObject);
        lines.Clear();
        byTile.Clear();
    }
}
