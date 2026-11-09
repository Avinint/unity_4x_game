using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedUnitState : BaseUnitState
{
    public override UnitState State => UnitState.Selected;

    public override void Enter(Unit unit)
    {
        Debug.Log("Unit " + unit.name + " selected");
    }

    public override void Exit(Unit unit)
    {
        Debug.Log("Unit " + unit.name + " deselected");
    }
}
