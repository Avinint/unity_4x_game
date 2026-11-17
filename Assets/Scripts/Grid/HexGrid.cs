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
    private RiverView riverView;

    private Task<Dictionary<Vector2Int, HexTile>> hexGenerationTask;
    // TODO: Methods to get, add, change, and remove hexes

    private Vector3 gridOrigin;

    public event System.Action<float> OnTileBatchGenerated;
    public event System.Action OnTileInstancesGenerated;
    
    public int PlayerCount { get; set; }

    [field: SerializeField] public int PlayerSpawningMinDistance { get; set; } = 0;

    [Header("Relief")]
    [SerializeField] private float hillHeight = 1.5f;
    [SerializeField] private float mountainHeight = 4f;
    public float ElevationOf(Relief relief) => MapRules.Elevation(relief, hillHeight, mountainHeight);

    private Mesh plinthMesh;
    public Mesh PlinthMesh => plinthMesh != null ? plinthMesh : (plinthMesh = HexMetrics.BuildPlinthMesh(HexSize));

    [Header("Brouillard")]
    [SerializeField] private Color fogColor = new Color(0.85f, 0.85f, 0.85f);   // brume (gris clair)
    [SerializeField, Range(0f, 1f)] private float fogAmount = 0.5f;             // 0 = couleur d'origine, 1 = brume pleine
    public Color FogColor => fogColor;
    public float FogAmount => fogAmount;

    // Modifié dans l'Inspector pendant le jeu : réapplique l'assombrissement à toutes les cases.
    private void OnValidate()
    {
        if (!UnityEngine.Application.isPlaying || Tiles == null) return;
        foreach (var tile in Tiles.Values) tile.RefreshDim();
    }

    private bool displayGizmo = true;

    private List<HexTile> path = new List<HexTile>();

    [field: SerializeField]
    public GameObject Selector { get; set; }

    private void Awake()
    {
        if (BatchSize == 0)
            BatchSize = 1;
        gridOrigin = transform.position;
        mapGenerator = FindAnyObjectByType<MapGenerator>();
        riverView = new GameObject("Rivers").AddComponent<RiverView>();
        riverView.transform.SetParent(transform, false);
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
        OnTileInstancesGenerated += BuildRivers;
    }

    private void ToggleGizmo()
    {
        displayGizmo = !displayGizmo;
    }

    // Cases instanciées : on trace les rivières calculées par MapGenerator.
    private void BuildRivers()
    {
        if (riverView != null && mapGenerator != null && mapGenerator.RiverEdges != null)
            riverView.Build(this, mapGenerator.RiverEdges);
    }

    // Appelé par HexTile.Discover : les rivières de cette case apparaissent.
    public void OnTileDiscovered(HexTile tile)
    {
        if (riverView != null) riverView.Reveal(tile);
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
        OnTileInstancesGenerated -= ToggleGizmo;
        OnTileInstancesGenerated -= BuildRivers;
    }

    public bool TryGetTile(int x, int y, out HexTile tile)
    {
        var coordinates = HexMetrics.OffsetToAxial(x, y, Orientation);

        return Tiles.TryGetValue(coordinates, out tile);
    }

    public bool TryGetTileAxial(Vector2Int axial, out HexTile tile) => Tiles.TryGetValue(axial, out tile);

    public IEnumerable<HexTile> AllTiles => Tiles.Values;

    // Cases existantes à une distance ≤ radius de center.
    public List<Vector2Int> CellsInRange(Vector2Int center, int radius)
    {
        var cells = new List<Vector2Int>();
        foreach (var axial in HexMetrics.AxialRange(center, radius))
            if (Tiles.ContainsKey(axial)) cells.Add(axial);
        return cells;
    }

    // Cases existantes vues depuis center dans un rayon, selon la ligne de vue (relief, forêts).
    public List<Vector2Int> CellsInSight(Vector2Int center, int radius)
    {
        int eye = Tiles.TryGetValue(center, out HexTile from) ? MapRules.SightHeight(from.Relief) : 0;
        return LineOfSight.Visible(center, CellsInRange(center, radius), ObstacleAt, SurfaceAt, eye);
    }

    private int ObstacleAt(Vector2Int axial) =>
        Tiles.TryGetValue(axial, out HexTile tile) ? MapRules.ObstacleHeight(tile.Relief, tile.Biome) : 0;

    private int SurfaceAt(Vector2Int axial) =>
        Tiles.TryGetValue(axial, out HexTile tile) ? MapRules.SightHeight(tile.Relief) : 0;

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
        if (show) HoverMarker.transform.position = _activeTile.DisplayPosition;
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

        Vector3 position = transform.position;   // repère de la grille, quel que soit le collider touché
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

    private void SetHexTileTerrainTypes(MapCell[,] cells)
    {
        ClearHexTiles();
        hexGenerationTask = Task.Run(() => GenerateHexTileData(cells));

        hexGenerationTask.ContinueWith(task =>
        {
            Tiles = task.Result;
            MainThreadDispatcher.Instance.Enqueue(() => StartCoroutine(InstantiateTiles(Tiles, Orientation)));
        });
    }


    private void ClearHexTiles() 
    {
        foreach (var (axial, tile) in Tiles)
        {
            tile.ClearTerrain();
        }

        Tiles.Clear();
    }
    
    //This will become map generation
    //No Unity API allowed - including lloking up transform data, Instantiation, etc.
    private Dictionary<Vector2Int, HexTile> GenerateHexTileData(MapCell[,] cells)
    {
        Dictionary<Vector2Int, HexTile> hexTiles = new Dictionary<Vector2Int, HexTile>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                HexTile tile = new HexTile(new Vector2Int(x, y), Orientation);
                tile.Grid = this;
                tile.HexSize = HexSize;
                tile.SetTerrainType(cells[x, y].Terrain);
                tile.Relief = cells[x, y].Relief;
                tile.Biome = cells[x, y].Biome;
                hexTiles.Add(tile.AxialCoordinates, tile);
            }
        }

        SetNeighbours(hexTiles);
        
        return hexTiles;
    }

    public void SetNeighbours(Dictionary<Vector2Int, HexTile> tiles)
    {
        foreach (var (axial, tile)  in tiles)
        {
            List<HexTile> neighbours = new List<HexTile>();
            // Get the axial coordinates of the current tile
            Vector2Int currentAxialCoordinates = axial;

            // Get the neighbor directions for the current tile
            
            List<Vector2Int> neighborCoordinates = HexMetrics.GetNeighbourCoordinatesList(currentAxialCoordinates);
            int neighborsFound = 0;

            foreach (var neighbourCoordinate in neighborCoordinates)
            {
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
        }
    }

    //Handled by coroutine and currently the most expensive operation
    private IEnumerator InstantiateTiles(Dictionary<Vector2Int, HexTile> hexTiles, HexOrientation orientation)
    {
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

        OnTileInstancesGenerated?.Invoke();
    }

    private void RemoveHexTiles()
    {
        Tiles.Clear();
    }

    /// <summary>
    /// Place les joueurs par échantillonnage du point le plus éloigné :
    /// le 1er au hasard, puis chacun sur une des cases de terre les plus éloignées des joueurs déjà placés.
    /// </summary>
    public void DispatchPlayers()
    {
        StartTiles.Clear();

        List<HexTile> candidates = Tiles.Values
            .Where(t => t.IsLand() && !t.IsCloseToGridEdge(DefaultVisibleRadius, Width, Height))
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogError("DispatchPlayers : aucune case de terre disponible pour placer les joueurs.");
            return;
        }

        // 0 = automatique : d'après la surface de terre disponible.
        int minDistance = PlayerSpawningMinDistance > 0
            ? PlayerSpawningMinDistance
            : StartPositionPicker.SuggestedMinDistance(candidates.Count, PlayerCount, DefaultVisibleRadius);

        var coordinates = candidates.Select(t => t.AxialCoordinates).ToList();
        var picks = StartPositionPicker.Pick(coordinates, PlayerCount, new System.Random(), minDistance);

        if (picks.Count < PlayerCount)
            Debug.LogWarning($"DispatchPlayers : {PlayerCount} joueurs demandés, {picks.Count} cases disponibles.");

        for (int p = 0; p < picks.Count; p++)
        {
            if (p > 0 && picks[p].DistanceToNearest < minDistance)
                // Aucune case assez éloignée : placé parmi les plus éloignées.
                Debug.Log($"Joueur {p} : distance minimale non respectée ({picks[p].DistanceToNearest} < {minDistance}).");

            StartTiles.Add(candidates[picks[p].Index]);
        }
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