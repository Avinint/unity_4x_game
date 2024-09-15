using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICellState
{
    CellState State { get; }
    void Enter(HexCell cell);
    void Exit(HexCell cell);

    ICellState OnMouseEnter();
    ICellState OnMouseExit();
    ICellState OnSelect();
    ICellState OnDeselect();
    ICellState OnFocus();
}
