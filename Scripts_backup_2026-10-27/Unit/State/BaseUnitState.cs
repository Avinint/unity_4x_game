using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseUnitState : Singleton<BaseUnitState>, IUnitState
{
    public abstract UnitState State { get; }
    public abstract void Enter(Unit unit);
    public abstract void Exit(Unit unit);

    public virtual IUnitState OnMoveUnit()
    {
        return this;
    }

    public virtual IUnitState OnMoveMouse()
    {
        return this;
    }
}
