using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitType type;
    [SerializeField] private float ownerBaseDiameter = 4.5f;

    private Player owner;
    private Renderer ownerBase;
    private bool isVisible = true;

    // Changer de propriétaire met à jour le socle de couleur sous l'unité.
    public Player Owner
    {
        get => owner;
        set { owner = value; UpdateOwnerBase(); }
    }

    // Disque plat à la couleur du propriétaire (en attendant une bannière).
    private void UpdateOwnerBase()
    {
        if (owner == null)
        {
            if (ownerBase != null) ownerBase.enabled = false;
            return;
        }
        if (ownerBase == null)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "OwnerBase";
            var col = disc.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            disc.transform.localScale = new Vector3(ownerBaseDiameter, 0.05f, ownerBaseDiameter);
            ownerBase = disc.GetComponent<Renderer>();
            ownerBase.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        ownerBase.sharedMaterial = owner.PlayerColorMaterial;
        ownerBase.enabled = isVisible;
    }

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

    // Affichée pour le joueur local (brouillard) : une unité cachée ne se sélectionne pas d'un clic.
    public bool IsVisible => isVisible;

    // Affiche ou masque les modèles de l'unité (brouillard) ; l'objet reste actif.
    public void SetVisible(bool visible)
    {
        isVisible = visible;
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

    // Case de l'unité, tenue à jour par HexTile.AddUnit / RemoveUnit.
    public HexTile Tile { get; internal set; }

    // Suit le chemin case par case. La case d'arrivée est réservée dès le départ ;
    // si l'unité doit s'arrêter plus tôt, elle reprend la case où elle s'est arrêtée.
    public void MoveAlong(List<HexTile> path)
    {
        if (IsMoving || path == null || path.Count == 0 || Tile == null) return;
        HexTile start = Tile;
        start.RemoveUnit(gameObject);
        path[path.Count - 1].AddUnit(gameObject);
        StartCoroutine(MoveRoutine(start, path));
    }

    // Débogage : pose l'unité sur `tile` sans coût ni animation, puis met à jour ce qu'elle voit.
    public void Teleport(HexTile tile)
    {
        if (IsMoving || tile == null || Tile == null) return;
        Tile.RemoveUnit(gameObject);
        tile.AddUnit(gameObject);
        transform.position = tile.CentrePosition;
        UpdateVision(tile);
    }

    // Peut-elle entrer dans la case voisine `next` ? Il reste un point, et la case, vue car voisine,
    // est vraiment praticable : de la terre sans unité d'un autre joueur, découverte en route au besoin.
    public bool CanEnter(HexTile next) =>
        MovementPoints > 0 && next.IsWalkableFor(Owner, explored: true, seenNow: true);

    // Avance case par case ; à chaque case atteinte, l'unité révèle ce qu'elle voit.
    // Elle paie le vrai coût de chaque case et s'arrête si la suivante se révèle impraticable.
    private IEnumerator MoveRoutine(HexTile start, List<HexTile> path)
    {
        IsMoving = true;
        HexTile current = start;
        foreach (var tile in path)
        {
            if (!CanEnter(tile)) break;
            MovementPoints = Mathf.Max(0, MovementPoints - tile.Grid.MoveCost(current, tile));   // rivière comprise
            Vector3 point = tile.Grid.NearestCopy(tile.CentrePosition, transform.position);   // un seul pas à la couture
            while (transform.position != point)
            {
                transform.position = Vector3.MoveTowards(transform.position, point, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = tile.CentrePosition;   // copie affichée de la case (identique hors de la couture)
            current = tile;
            UpdateVision(tile);
        }
        // Arrêt avant la case réservée : l'unité reprend la case atteinte.
        HexTile end = path[path.Count - 1];
        if (current != end)
        {
            end.RemoveUnit(gameObject);
            current.AddUnit(gameObject);
        }
        IsMoving = false;
    }

    private List<Vector2Int> seenCells;   // cases vues depuis la dernière position

    // L'unité voit désormais depuis `tile` : met à jour la connaissance de son propriétaire.
    public void UpdateVision(HexTile tile)
    {
        if (Owner == null || type == null) return;
        var fog = GameManager.Instance.Fog;
        if (seenCells != null) fog.RemoveWatcher(Owner, seenCells);
        seenCells = tile.Grid.CellsInSight(tile.AxialCoordinates, type.SightRadius);
        fog.AddWatcher(Owner, seenCells);
    }
}
