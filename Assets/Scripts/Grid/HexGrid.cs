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

    private Dictionary<Vector2Int, HexTile> Tiles = new Dictionary<Vector2Int, HexTile>();
    private HexTile _activeTile;
    public HexTile HoveredTile => _activeTile;

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
        mapGenerator = FindAnyObjectByType<MapGenerator>();
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
        MouseController.Instance.onMouseExit += OnMouseLeaveGrid;
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
        MouseController.Instance.onMouseExit -= OnMouseLeaveGrid;
    }

    public bool TryGetTile(int x, int y, out HexTile tile)
    {
        var coordinates = HexMetrics.OffsetToAxial(x, y, Orientation);

        return Tiles.TryGetValue(coordinates, out tile);
    }

    public bool TryGetTileAxial(Vector2Int axial, out HexTile tile) => Tiles.TryGetValue(axial, out tile);

    // Rectangle (x, z) couvrant les centres des cases, en coordonnées monde.
    public Rect WorldBounds
    {
        get
        {
            Rect bounds = HexMetrics.GridBounds(Width, Height, HexSize, Orientation);
            bounds.position += new Vector2(transform.position.x, transform.position.z);
            return bounds;
        }
    }
    
    [field: SerializeField] public GameObject HoverMarker { get; set; }

    // Le marqueur de survol suit la case survolée (découverte) et se masque sinon.
    public void UpdateHoverMarker()
    {
        if (HoverMarker == null) return;
        // Pas de marqueur de survol sur la case sélectionnée (elle a déjà le sien).
        HexTile selected = SelectionController.Instance != null ? SelectionController.Instance.SelectedTile : null;
        bool show = _activeTile != null && _activeTile.IsHovered && _activeTile != selected;
        HoverMarker.SetActive(show);
        if (show) HoverMarker.transform.position = _activeTile.CentrePosition;
    }

    // La souris quitte la grille : le survol s'efface.
    private void OnMouseLeaveGrid()
    {
        if (CameraController.Instance.IsLocked) return;
        if (_activeTile == null) return;

        _activeTile.OnMouseExit();
        _activeTile = null;
        UpdateHoverMarker();
    }

    private void OnMouseMove(RaycastHit hit)
    {
        if (CameraController.Instance.IsLocked) return;

        Vector3 position = hit.transform.position;
        float localX = hit.point.x - position.x;
        float localZ = hit.point.z - position.z;
        Vector2Int axial = HexMetrics.CoordinateToAxial(localX, localZ, HexSize, Orientation);

        // Souris hors d'une case du jeu : cas normal, on ignore.
        if (!Tiles.TryGetValue(axial, out HexTile tile)) return;

        // Même case que la précédente : rien à faire (évite Exit/Enter à chaque mouvement).
        if (tile == _activeTile) return;

        if (_activeTile != null) _activeTile.OnMouseExit();
        _activeTile = tile;
        _activeTile.OnMouseEnter();
        UpdateHoverMarker();
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
        } while (!Tiles.TryGetValue(axialCoordinates, out startTile) || startTile.IsNotLand());
        
        

        CameraController.Instance.CameraTarget.transform.position = startTile.Terrain.transform.position;

        return startTile;
    }
 
    /// <summary>
    /// Place les joueurs par échantillonnage du point le plus éloigné :
    /// le 1er au hasard, puis chacun sur une des cases de terre les plus éloignées des joueurs déjà placés.
    /// </summary>
    public void DispatchPlayers()
    {
        StartTiles.Clear();

        if (PlayerSpawningMinDistance == 0)
        {
            int avg = (Width + Height) / 2;
            PlayerSpawningMinDistance = avg / Mathf.Max(1, PlayerCount / 2);
        }

        List<HexTile> candidates = Tiles.Values
            .Where(t => t.IsLand() && !t.IsCloseToGridEdge(DefaultVisibleRadius, Width, Height))
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogError("DispatchPlayers : aucune case de terre disponible pour placer les joueurs.");
            return;
        }

        var coordinates = candidates.Select(t => t.AxialCoordinates).ToList();
        var picks = StartPositionPicker.Pick(coordinates, PlayerCount, new System.Random());

        if (picks.Count < PlayerCount)
            Debug.LogWarning($"DispatchPlayers : {PlayerCount} joueurs demandés, {picks.Count} cases disponibles.");

        for (int p = 0; p < picks.Count; p++)
        {
            if (p > 0 && picks[p].DistanceToNearest < PlayerSpawningMinDistance)
                Debug.LogWarning($"Joueur {p} : distance minimale non respectée ({picks[p].DistanceToNearest} < {PlayerSpawningMinDistance}).");

            StartTiles.Add(candidates[picks[p].Index]);
        }
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
                    if (!startingArea.ContainsKey(neighbor.AxialCoordinates))
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