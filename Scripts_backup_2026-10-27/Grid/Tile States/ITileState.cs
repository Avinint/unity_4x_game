using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITileState
{
    TileState State { get; }
    void Enter(HexTile tile);
    void Exit(HexTile tile);

    ITileState OnMouseEnter();
    ITileState OnMouseExit();
    ITileState OnSelect();
    ITileState OnDeselect();
    ITileState OnFocus();
}
