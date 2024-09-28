using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusedState : BaseTileState
{
    public override TileState State => TileState.Focused;
    
    public override void Enter(HexTile tile)
    {
        // Debug.Log("tile "+ tile.AxialCoordinates + " is entering Focused State");
        CameraController.Instance.onDeselectAction += tile.OnDeselect;
        CameraController.Instance.ChangeCamera(CameraMode.Focus);
        CameraController.Instance.IsLocked = true;
    }

    public override void Exit(HexTile tile)
    {
        // Debug.Log("tile "+ tile.AxialCoordinates + " is exiting Focused State");
        CameraController.Instance.onDeselectAction -= tile.OnDeselect;
        CameraController.Instance.ChangeCamera(CameraMode.Overhead);
        CameraController.Instance.IsLocked = false;
    }
    
    public override ITileState OnDeselect()
    {
        return new SelectedState();
    }
}
