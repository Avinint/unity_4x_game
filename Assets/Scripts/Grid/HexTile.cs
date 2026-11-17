using System;
using System.Collections;
using System.Collections.Generic;
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

    // Position affichée (marqueur de survol, aperçu) : à plat sous le brouillard, pour ne pas trahir le relief.
    public Vector3 DisplayPosition => IsDiscovered ? CentrePosition : CentrePosition - Vector3.up * Elevation;
    

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
        foreach (var go in units) UpdateUnitVisibility(go);
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
        AddPlinth();
        AddColliders();
        ApplyDim();
    }

    // Colliders sur le terrain révélé (dessus et socle) : la souris vise la case surélevée, pas le sol derrière.
    private void AddColliders()
    {
        foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.GetComponent<Collider>() != null) continue;
            mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            mf.gameObject.tag = "Grid";
        }
    }

    public float Elevation { get; private set; }

    // Socle sous une case surélevée : même matériau que le terrain, du sol jusqu'à la case.
    private void AddPlinth()
    {
        if (Elevation <= 0f) return;
        var topRenderer = terrain.GetComponentInChildren<Renderer>();
        var plinth = new GameObject("Plinth");
        plinth.layer = terrain.gameObject.layer;
        plinth.transform.SetParent(terrain, false);
        plinth.transform.localPosition = Vector3.down * Elevation;
        plinth.transform.localScale = new Vector3(1f, Elevation, 1f);
        plinth.AddComponent<MeshFilter>().sharedMesh = Grid.PlinthMesh;
        var r = plinth.AddComponent<MeshRenderer>();
        r.sharedMaterial = topRenderer != null ? topRenderer.sharedMaterial : null;
    }

    // Vue en ce moment par le joueur affiché (sinon assombrie si découverte).
    public bool IsVisible { get; private set; }

    // Joueur dont on affiche le brouillard (le joueur local) ; ses unités restent toujours visibles.
    public static Player Viewer { get; set; }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public void SetVisibleNow(bool visible)
    {
        if (IsVisible == visible) return;
        IsVisible = visible;
        ApplyDim();
        foreach (var go in units) UpdateUnitVisibility(go);
    }

    public void RefreshDim() => ApplyDim();

    // Terrain découvert mais pas vu : couleur mêlée de brume claire (MaterialPropertyBlock, matériaux intacts).
    private void ApplyDim()
    {
        if (terrain == null || !IsDiscovered) return;
        var block = new MaterialPropertyBlock();
        foreach (var r in terrain.GetComponentsInChildren<Renderer>())
        {
            if (IsVisible || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(BaseColorId))
            {
                r.SetPropertyBlock(null);
                continue;
            }
            Color baseColor = r.sharedMaterial.GetColor(BaseColorId);
            Color c = Grid != null ? Color.Lerp(baseColor, Grid.FogColor, Grid.FogAmount) : baseColor;
            c.a = 1f;
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }
    }

    public bool IsHovered { get; private set; }

    public void OnMouseEnter()
    {
        if (IsHovered) return;   // cases inexplorées comprises : on peut y envoyer une unité
        IsHovered = true;
    }

    public void OnMouseExit()
    {
        IsHovered = false;
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
        gizmo = terrain.gameObject.AddComponent<HexTileGizmo>();
        gizmo.Neighbours = Neighbours;
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

        Elevation = Grid.ElevationOf(Relief);
        CentrePosition = HexMetrics.Center(
            HexSize,
            (int)OffsetCoordinates.x,
            (int)OffsetCoordinates.y, orientation
        ) + Grid.transform.position
          + Vector3.up * Elevation;

        terrain = UnityEngine.Object.Instantiate(
            unknownPrefab,
            CentrePosition - Vector3.up * Elevation,   // brouillard : à plat, sans trahir le relief
            orientation == HexOrientation.PointyTop ? Quaternion.Euler(0, 0, 0) : Quaternion.identity, 
            Grid.transform
        );

       AddGizmo();

        //TODO: Adjust the size of the prefab to the size of the grid tile
        
        if(Orientation == HexOrientation.FlatTop)
        {
            terrain.Rotate(new Vector3(0, 30, 0));
        }
    }

    public void SetNeighbours(List<HexTile> neighbours)
    {
        Neighbours = neighbours;
    }
    
    public void ClearTerrain()
    {
        if(terrain != null)
        {
            UnityEngine.Object.Destroy(terrain.gameObject);
        }
    }

    public Relief Relief { get; set; }
    public Biome Biome { get; set; }

    // Coût du biome, +1 en colline.
    public int MovementCost => (TerrainType != null ? TerrainType.MovementCost : 1) + (Relief == Relief.Hill ? 1 : 0);

    // Selon ce que sait le joueur : une case inexplorée est supposée praticable, au coût 1.
    public bool IsWalkableFor(Player owner, bool explored) =>
        !explored || (IsLand() && !HasUnitOfOtherPlayer(owner));

    public int MovementCostFor(bool explored) => explored ? MovementCost : 1;

    public bool IsNotLand()
    {
        return Relief == Relief.Water || Relief == Relief.Mountain;   // plus de dépendance au nom des assets
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
        UpdateUnitVisibility(unit);
    }

    // Visibilité d'une unité selon l'état de la case pour le joueur affiché (Viewer).
    private void UpdateUnitVisibility(GameObject unitObject)
    {
        if (unitObject == null || !unitObject.TryGetComponent(out Unit unit)) return;
        // Ses propres unités toujours ; les autres seulement sur une case vue en ce moment.
        bool own = unit.Owner != null && unit.Owner == Viewer;
        unit.SetVisible(IsDiscovered && (own || IsVisible));
    }

    public void RemoveUnit(GameObject unit)
    {
        units.Remove(unit);
    }

    // Vrai si la case porte une unité d'un autre joueur que `player`.
    public bool HasUnitOfOtherPlayer(Player player)
    {
        foreach (var go in units)
            if (go != null && go.TryGetComponent(out Unit unit) && unit.Owner != null && unit.Owner != player)
                return true;
        return false;
    }

    // Empilement : au plus une unité civile et une militaire par joueur sur une case.
    // Faux si une autre unité du même propriétaire et de la même famille s'y trouve déjà.
    // Les unités des autres joueurs sont exclues ailleurs (IsWalkableFor).
    public bool CanStop(Unit unit)
    {
        foreach (var go in units)
            if (go != null && go.TryGetComponent(out Unit other) && other != unit
                && other.Owner == unit.Owner && IsMilitary(other) == IsMilitary(unit))
                return false;
        return true;
    }

    private static bool IsMilitary(Unit unit) => unit.type != null && unit.type.IsMilitary;

    // Première unité de la case, en priorité celle de `owner` (null s'il n'y en a pas).
    public Unit GetFirstUnit(Player owner = null)
    {
        Unit first = null;
        foreach (var go in units)
        {
            if (go == null || !go.TryGetComponent(out Unit unit)) continue;
            if (owner != null && unit.Owner == owner) return unit;
            if (first == null) first = unit;
        }
        return first;
    }
    
    
    
    
    public bool Equals(HexTile other)
    {

        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return OffsetCoordinates.Equals(other.OffsetCoordinates);
    }

    
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
