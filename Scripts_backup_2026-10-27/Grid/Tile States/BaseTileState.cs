using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTileState : ITileState
{
    public abstract TileState State { get; }
    public abstract void Enter(HexTile tile);
    public abstract void Exit(HexTile tile);

    public virtual ITileState OnDeselect()
    {
        return this;
    }
    
    public virtual ITileState OnFocus()
    {
        return this;
    }
    
    public virtual ITileState OnMouseEnter()
    {
        return this;
    }
    
    public virtual ITileState OnMouseExit()
    {
        return this;
    }
    
    public virtual ITileState OnSelect()
    {
        return this;
    }

    public new String ToString()
    {
        return State.ToString();
    }
}
