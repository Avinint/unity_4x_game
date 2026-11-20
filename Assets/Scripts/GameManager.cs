using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : Singleton<GameManager>
{
    public List<Player> Players = new List<Player>();
    [SerializeField] private Player localPlayer;       // joueur local (par défaut : le premier de la liste)
    public bool RevealAllPlayers = true;               // débogage : révéler les zones de tous les joueurs
    public bool RevealWholeMap = false;                // débogage : toute la carte découverte et visible, toutes les unités affichées

    public Player LocalPlayer => localPlayer != null ? localPlayer : (Players.Count > 0 ? Players[0] : null);
    [field:SerializeField] public HexGrid HexGrid { get; private set; }
    
    public List<Vector2> DefaultVisibleTiles = new List<Vector2> { new Vector2(-1, -1) };

    [Range(1,6)]
    public int DefaultVisibleRadius = 2;
    public List<GameObject> StartingUnits = new List<GameObject>();


    public int MaxTurns = 500;

    public FogOfWar<Player> Fog { get; } = new FogOfWar<Player>();
    private TurnCycle<Player> turns = new TurnCycle<Player>(new List<Player>(), null);
    public int CurrentTurn => turns.Round;
    public IReadOnlyList<Player> TurnOrder => turns.Order;
    public Player CurrentPlayer => turns.Current;

    public event System.Action GameStart;
    public event System.Action TurnChange;                // nouvelle manche
    public event System.Action<Player> PlayerTurnStart;   // début du tour d'un joueur


    protected override void Awake()
    {
        if (HexGrid == null)
            HexGrid = GameObject.FindAnyObjectByType<HexGrid>();
        if (HexGrid == null)
            Debug.LogError("No HexGrid component found");
    }

    private void OnEnable()
    {
        HexGrid.DefaultVisibleTiles = DefaultVisibleTiles;
        HexGrid.DefaultVisibleRadius = DefaultVisibleRadius;
        HexGrid.OnTileInstancesGenerated += InitPlayers;
        Fog.TileExplored += OnTileExplored;
        Fog.VisibilityChanged += OnVisibilityChanged;
        // Le Singleton crée le contrôleur de sélection au premier accès.
        SelectionController.Instance.name = "SelectionController";
    }

    private void OnDisable()
    {

        HexGrid.OnTileInstancesGenerated -= InitPlayers;
        Fog.TileExplored -= OnTileExplored;
        Fog.VisibilityChanged -= OnVisibilityChanged;
    }



    private void InitPlayers()
    {
        HexTile.Viewer = LocalPlayer;
        HexGrid.PlayerCount = Players.Count;

        HexGrid.DispatchPlayers();

        if (Players.Count > HexGrid.StartTiles.Count)
        {
            throw new Exception("Insufficient start locations for the number of players");
        }
        
        foreach (var player in Players)
        {
            int index = UnityEngine.Random.Range(0, HexGrid.StartTiles.Count);
            HexTile tile = HexGrid.StartTiles[index];
            player.StartingTile = tile;
            HexGrid.StartTiles.RemoveAt(index);
            player.StartingArea = HexGrid.GetStartingArea( player.StartingTile);
            Fog.Explore(player, player.StartingArea.Keys);
            if (RevealAllPlayers && player != LocalPlayer) SetVisibleTiles(player.StartingArea);   // débogage
            AddStartingUnits(player);
        }

        // La caméra démarre sur la case de départ du joueur local.
        if (LocalPlayer != null && LocalPlayer.StartingTile != null)
            CameraController.Instance.CameraTarget.transform.position = LocalPlayer.StartingTile.CentrePosition;

        BuildTurnOrder();

        if (RevealWholeMap)
            foreach (var tile in HexGrid.AllTiles)
            {
                tile.Discover();
                tile.SetVisibleNow(true);
            }
    }

    // Ordre de jeu : le joueur local d'abord, puis les autres dans l'ordre de Players.
    private void BuildTurnOrder()
    {
        turns = new TurnCycle<Player>(Players, LocalPlayer);
        PlayerTurnStart?.Invoke(CurrentPlayer);   // début de partie : tour du premier joueur
    }
    
    // L'affichage suit la connaissance du joueur local.
    private void OnTileExplored(Player player, Vector2Int cell)
    {
        if (player == LocalPlayer && HexGrid.TryGetTileAxial(cell, out HexTile tile)) tile.Discover();
    }

    // Cases vues / plus vues par le joueur local : terrain normal ou assombri, unités des autres affichées ou non.
    private void OnVisibilityChanged(Player player, Vector2Int cell, bool visible)
    {
        if (RevealWholeMap) return;   // débogage : tout reste visible
        if (player == LocalPlayer && HexGrid.TryGetTileAxial(cell, out HexTile tile)) tile.SetVisibleNow(visible);
    }

    private void SetVisibleTiles(Dictionary<Vector2Int, HexTile> tiles)
    {
        foreach (var ( coordinates, tile) in tiles)
        {
            tile.Discover();
            tile.SetVisibleNow(true);   // débogage : zones des autres joueurs affichées en clair
        }
    }

    // Touche de débogage lue directement : pas d'action dans PlayerControls pour un outil provisoire.
    private void Update()
    {
        if (Debug.isDebugBuild && Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            L10n.NextLocale();
    }

    // Entrée : fin du tour du joueur local.
    // Ignorée tant qu'une de ses unités est en marche : un tour n'est fini qu'une fois tous ses mouvements terminés.
    public void ChangeTurn(InputAction.CallbackContext context)
    {
        if (!context.performed || CurrentPlayer != LocalPlayer || HasMovingUnit(CurrentPlayer)) return;
        EndPlayerTurn();
    }

    private static bool HasMovingUnit(Player player)
    {
        foreach (var go in player.units)
            if (go != null && go.TryGetComponent(out Unit unit) && unit.IsMoving) return true;
        return false;
    }

    // Passe au joueur suivant ; nouvelle manche après le dernier.
    // Les joueurs sans IA passent leur tour aussitôt, jusqu'au retour du joueur local.
    private void EndPlayerTurn()
    {
        do
        {
            if (turns.Advance()) TurnChange?.Invoke();
            PlayerTurnStart?.Invoke(CurrentPlayer);
        } while (CurrentPlayer != LocalPlayer);
    }

    // Unité de départ : propriétaire, liste du joueur, puis ce qu'elle voit depuis sa case.
    private static void SetupStartingUnit(GameObject unitObject, Player player, HexTile tile)
    {
        player.units.Add(unitObject);
        if (!unitObject.TryGetComponent(out Unit unit)) return;
        unit.Owner = player;
        unit.UpdateVision(tile);
    }

    private void Start()
    {
        GameStart?.Invoke();
    }
    

    public void AddStartingUnits(Player player)
    {
       
        var units = StartingUnits;

        HexTile tile = player.StartingTile;
        if (tile == null)
        {
            Debug.LogError("Le joueur n'a pas de case de départ");
            return;
        }

          // CentrePosition inclut la hauteur de la case (colline).
          GameObject firstUnit = Instantiate(
            units[0],
            tile.CentrePosition,
            HexGrid.Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity,
            HexGrid.transform);

          firstUnit.layer = LayerMask.NameToLayer("Grid");
          tile.AddUnit(firstUnit);
          SetupStartingUnit(firstUnit, player, tile);
        var otherUnits = units.Skip(1).ToList();

        if (otherUnits.Count == 0) return;

        Dictionary<Vector2Int, HexTile> startingArea = player.StartingArea;

        // We don't want to add two units on same tile

        startingArea.Remove(player.StartingTile.AxialCoordinates);

        foreach (var unit in otherUnits)
        {
            int tries = 0;
           
            int randomIndex;
            do
            {
                randomIndex = UnityEngine.Random.Range(1, startingArea.Values.Count - 1);
                tile = startingArea.ElementAt(randomIndex).Value;

                tries++;
            } while (tile.IsNotLand() && tries < startingArea.Count);

            // TODO vérifier que le nombre de cases traversables soient au moins égales au nombre d'unités
 
            if (tile.IsLand())
            {
                startingArea.Remove(tile.AxialCoordinates);

                GameObject instance = Instantiate(
                    unit,
                    tile.CentrePosition,
                    HexGrid.Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity,
                    HexGrid.transform);
                tile.AddUnit(instance);
                SetupStartingUnit(instance, player, tile);
            }
        }

      
    }
    
    
    
}
