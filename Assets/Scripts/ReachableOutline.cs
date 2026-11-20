using System.Collections.Generic;
using UnityEngine;

// Contour de la zone atteignable ce tour-ci par l'unité sélectionnée : une bande plate le long de chaque bord,
// côté intérieur de la case, à sa hauteur (à plat sous le brouillard).
public class ReachableOutline : MonoBehaviour
{
    [SerializeField] private float width = 0.35f;
    [SerializeField] private float heightOffset = 0.2f;   // au-dessus des rivières (0,1), sinon elles passent dessus

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Material material;

    private void Awake()
    {
        mesh = new Mesh { name = "ReachableOutline" };
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Cull", 0f);   // deux faces : pas de souci de sens des triangles
        meshRenderer.sharedMaterial = material;
        Hide();
    }

    public void Show(HexGrid grid, List<(Vector2Int cell, Vector2Int outside)> edges, Color color)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        foreach (var (cell, outside) in edges)
        {
            if (!grid.TryGetTileAxial(cell, out HexTile tile)) continue;
            Vector3 centre = tile.DisplayPosition;
            Vector3 toOutside = grid.AxialToWorldDelta(cell, outside);   // vers la voisine, telle quelle
            var (p, q) = HexMetrics.SharedEdge(centre, centre + toOutside, grid.HexSize, grid.Orientation);
            p.y = q.y = centre.y + heightOffset;
            Vector3 inward = -new Vector3(toOutside.x, 0f, toOutside.z).normalized * width;
            int v = vertices.Count;
            vertices.Add(p); vertices.Add(q); vertices.Add(q + inward); vertices.Add(p + inward);
            triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        }
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        material.SetColor("_BaseColor", color);
        meshRenderer.enabled = vertices.Count > 0;
    }

    public void Hide() => meshRenderer.enabled = false;
}
