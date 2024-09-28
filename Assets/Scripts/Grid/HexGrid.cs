using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.WSA;

public class HexGrid : MonoBehaviour
{
    [field:SerializeField] public HexOrientation Orientation { get; private set; }
    [field:SerializeField, Range(10, 100)] public int Width { get; private set; }
    [field:SerializeField, Range(10, 100)] public int Height { get; private set; }
    [field:SerializeField] public float HexSize { get; private set; }
    [field: SerializeField] public int BatchSize { get; private set; }
    public List<Vector2> DefaultVisibleCells { get; set; } = new List<Vector2> { new Vector2(0, 0) };
    public int DefaultVisibleRadius { get; set; } = 1;


    public List<HexCell> StartTiles { get; set; } = new List<HexCell>();

    [SerializeField] private Dictionary<Vector2Int, HexCell> Tiles = new Dictionary<Vector2Int, HexCell>();
    private HexCell activeCell;

    [SerializeField] private Transform unknownPrefab;
    [SerializeField] private Transform debugPrefab;
    
    private MapGenerator mapGenerator;

    private Task<Dictionary<Vector2Int, HexCell>> hexGenerationTask;
    // TODO: Methods to get, add, change, and remove hexes

    private Vector3 gridOrigin;

    public event System.Action OnMapInfoGenerated;
    public event System.Action<float> OnCellBatchGenerated;
    public event System.Action OnCellInstancesGenerated;
    
    public int PlayerCount { get; set; }

    [field: SerializeField] public int PlayerSpawningMinDistance { get; set; } = 0;
    
    // [field:SerializeField] public Player Player { get; set; }
    
    
    // public List<GameObject> StartingUnits = new List<GameObject>();
    
    private void Awake()
    {
        if (BatchSize == 0)
            BatchSize = 1;
        gridOrigin = transform.position;
        mapGenerator = FindObjectOfType<MapGenerator>();


        if (PlayerSpawningMinDistance == 0)
        {
            int avg = (Width + Height) / 2;
            PlayerSpawningMinDistance = (int) Mathf.Ceil(avg / (PlayerCount / 2));
        }
    }

    private void OnEnable()
    {

        if(mapGenerator != null)
        {
            mapGenerator.OnTerrainMapGenerated += SetHexCellTerrainTypes;
            mapGenerator.onTerrainMapCleared += RemoveHexCells;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        

        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        MouseController.Instance.OnMouseMoving += OnMouseMove;
        
    }

    private void OnDisable()
    {
        if (mapGenerator != null)
        {
            mapGenerator.OnTerrainMapGenerated -= SetHexCellTerrainTypes;
            mapGenerator.onTerrainMapCleared -= RemoveHexCells;
        }
        if (hexGenerationTask != null && hexGenerationTask.Status == TaskStatus.Running)
        {
            hexGenerationTask.Dispose();
        }

        MouseController.Instance.OnMouseMoving -= OnMouseMove;
    }
    
    private void OnMouseMove(RaycastHit hit)
    {
        if (CameraController.Instance.IsLocked) return;
        if (activeCell != null) activeCell.OnMouseExit();

     
        //Transform objectHit = hit.transform;
        
        
        Vector3 position = hit.transform.position;
        float localX = hit.point.x - position.x;
        float localZ = hit.point.z - position.z;
        Vector2Int axial = HexMetrics.CoordinateToAxial(localX, localZ, HexSize, Orientation);

        // if (objectHit.TryGetComponent<HexCell>(out HexCell target))
        // {
        //     activeCell = target;
        //     activeCell.OnMouseEnter();
        //     Debug.Log("Entering tile : " + activeCell.TerrainType.Name);
        // }

        HexCell cell = Tiles[axial];
        
        // HexCell cell = Tiles.Find(c => c.OffsetCoordinates == location);
        if (cell != null)
        {
           activeCell = cell;
           activeCell.OnMouseEnter();
        }
    }

    private void SetHexCellTerrainTypes(TerrainType[,] terrainMap)
    {
        ClearHexCells();
        hexGenerationTask = Task.Run(() => GenerateHexCellData(terrainMap));

        hexGenerationTask.ContinueWith(task =>
        {
            Debug.Log(" Setting cells");
            Tiles = task.Result;
            MainThreadDispatcher.Instance.Enqueue(() => StartCoroutine(InstantiateCells(Tiles, Orientation)));
        });
    }


    private void ClearHexCells() 
    {
        // TODO remove
        // for (int i = 0; i < Tiles.Keys.Count; i++)
        // {
        //     Tiles[i].ClearTerrain();
        // }

        foreach (var (axial, tile) in Tiles)
        {
            tile.ClearTerrain();
        }

        Tiles.Clear();
    }
    
    //This will become map generation
    //No Unity API allowed - including lloking up transform data, Instantiation, etc.
    private Dictionary<Vector2Int, HexCell> GenerateHexCellData(TerrainType[,] terrainMap)
    {
        Dictionary<Vector2Int, HexCell> hexCells = new Dictionary<Vector2Int, HexCell>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int flippedX = Width - x - 1;
                int flippedY = Height - y - 1;

                HexCell cell = new HexCell(new Vector2Int(x, y), Orientation);
                cell.Grid = this;
                cell.HexSize = HexSize;
                cell.SetTerrainType(terrainMap[flippedX, flippedY]);
                cell.InitializeState(new HiddenState());
                hexCells.Add(cell.AxialCoordinates, cell);
            }
        }

