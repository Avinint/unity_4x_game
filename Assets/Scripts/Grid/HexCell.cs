using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour, IEquatable<HexCell>, IComparable<HexCell>
{
    
    [Header("Cell Properties")] [SerializeField]
    private HexOrientation Orientation;
    [field:SerializeField] public HexGrid Grid { get; set; }
    [field:SerializeField] public float HexSize { get; set; }
    [field:SerializeField] public TerrainType TerrainType { get; set; }
    [field:SerializeField] public Vector2Int OffsetCoordinates { get; }
    [field:SerializeField] public Vector2Int AxialCoordinates { get; set; }

    [field:SerializeField] public Vector3Int CubeCoordinates { get; set; }
    [field:NonSerialized] public List<HexCell> Neighbours { get; set; }

    private Vector3 CentrePosition { get; set; }
    

    [SerializeField] private CellState cellState;
    private ICellState state;

    public ICellState State
    {
        get { return state; }
        private set
        {
            state = value;
            cellState = state.State;
        }
    }

    private Transform terrain;
    public Transform Terrain {get { return terrain; } }

    public override string ToString()
    {
        return AxialCoordinates.ToString();
    }

    public void InitializeState(ICellState initialState = null)
    {
        if (initialState == null)
            ChangeState(new VisibleState());
        else
            ChangeState(initialState);
    
    }
    
    private void ChangeState(ICellState newState)
    {
        if(newState == null)
        {
            Debug.LogError("Trying to set null state.");
            return;
        }
    
        if(State != newState)
        {
            // Debug.Log($"Changing state from {State} to {newState}");
            if(State != null)
                State.Exit(this);
            State = newState;
            State.Enter(this);
        }
    }

    public void Discover()
    {
        ChangeState(new VisibleState());
    }
    
    public void Reveal()
    {
        UnityEngine.Object.Destroy(terrain.gameObject);
        terrain = UnityEngine.Object.Instantiate(
            TerrainType.Prefab,
            CentrePosition, 
            Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            Grid.transform
        );
        
        terrain.gameObject.layer = LayerMask.NameToLayer("Grid");

        //TODO: Adjust the size of the prefab to the size of the grid cell

        if(Orientation == HexOrientation.FlatTop)
        {
            terrain.Rotate(new Vector3(0, 30, 0));
        }
    }

    public void OnMouseEnter()
    {
        ChangeState(State.OnMouseEnter());
    }
    
    public void OnMouseExit()
    {
        ChangeState(State.OnMouseExit());
    }
    
    public void OnSelect()
    {
        ChangeState(State.OnSelect());
        OnDeselect();
    }

    public void OnMoveUnit()
    {
        CameraController.Instance.CameraTarget.transform.position = Terrain.transform.position;
    }

  
    
    
    public void OnDeselect()
    {
        ChangeState(State.OnDeselect());
    }
    
    public void OnFocus()
    {
        ChangeState(State.OnFocus());
        
        Debug.Log("STATE "  + State);
    }

    public HexCell(Vector2Int coordinates, HexOrientation orientation)
    {
        Orientation = orientation;
        OffsetCoordinates = coordinates;
        AxialCoordinates = HexMetrics.OffsetToAxial(coordinates.x, coordinates.y, orientation);
        CubeCoordinates =  HexMetrics.AxialToCube(AxialCoordinates.x, AxialCoordinates.y);
        
    }

    public void SetTerrainType(TerrainType terrainType)
    {
        TerrainType = terrainType;
    }

    public void CreateTerrain(HexOrientation orientation, Transform unknownPrefab)
    {
        if(TerrainType == null)
        {
            Debug.LogError("TerrainType is null");
            return;
        }
        if(Grid == null)
        {
            Debug.LogError("Grid is null");
            return;
        }
        if (HexSize == 0)
        {
            Debug.LogError("HexSize is 0");
            return;
        }
        if (TerrainType.Prefab == null)
        {
            Debug.LogError("TerrainType Prefab is null");
            return;
        }

        CentrePosition = HexMetrics.Center(
            HexSize, 
            (int)OffsetCoordinates.x, 
            (int)OffsetCoordinates.y, orientation
        ) + Grid.transform.position;

        terrain = UnityEngine.Object.Instantiate(
            unknownPrefab,
            CentrePosition, 
            orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            Grid.transform
        );

        // terrain = UnityEngine.Object.Instantiate(
        //     TerrainType.Prefab,
        //     centrePosition, 
        //     orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
        //     Grid.transform
        // );

        terrain.gameObject.layer = LayerMask.NameToLayer("Grid");

        //TODO: Adjust the size of the prefab to the size of the grid cell
        
        if(Orientation == HexOrientation.FlatTop)
        {
            terrain.Rotate(new Vector3(0, 30, 0));
        }

        //Temporary random rotation to make the terrain look more natural
        //int randomRotation = UnityEngine.Random.Range(0, 6);
        //terrain.Rotate(new Vector3(0, randomRotation*60, 0));
        // HexTerrain hexTerrain = terrain.GetComponentInChildren<HexTerrain>();
        // if (hexTerrain == null)
        // {
        //     Debug.Log(terrain.gameObject.GetComponent<HexTerrain>());
        //     Debug.LogError("Transform " + terrain.position);
        //     Debug.LogError("Hex terrain manquant sur la prefab de " + TerrainType.Name);
        // }
        // hexTerrain.OnMouseEnterAction += OnMouseEnter;
        // hexTerrain.OnMouseExitAction += OnMouseExit;
 
        // Debug.Log( terrain.gameObject);
      
    }

    public void SetNeighbours(List<HexCell> neighbours)
    {
        Neighbours = neighbours;
    }
    
    public void ClearTerrain()
    {
        if(terrain != null)
        {
            // HexTerrain hexTerrain = terrain.GetComponentInChildren<HexTerrain>();
            // hexTerrain.OnMouseEnterAction -= OnMouseEnter;
            // hexTerrain.OnMouseExitAction -= OnMouseExit;
            UnityEngine.Object.Destroy(terrain.gameObject);
        }
    }

    public bool IsNotLand()
    {
        return TerrainType.Name == "Ocean" || TerrainType.Name == "Coast" || TerrainType.Name == "Mountain";
    }

    public bool IsLand()
    {
        return !IsNotLand();
    }

    /*
     * Hex is not too close to edge of grid
     */
    public bool IsNotCloseToGridEdge(int radius, int gridWidth, int gridHeight)
    {
        return OffsetCoordinates.x > radius && OffsetCoordinates.x < gridWidth - radius &&
               OffsetCoordinates.y > radius && OffsetCoordinates.y < gridHeight - radius;
    }

    public bool IsCloseToGridEdge(int radius, int gridWidth, int gridHeight)
    {
        return OffsetCoordinates.x <= radius || OffsetCoordinates.x >= gridWidth - radius ||
               OffsetCoordinates.y <= radius || OffsetCoordinates.y >= gridHeight - radius;
    }

    public bool Equals(HexCell other)
    {

        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return OffsetCoordinates.Equals(other.OffsetCoordinates);
    }

    // public override bool Equals(object obj)
    // {
    //     if (ReferenceEquals(null, obj)) return false;
    //     if (ReferenceEquals(this, obj)) return true;
    //     if (obj.GetType() != this.GetType()) return false;
    //     return Equals((HexCell)obj);
    // }

    
    public int CompareTo(HexCell other)
    {
        if (other == null) return 1;
    
        if (AxialCoordinates.x == other.AxialCoordinates.x && AxialCoordinates.y == other.AxialCoordinates.y)
        {
            return 0;
        }
        else if ((AxialCoordinates.x > other.AxialCoordinates.x  && AxialCoordinates.y >= other.AxialCoordinates.y)  || (AxialCoordinates.x >= other.AxialCoordinates.x && AxialCoordinates.y > other.AxialCoordinates.y))
        {
            return 1;
        }
        else
        {
            return -1;
        }
    
    }

    public override int GetHashCode()
    {
        return OffsetCoordinates.GetHashCode();
    }

    // public void OnDrawGizmosSelected()
    // {
    //     foreach (HexCell neighbour in Neighbours)
    //     {
    //         Gizmos.color = Color.blue;
    //         Gizmos.DrawSphere(transform.position, 0.1f);
    //         Gizmos.color = Color.white;
    //         Gizmos.DrawLine(transform.position, neighbour.transform.position);
    //     }
    // } sert à rien TODO remove
}
