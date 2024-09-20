using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : Singleton<GameManager>
{
    public List<Player> Players = new List<Player>();
    [field:SerializeField] public HexGrid HexGrid { get; private set; }
    
    public List<Vector2> DefaultVisibleCells = new List<Vector2> { new Vector2(-1, -1) };

    [Range(1,6)]
    public int DefaultVisibleRadius = 2;
    public List<GameObject> StartingUnits = new List<GameObject>();

    public int MaxTurns = 500;
    
    
    public event System.Action GameStart;
    public event System.Action TurnChange;


    protected override void Awake()
    {
        if (HexGrid == null)
            HexGrid = GameObject.FindObjectOfType<HexGrid>();
        if (HexGrid == null)
            Debug.LogError("No HexGrid component found");
    }

    private void OnEnable()
    {
        HexGrid.DefaultVisibleCells = DefaultVisibleCells;
        HexGrid.DefaultVisibleRadius = DefaultVisibleRadius;


        HexGrid.OnCellInstancesGenerated += InitPlayers;

       
    }


    private void InitPlayers()
    {
        HexGrid.PlayerCount = Players.Count;
        int i = 0;

        HexGrid.DispatchPlayers();
        
        
        foreach (var player in Players)
        {
            player.StartingTile = HexGrid.GetStartingTile(i++);
            HexGrid.StartTiles.Add(player.StartingTile );
           
        }

        foreach (var player in Players)
        {
            player.StartingArea = HexGrid.GetStartingArea( player.StartingTile);
            SetVisibleCells(player.StartingArea);
            AddStartingUnits(player);
        }
    }
    
    private void SetVisibleCells(List<HexCell> cells)
    {
        foreach (var cell in cells)
        {
            cell.Discover();
        }
    }
    

    public void ChangeTurn(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TurnChange?.Invoke();
        }
    }


    private void Start()
    {
        GameStart?.Invoke();
    }
    
    

    // Start is called before the first frame update
  

    // Update is called once per frame
    void Update()
    {
        
    }
    
  
    public void AddStartingUnits(Player player)
    {
       
        
        var units = StartingUnits;
        var coordinates = new Vector3(player.StartingTile.OffsetCoordinates.x, 0, player.StartingTile.OffsetCoordinates.y); 

        Vector3 centrePosition = HexMetrics.Center(
            HexGrid.HexSize, 
            (int)coordinates.x, 
            (int)coordinates.z, HexGrid.Orientation
        ) + HexGrid.transform.position;

        Instantiate(
            units[0],
            centrePosition, 
            HexGrid.Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            HexGrid.transform);

        var otherUnits = units.Skip(1).ToList();
        if (otherUnits.Count == 0) return;

        List<HexCell> startingArea = player.StartingArea;

        
        // We don't want to add two units on same tile
        int index = startingArea.IndexOf(player.StartingTile);
        Debug.Log("Count hexes : " + startingArea.Count);
        Debug.Log("index : " + index);
        startingArea.RemoveAt(index);


        foreach (var unit in otherUnits)
        {
            int tries = 0;
            HexCell tile;
            int randomIndex;
            do
            {
                randomIndex = UnityEngine.Random.Range(0, startingArea.Count - 1);
                tile = startingArea[randomIndex];
            
                Debug.Log(tile.TerrainType.Name);

                tries++;
            } while (tile.IsNotLand() && tries < startingArea.Count);

            // TODO vérifier que le nombre de cases traversables soient au moins égales au nombre d'unités
            if (tile.IsLand())
            {
                startingArea.RemoveAt(randomIndex);
                coordinates = new Vector3(tile.OffsetCoordinates.x, 0, tile.OffsetCoordinates.y); 
            
                centrePosition = HexMetrics.Center(
                    HexGrid.HexSize, 
                    (int)coordinates.x, 
                    (int)coordinates.z, HexGrid.Orientation
                ) + HexGrid.transform.position;

                Instantiate(
                    unit,
                    centrePosition, 
                    HexGrid.Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
                    HexGrid.transform);
            }
        }

      
    }
    
    
    
}
