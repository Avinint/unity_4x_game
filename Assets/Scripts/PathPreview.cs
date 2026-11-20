using System.Collections.Generic;
using UnityEngine;

public enum DestinationState { Reachable, Forbidden, OutOfSight }

// Aperçu du trajet pendant la visée (clic droit maintenu) : partie atteignable ce tour-ci et reste du chemin.
public class PathPreview : MonoBehaviour
{
    [SerializeField] private float width = 0.6f;
    [SerializeField] private float heightOffset = 0.3f;

    private LineRenderer reachableLine;
    private LineRenderer beyondLine;

    // Pas de chemin : interdite (rouge, sans trait) ; destination pas vue en ce moment : trajet estimé (marron) ; sinon doré.
    public static DestinationState StateFor(bool hasPath, bool destinationSeen) =>
        !hasPath ? DestinationState.Forbidden : destinationSeen ? DestinationState.Reachable : DestinationState.OutOfSight;

    private void Awake()
    {
        // Sprites/Default : shader toujours inclus, couleur par sommet.
        var material = new Material(Shader.Find("Sprites/Default"));
        reachableLine = CreateLine("Reachable", material);
        beyondLine = CreateLine("Beyond", material);
        Hide();
    }

    private LineRenderer CreateLine(string lineName, Material material)
    {
        var line = new GameObject(lineName).AddComponent<LineRenderer>();
        line.transform.SetParent(transform, false);
        line.sharedMaterial = material;
        line.startWidth = line.endWidth = width;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    // start : position de l'unité ; steps : centres des cases du chemin ; reachableCount cases en `now`,
    // la suite en `later` (même couleur pour un trajet estimé).
    public void Show(Vector3 start, List<Vector3> steps, int reachableCount, Color now, Color later)
    {
        reachableCount = Mathf.Clamp(reachableCount, 0, steps.Count);
        var reachable = new List<Vector3> { start };
        reachable.AddRange(steps.GetRange(0, reachableCount));
        var beyond = new List<Vector3> { reachable[reachable.Count - 1] };
        beyond.AddRange(steps.GetRange(reachableCount, steps.Count - reachableCount));
        reachableLine.startColor = reachableLine.endColor = now;
        beyondLine.startColor = beyondLine.endColor = later;
        SetPoints(reachableLine, reachable);
        SetPoints(beyondLine, beyond);
    }

    public void Hide()
    {
        reachableLine.enabled = false;
        beyondLine.enabled = false;
    }

    private void SetPoints(LineRenderer line, List<Vector3> points)
    {
        line.enabled = points.Count >= 2;
        if (!line.enabled) return;
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i] + Vector3.up * heightOffset);
    }
}
