using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitType type;

    public IUnitState State { get; private set; }
    public bool IsSelected => State != null && State.State == UnitState.Selected;

    private void Awake()
    {
        ChangeState(new IdleUnitState());
    }

    public void ChangeState(IUnitState newState)
    {
        if (newState == null)
        {
            Debug.LogError("Trying to set null unit state.");
            return;
        }

        State?.Exit(this);
        State = newState;
        State.Enter(this);
    }

    public void Select()
    {
        if (!IsSelected) ChangeState(new SelectedUnitState());
    }

    public void Deselect()
    {
        if (IsSelected) ChangeState(new IdleUnitState());
    }
}
