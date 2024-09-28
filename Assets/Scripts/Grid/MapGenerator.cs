using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public HexGrid HexGrid;

    public int Width = 256;
    public int Height = 256;

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

    [Tooltip("The offset of the noise map")]
    public Vector2 Offset = Vector2.zero;

    [Tooltip("Whether or not to update the noise map when a value is changed")]
    public bool AutoUpdate = true;
   
    [Tooltip("Whether or not to use the hex grid size information to generate the noise map")]
    public bool UseHexGrid = true;


    public bool GenerateMapOnStart = true;
    
    public bool UseThreadedGeneration = true;

    public List<TerrainHeight> Biomes = new List<TerrainHeight>();
    
    public float [,] ReliefMap { get; private set; }
    
    public TerrainType[,] TerrainMap { get; private set; }
    
    public Color[] ColorMap { get; private set; }
    
    public event Action<float[,]> OnNoiseMapGenerated;
    public event Action<TerrainType[,]> OnTerrainMapGenerated;
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
        if (UseHexGrid && (HexGrid != null))
        {
            Width = HexGrid.Width;
            Height = HexGrid.Height;
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
        // Make sure the width and height are not less than 1

        Width = Mathf.Max(Width, 1);
        Height = Mathf.Max(Height, 1);
    }
    
    private IEnumerator GenerateMapCoroutine()
    {
        // Clear the current maps
        ReliefMap = null;
        TerrainMap = null;
        ColorMap = null;

        // If we are in play mode, we generate the noise map on a separate thread
        if(Application.isPlaying && UseThreadedGeneration)
        {
            Task task =  Task.Run(() =>
            {
                ReliefMap = Noise.GenerateNoiseMap(Width, Height, NoiseScale, Seed, Octaves, Persistence, Lacunarity, Offset);
                TerrainMap = AssignTerrainTypes(ReliefMap);
                ColorMap = GenerateColorsFromTerrain(TerrainMap);

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
            ReliefMap = Noise.GenerateNoiseMap(Width, Height, NoiseScale, Seed, Octaves, Persistence, Lacunarity, Offset);
            TerrainMap = AssignTerrainTypes(ReliefMap);
            ColorMap = GenerateColorsFromTerrain(TerrainMap);
        }
        //We invoke separate events for each map generated so that the parts of code that interested only in one map can subscribe to that event
        OnNoiseMapGenerated?.Invoke(ReliefMap);
        OnColorMapGenerated?.Invoke(ColorMap, Width, Height);
        OnTerrainMapGenerated?.Invoke(TerrainMap);


        yield return null;
    }
    
    // Assigns a terrain type to each point on the noise map based on the height of the point as compared to the height of the biomes
    private TerrainType[,] AssignTerrainTypes(float[,] reliefMap)
    {
        TerrainType[,] terrainMap = new TerrainType[Width, Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float currentHeight = reliefMap[x, y];
                for (int i = 0; i < Biomes.Count; i++)
                {
                    if (currentHeight <= Biomes[i].Height)
                    {
                        terrainMap[x,y] = Biomes[i].TerrainType;
                        break;
                    }
                }
            }
        }

        return terrainMap;
    }
    
    // Generates a color map from the terrain map by getting the color of each terrain type
    private Color[] GenerateColorsFromTerrain(TerrainType[,] terrainMap)
    {
        Color[] colorMap = new Color[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                colorMap[y * Width + x] = terrainMap[x,y].Colour;
            }
        }
        return colorMap;
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

}



[System.Serializable]
public struct TerrainHeight
{
    public float Height;
    public TerrainType TerrainType;
}
