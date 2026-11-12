using System.Collections.Generic;
using UnityEngine;

public class SelectionController : Singleton<SelectionController>
{
    public Unit SelectedUnit { get; private set; }
    public HexTile SelectedTile { get; private set; }

    private HexGrid Grid => GameManager.Instance.HexGrid;

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

    // Seule une case découverte et survolée répond.
    private HexTile HoveredTile()
    {
        HexTile tile = Grid.HoveredTile;
        return tile != null && tile.IsDiscovered && tile.IsHovered ? tile : null;
    }

    private void Select()
    {
        HexTile tile = HoveredTile();
        if (tile == null) return;

        // Clic sur une case sans unité : désélection.
        Unit unit = tile.GetFirstUnit();
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
        HexTile target = HoveredTile();
        if (SelectedUnit == null || SelectedUnit.IsMoving || SelectedUnit.MovementPoints <= 0
            || target == null || target == SelectedTile) return;

        List<Vector2Int> path = PathFinder.FindPath(SelectedTile.AxialCoordinates, target.AxialCoordinates, IsWalkable);
        if (path == null || path.Count == 0) return;

        // On s'arrête là où les points s'épuisent.
        if (path.Count > SelectedUnit.MovementPoints) path.RemoveRange(SelectedUnit.MovementPoints, path.Count - SelectedUnit.MovementPoints);
        Grid.TryGetTileAxial(path[path.Count - 1], out HexTile end);

        var points = new List<Vector3>();
        foreach (var axial in path)
        {
            Grid.TryGetTileAxial(axial, out HexTile step);
            points.Add(step.CentrePosition);
        }

        // La case de l'unité change dès le départ.
        SelectedTile.RemoveUnit(SelectedUnit.gameObject);
        end.AddUnit(SelectedUnit.gameObject);
        SelectedTile = end;
        SelectedUnit.MoveAlong(points);
        Grid.UpdateHoverMarker();
    }

    private bool IsWalkable(Vector2Int axial) =>
        Grid.TryGetTileAxial(axial, out HexTile tile) && tile.IsDiscovered && tile.IsLand();

    // Le sélecteur suit l'unité, y compris pendant son déplacement.
    private void Update()
    {
        if (SelectedUnit != null && Grid.Selector != null)
            Grid.Selector.transform.position = SelectedUnit.transform.position;
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
