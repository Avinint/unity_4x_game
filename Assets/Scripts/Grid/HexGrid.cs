using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class HexGrid : MonoBehaviour
{
    [field:SerializeField] public HexOrientation Orientation { get; private set; }

    [field:SerializeField] public int Width { get; private set; }
    [field:SerializeField] public int Height { get; private set; }
    [field:SerializeField] public float HexSize { get; private set; }
    [field: SerializeField] public int BatchSize { get; private set; }
    public List<Vector2> DefaultVisibleCells = new List<Vector2> { new Vector2(0, 0) };
    public int DefaultVisibleRadius = 1;

    [SerializeField] private List<HexCell> cells = new List<HexCell>();
    private HexCell activeCell;

    [SerializeField] private Transform unknownPrefab;
    
    
    private MapGenerator mapGenerator;

    private Task<List<HexCell>> hexGenerationTask;
    // TODO: Methods to get, add, change, and remove hexes

    private Vector3 gridOrigin;

    public event System.Action OnMapInfoGenerated;
    public event System.Action<float> OnCellBatchGenerated;
    public event System.Action OnCellInstancesGenerated;

    private void Awake()
    {
        if (BatchSize == 0)
            BatchSize = 1;
        gridOrigin = transform.position;
        mapGenerator = FindObjectOfType<MapGenerator>();
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

        OnCellInstancesGenerated += SetVisibleCells;

        MouseController.Instance.OnMouseMoving += OnMouseMove;
    }

    private void OnDisable()
    {
        if (mapGenerator != null)
        {
            mapGenerator.OnTerrainMapGenerated -= SetHexCellTerrainTypes;
        }
        if (hexGenerationTask != null && hexGenerationTask.Status == TaskStatus.Running)
        {
            hexGenerationTask.Dispose();
        }
        OnCellInstancesGenerated -= SetVisibleCells;
        
        MouseController.Instance.OnMouseMoving -= OnMouseMove;
    }
    
    private void OnMouseMove(RaycastHit hit)
    {
        if (CameraController.Instance.IsLocked) return;
         if (activeCell != null) activeCell.OnMouseExit();
        Vector3 position = hit.transform.position;
        float localX = hit.point.x - position.x;
        float localZ = hit.point.z - position.z;
        Vector2 location = HexMetrics.CoordinateToOffset(localX, localZ, HexSize, Orientation);

        HexCell cell = cells.Find(c => c.OffsetCoordinates == location);
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
       
        hexGenerationTask.ContinueWith ( task =>
        {
            Debug.Log(" Setting cells");
            cells = task.Result;
            Debug.Log("results");
            MainThreadDispatcher.Instance.Enqueue(() => StartCoroutine(InstantiateCells(cells, Orientation)));
        });
    }

    private void ClearHexCells() 
    {
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].ClearTerrain();
        }

        cells.Clear();
    }
    
    //This will become map generation
    //No Unity API allowed - including lloking up transform data, Instantiation, etc.
    private List<HexCell> GenerateHexCellData(TerrainType[,] terrainMap)
    {
        List<HexCell> hexCells = new List<HexCell>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int flippedX = Width - x - 1;
                int flippedY = Height - y - 1;

                HexCell cell = new HexCell();
                cell.SetCoordinates(new Vector2(x, y), Orientation);
                cell.Grid = this;
                cell.HexSize = HexSize;
                cell.SetTerrainType(terrainMap[flippedX, flippedY]);
                cell.InitializeState(new HiddenState());
                hexCells.Add(cell);
            }
        }

        SetNeighbours(hexCells);

        Debug.Log("GenerateHexCellData");
        
        return hexCells;
    }

    public void SetNeighbours(List<HexCell> cells)
    {
        foreach (HexCell cell in cells)
        {
            // Debug.Log("cell : " + cell.AxialCoordinates);
            List<HexCell> neighbours = new List<HexCell>();
            // Get the axial coordinates of the current cell
            Vector2 currentAxialCoordinates = cell.AxialCoordinates;

            // Get the neighbor directions for the current cell
            List<Vector2> neighborCoordinates = HexMetrics.GetNeighbourCoordinatesList(currentAxialCoordinates);
            int neighborsFound = 0;
 
            foreach (Vector2 neighbourCoordinate in neighborCoordinates)
            {
                if (cell.OffsetCoordinates == Vector2.one)
                {
                    Debug.Log("Neighbor: " + neighbourCoordinate);
                }
                // Find the neighbor cell based on the direction
                HexCell neighbor = cells.Find(c => c.AxialCoordinates == neighbourCoordinate);

                // If the neighbor exists, add it to the Neighbours list
                if (neighbor != null)
                {
                    neighbours.Add(neighbor);
                    neighborsFound++;
                }
            }

            cell.SetNeighbours(neighbours);
            // Debug.Log($"Cell {cell.AxialCoordinates} has {neighborsFound} neighbours found");
        }
    }

    //Handled by coroutine and currently the most expensive operation
    private IEnumerator InstantiateCells(List<HexCell> hexCells, HexOrientation orientation)
    {
        Debug.Log("Instantiating Hex Cells");
        int batchCount = 0;
        int totalBatches = Mathf.CeilToInt(hexCells.Count / BatchSize);

        for (int i = 0; i < cells.Count; i++)
        {
            // bug sur l'orientation
            cells[i].CreateTerrain(orientation, unknownPrefab);
            // Yield every batchSize hex cells
            if (i % BatchSize == 0 && i != 0)
            {
                batchCount++;
                OnCellBatchGenerated?.Invoke((float)batchCount / totalBatches);
                yield return null;
            }
        }
        
        Debug.Log("Grid generated");

        OnCellInstancesGenerated?.Invoke();
    }

    private void RemoveHexCells()
    {
        cells.Clear();
    }
    

    private void SetVisibleCells()
    {
        Debug.Log("setting cell visibility");
        // Create a queue to store the cells to be processed
        Queue<HexCell> cellQueue = new Queue<HexCell>();

        // Set the visibility of the default visible cells
        foreach (Vector2 coordinates in DefaultVisibleCells)
        {
            HexCell cell = cells.Find(c => c.OffsetCoordinates == coordinates);
            Debug.Log("cells coordinates = "  + coordinates);
            Debug.Log("cells axial coordinates = "  + cell.AxialCoordinates);
            Debug.Log("cell not null : " + (cell != null));
            if (cell != null)
            {
               cell.Discover();
                cellQueue.Enqueue(cell);
            }
        }

        // Iterate through the neighbors and their neighbors up to the specified depth
        int currentDepth = 0;

        while (cellQueue.Count > 0 && currentDepth < DefaultVisibleRadius)
        {
           
            int queueSize = cellQueue.Count;
            for (int i = 0; i < queueSize; i++)
            {
                HexCell currentCell = cellQueue.Dequeue();
        
                // Iterate through the neighbors of the current cell
                foreach (HexCell neighbor in currentCell.Neighbours)
                {
                    if (neighbor.State != new VisibleState()) 
                    {
                        neighbor.Discover();
                        cellQueue.Enqueue(neighbor);
                    }
                }
            }
     
            currentDepth++;
        }
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