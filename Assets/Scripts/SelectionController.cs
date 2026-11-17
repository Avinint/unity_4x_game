using System.Collections.Generic;
using UnityEngine;

public class SelectionController : Singleton<SelectionController>
{
    public Unit SelectedUnit { get; private set; }
    public HexTile SelectedTile { get; private set; }

    private HexGrid Grid => GameManager.Instance.HexGrid;

    private PathPreview preview;

    protected override void Init()
    {
        preview = new GameObject("PathPreview").AddComponent<PathPreview>();
        preview.transform.SetParent(transform, false);
    }

    private void OnEnable()
    {
        var cam = CameraController.Instance;
        cam.onSelectAction += Select;
        cam.onCommandAction += Command;
        cam.onDeselectAction += Deselect;

        // Le marqueur n'apparaît qu'avec une unité sélectionnée.
        if (Grid != null && Grid.Selector != null) Grid.Selector.SetActive(false);
    }

    private void OnDisable()
    {
        var cam = CameraController.Instance;
        if (cam == null) return;
        cam.onSelectAction -= Select;
        cam.onCommandAction -= Command;
        cam.onDeselectAction -= Deselect;
    }

    // Case survolée, explorée ou non.
    private HexTile HoveredTile()
    {
        HexTile tile = Grid.HoveredTile;
        return tile != null && tile.IsHovered ? tile : null;
    }

    private void Select()
    {
        HexTile tile = HoveredTile();
        if (tile == null || !tile.IsDiscovered) return;   // on ne sélectionne rien sous le brouillard

        // Clic sur une case sans unité : désélection.
        Unit unit = tile.GetFirstUnit(GameManager.Instance.LocalPlayer);
        if (unit == null) { Deselect(); return; }
        if (SelectedUnit != null && SelectedUnit != unit) SelectedUnit.Deselect();
        SelectedUnit = unit;
        SelectedUnit.Select();
        SelectedTile = tile;

        if (Grid.Selector != null)
        {
            Grid.Selector.transform.position = tile.CentrePosition;
            Grid.Selector.SetActive(true);
        }

        Grid.UpdateHoverMarker();
    }

    // Déplacement par chemin (terre découverte) de l'unité sélectionnée.
    private void Command()
    {
        if (!CanCommand() || SelectedUnit.MovementPoints <= 0) return;
        List<Vector2Int> path = PathTo(HoveredTile());
        if (path == null) return;

        // On s'arrête là où les points s'épuisent, sur une case libre (empilement).
        int reachable = StopCount(path, SelectedUnit.MovementPoints);
        if (reachable == 0) return;
        if (path.Count > reachable) path.RemoveRange(reachable, path.Count - reachable);
        Grid.TryGetTileAxial(path[path.Count - 1], out HexTile end);

        var steps = new List<HexTile>();
        foreach (var axial in path)
        {
            Grid.TryGetTileAxial(axial, out HexTile step);
            steps.Add(step);
        }

        // La case de l'unité change dès le départ.
        SelectedTile.RemoveUnit(SelectedUnit.gameObject);
        end.AddUnit(SelectedUnit.gameObject);
        SelectedTile = end;
        SelectedUnit.MoveAlong(steps);
        Grid.UpdateHoverMarker();
    }

    // Selon la connaissance du propriétaire de l'unité, pas selon l'affichage.
    private bool Knows(Vector2Int axial) => GameManager.Instance.Fog.IsExplored(SelectedUnit.Owner, axial);

    private bool Sees(Vector2Int axial) => GameManager.Instance.Fog.IsVisible(SelectedUnit.Owner, axial);

    private bool IsWalkable(Vector2Int axial) =>
        Grid.TryGetTileAxial(axial, out HexTile tile) && tile.IsWalkableFor(SelectedUnit.Owner, Knows(axial), Sees(axial));

    // Seule une unité du joueur courant, à l'arrêt, reçoit des ordres.
    private bool CanCommand() =>
        SelectedUnit != null && SelectedUnit.Owner == GameManager.Instance.CurrentPlayer && !SelectedUnit.IsMoving;

    // Chemin complet (non coupé aux points) vers une case de terre découverte ; null si impossible.
    private List<Vector2Int> PathTo(HexTile target)
    {
        if (SelectedTile == null || target == null || target == SelectedTile) return null;
        if (!target.CanStop(SelectedUnit)) return null;   // case pleine : on peut la traverser, pas s'y arrêter
        var path = PathFinder.FindPath(SelectedTile.AxialCoordinates, target.AxialCoordinates, IsWalkable, StepCost);
        return path != null && path.Count > 0 ? path : null;
    }

    private int StepCost(Vector2Int axial) =>
        Grid.TryGetTileAxial(axial, out HexTile tile) ? tile.MovementCostFor(Knows(axial)) : 1;

    // Cases parcourues ce tour-ci : limitées par les points, puis recul jusqu'à une case où l'unité peut s'arrêter.
    private int StopCount(List<Vector2Int> path, int points) =>
        PathFinder.LastStop(PathFinder.ReachableCount(StepCosts(path), points), i => CanStopAt(path[i]));

    private bool CanStopAt(Vector2Int axial) =>
        Grid.TryGetTileAxial(axial, out HexTile tile) && tile.CanStop(SelectedUnit);

    private List<int> StepCosts(List<Vector2Int> path)
    {
        var costs = new List<int>(path.Count);
        foreach (var axial in path) costs.Add(StepCost(axial));
        return costs;
    }

    // Le sélecteur suit l'unité, y compris pendant son déplacement.
    private void Update()
    {
        if (SelectedUnit != null && Grid.Selector != null)
            Grid.Selector.transform.position = SelectedUnit.transform.position;
        UpdatePreview();
    }

    private HexTile previewTarget, previewOrigin;
    private int previewPoints = -1;

    // Aperçu du trajet vers la case survolée ; recalculé seulement si la cible, l'origine ou les points changent.
    private void UpdatePreview()
    {
        HexTile target = CanCommand() ? HoveredTile() : null;
        int points = SelectedUnit != null ? SelectedUnit.MovementPoints : 0;
        if (target == previewTarget && SelectedTile == previewOrigin && points == previewPoints) return;
        previewTarget = target;
        previewOrigin = SelectedTile;
        previewPoints = points;

        List<Vector2Int> path = target != null ? PathTo(target) : null;
        bool forbidden = false;
        if (path == null && target != null && target != SelectedTile)
        {
            // Case interdite (eau, montagne) : chemin jusqu'à elle, affiché en rouge.
            path = PathFinder.FindPath(SelectedTile.AxialCoordinates, target.AxialCoordinates,
                a => a == target.AxialCoordinates || IsWalkable(a));
            forbidden = path != null && path.Count > 0;
            if (!forbidden) path = null;
        }
        if (path == null) { preview.Hide(); return; }
        var steps = new List<Vector3>();
        foreach (var axial in path)
        {
            Grid.TryGetTileAxial(axial, out HexTile step);
            steps.Add(step.DisplayPosition);
        }
        if (forbidden) preview.ShowForbidden(SelectedTile.DisplayPosition, steps);
        else preview.Show(SelectedTile.DisplayPosition, steps, StopCount(path, points));
    }

    private void Deselect()
    {
        if (SelectedUnit == null) return;
        SelectedUnit.Deselect();
        SelectedUnit = null;
        SelectedTile = null;

        if (Grid.Selector != null) Grid.Selector.SetActive(false);

        Grid.UpdateHoverMarker();
    }
}
