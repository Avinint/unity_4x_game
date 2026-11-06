using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUnitState
{
    UnitState State { get; }
    
    void Enter(Unit unit);
    void Exit(Unit unit);
    
    IUnitState OnMoveUnit();
    IUnitState OnMoveMouse();
}
