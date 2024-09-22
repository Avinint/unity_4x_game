using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HexMetrics
{
    public static float OuterRadius(float hexSize)
    {
        return hexSize;
    }
    
    public static float InnerRadius(float hexSize)
    {
        return hexSize * 0.86602540378f;
    }

    public static Vector3[] Corners(float hexSize, HexOrientation orientation)
    {
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            corners[i] = Corner(hexSize, orientation, i);
        }

        return corners;
    }

    public static Vector3 Corner(float hexSize, HexOrientation orientation, int index)
    {
        float angle = 60f * -index;
        if (orientation == HexOrientation.PointyTop)
        {
            angle -= 30f;
        }

        Vector3 corner = new Vector3(hexSize * Mathf.Cos(angle * Mathf.Deg2Rad), 0f,
            hexSize * Mathf.Sin(angle * Mathf.Deg2Rad));

        return corner;
    }

    public static Vector3 Center(float hexSize, int x, int z, HexOrientation orientation)
    {
        Vector3 centrePosition;
        if (orientation == HexOrientation.PointyTop)
        {
            centrePosition.x = (x + z * 0.5f - z / 2) * (InnerRadius(hexSize) * 2f);
            centrePosition.y = 0f;
            centrePosition.z = z * (OuterRadius(hexSize) * 1.5f);
        }
        else
        {
            centrePosition.x = (x) * (OuterRadius(hexSize) * 1.5f);
            centrePosition.y = 0f;
            centrePosition.z = (z + x * 0.5f - x / 2) * (InnerRadius(hexSize) * 2f);
        }

        return centrePosition;
    }
    
    public static Vector3Int OffsetToCube(Vector2Int offsetCoord, HexOrientation orientation)
    {
        return OffsetToCube((int)offsetCoord.x, (int)offsetCoord.y, orientation);
    }

    
    public static Vector3Int OffsetToCube(int col, int row, HexOrientation orientation)
    {
        if (orientation == HexOrientation.PointyTop)
        {
            return AxialToCube(OffsetToAxialPointy(col, row));
        }
        else
        {
            return AxialToCube(OffsetToAxialFlat(col, row));
        }
    }

    public static Vector3Int AxialToCube(int q, int r)
    {
        return new Vector3Int(q, r, -q - r);
    }
    
    public static Vector3Int AxialToCube(Vector2Int axialCoord)
    {
        return AxialToCube(axialCoord.x, axialCoord.y);
    }
    
    public static Vector2Int CubeToAxial(Vector3 cube)
    {
        return new Vector2Int((int)cube.x, (int)cube.y);
    }
    
    public static Vector2Int OffsetToAxial(int x, int z, HexOrientation orientation)
    {
        if (orientation == HexOrientation.PointyTop)
        {
            return OffsetToAxialPointy(x, z);
        }
        else
        {
            return OffsetToAxialFlat(x, z);
        }
    }

    public static Vector2Int OffsetToAxialFlat(int col, int row)
    {
        int q = col;
        int r = row - (col + (col & 1)) / 2;

        return new Vector2Int(q, r);
    }


    public static Vector2Int OffsetToAxialPointy(int col, int row)
    {
        int q = col - (row + (row & 1)) / 2;
        int r = row;

        return new Vector2Int(q, r);
    }
    
    public static Vector2Int CubeToOffset(int x, int y, int z, HexOrientation orientation)
    {
        if (orientation == HexOrientation.PointyTop)
        {
            return CubeToOffsetPointy(x, y, z);
        }
        else
        {
            return CubeToOffsetFlat(x, y, z);
        }
    }
    
    public static Vector2Int CubeToOffset(Vector3 offsetCoord, HexOrientation orientation)
    {
        return CubeToOffset((int)offsetCoord.x, (int)offsetCoord.y, (int)offsetCoord.z, orientation);
    }

    private static Vector2Int CubeToOffsetPointy(int x, int y, int z)
    {
        Vector2Int offsetCoordinates = new Vector2Int(x + (y - (y & 1)) / 2, y);
        return offsetCoordinates;
    }
    
    
    private static Vector2Int CubeToOffsetFlat(int x, int y, int z)
    {
        Vector2Int offsetCoordinates = new Vector2Int(x, y + (x - (x & 1)) / 2);
        return offsetCoordinates;
    }
    
    private static Vector3 CubeRound(Vector3 frac)
    {
        Vector3 roundedCoordinates = new Vector3();
        int rx = Mathf.RoundToInt(frac.x);
        int ry = Mathf.RoundToInt(frac.y);
        int rz = Mathf.RoundToInt(frac.z);
        float xDiff = Mathf.Abs(rx - frac.x);
        float yDiff = Mathf.Abs(ry - frac.y);
        float zDiff = Mathf.Abs(rz - frac.z);

        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -ry - rz;
        }
        else if (yDiff > zDiff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        roundedCoordinates.x = rx;
        roundedCoordinates.y = ry;
        roundedCoordinates.z = rz;

        return roundedCoordinates;
    }

    public static Vector2Int AxialRound(Vector2Int coordinates)
    {
        return CubeToAxial(CubeRound(AxialToCube(coordinates.x, coordinates.y)));
    }

    public static Vector2Int CoordinateToAxial(float x, float z, float hexSize, HexOrientation orientation)
    {
        if (orientation == HexOrientation.PointyTop)
            return CoordinateToPointyAxial(x, z, hexSize);
        else
            return CoordinateToFlatAxial(x, z, hexSize);
    }

    private static Vector2Int CoordinateToPointyAxial(float x, float z, float hexSize)
    {
        Vector2Int pointyHexCoordinates = new Vector2Int();
        pointyHexCoordinates.x = (int)((Mathf.Sqrt(3) / 3 * x - 1f / 3 * z) / hexSize);
        pointyHexCoordinates.y = (int)((2f / 3 * z) / hexSize);

        return AxialRound(pointyHexCoordinates);
    }

    private static Vector2Int CoordinateToFlatAxial(float x, float z, float hexSize)
    {
        Vector2Int flatHexCoordinates = new Vector2Int();
        flatHexCoordinates.x =  (int)((2f / 3 * x) / hexSize);
        flatHexCoordinates.y =  (int) ((-1f / 3 * x + Mathf.Sqrt(3) / 3 * z) / hexSize);
        return AxialRound(flatHexCoordinates);
    }

    public static Vector2Int CoordinateToOffset(float x, float z, float hexSize, HexOrientation orientation)
    {
        return CubeToOffset(AxialToCube(CoordinateToAxial(x, z, hexSize, orientation)), orientation);
    }

    public static List<Vector2Int> GetNeighbourCoordinatesList(Vector2Int axialCoordinates)
    {
     
        List<Vector2Int> neighbours = new List<Vector2Int>();
        neighbours.Add(new Vector2Int(axialCoordinates.x + 1, axialCoordinates.y));
        neighbours.Add(new Vector2Int(axialCoordinates.x - 1, axialCoordinates.y));

        if (((int)axialCoordinates.y & 1) == 1)
        {
            neighbours.Add(new Vector2Int(axialCoordinates.x, axialCoordinates.y + 1));
            neighbours.Add(new Vector2Int(axialCoordinates.x + 1, axialCoordinates.y + 1));
        
        
            neighbours.Add(new Vector2Int(axialCoordinates.x + 2, axialCoordinates.y - 1));
            neighbours.Add(new Vector2Int(axialCoordinates.x + 1, axialCoordinates.y - 1));
        }
        else
        {
            neighbours.Add(new Vector2Int(axialCoordinates.x - 2, axialCoordinates.y + 1));
            neighbours.Add(new Vector2Int(axialCoordinates.x - 1, axialCoordinates.y + 1));
            
            
            neighbours.Add(new Vector2Int(axialCoordinates.x, axialCoordinates.y - 1));
            neighbours.Add(new Vector2Int(axialCoordinates.x - 1, axialCoordinates.y - 1));
        }
        

        return neighbours;
    }
    
    public static List<Vector2Int> GetCoordinatesAtDistance(Vector2Int axialCoordinates, int distance = 1)
    {
     
        List<Vector2Int> list = new List<Vector2Int>();
        list.Add(new Vector2Int(axialCoordinates.x + distance, axialCoordinates.y));
        list.Add(new Vector2Int(axialCoordinates.x - distance, axialCoordinates.y));
        
        
        if (axialCoordinates.y % 2 == 1)
        {
            list.Add(new Vector2Int(axialCoordinates.x, axialCoordinates.y + distance));
            list.Add(new Vector2Int(axialCoordinates.x + distance, axialCoordinates.y + distance));
        
        
            list.Add(new Vector2Int(axialCoordinates.x + distance  + 1, axialCoordinates.y - distance));
            list.Add(new Vector2Int(axialCoordinates.x + distance, axialCoordinates.y - distance));
        }
        else
        {
            list.Add(new Vector2Int(axialCoordinates.x - distance - 1, axialCoordinates.y + distance));
            list.Add(new Vector2Int(axialCoordinates.x - distance, axialCoordinates.y + distance));
        
        
            list.Add(new Vector2Int(axialCoordinates.x, axialCoordinates.y - distance));
            list.Add(new Vector2Int(axialCoordinates.x - distance, axialCoordinates.y - distance));
        }

        return list;
    }

    public static Vector3 CubeSubtract(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static float CubeDistance(Vector3 a, Vector3 b)
    {
        var vec = CubeSubtract(a, b);
        return (Mathf.Abs(vec.x) + Mathf.Abs(vec.y) + Mathf.Abs(vec.z)) / 2;
    }

    public static float AxialDistance(Vector2Int a, Vector2Int b)
    {
        var aCube = AxialToCube(a);
        var bCube = AxialToCube(b);
        
        return CubeDistance(aCube, bCube);
    }

    public static float OffsetDistance(Vector2Int a, Vector2Int b,  HexOrientation orientation)
    {
        var ac = OffsetToAxial((int)a.x, (int)a.y, orientation);
        var bc =  OffsetToAxial((int)b.x, (int)b.y, orientation);

        return AxialDistance(ac, bc);
    }


}
