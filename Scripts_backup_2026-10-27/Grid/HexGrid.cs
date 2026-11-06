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
    public List<Vector2> DefaultVisibleTiles { get; set; } = new List<Vector2> { new Vector2(0, 0) };
    public int DefaultVisibleRadius { get; set; } = 1;


    public List<HexTile> StartTiles { get; set; } = new List<HexTile>();

    [SerializeField] private Dictionary<Vector2Int, HexTile> Tiles = new Dictionary<Vector2Int, HexTile>();
    private HexTile _activeTile;

    [SerializeField] private Transform unknownPrefab;
    [SerializeField] private Transform debugPrefab;
    
    private MapGenerator mapGenerator;

    private Task<Dictionary<Vector2Int, HexTile>> hexGenerationTask;
    // TODO: Methods to get, add, change, and remove hexes

    private Vector3 gridOrigin;

    public event System.Action OnMapInfoGenerated;
    public event System.Action<float> OnTileBatchGenerated;
    public event System.Action OnTileInstancesGenerated;
    
    public int PlayerCount { get; set; }

    [field: SerializeField] public int PlayerSpawningMinDistance { get; set; } = 0;

    private bool displayGizmo = true;

    private List<HexTile> path = new List<HexTile>();
    
    // [field:SerializeField] public Player Player { get; set; }
    
    [field: SerializeField]
    public GameObject Selector { get; set; }
    
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
            mapGenerator.OnTerrainMapGenerated += SetHexTileTerrainTypes;
            mapGenerator.onTerrainMapCleared += RemoveHexTiles;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        

        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        MouseController.Instance.OnMouseMoving += OnMouseMove;
        OnTileInstancesGenerated += ToggleGizmo;

    }

    private void ToggleGizmo()
    {
        displayGizmo = !displayGizmo;
    }

    private void OnDisable()
    {
        if (mapGenerator != null)
        {
            mapGenerator.OnTerrainMapGenerated -= SetHexTileTerrainTypes;
            mapGenerator.onTerrainMapCleared -= RemoveHexTiles;
        }
        if (hexGenerationTask != null && hexGenerationTask.Status == TaskStatus.Running)
        {
            hexGenerationTask.Dispose();
        }

        MouseController.Instance.OnMouseMoving -= OnMouseMove;
    }

    public HexTile GetTile(int x, int y)
    {
       var coordinates =  HexMetrics.OffsetToAxial(x, y, Orientation);

       return Tiles[coordinates];
    }
    
    private void OnMouseMove(RaycastHit hit)
    {
        if (CameraController.Instance.IsLocked) return;
        if (_activeTile != null) _activeTile.OnMouseExit();

     
        //Transform objectHit = hit.transform;
        
        
        Vector3 position = hit.transform.position;
        float localX = hit.point.x - position.x;
        float localZ = hit.point.z - position.z;
        Vector2Int axial = HexMetrics.CoordinateToAxial(localX, localZ, HexSize, Orientation);

        // if (objectHit.TryGetComponent<HexTell>(out HexTell target))
        // {
        //     activeTile = target;
        //     activeTile.OnMouseEnter();
        //     Debug.Log("Entering tile : " + activeTile.TerrainType.Name);
        // }

        HexTile tile = Tiles[axial];
        
        // HexTile tile = Tiles.Find(c => c.OffsetCoordinates == location);
        if (tile != null)
        {
           _activeTile = tile;
           _activeTile.OnMouseEnter();
        }
    }

    private void SetHexTileTerrainTypes(TerrainType[,] terrainMap)
    {
        ClearHexTiles();
        hexGenerationTask = Task.Run(() => GenerateHexTileData(terrainMap));

        hexGenerationTask.ContinueWith(task =>
        {
            Debug.Log(" Setting tiles");
            Tiles = task.Result;
            MainThreadDispatcher.Instance.Enqueue(() => StartCoroutine(InstantiateTiles(Tiles, Orientation)));
        });
    }


    private void ClearHexTiles() 
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
    private Dictionary<Vector2Int, HexTile> GenerateHexTileData(TerrainType[,] terrainMap)
    {
        Dictionary<Vector2Int, HexTile> hexTiles = new Dictionary<Vector2Int, HexTile>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int flippedX = Width - x - 1;
                int flippedY = Height - y - 1;

                HexTile tile = new HexTile(new Vector2Int(x, y), Orientation);
                tile.Grid = this;
                tile.HexSize = HexSize;
                tile.SetTerrainType(terrainMap[flippedX, flippedY]);
                tile.InitializeState(new HiddenState());
                hexTiles.Add(tile.AxialCoordinates, tile);
            }
        }

        SetNeighbours(hexTiles);

        Debug.Log("GenerateHexTileData");
        
        return hexTiles;
    }

    public void SetNeighbours(Dictionary<Vector2Int, HexTile> tiles)
    {
        foreach (var (axial, tile)  in tiles)
        {
            // Debug.Log("tile : " + tile.AxialCoordinates);
            List<HexTile> neighbours = new List<HexTile>();
            // Get the axial coordinates of the current tile
            Vector2Int currentAxialCoordinates = axial;

            // Get the neighbor directions for the current tile
            
            List<Vector2Int> neighborCoordinates = HexMetrics.GetNeighbourCoordinatesList(currentAxialCoordinates);
            int neighborsFound = 0;

            foreach (var neighbourCoordinate in neighborCoordinates)
            {
                // foreach (var c in tiles)
                // {
                //     if (c.AxialCoordinates == neighbourCoordinate) 
                //         Debug.Log("FOREACH TILES : " + c.AxialCoordinates + " " + neighbourCoordinate +" " +  (c.AxialCoordinates == neighbourCoordinate));
                // }
                // Find the neighbor tile based on the direction
                HexTile neighbor;
                if (tiles.ContainsKey(neighbourCoordinate))
                {
                    neighbor = tiles[neighbourCoordinate];
                    neighbours.Add(neighbor);
                    neighborsFound++;
                }
            }

            tile.SetNeighbours(neighbours);
            // Debug.Log($"Tile {tile.AxialCoordinates} has {neighborsFound} neighbours found");
        }
    }

    //Handled by coroutine and currently the most expensive operation
    private IEnumerator InstantiateTiles(Dictionary<Vector2Int, HexTile> hexTiles, HexOrientation orientation)
    {
        Debug.Log("Instantiating Hex Tiles");
        int batchCount = 0;
        int totalBatches = Mathf.CeilToInt((float)hexTiles.Count / BatchSize);


        int i = 0;
        foreach (var (axial, tile) in hexTiles)
        {
            tile.CreateTerrain(orientation, unknownPrefab);
            
            // Yield every batchSize hex tiles
            if (i % BatchSize == 0 && i != 0)
            {
                batchCount++;
                OnTileBatchGenerated?.Invoke((float)batchCount / totalBatches);
                yield return null;
            }

            i++;
        }
        
        
        // for (int i = 0; i < Tiles.Count; i++)
        // {
        //     
        //     Tiles[i].CreateTerrain(orientation, unknownPrefab);
        //     // Yield every batchSize hex tiles
        //     if (i % BatchSize == 0 && i != 0)
        //     {
        //         batchCount++;
        //         OnTileBatchGenerated?.Invoke((float)batchCount / totalBatches);
        //         yield return null;
        //     }
        // }
        
        Debug.Log("Grid generated");

        OnTileInstancesGenerated?.Invoke();
    }

    private void RemoveHexTiles()
    {
        Tiles.Clear();
    }

    public HexTile GetStartingTile(int playerIndex)
    {
        Vector2Int axialCoordinates;
        HexTile startTile;
        
        
        do
        {
            axialCoordinates = HexMetrics.OffsetToAxial(
                UnityEngine.Random.Range(0, Width),
                UnityEngine.Random.Range(0, Height), 
                Orientation);
           // startCoordinates = new Vector2(UnityEngine.Random.Range(0, Width), UnityEngine.Random.Range(0, Height));
            startTile = Tiles[axialCoordinates];
 
        } while ( startTile.IsNotLand());
        
        

        CameraController.Instance.CameraTarget.transform.position = startTile.Terrain.transform.position;

        return startTile;
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
        HexTile tile;

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
        HexTile tile;
        
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
        
       
        // foreach (var newTile in ring)
        // {
        //     if (Tiles.ContainsKey( newTile.AxialCoordinates) &&  newTile.IsNotCloseToGridEdge(DefaultVisibleRadius, Width, Height))
        //     {
        //        
        //         candidateTiles.Add( newTile.AxialCoordinates,  newTile);
        //          newTile.Discover();
        //         Debug.Log( newTile.AxialCoordinates);
        //     }
        //
        //
        // }
    //     
    //     var gridSize = PlayerSpawningMinDistance / Mathf.Sqrt(2);
    //     
    //     
    }

    private bool TileNotViable(HexTile tile, bool isCloseToStartingPoint)
    {
        return isCloseToStartingPoint || tile.IsNotLand() ||  tile.IsCloseToGridEdge(DefaultVisibleRadius, Width, Height);
    }

    private HexTile PickTileRandomly(List<HexTile> area, ref bool isCloseToStartingPoint)
    {
        HexTile newTile;
        var randomIndex = UnityEngine.Random.Range(0, area.Count);
        newTile = area[randomIndex];
        newTile.Discover();
        foreach (var tile in StartTiles)
        {
            if (HexMetrics.AxialDistance(newTile.AxialCoordinates, tile.AxialCoordinates) <
                PlayerSpawningMinDistance)
            {
                isCloseToStartingPoint = true;
                break;
            }
        }

        return newTile;
    }


    private List<HexTile> CreateDistanceRing(HexTile startTile, int distance = 0)
    {
        Dictionary<Vector2Int, HexTile> ringCenter = new Dictionary<Vector2Int, HexTile>();
        ringCenter.Add(startTile.AxialCoordinates, startTile);
        int ringNumber = 0;
        List<HexTile> outerRing = new List<HexTile>(startTile.Neighbours);

        while (ringNumber++ < distance)
        {
            List<HexTile> prevOuterRing = new List<HexTile>(outerRing);
            outerRing.Clear();
            foreach (var tile in prevOuterRing)
            {
                List<HexTile> neighbours = tile.Neighbours;
                foreach (HexTile neighbour in neighbours)
                {
                    if (!ringCenter.ContainsKey(neighbour.AxialCoordinates))
                    {
                        ringCenter.Add(neighbour.AxialCoordinates, tile);
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

    public Dictionary<Vector2Int, HexTile> GetStartingArea(HexTile startingTile)
    {
        Dictionary<Vector2Int, HexTile> startingArea = new Dictionary<Vector2Int, HexTile>();
        Queue<HexTile> tileQueue = new Queue<HexTile>();

        tileQueue.Enqueue(startingTile);

        int currentDepth = 0;

        while (tileQueue.Count > 0 && currentDepth <= DefaultVisibleRadius)
        {
            int queueSize = tileQueue.Count;
               
            for (int i = 0; i < queueSize; i++)
            {
                HexTile currentTile = tileQueue.Dequeue();

                if (!startingArea.ContainsKey(currentTile.AxialCoordinates))
                {
                    startingArea.Add(currentTile.AxialCoordinates, currentTile);
                }

                // Iterate through the neighbors of the current tile
                foreach (HexTile neighbor in currentTile.Neighbours)
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
        if (!displayGizmo) return;

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
        
        
        
        if (path != null)
        {
            foreach (HexTile tile in path)
            {
                Gizmos.DrawCube(tile.CentrePosition + new Vector3(0f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f));
            }
        }
    }
    
    
}

public enum HexOrientation
{
    FlatTop,
    PointyTop
}