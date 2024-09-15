using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseCellState : ICellState
{
    public abstract CellState State { get; }
    public abstract void Enter(HexCell cell);
    public abstract void Exit(HexCell cell);

    public virtual ICellState OnDeselect()
    {
        return this;
    }
    
    public virtual ICellState OnFocus()
    {
        return this;
    }
    
    public virtual ICellState OnMouseEnter()
    {
        return this;
    }
    
    public virtual ICellState OnMouseExit()
    {
        return this;
    }
    
    public virtual ICellState OnSelect()
    {
        return this;
    }

    public new String ToString()
    {
        return State.ToString();
    }
}
