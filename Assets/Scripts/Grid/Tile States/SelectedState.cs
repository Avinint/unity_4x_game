using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedState: BaseTileState
{
    public override TileState State => TileState.Selected;
    
    public override void Enter(HexTile tile)
    {
        Debug.Log("tile "+ tile.AxialCoordinates + "is entering Selected State");
       
        CameraController.Instance.onDeselectAction += tile.OnDeselect;
        //CameraController.Instance.onFocusAction += tile.OnFocus;
        
     
        CameraController.Instance.CameraTarget.transform.position = tile.Terrain.transform.position;
       


    }

    public override void Exit(HexTile tile)
    {
        Debug.Log("tile "+ tile.AxialCoordinates + "is exiting Selected State");
        CameraController.Instance.onDeselectAction -= tile.OnDeselect;
        // CameraController.Instance.onFocusAction -= tile.OnFocus;
    }

    public override ITileState OnDeselect()
    {
        return new VisibleState();
    }
    
    // public override ITiletate OnFocus()
    // {
    //     return new FocusedState();
    // }
}
