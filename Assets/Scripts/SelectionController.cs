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
    private ReachableOutline outline;
    private bool aiming;   // clic droit maintenu : on choisit la destination

    protected override void Init()
    {
        preview = new GameObject("PathPreview").AddComponent<PathPreview>();
        preview.transform.SetParent(transform, false);
        outline = new GameObject("ReachableOutline").AddComponent<ReachableOutline>();
        outline.transform.SetParent(transform, false);
    }

    private void OnEnable()
    {
        var cam = CameraController.Instance;
        cam.onSelectAction += Select;
        cam.onCommandStart += StartAiming;
        cam.onCommandRelease += ReleaseAiming;
        cam.onDeselectAction += Deselect;

        // Le marqueur n'apparaît qu'avec une unité sélectionnée.
        if (Grid != null && Grid.Selector != null) Grid.Selector.SetActive(false);
    }

    private void OnDisable()
    {
        var cam = CameraController.Instance;
        if (cam == null) return;
        cam.onSelectAction -= Select;
        cam.onCommandStart -= StartAiming;
        cam.onCommandRelease -= ReleaseAiming;
        cam.onDeselectAction -= Deselect;
    }

    // Appui droit : la visée commence, seulement pour une unité commandable.
    private void StartAiming() => aiming = CanCommand();

    // Relâchement droit : l'ordre part vers la case visée.
    private void ReleaseAiming()
    {
        if (!aiming) return;
        aiming = false;
        Command();
    }

    private void CancelAiming() => aiming = false;

    // Case survolée, explorée ou non.
    private HexTile HoveredTile()
    {
        HexTile tile = Grid.HoveredTile;
        return tile != null && tile.IsHovered ? tile : null;
    }

    private void Select()
    {
        // Clic sur le modèle d'une unité visible : c'est elle (avec la caméra inclinée, le sol sous la souris
        // est souvent la case derrière l'unité). Sinon : une unité de la case survolée.
        Unit unit = UnitUnderMouse();
        HexTile tile = unit != null ? unit.Tile : HoveredTile();
        if (tile == null || !tile.IsDiscovered) return;   // on ne sélectionne rien sous le brouillard
        if (unit == null) unit = tile.GetFirstUnit(GameManager.Instance.LocalPlayer);

        // Clic sur une case sans unité : désélection.
        if (unit == null) { Deselect(); return; }
        // Reclic sur la case de l'unité déjà sélectionnée : on passe à l'autre unité du joueur local sur la case.
        if (unit.Tile == SelectedTile && SelectedUnit != null)
            unit = tile.GetNextUnit(GameManager.Instance.LocalPlayer, SelectedUnit) ?? SelectedUnit;
        CancelAiming();
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
    }

    // Unité visible la plus proche sous le curseur (collider de son modèle).
    private static Unit UnitUnderMouse()
    {
        if (Camera.main == null || Mouse.current == null) return null;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Unit best = null;
        float bestDistance = float.MaxValue;
        foreach (var hit in Physics.RaycastAll(ray))
        {
            var unit = hit.collider.GetComponentInParent<Unit>();
            if (unit == null || !unit.IsVisible || unit.Tile == null || hit.distance >= bestDistance) continue;
            best = unit;
            bestDistance = hit.distance;
        }
        return best;
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
        UpdateOutline();
    }

    private Unit outlineUnit;
    private HexTile outlineOrigin;
    private int outlinePoints = -1, outlineShift = -1;

    // Contour de la zone atteignable ce tour-ci : unité commandable, à l'arrêt, avec au moins un point.
    // Recalculé seulement si l'unité, sa case, ses points ou les décalages d'affichage changent.
    private void UpdateOutline()
    {
        bool show = CanCommand() && SelectedUnit.MovementPoints > 0;
        Unit unit = show ? SelectedUnit : null;
        HexTile origin = show ? SelectedTile : null;
        int points = show ? SelectedUnit.MovementPoints : 0;
        if (unit == outlineUnit && origin == outlineOrigin && points == outlinePoints
            && Grid.ShiftVersion == outlineShift) return;
        outlineUnit = unit; outlineOrigin = origin; outlinePoints = points; outlineShift = Grid.ShiftVersion;
        if (!show || origin == null) { outline.Hide(); return; }

        // Mêmes règles que le chemin (connaissance du propriétaire, rivières) ; seulement les cases où s'arrêter.
        var zone = PathFinder.Reachable(origin.AxialCoordinates, IsWalkable, StepCost, points, Grid.Wrap);
        zone.RemoveWhere(a => a != origin.AxialCoordinates && !CanStopAt(a));
        outline.Show(Grid, HexMetrics.BoundaryEdges(zone, Grid.Wrap), Grid.OutlineColor);
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
        }
        if (keyboard.f10Key.wasPressedThisFrame) SelectedUnit.ResetMovementPoints();
    }

    private HexTile previewTarget, previewOrigin;
    private int previewPoints = -1;
    private int previewShift = -1;

    // Aperçu pendant la visée seulement ; recalculé si la cible, l'origine, les points ou les décalages changent.
    private void UpdatePreview()
    {
        HexTile target = aiming && CanCommand() ? HoveredTile() : null;
        int points = SelectedUnit != null ? SelectedUnit.MovementPoints : 0;
        if (target == previewTarget && SelectedTile == previewOrigin && points == previewPoints
            && Grid.ShiftVersion == previewShift) return;
        previewShift = Grid.ShiftVersion;
        previewTarget = target;
        previewOrigin = SelectedTile;
        previewPoints = points;

        if (target == null || target == SelectedTile) { preview.Hide(); Grid.HideDestination(); return; }
        List<Vector2Int> path = PathTo(target);
        var state = PathPreview.StateFor(path != null, Sees(target.AxialCoordinates));
        if (state == DestinationState.Forbidden)
        {
            preview.Hide();   // interdite : cercle rouge, pas de trait
            Grid.ShowDestination(target, Grid.ForbiddenColor);
            return;
        }
        // Chaque pas pris à la copie la plus proche du précédent : pas de trait à travers la carte à la couture.
        var steps = new List<Vector3>();
        Vector3 previous = SelectedTile.DisplayPosition;
        foreach (var axial in path)
        {
            Grid.TryGetTileAxial(axial, out HexTile step);
            previous = Grid.NearestCopy(step.DisplayPosition, previous);
            steps.Add(previous);
        }
        if (state == DestinationState.OutOfSight)
        {
            // Trajet estimé (cases inconnues comptées à 1) : tout en marron, sans découpage par tour.
            preview.Show(SelectedTile.DisplayPosition, steps, steps.Count, Grid.OutOfSightColor, Grid.OutOfSightColor);
            Grid.ShowDestination(target, Grid.OutOfSightColor);
        }
        else
        {
            int reachable = StopCount(path, points);
            preview.Show(SelectedTile.DisplayPosition, steps, reachable, Grid.ReachableColor, Grid.BeyondColor);
            // Le cercle prend la couleur de la fin du trait : doré si l'unité y arrive ce tour-ci, bleu marine sinon.
            Grid.ShowDestination(target, reachable >= path.Count ? Grid.ReachableColor : Grid.BeyondColor);
        }
    }

    // Échap / Retour arrière : annule d'abord une visée en cours, sinon désélectionne.
    private void Deselect()
    {
        if (aiming) { CancelAiming(); return; }
        if (SelectedUnit == null) return;
        SelectedUnit.Deselect();
        SelectedUnit = null;

        if (Grid.Selector != null) Grid.Selector.SetActive(false);
    }
}
