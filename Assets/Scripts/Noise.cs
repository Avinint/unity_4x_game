using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int seed, int octaves,
        float persistence, float lacunarity, Vector2 offset)
    {
        System.Random prng = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        
        // division par scale donc 0 interdit
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        // We keep track of the max and min noise heights so we can normalize the noise map later
        // WHY ?
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Move the center of the map to the center of the game object (where noise starts from)
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        float[,] noiseMap = new float[mapWidth, mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    
                    frequency *= lacunarity;
                }

                if(noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if(noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }
        
        NormalizeMap(noiseMap, maxNoiseHeight, minNoiseHeight);

        return noiseMap;
    }
    
    public static void NormalizeMap(float[,] map, float maxHeight, float minHeight)
    {
        for(int y = 0; y < map.GetLength(1); y++)
        {
            for(int x = 0; x < map.GetLength(0); x++)
            {
                // We normalize the noise map by subtracting the min height from each point and dividing by the difference between the max and min heights
                map[x, y] = Mathf.InverseLerp(minHeight, maxHeight, map[x, y]);
            }
        }
    }
}
