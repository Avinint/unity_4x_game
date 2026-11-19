using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionController : Singleton<SelectionController>
{
    public Unit SelectedUnit { get; private set; }
    // Case de l'unité sélectionnée (suit l'unité, y compris quand elle s'arrête plus tôt que prévu).
    public HexTile SelectedTile => SelectedUnit != null ? SelectedUnit.Tile : null;

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
        bool changed = unit != SelectedUnit;
        if (SelectedUnit != null && SelectedUnit != unit) SelectedUnit.Deselect();
        SelectedUnit = unit;
        SelectedUnit.Select();
        if (changed) CameraController.Instance.CenterOn(tile.CentrePosition);

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

        var steps = new List<HexTile>();
        foreach (var axial in path)
        {
            Grid.TryGetTileAxial(axial, out HexTile step);
            steps.Add(step);
        }

        // L'unité change de case elle-même (réservée dès le départ, corrigée si elle s'arrête plus tôt).
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
        // Le moins de tours possible (traverser une rivière plutôt qu'un détour plus long en tours).
        var path = PathFinder.FindPath(SelectedTile.AxialCoordinates, target.AxialCoordinates, IsWalkable, StepCost,
            SelectedUnit.MovementPoints, SelectedUnit.type.MovementPoints, Grid.Wrap, Grid.Distance);
        return path != null && path.Count > 0 ? path : null;
    }

    // Coût d'un pas selon ce que sait le propriétaire : terrain (1 si inconnu) + rivière connue (une rive explorée).
    private int StepCost(Vector2Int from, Vector2Int to)
    {
        int cost = Grid.TryGetTileAxial(to, out HexTile tile) ? tile.MovementCostFor(Knows(to)) : 1;
        bool riverKnown = (Knows(from) || Knows(to)) && Grid.HasRiver(from, to);
        return cost + (riverKnown ? Grid.RiverCrossingCost : 0);
    }

    // Cases parcourues ce tour-ci : limitées par les points, puis recul jusqu'à une case où l'unité peut s'arrêter.
    private int StopCount(List<Vector2Int> path, int points) =>
        PathFinder.LastStop(PathFinder.ReachableCount(StepCosts(path), points), i => CanStopAt(path[i]));

    private bool CanStopAt(Vector2Int axial) =>
        Grid.TryGetTileAxial(axial, out HexTile tile) && tile.CanStop(SelectedUnit);

    private List<int> StepCosts(List<Vector2Int> path)
    {
        var costs = new List<int>(path.Count);
        Vector2Int from = SelectedTile.AxialCoordinates;
        foreach (var to in path)
        {
            costs.Add(StepCost(from, to));
            from = to;
        }
        return costs;
    }

    // Le sélecteur suit l'unité, y compris pendant son déplacement.
    private void Update()
    {
        DebugKeys();
        if (SelectedUnit != null && Grid.Selector != null)
            Grid.Selector.transform.position = SelectedUnit.transform.position;
        UpdatePreview();
    }

    // Touches de débogage lues directement : pas d'action dans PlayerControls pour un outil provisoire.
    // F9 : téléporte l'unité sélectionnée sur la case survolée ; F10 : lui rend ses points de mouvement.
    private void DebugKeys()
    {
        var keyboard = Keyboard.current;
        if (!Debug.isDebugBuild || keyboard == null || SelectedUnit == null) return;
        if (keyboard.f9Key.wasPressedThisFrame)
        {
            HexTile target = HoveredTile();
            if (target != null && target.CanStop(SelectedUnit)) SelectedUnit.Teleport(target);
            Grid.UpdateHoverMarker();
        }
        if (keyboard.f10Key.wasPressedThisFrame) SelectedUnit.ResetMovementPoints();
    }

    private HexTile previewTarget, previewOrigin;
    private int previewPoints = -1;
    private int previewShift = -1;

    // Aperçu du trajet vers la case survolée ; recalculé seulement si la cible, l'origine ou les points changent.
    private void UpdatePreview()
    {
        HexTile target = CanCommand() ? HoveredTile() : null;
        int points = SelectedUnit != null ? SelectedUnit.MovementPoints : 0;
        if (target == previewTarget && SelectedTile == previewOrigin && points == previewPoints
            && Grid.ShiftVersion == previewShift) return;
        previewShift = Grid.ShiftVersion;
        previewTarget = target;
        previewOrigin = SelectedTile;
        previewPoints = points;

        List<Vector2Int> path = target != null ? PathTo(target) : null;
        bool forbidden = false;
        if (path == null && target != null && target != SelectedTile)
        {
            // Case interdite (eau, montagne) : chemin jusqu'à elle, affiché en rouge.
            path = PathFinder.FindPath(SelectedTile.AxialCoordinates, target.AxialCoordinates,
                a => a == target.AxialCoordinates || IsWalkable(a), Grid.Wrap, Grid.Distance);
            forbidden = path != null && path.Count > 0;
            if (!forbidden) path = null;
        }
        if (path == null) { preview.Hide(); return; }
        // Chaque pas pris à la copie la plus proche du précédent : pas de trait à travers la carte à la couture.
        var steps = new List<Vector3>();
        Vector3 previous = SelectedTile.DisplayPosition;
        foreach (var axial in path)
        {
            Grid.TryGetTileAxial(axial, out HexTile step);
            previous = Grid.NearestCopy(step.DisplayPosition, previous);
            steps.Add(previous);
        }
        if (forbidden) preview.ShowForbidden(SelectedTile.DisplayPosition, steps);
        else preview.Show(SelectedTile.DisplayPosition, steps, StopCount(path, points));
    }

    private void Deselect()
    {
        if (SelectedUnit == null) return;
        SelectedUnit.Deselect();
        SelectedUnit = null;

        if (Grid.Selector != null) Grid.Selector.SetActive(false);

        Grid.UpdateHoverMarker();
    }
}
