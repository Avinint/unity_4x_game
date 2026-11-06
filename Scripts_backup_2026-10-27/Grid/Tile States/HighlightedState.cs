using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightedState : BaseTileState
{
    public override TileState State => TileState.Highlighted;

    public override void Enter(HexTile tile)
    {
       
        Debug.Log("tile "+ tile.AxialCoordinates + "is entering Highlighted State");
        LeanTween.scale(tile.Terrain.gameObject, Vector3.one * 1.2f, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(tile.Terrain.gameObject, 5f, .2f).setEase(LeanTweenType.easeOutBack);
        CameraController.Instance.onSelectAction += tile.OnSelect;
        CameraController.Instance.onCommandAction += tile.OnMoveUnit;
    }

    public override void Exit(HexTile tile)
    {
        Debug.Log("tile "+ tile.AxialCoordinates + "is exiting Highlighted State");
        LeanTween.scale(tile.Terrain.gameObject, Vector3.one, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(tile.Terrain.gameObject, 0f, .2f).setEase(LeanTweenType.easeOutBack);
        CameraController.Instance.onSelectAction -= tile.OnSelect;
        CameraController.Instance.onCommandAction += tile.OnMoveUnit;
        
    }

    public override ITileState OnMouseExit()
    {
        return new VisibleState();
    }

    public override ITileState OnSelect()
    {
        return new SelectedState();
    }
}
