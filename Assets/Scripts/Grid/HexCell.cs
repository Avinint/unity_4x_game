using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell :  IEquatable<HexCell>
{
    

    [Header("Cell Properties")] [SerializeField]
    private HexOrientation orientation;
    [field:SerializeField] public HexGrid Grid { get; set; }
    [field:SerializeField] public float HexSize { get; set; }
    [field:SerializeField] public TerrainType TerrainType { get; set; }
    [field:SerializeField] public Vector2 OffsetCoordinates { get; }
    [field:SerializeField] public Vector2 AxialCoordinates { get; set; }

    [field:SerializeField] public Vector3 CubeCoordinates { get; set; }
    [field:NonSerialized] public List<HexCell> Neighbours { get; set; }
    
  
    [field:NonSerialized] public Transform UnknownPrefab { get; set; }
    

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
        
        Debug.Log(State);
    }

    public HexCell(Vector2 coordinates, HexOrientation orientation)
    {
        orientation = orientation;
        OffsetCoordinates = coordinates;
        CubeCoordinates = HexMetrics.OffsetToCube(OffsetCoordinates, orientation);
        AxialCoordinates = HexMetrics.CubeToAxial(CubeCoordinates);
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

        Vector3 centrePosition = HexMetrics.Center(
            HexSize, 
            (int)OffsetCoordinates.x, 
            (int)OffsetCoordinates.y, orientation
        ) + Grid.transform.position;

        UnknownPrefab = UnityEngine.Object.Instantiate(
            unknownPrefab,
            centrePosition, 
            orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            Grid.transform
        );
        
        terrain = UnityEngine.Object.Instantiate(
            TerrainType.Prefab,
            centrePosition, 
            orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            Grid.transform
        );

        terrain.gameObject.layer = LayerMask.NameToLayer("Grid");

        //TODO: Adjust the size of the prefab to the size of the grid cell
        
        if(orientation == HexOrientation.FlatTop)
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
        terrain.gameObject.SetActive(false);
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

    public override int GetHashCode()
    {
        return OffsetCoordinates.GetHashCode();
    }
}
