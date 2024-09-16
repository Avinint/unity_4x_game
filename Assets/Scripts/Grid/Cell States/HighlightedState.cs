using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightedState : BaseCellState
{
    public override CellState State => CellState.Highlighted;

    public override void Enter(HexCell cell)
    {
       
        Debug.Log("cell "+ cell.AxialCoordinates + "is entering Highlighted State");
        LeanTween.scale(cell.Terrain.gameObject, Vector3.one * 1.2f, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(cell.Terrain.gameObject, 5f, .2f).setEase(LeanTweenType.easeOutBack);
        CameraController.Instance.onSelectAction += cell.OnSelect;
        CameraController.Instance.onCommandAction += cell.OnMoveUnit;
    }

    public override void Exit(HexCell cell)
    {
        Debug.Log("cell "+ cell.AxialCoordinates + "is exiting Highlighted State");
        LeanTween.scale(cell.Terrain.gameObject, Vector3.one, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(cell.Terrain.gameObject, 0f, .2f).setEase(LeanTweenType.easeOutBack);
        CameraController.Instance.onSelectAction -= cell.OnSelect;
        CameraController.Instance.onCommandAction += cell.OnMoveUnit;
        
    }

    public override ICellState OnMouseExit()
    {
        return new VisibleState();
    }

    public override ICellState OnSelect()
    {
        return new SelectedState();
    }
}
