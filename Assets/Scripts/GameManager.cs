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

    public Player LocalPlayer => localPlayer != null ? localPlayer : (Players.Count > 0 ? Players[0] : null);
    [field:SerializeField] public HexGrid HexGrid { get; private set; }
    
    public List<Vector2> DefaultVisibleTiles = new List<Vector2> { new Vector2(-1, -1) };

    [Range(1,6)]
    public int DefaultVisibleRadius = 2;
    public List<GameObject> StartingUnits = new List<GameObject>();

    public Dictionary<Vector2, GameObject> Units { get; private set; } = new Dictionary<Vector2, GameObject>();

    public int MaxTurns = 500;
    public int CurrentTurn { get; private set; } = 1;

    public event System.Action GameStart;
    public event System.Action TurnChange;


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
        // Le Singleton crée le contrôleur de sélection au premier accès.
        SelectionController.Instance.name = "SelectionController";
    }

    private void OnDisable()
    {

        HexGrid.OnTileInstancesGenerated -= InitPlayers;
    }



    private void InitPlayers()
    {
        HexGrid.PlayerCount = Players.Count;

        HexGrid.DispatchPlayers();
        

        // foreach (var player in Players)
        // {
        //     player.StartingTile = HexGrid.GetStartingTile(i++);
        //     HexGrid.StartTiles.Add(player.StartingTile );
        // }

        // int i = 0;
        Debug.Log("Starting tiles count " + HexGrid.StartTiles.Count);
        // foreach (HexTile tile in HexGrid.StartTiles)
        // {
        //     Players[i++].StartingTile = tile;
        // }
        //

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
            if (RevealAllPlayers || player == LocalPlayer) SetVisibleTiles(player.StartingArea);
            AddStartingUnits(player);
        }

        // La caméra démarre sur la case de départ du joueur local.
        if (LocalPlayer != null && LocalPlayer.StartingTile != null)
            CameraController.Instance.CameraTarget.transform.position = LocalPlayer.StartingTile.CentrePosition;
    }
    
    private void SetVisibleTiles(Dictionary<Vector2Int, HexTile> tiles)
    {
        foreach (var ( coordinates, tile) in tiles)
        {
            tile.Discover();
        }
    }

    public void ChangeTurn(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CurrentTurn++;
            TurnChange?.Invoke();
        }
    }

    private void Start()
    {
        GameStart?.Invoke();
    }
    
    // chaque unité appartient à un joueur mais la liste sur la grille 

    public void AddStartingUnits(Player player)
    {
       
        var units = StartingUnits;
        GameObject firstUnit = null;
        var coordinates = new Vector3Int(player.StartingTile.OffsetCoordinates.x, 0, player.StartingTile.OffsetCoordinates.y); 

        Vector3 centrePosition = HexMetrics.Center(
            HexGrid.HexSize, 
            coordinates.x, 
            coordinates.z, HexGrid.Orientation
        ) + HexGrid.transform.position;

        // Le Vector3Int a y = 0 : la case de départ est déjà connue, pas besoin de la rechercher.
        HexTile tile = player.StartingTile;
        if (tile == null)
        {
            Debug.LogError("Le joueur n'a pas de case de départ");
            return;
        }
        
          Units[tile.AxialCoordinates]  = Instantiate(
            units[0],
            centrePosition, 
            HexGrid.Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            HexGrid.transform);

          Debug.Log("my test " + Units[tile.AxialCoordinates]);
          Debug.Log("my test2 " + tile);
          
          Units[tile.AxialCoordinates] .gameObject.layer = LayerMask.NameToLayer("Grid");
          tile.AddUnit(Units[tile.AxialCoordinates] );
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
                coordinates = new Vector3Int(tile.OffsetCoordinates.x, 0, tile.OffsetCoordinates.y); 
            
                centrePosition = HexMetrics.Center(
                    HexGrid.HexSize, 
                    (int)coordinates.x, 
                    (int)coordinates.z, HexGrid.Orientation
                ) + HexGrid.transform.position;

                GameObject instance = Instantiate(
                    unit,
                    centrePosition,
                    HexGrid.Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity,
                    HexGrid.transform);
                tile.AddUnit(instance);
            }
        }

      
    }
    
    
    
}
