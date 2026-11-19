using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int seed, int octaves,
        float persistence, float lacunarity, Vector2 offset, bool wrapX = false)
    {
        System.Random prng = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Décalages propres au bouclage, tirés après les autres : les cartes à bords fixes ne changent pas.
        // Plus petits (±1000) : en 3D, de trop grandes coordonnées perdent en précision.
        float3[] wrapOffsets = new float3[octaves];
        if (wrapX)
            for (int i = 0; i < octaves; i++)
                wrapOffsets[i] = new float3(prng.Next(-1000, 1000), prng.Next(-1000, 1000), prng.Next(-1000, 1000));

        // division par scale donc 0 interdit
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        // Bouclage est-ouest : chaque colonne devient un angle sur un cylindre de circonférence mapWidth / scale
        // (une case = la même distance dans le bruit qu'en vertical) ; un bruit 3D lu sur ce cercle revient
        // au même point après un tour, donc le bord ouest se raccorde au bord est.
        float radius = mapWidth / (2f * Mathf.PI) / scale;

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
                    float perlinValue;
                    if (wrapX)
                    {
                        float angle = 2f * Mathf.PI * x / mapWidth;
                        var p = new float3(math.cos(angle) * radius, math.sin(angle) * radius, (y - halfHeight) / scale);
                        perlinValue = noise.cnoise(p * frequency + wrapOffsets[i]);   // déjà entre -1 et 1
                    }
                    else
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                        float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;
                        perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    }

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
