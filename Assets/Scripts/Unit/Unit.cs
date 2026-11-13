using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitType type;
    public Player Owner { get; set; }

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
        if (GameManager.Instance != null) GameManager.Instance.PlayerTurnStart += OnPlayerTurnStart;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.PlayerTurnStart -= OnPlayerTurnStart;
    }

    // Points rechargés au début du tour de son propriétaire.
    private void OnPlayerTurnStart(Player player)
    {
        if (player == Owner) ResetMovementPoints();
    }

    // Affiche ou masque les modèles de l'unité (brouillard) ; l'objet reste actif.
    public void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
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
    public void MoveAlong(List<HexTile> path)
    {
        if (IsMoving || path == null || path.Count == 0) return;
        MovementPoints -= path.Count;
        StartCoroutine(MoveRoutine(path));
    }

    // Avance case par case ; à chaque case atteinte, l'unité révèle ce qu'elle voit.
    private IEnumerator MoveRoutine(List<HexTile> path)
    {
        IsMoving = true;
        foreach (var tile in path)
        {
            Vector3 point = tile.CentrePosition;
            while (transform.position != point)
            {
                transform.position = Vector3.MoveTowards(transform.position, point, moveSpeed * Time.deltaTime);
                yield return null;
            }
            RevealAround(tile);
        }
        IsMoving = false;
    }

    // Seul le joueur local découvre la carte (le brouillard est le sien).
    private void RevealAround(HexTile tile)
    {
        if (Owner == null || Owner != GameManager.Instance.LocalPlayer || type == null) return;
        tile.Grid.RevealAround(tile.AxialCoordinates, type.SightRadius);
    }
}
