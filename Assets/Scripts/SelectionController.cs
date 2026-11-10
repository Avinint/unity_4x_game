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

        Unit unit = tile.GetFirstUnit();
        if (unit == null) return;
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

    private void Command() => HoveredTile()?.OnMoveUnit();

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
