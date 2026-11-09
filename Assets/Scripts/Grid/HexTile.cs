using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HexTile : IEquatable<HexTile>, IComparable<HexTile>
{
    
    [Header("Tile Properties")] [SerializeField]
    private HexOrientation Orientation;
    [field:SerializeField] public HexGrid Grid { get; set; }
    [field:SerializeField] public float HexSize { get; set; }
    [field:SerializeField] public TerrainType TerrainType { get; set; }
    [field:SerializeField] public Vector2Int OffsetCoordinates { get; }
    [field:SerializeField] public Vector2Int AxialCoordinates { get; set; }

    [field:SerializeField] public Vector3Int CubeCoordinates { get; set; }
    [field:NonSerialized] public List<HexTile> Neighbours { get; set; }

    private HexTileGizmo gizmo;

    public Vector3 CentrePosition { get; set; }
    

    private Transform terrain;
    public Transform Terrain {get { return terrain; } }

    private List<GameObject> units = new();





    public override string ToString()
    {
        return AxialCoordinates.ToString();
    }


    public bool IsDiscovered { get; private set; }

    public void Discover()
    {
        if (IsDiscovered) return;   // déjà révélée : rien à refaire
        IsDiscovered = true;
        if (terrain != null) Reveal();
    }

    public void Reveal()
    {
        UnityEngine.Object.Destroy(terrain.GetComponent<HexTileGizmo>());
        UnityEngine.Object.Destroy(terrain.gameObject);
        UnityEngine.Object.Destroy(terrain);
        terrain = UnityEngine.Object.Instantiate(
            TerrainType.Prefab,
            CentrePosition, 
            Orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            Grid.transform
        );
        
        terrain.gameObject.layer = LayerMask.NameToLayer("Grid");

        if(Orientation == HexOrientation.FlatTop)
        {
            terrain.Rotate(new Vector3(0, 30, 0));
        }

       

        AddGizmo();
        gizmo.Unlock();
    }

    public bool IsHovered { get; private set; }

    public void OnMouseEnter()
    {
        if (!IsDiscovered || IsHovered) return;
        IsHovered = true;
        LeanTween.cancel(terrain.gameObject);
        LeanTween.scale(terrain.gameObject, Vector3.one * 1.2f, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(terrain.gameObject, 5f, .2f).setEase(LeanTweenType.easeOutBack);
    }

    public void OnMouseExit()
    {
        if (!IsHovered) return;
        IsHovered = false;
        LeanTween.cancel(terrain.gameObject);
        LeanTween.scale(terrain.gameObject, Vector3.one, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(terrain.gameObject, 0f, .2f).setEase(LeanTweenType.easeOutBack);
    }

    public void OnMoveUnit()
    {
      
        //CameraController.Instance.CameraTarget.transform.position = pos;
        // Grid.Selector.transform.position = pos;
        
      
        Grid.Selector.transform.position = CentrePosition;
        
    }

    public HexTile(Vector2Int coordinates, HexOrientation orientation)
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

    private void AddGizmo()
    {
        gizmo = terrain.AddComponent<HexTileGizmo>();
        gizmo.Neighbours = Neighbours;
        gizmo.transform.position = CentrePosition;
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

       AddGizmo();
       // terrain = UnityEngine.Object.Instantiate(
        //     TerrainType.Prefab,
        //     centrePosition, 
        //     orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
        //     Grid.transform
        // );

        //terrain.gameObject.layer = LayerMask.NameToLayer("Grid");

        //TODO: Adjust the size of the prefab to the size of the grid tile
        
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

    public void SetNeighbours(List<HexTile> neighbours)
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

    public void AddUnit(GameObject unit)
    {
        units.Add(unit);
    }

    // Première unité de la case (null s'il n'y en a pas).
    public Unit GetFirstUnit()
    {
        foreach (var go in units)
        {
            if (go != null && go.TryGetComponent(out Unit unit)) return unit;
        }
        return null;
    }
    
    
    
    
    public bool Equals(HexTile other)
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
    //     return Equals((HexTile)obj);
    // }

    
    public int CompareTo(HexTile other)
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
}
