using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedState: BaseCellState
{
    public override CellState State => CellState.Selected;
    
    public override void Enter(HexCell cell)
    {
        Debug.Log("cell "+ cell.AxialCoordinates + "is entering Selected State");
       
        CameraController.Instance.onDeselectAction += cell.OnDeselect;
        //CameraController.Instance.onFocusAction += cell.OnFocus;
        
     
        CameraController.Instance.CameraTarget.transform.position = cell.Terrain.transform.position;
       


    }

    public override void Exit(HexCell cell)
    {
        Debug.Log("cell "+ cell.AxialCoordinates + "is exiting Selected State");
        CameraController.Instance.onDeselectAction -= cell.OnDeselect;
        // CameraController.Instance.onFocusAction -= cell.OnFocus;
    }

    public override ICellState OnDeselect()
    {
        return new VisibleState();
    }
    
    // public override ICellState OnFocus()
    // {
    //     return new FocusedState();
    // }
}
