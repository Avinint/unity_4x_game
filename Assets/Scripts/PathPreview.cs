using System.Collections.Generic;
using UnityEngine;

// Aperçu du trajet : partie atteignable ce tour-ci et reste du chemin, en deux lignes.
public class PathPreview : MonoBehaviour
{
    [SerializeField] private float width = 0.6f;
    [SerializeField] private float heightOffset = 0.3f;
    [SerializeField] private Color reachableColor = new Color(0.92f, 0.86f, 0.75f, 0.9f);
    [SerializeField] private Color beyondColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
    [SerializeField] private Color forbiddenColor = new Color(0.85f, 0.2f, 0.15f, 0.8f);

    private LineRenderer reachableLine;
    private LineRenderer beyondLine;
    private LineRenderer forbiddenLine;

    private void Awake()
    {
        // Sprites/Default : shader toujours inclus, couleur par sommet.
        var material = new Material(Shader.Find("Sprites/Default"));
        reachableLine = CreateLine("Reachable", material, reachableColor);
        beyondLine = CreateLine("Beyond", material, beyondColor);
        forbiddenLine = CreateLine("Forbidden", material, forbiddenColor);
        Hide();
    }

    private LineRenderer CreateLine(string lineName, Material material, Color color)
    {
        var line = new GameObject(lineName).AddComponent<LineRenderer>();
        line.transform.SetParent(transform, false);
        line.sharedMaterial = material;
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    // start : position de l'unité ; steps : centres des cases du chemin ; reachableCount : cases atteignables ce tour-ci.
    public void Show(Vector3 start, List<Vector3> steps, int reachableCount)
    {
        forbiddenLine.enabled = false;
        reachableCount = Mathf.Clamp(reachableCount, 0, steps.Count);
        var reachable = new List<Vector3> { start };
        reachable.AddRange(steps.GetRange(0, reachableCount));
        var beyond = new List<Vector3> { reachable[reachable.Count - 1] };
        beyond.AddRange(steps.GetRange(reachableCount, steps.Count - reachableCount));
        SetPoints(reachableLine, reachable);
        SetPoints(beyondLine, beyond);
    }

    // Trajet vers une case interdite : toute la ligne en rouge.
    public void ShowForbidden(Vector3 start, List<Vector3> steps)
    {
        reachableLine.enabled = false;
        beyondLine.enabled = false;
        var points = new List<Vector3> { start };
        points.AddRange(steps);
        SetPoints(forbiddenLine, points);
    }

    public void Hide()
    {
        reachableLine.enabled = false;
        beyondLine.enabled = false;
        forbiddenLine.enabled = false;
    }

    private void SetPoints(LineRenderer line, List<Vector3> points)
    {
        line.enabled = points.Count >= 2;
        if (!line.enabled) return;
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i] + Vector3.up * heightOffset);
    }
}
