using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VisibleState : BaseTileState
{
    public override TileState State => TileState.Visible;
    
    public override void Enter(HexTile tile) { }

    public override void Exit(HexTile tile)
    {
        // Debug.Log("cell "+ cell.AxialCoordinates + "is exiting Visible State");
    }

    public override ITileState OnMouseEnter()
    {
        return new HighlightedState();
    }
}
