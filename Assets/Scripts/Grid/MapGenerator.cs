using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public HexGrid HexGrid;

    // Taille de la carte : toujours celle de HexGrid (lue au lancement de la génération).
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool WrapWorld { get; private set; }   // lu sur HexGrid.WrapWorld au lancement

    [Tooltip("The scale of the noise map")] public float NoiseScale = .5f;

    [Tooltip("The number of layers of noise to generate")]
    public int Octaves = 6;

    [Range(0, 1)]
    [Tooltip("The change of amplitude between octaves")]
    public float Persistence = .5f;

    [Tooltip("The change of frequency between octaves")]
    public float Lacunarity = 2f;

    [Tooltip("The seed used to generate the noise map")]
    public int Seed = 0;

    [Tooltip("Nouvelle graine au hasard à chaque partie (décocher pour rejouer la graine ci-dessus)")]
    public bool RandomSeed = true;

    [Tooltip("The offset of the noise map")]
    public Vector2 Offset = Vector2.zero;

    [Tooltip("Whether or not to update the noise map when a value is changed")]
    public bool AutoUpdate = true;


    public bool GenerateMapOnStart = true;

    public bool UseThreadedGeneration = true;

    [Header("Règles de génération")]
    public MapRulesSettings Rules = new MapRulesSettings();
    public List<BiomeTerrain> BiomeTerrains = new List<BiomeTerrain>();   // Biome → TerrainType affiché
    public TerrainType MountainTerrain;

    public float [,] ReliefMap { get; private set; }

    public MapCell[,] Cells { get; private set; }

    public HashSet<RiverEdge> RiverEdges { get; private set; }   // rivières sur les arêtes (axial), tracées par RiverView

    public Color[] ColorMap { get; private set; }

    public event Action<float[,]> OnNoiseMapGenerated;
    public event Action<MapCell[,]> OnTerrainMapGenerated;
    public event Action onTerrainMapCleared;
    public event Action<Color[], int, int> OnColorMapGenerated;

    private void Awake()
    {
        HexGrid = GetComponent<HexGrid>();
    }

    private void Start()
    {
        if (GenerateMapOnStart)
        {
            GenerateMap();
        }
    }

    public void GenerateMap()
    {
        // Awake n'est pas appelé quand l'Inspector régénère hors du mode Play.
        if (HexGrid == null) HexGrid = GetComponent<HexGrid>();
        if (HexGrid == null)
        {
            Debug.LogError("MapGenerator : aucun HexGrid sur l'objet, taille de carte inconnue.");
            return;
        }
        Width = HexGrid.Width;
        Height = HexGrid.Height;
        WrapWorld = HexGrid.WrapWorld;

        // En jeu : nouvelle carte à chaque partie ; la graine tirée est affichée pour pouvoir la rejouer.
        if (RandomSeed && Application.isPlaying)
        {
            Seed = UnityEngine.Random.Range(0, 1000000);
            Debug.Log($"MapGenerator : graine {Seed}");
        }

        ValidateSettings();
        StartCoroutine(GenerateMapCoroutine());
    }

    public void ClearMap()
    {
        onTerrainMapCleared?.Invoke();
    }

    private void ValidateSettings()
    {
        // We make sure the octaves is not less than 0
        Octaves = Mathf.Max(Octaves, 0);
        // We make sure the lacunarity is not less than 1
        Lacunarity = Mathf.Max(Lacunarity, 1);
        // We make sure the persistance is between 0 and 1
        Persistence = Mathf.Clamp01(Persistence);
        // We make sure the scale is not 0 because we will be dividing by it
        NoiseScale = Mathf.Max(NoiseScale, 0.0001f);
    }

    private IEnumerator GenerateMapCoroutine()
    {
        // Clear the current maps
        ReliefMap = null;
        Cells = null;
        ColorMap = null;

        // Lue sur le thread principal : le calcul des voisins en a besoin.
        HexOrientation orientation = HexGrid != null ? HexGrid.Orientation : HexOrientation.PointyTop;

        // If we are in play mode, we generate the noise map on a separate thread
        if(Application.isPlaying && UseThreadedGeneration)
        {
            Task task =  Task.Run(() =>
            {
                Cells = BuildCells(orientation);
                ColorMap = GenerateColorsFromCells(Cells);

            }).ContinueWith(task =>
            {
                // Handle exceptions if any
                if (task.Exception != null)
                {
                    Debug.LogError(task.Exception);
                }
            });

            while (!task.IsCompleted)
            {
                yield return null;
            }
        }
        // If we are not in play mode, we generate the noise map on the main thread
        // In testing I found that threading is much slower in the editor than in a build or play mode
        else
        {
            Cells = BuildCells(orientation);
            ColorMap = GenerateColorsFromCells(Cells);
        }
        //We invoke separate events for each map generated so that the parts of code that interested only in one map can subscribe to that event
        OnNoiseMapGenerated?.Invoke(ReliefMap);
        OnColorMapGenerated?.Invoke(ColorMap, Width, Height);
        OnTerrainMapGenerated?.Invoke(Cells);


        yield return null;
    }

    // Altitude → relief ; latitude + bruit + altitude → température ; bruit → humidité ; puis biome.
    private MapCell[,] BuildCells(HexOrientation orientation)
    {
        float[,] altitude = Noise.GenerateNoiseMap(Width, Height, NoiseScale, Seed, Octaves, Persistence, Lacunarity, Offset, WrapWorld);
        float[,] humidity = Noise.GenerateNoiseMap(Width, Height, NoiseScale, Seed + 1, Octaves, Persistence, Lacunarity, Offset, WrapWorld);
        float[,] heat = Noise.GenerateNoiseMap(Width, Height, NoiseScale, Seed + 2, Octaves, Persistence, Lacunarity, Offset, WrapWorld);
        float[,] ridges = Noise.GenerateNoiseMap(Width, Height, NoiseScale, Seed + 3, Octaves, Persistence, Lacunarity, Offset, WrapWorld);

        float sea = MapRules.Percentile(altitude, Rules.WaterFraction);
        altitude = MapRules.AddRidges(altitude, ridges, sea, Rules.RidgeWeight);
        ReliefMap = altitude;
        var land = new List<float>();
        foreach (float a in altitude) if (a >= sea) land.Add(a);
        float hill = MapRules.Percentile(land, 1f - Rules.HillFraction - Rules.MountainFraction);
        float mountain = MapRules.Percentile(land, 1f - Rules.MountainFraction);
        float lowland = MapRules.Percentile(land, Rules.LowlandFraction);
        float top = land.Count > 0 ? land.Max() : 1f;

        var relief = new Relief[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                relief[x, y] = MapRules.ReliefAt(altitude[x, y], sea, hill, mountain);
        bool[,] coast = MapRules.CoastMask(relief, (x, y) => Neighbours(x, y, orientation));
        bool[,] lake = MapRules.LakeMask(relief, (x, y) => Neighbours(x, y, orientation), Rules.LakeMaxSize);

        // Écoulement de case en case : case aval, débit (pluie = humidité), puis fleuves navigables.
        var (downstream, flow) = Hydrology.Flow(altitude, relief, humidity, (x, y) => Neighbours(x, y, orientation));
        bool[,] navigable = Hydrology.Navigable(flow, relief, Rules);
        navigable = Hydrology.SingleWidth(navigable, flow, downstream, (x, y) => Neighbours(x, y, orientation));
        navigable = Hydrology.PruneShortTributaries(navigable, flow, downstream, Rules.TributaryMinLength);
        navigable = Hydrology.KeepLongRivers(navigable, downstream, lake, Rules.NavigableMinLength);

        // Températures des terres, puis seuils de climat en proportions.
        var temperature = new float[Width, Height];
        var landTemps = new List<float>();
        var landHums = new List<float>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (relief[x, y] == Relief.Water) continue;
                float latitude = Height > 1 ? (float)y / (Height - 1) : 0.5f;   // 0 = sud, 1 = nord
                float aboveSea = Mathf.InverseLerp(sea, top, altitude[x, y]);
                temperature[x, y] = MapRules.Temperature(latitude, heat[x, y], aboveSea, Rules);
                landTemps.Add(temperature[x, y]);
                landHums.Add(humidity[x, y]);
            }
        MapRulesSettings climate = MapRules.WithClimateThresholds(Rules, landTemps, landHums);

        var cells = new MapCell[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                // Fleuve navigable : la case devient de l'eau douce (hauteur 0, infranchissable à pied).
                bool isNavigable = navigable[x, y];
                Relief r = isNavigable ? Relief.Water : relief[x, y];
                Biome biome;
                if (isNavigable) biome = Biome.River;
                else if (lake[x, y]) biome = Biome.Lake;
                else if (r == Relief.Water) biome = MapRules.WaterBiome(coast[x, y]);
                else
                    biome = MapRules.LandBiome(temperature[x, y], humidity[x, y], relief[x, y], altitude[x, y] < lowland, climate);
                TerrainType terrain = r == Relief.Mountain ? MountainTerrain : TerrainFor(biome);
                cells[x, y] = new MapCell { Relief = r, Biome = biome, Terrain = terrain,
                                            Flow = flow[x, y], Downstream = downstream[x, y], Navigable = isNavigable };
            }

        // Rivières d'arêtes, sur la carte finale (fleuves navigables = eau, les rivières s'y jettent).
        Vector2Int Off(Vector2Int a) => HexMetrics.CubeToOffset(HexMetrics.AxialToCube(a), orientation);
        bool Exists(Vector2Int a) { var o = Off(a); return o.x >= 0 && o.x < Width && o.y >= 0 && o.y < Height; }
        var axialCells = new List<Vector2Int>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                axialCells.Add(HexMetrics.OffsetToAxial(x, y, orientation));
        RiverEdges = RiverNetwork.Rivers(axialCells, Exists,
            a => { var o = Off(a); return cells[o.x, o.y].Relief == Relief.Water; },
            a => { var o = Off(a); return altitude[o.x, o.y]; },
            a => { var o = Off(a); return humidity[o.x, o.y]; },
            Rules.RiverFraction, Rules.RiverMinLength);

        int navigableCount = 0;
        foreach (bool b in navigable) if (b) navigableCount++;
        Debug.Log($"MapGenerator : {RiverEdges.Count} arêtes de rivière, {navigableCount} cases de fleuve navigable");
        return cells;
    }

    // Voisins hexagonaux d'une case en coordonnées offset ; monde qui fait le tour : la colonne revient dans [0, Width).
    private IEnumerable<Vector2Int> Neighbours(int x, int y, HexOrientation orientation)
    {
        foreach (var n in HexMetrics.GetNeighbourCoordinatesList(HexMetrics.OffsetToAxial(x, y, orientation)))
        {
            var o = HexMetrics.CubeToOffset(HexMetrics.AxialToCube(n), orientation);
            yield return WrapWorld ? new Vector2Int((o.x % Width + Width) % Width, o.y) : o;
        }
    }

    private TerrainType TerrainFor(Biome biome)
    {
        foreach (var bt in BiomeTerrains) if (bt.Biome == biome) return bt.Terrain;
        Debug.LogWarning($"MapGenerator : aucun TerrainType pour le biome {biome}.");
        return null;
    }

    // Generates a color map from the cells by getting the color of each terrain type
    private Color[] GenerateColorsFromCells(MapCell[,] cells)
    {
        Color[] colorMap = new Color[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                TerrainType terrain = cells[x, y].Terrain;
                colorMap[y * Width + x] = terrain != null ? terrain.Colour : Color.magenta;
            }
        }
        return colorMap;
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

}

[Serializable]
public struct BiomeTerrain
{
    public Biome Biome;
    public TerrainType Terrain;
}

// Ce que la génération produit pour chaque case.
public struct MapCell
{
    public Relief Relief;
    public Biome Biome;
    public TerrainType Terrain;
    public float Flow;              // débit cumulé (pluie = humidité)
    public Vector2Int Downstream;   // case aval (offset), Hydrology.NoDownstream si aucune
    public bool Navigable;          // fleuve navigable (case d'eau douce) ; les rivières sont sur les arêtes
}