        SetNeighbours(hexCells);

        Debug.Log("GenerateHexCellData");
        
        return hexCells;
    }

    public void SetNeighbours(Dictionary<Vector2Int, HexCell> cells)
    {
        foreach (var (axial, tile)  in cells)
        {
            // Debug.Log("cell : " + cell.AxialCoordinates);
            List<HexCell> neighbours = new List<HexCell>();
            // Get the axial coordinates of the current cell
            Vector2Int currentAxialCoordinates = axial;

            // Get the neighbor directions for the current cell
            
            List<Vector2Int> neighborCoordinates = HexMetrics.GetNeighbourCoordinatesList(currentAxialCoordinates);
            int neighborsFound = 0;

            foreach (var neighbourCoordinate in neighborCoordinates)
            {
                // foreach (var c in cells)
                // {
                //     if (c.AxialCoordinates == neighbourCoordinate) 
                //         Debug.Log("FOREACH CELLS : " + c.AxialCoordinates + " " + neighbourCoordinate +" " +  (c.AxialCoordinates == neighbourCoordinate));
                // }
                // Find the neighbor cell based on the direction
                HexCell neighbor;
                if (cells.ContainsKey(neighbourCoordinate))
                {
                    neighbor = cells[neighbourCoordinate];
                    neighbours.Add(neighbor);
                    neighborsFound++;
                }
            }

            tile.SetNeighbours(neighbours);
            // Debug.Log($"Cell {cell.AxialCoordinates} has {neighborsFound} neighbours found");
        }
    }

    //Handled by coroutine and currently the most expensive operation
    private IEnumerator InstantiateCells(Dictionary<Vector2Int, HexCell> hexCells, HexOrientation orientation)
    {
        Debug.Log("Instantiating Hex Cells");
        int batchCount = 0;
        int totalBatches = Mathf.CeilToInt((float)hexCells.Count / BatchSize);


        int i = 0;
        foreach (var (axial, tile) in hexCells)
        {
            tile.CreateTerrain(orientation, unknownPrefab);
            
            // Yield every batchSize hex cells
            if (i % BatchSize == 0 && i != 0)
            {
                batchCount++;
                OnCellBatchGenerated?.Invoke((float)batchCount / totalBatches);
                yield return null;
            }

            i++;
        }
        
        
        // for (int i = 0; i < Tiles.Count; i++)
        // {
        //     
        //     Tiles[i].CreateTerrain(orientation, unknownPrefab);
        //     // Yield every batchSize hex cells
        //     if (i % BatchSize == 0 && i != 0)
        //     {
        //         batchCount++;
        //         OnCellBatchGenerated?.Invoke((float)batchCount / totalBatches);
        //         yield return null;
        //     }
        // }
        
        Debug.Log("Grid generated");

        OnCellInstancesGenerated?.Invoke();
    }

    private void RemoveHexCells()
    {
        Tiles.Clear();
    }

    public HexCell GetStartingTile(int playerIndex)
    {
        Vector2Int axialCoordinates;
        HexCell startCell;
        
        
        do
        {
            axialCoordinates = HexMetrics.OffsetToAxial(
                UnityEngine.Random.Range(0, Width),
                UnityEngine.Random.Range(0, Height), 
                Orientation);
           // startCoordinates = new Vector2(UnityEngine.Random.Range(0, Width), UnityEngine.Random.Range(0, Height));
            startCell = Tiles[axialCoordinates];
 
        } while ( startCell.IsNotLand());
        
        

        CameraController.Instance.CameraTarget.transform.position = startCell.Terrain.transform.position;

        return startCell;
    }
 
    public void DispatchPlayers()
    {
        var gridSize = PlayerSpawningMinDistance / Mathf.Sqrt(2);
        
        PlaceFirstPlayer();

        for (int i = 1; i < PlayerCount ; i++)
        {
            PlaceSubsequentPlayer();
        }
    }
    
    private void PlaceFirstPlayer()
    {
        Vector2Int axialCoordinates;
        HexCell tile;

        do
        {
            axialCoordinates = HexMetrics.OffsetToAxial(
                UnityEngine.Random.Range((Width/2) - PlayerSpawningMinDistance  , (Width/2) + PlayerSpawningMinDistance ),
                UnityEngine.Random.Range((Height / 2) - PlayerSpawningMinDistance  , (Height / 2) + PlayerSpawningMinDistance ), 
                Orientation);
            tile =  Tiles[axialCoordinates];
 
        } while (tile.IsNotLand());

        CameraController.Instance.CameraTarget.transform.position = tile.Terrain.transform.position;

        StartTiles.Add(tile);
        tile.Discover();
    }

    private void PlaceSubsequentPlayer()
    {
        Vector2Int axialCoordinates;
        HexCell tile;
        
        switch(StartTiles.Count)
        {
            case 1:
                do
                {
                    axialCoordinates = HexMetrics.OffsetToAxial(
                        UnityEngine.Random.Range(DefaultVisibleRadius, (Width / 2) - PlayerSpawningMinDistance),
                        UnityEngine.Random.Range(DefaultVisibleRadius, (Height / 2) - PlayerSpawningMinDistance),
                        Orientation);
                    tile = Tiles[axialCoordinates];
                } while (tile.IsNotLand());

                break;

            case 2:
                do
                {
                    axialCoordinates = HexMetrics.OffsetToAxial(
                        UnityEngine.Random.Range( (Width / 2 ) + PlayerSpawningMinDistance, Width - DefaultVisibleRadius),
                        UnityEngine.Random.Range(DefaultVisibleRadius, (Height / 2) - PlayerSpawningMinDistance),
                        Orientation);
                    tile = Tiles[axialCoordinates];
                } while (tile.IsNotLand());

                break;
            
            case 3:
                do
                {
                    axialCoordinates = HexMetrics.OffsetToAxial(
                        UnityEngine.Random.Range(DefaultVisibleRadius, (Width / 2) - PlayerSpawningMinDistance),
                        UnityEngine.Random.Range( (Height / 2 ) + PlayerSpawningMinDistance, Height - DefaultVisibleRadius),
                        Orientation);
                    tile = Tiles[axialCoordinates];
                } while (tile.IsNotLand());

                break;
            
            case 4:
                do
                {
                    axialCoordinates = HexMetrics.OffsetToAxial(
                        UnityEngine.Random.Range( (Width / 2 ) + PlayerSpawningMinDistance, Width - DefaultVisibleRadius),
                        UnityEngine.Random.Range( (Height / 2 ) + PlayerSpawningMinDistance, Height - DefaultVisibleRadius),
                        Orientation);
                    tile = Tiles[axialCoordinates];
                } while (tile.IsNotLand());

                break;
            default:
                do
                {
                    axialCoordinates = HexMetrics.OffsetToAxial(
                        UnityEngine.Random.Range( (Width / 2 ) + PlayerSpawningMinDistance, Width - DefaultVisibleRadius),
                        UnityEngine.Random.Range( (Height / 2 ) + PlayerSpawningMinDistance, Height - DefaultVisibleRadius),
                        Orientation);
                    tile = Tiles[axialCoordinates];
                } while (tile.IsNotLand());
                break;
        }
        
        
        // foreach (var tile in StartTiles)
        // {
        //     if (HexMetrics.AxialDistance(newTile.AxialCoordinates, tile.AxialCoordinates) <
        //         PlayerSpawningMinDistance)
        //     {
        //         isCloseToStartingPoint = true;
        //         break;
        //     }
        // }
        
          
        // bool failure = TileNotViable(newTile, isCloseToStartingPoint);
        // int tries = 0;
        //
        // if (failure)
        // {
        //     var subList1 = candidateTiles.GetRange(0, randomIndex);
        //     subList1.Reverse();
        //     var subList2 =  candidateTiles.GetRange(0, randomIndex);
        //     do
        //     {
        //         newTile = candidateTiles[randomIndex];
        //
        //         tries++;
        //         Debug.Log(tries);
        //
        //         failure = TileNotViable(newTile, isCloseToStartingPoint);
        //     } while (tries <= (candidateTiles.Count )  && failure == true);
        //
        //     if (failure)
        //     {
        //         throw new Exception("Impossible de trouver une case viable, Type : " + newTile.TerrainType.Name);
            // }
            StartTiles.Add(tile);
            tile.Discover();
        // }
        
       
        // foreach (var newCell in ring)
        // {
        //     if (Tiles.ContainsKey(newCell.AxialCoordinates) && newCell.IsNotCloseToGridEdge(DefaultVisibleRadius, Width, Height))
        //     {
        //        
        //         candidateCells.Add(newCell.AxialCoordinates, newCell);
        //         newCell.Discover();
        //         Debug.Log(newCell.AxialCoordinates);
        //     }
        //
        //
        // }
    //     
    //     var gridSize = PlayerSpawningMinDistance / Mathf.Sqrt(2);
    //     
    //     
    }

    private bool TileNotViable(HexCell tile, bool isCloseToStartingPoint)
    {
        return isCloseToStartingPoint || tile.IsNotLand() ||  tile.IsCloseToGridEdge(DefaultVisibleRadius, Width, Height);
    }

    private HexCell PickCellRandomly(List<HexCell> area, ref bool isCloseToStartingPoint)
    {
        HexCell newCell;
        var randomIndex = UnityEngine.Random.Range(0, area.Count);
        newCell = area[randomIndex];
        newCell.Discover();
        foreach (var tile in StartTiles)
        {
            if (HexMetrics.AxialDistance(newCell.AxialCoordinates, tile.AxialCoordinates) <
                PlayerSpawningMinDistance)
            {
                isCloseToStartingPoint = true;
                break;
            }
        }

        return newCell;
    }


    private List<HexCell> CreateDistanceRing(HexCell startCell, int distance = 0)
    {
        Dictionary<Vector2Int, HexCell> ringCenter = new Dictionary<Vector2Int, HexCell>();
        ringCenter.Add(startCell.AxialCoordinates, startCell);
        int ringNumber = 0;
        List<HexCell> outerRing = new List<HexCell>(startCell.Neighbours);

        while (ringNumber++ < distance)
        {
            List<HexCell> prevOuterRing = new List<HexCell>(outerRing);
            outerRing.Clear();
            foreach (var cell in prevOuterRing)
            {
                List<HexCell> neighbours = cell.Neighbours;
                foreach (HexCell neighbour in neighbours)
                {
                    if (!ringCenter.ContainsKey(neighbour.AxialCoordinates))
                    {
                        ringCenter.Add(neighbour.AxialCoordinates, cell);
                        outerRing.Add(neighbour);
                    }
                }
            }
        }

        // List<Vector2Int> coordinates = new List<Vector2Int>();
        // foreach (var tile in outerRing)
        // {
        //     coordinates.Add(tile.AxialCoordinates);
        // }

        return outerRing;
    }

    public Dictionary<Vector2Int, HexCell> GetStartingArea(HexCell startingTile)
    {
        Dictionary<Vector2Int, HexCell> startingArea = new Dictionary<Vector2Int, HexCell>();
        Queue<HexCell> tileQueue = new Queue<HexCell>();

        tileQueue.Enqueue(startingTile);

        int currentDepth = 0;

        while (tileQueue.Count > 0 && currentDepth <= DefaultVisibleRadius)
        {
            int queueSize = tileQueue.Count;
               
            for (int i = 0; i < queueSize; i++)
            {
                HexCell currentTile = tileQueue.Dequeue();

                if (!startingArea.ContainsKey(currentTile.AxialCoordinates))
                {
                    startingArea.Add(currentTile.AxialCoordinates, currentTile);
                }

                // Iterate through the neighbors of the current cell
                foreach (HexCell neighbor in currentTile.Neighbours)
                {
                    if (neighbor.State != new VisibleState()) 
                    {
                        tileQueue.Enqueue(neighbor);
                    }
                }
            }

            currentDepth++;
        }

        return startingArea;

    }


    Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };

    private void OnDrawGizmos()
    {
        for (int z = 0; z < Height; z++)
        for (int x = 0; x < Width; x++)
        {
            Vector3 centrePosition = HexMetrics.Center(HexSize, x, z, Orientation) + transform.position;
            Vector3[] corners = HexMetrics.Corners(HexSize, Orientation);

            Gizmos.color = Color.white;
            for (int s = 0; s < 6; s++)
            {
                Gizmos.DrawLine(
                    centrePosition + corners[s],
                    centrePosition + corners[(s + 1) % 6]
                );
                Gizmos.color = colors[s % 6];
                Gizmos.DrawSphere(centrePosition + HexMetrics.Corners(HexSize, Orientation)[s % 6], HexSize * 0.1f);

            }
        }
    }
}

public enum HexOrientation
{
    FlatTop,
    PointyTop
}