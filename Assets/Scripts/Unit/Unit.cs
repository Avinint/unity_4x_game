using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitType type;

    public IUnitState State { get; private set; }
    public bool IsSelected => State != null && State.State == UnitState.Selected;

    public int MovementPoints { get; private set; }

    private void Awake()
    {
        ChangeState(new IdleUnitState());
        ResetMovementPoints();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.TurnChange += ResetMovementPoints;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.TurnChange -= ResetMovementPoints;
    }

    // Points de mouvement du type (appelé au début de chaque tour).
    public void ResetMovementPoints() => MovementPoints = type != null ? type.MovementPoints : 0;

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

    [SerializeField] private float moveSpeed = 8f; // unités monde par seconde

    public bool IsMoving { get; private set; }

    // Suit les points dans l'ordre, à vitesse constante.
    public void MoveAlong(List<Vector3> points)
    {
        if (IsMoving || points == null || points.Count == 0) return;
        MovementPoints -= points.Count;
        StartCoroutine(MoveRoutine(points));
    }

    private IEnumerator MoveRoutine(List<Vector3> points)
    {
        IsMoving = true;
        foreach (var point in points)
        {
            while (transform.position != point)
            {
                transform.position = Vector3.MoveTowards(transform.position, point, moveSpeed * Time.deltaTime);
                yield return null;
            }
        }
        IsMoving = false;
    }
}
