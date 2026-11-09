using UnityEngine;

public class SelectionController : Singleton<SelectionController>
{
    public Unit SelectedUnit { get; private set; }

    private HexGrid Grid => GameManager.Instance.HexGrid;

    private void OnEnable()
    {
        var cam = CameraController.Instance;
        cam.onSelectAction += Select;
        cam.onCommandAction += Command;
        cam.onDeselectAction += Deselect;
    }

    private void OnDisable()
    {
        var cam = CameraController.Instance;
        if (cam == null) return;
        cam.onSelectAction -= Select;
        cam.onCommandAction -= Command;
        cam.onDeselectAction -= Deselect;
    }

    // Seule une case découverte et survolée (état Highlighted) répond.
    private HexTile HighlightedTile()
    {
        HexTile tile = Grid.HoveredTile;
        return tile != null && tile.IsDiscovered && tile.State.State == TileState.Highlighted ? tile : null;
    }

    private void Select()
    {
        HexTile tile = HighlightedTile();
        if (tile == null) return;

        tile.OnSelect();

        Unit unit = tile.GetFirstUnit();
        if (unit == null) return;
        if (SelectedUnit != null && SelectedUnit != unit) SelectedUnit.Deselect();
        SelectedUnit = unit;
        SelectedUnit.Select();
    }

    private void Command() => HighlightedTile()?.OnMoveUnit();

    private void Deselect()
    {
        if (SelectedUnit == null) return;
        SelectedUnit.Deselect();
        SelectedUnit = null;
    }
}
