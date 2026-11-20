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

    // Les deux coins de la case de centre `a` partagés avec sa voisine de centre `b` (à la hauteur de a).
    // Ce sont les deux coins qui avancent le plus vers b.
    public static (Vector3, Vector3) SharedEdge(Vector3 a, Vector3 b, float hexSize, HexOrientation orientation)
    {
        Vector3[] corners = Corners(hexSize, orientation);
        Vector3 dir = b - a;
        dir.y = 0f;
        System.Array.Sort(corners, (u, v) => Vector3.Dot(v, dir).CompareTo(Vector3.Dot(u, dir)));
        return (a + corners[0], a + corners[1]);
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

    // Coordonnées axiales à une distance ≤ radius de center (center compris) : 1 + 3·r·(r+1) cases.
    public static List<Vector2Int> AxialRange(Vector2Int center, int radius)
    {
        var result = new List<Vector2Int>();
        for (int q = -radius; q <= radius; q++)
        {
            int rMin = Mathf.Max(-radius, -q - radius);
            int rMax = Mathf.Min(radius, -q + radius);
            for (int r = rMin; r <= rMax; r++)
                result.Add(center + new Vector2Int(q, r));
        }
        return result;
    }

    // Anneaux plats concentriques (centre à l'origine), juste au-dessus du sol, faces vers le haut.
    // Chaque anneau : rayon extérieur et largeur. Sert au marqueur de sélection.
    public static Mesh BuildRingMesh(int segments, params (float outer, float width)[] rings)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        foreach (var (outer, width) in rings)
        {
            float inner = outer - width;
            int start = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices.Add(dir * outer + Vector3.up * 0.05f);   // un peu au-dessus du sol : pas de scintillement
                vertices.Add(dir * inner + Vector3.up * 0.05f);
            }
            for (int i = 0; i < segments; i++)
            {
                int o = start + 2 * i, n = start + 2 * ((i + 1) % segments);
                triangles.AddRange(new[] { o, o + 1, n, o + 1, n + 1, n });   // sens horaire vu d'en haut
            }
        }
        var mesh = new Mesh { name = "SelectionRings" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Rectangle (x, z) couvrant les centres de toutes les cases d'une grille width × height (coordonnées locales).
    // Prisme hexagonal sans couvercle, de y = 0 à y = 1 (mis à l'échelle par case). Pointe en haut, comme les modèles.
    public static Mesh BuildPlinthMesh(float hexSize)
    {
        Vector3[] c = Corners(hexSize, HexOrientation.PointyTop);
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();
        for (int i = 0; i < 6; i++)
        {
            Vector3 a = c[i], b = c[(i + 1) % 6];
            Vector3 n = ((a + b) * 0.5f).normalized;           // vers l'extérieur
            int v = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(b + Vector3.up); vertices.Add(a + Vector3.up);
            for (int k = 0; k < 4; k++) normals.Add(n);
            triangles.AddRange(new[] { v, v + 2, v + 1, v, v + 3, v + 2 });
        }
        var mesh = new Mesh { name = "HexPlinth" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Rect GridBounds(int width, int height, float hexSize, HexOrientation orientation)
    {
        if (width <= 0 || height <= 0) return Rect.zero;
        Vector3 first = Center(hexSize, 0, 0, orientation);
        float minX = first.x, maxX = first.x, minZ = first.z, maxZ = first.z;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 c = Center(hexSize, x, z, orientation);
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minZ = Mathf.Min(minZ, c.z); maxZ = Mathf.Max(maxZ, c.z);
            }
        }
        return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
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
        int r = row - (col - (col & 1)) / 2;

        return new Vector2Int(q, r);
    }


    public static Vector2Int OffsetToAxialPointy(int col, int row)
    {
        int q = col - (row - (row & 1)) / 2;
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

    private static Vector2Int CubeToOffsetPointy(int q, int r, int s)
    {
        Vector2Int offsetCoordinates = new Vector2Int(q + (r - (r & 1)) / 2, r);
        return offsetCoordinates;
    }
    
    
    private static Vector2Int CubeToOffsetFlat(int q, int r, int s)
    {
        Vector2Int offsetCoordinates = new Vector2Int(q, r + (q - (q & 1)) / 2);
        return offsetCoordinates;
    }
    
    private static Vector3Int CubeRound(Vector3 frac)
    {
        Vector3Int roundedCoordinates = new Vector3Int();
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

    // Cases traversées par la ligne droite de a à b, a et b compris (une case par pas).
    // nudge : léger décalage pour trancher quand la ligne longe une arête (signe + ou − : un côté ou l'autre).
    public static List<Vector2Int> AxialLine(Vector2Int a, Vector2Int b, float nudge = 1e-6f)
    {
        int n = AxialDistance(a, b);
        var offset = new Vector3(1f, 2f, -3f) * nudge;
        Vector3 ca = (Vector3)AxialToCube(a) + offset;
        Vector3 cb = (Vector3)AxialToCube(b) + offset;
        var line = new List<Vector2Int>(n + 1);
        for (int i = 0; i <= n; i++)
            line.Add(CubeToAxial(CubeRound(n == 0 ? ca : Vector3.Lerp(ca, cb, (float)i / n))));
        return line;
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
        float q = (Mathf.Sqrt(3) / 3 * x - 1f / 3 * z) / hexSize;
        float r = (2f / 3 * z) / hexSize;

        return AxialRound(q, r);
    }

    private static Vector2Int CoordinateToFlatAxial(float x, float z, float hexSize)
    {
        float q = (2f / 3 * x) / hexSize;
        float r = (-1f / 3 * x + Mathf.Sqrt(3) / 3 * z) / hexSize;

        return AxialRound(q, r);
    }

    // Arrondit une coordonnée axiale fractionnaire vers l'hexagone le plus proche.
    private static Vector2Int AxialRound(float q, float r)
    {
        return CubeToAxial(CubeRound(new Vector3(q, r, -q - r)));
    }

    public static Vector2Int CoordinateToOffset(float x, float z, float hexSize, HexOrientation orientation)
    {
        return CubeToOffset(AxialToCube(CoordinateToAxial(x, z, hexSize, orientation)), orientation);
    }

    public static List<Vector2Int> GetNeighbourCoordinatesList(Vector2Int axialCoordinates)
    {
     
        List<Vector2Int> neighbours = new List<Vector2Int>();
        
        
        Vector2Int[] neighbourCoords = new Vector2Int[]
        {
            new Vector2Int(1, -1),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
        };

        foreach (var coord in neighbourCoords)
        {
            neighbours.Add(axialCoordinates + coord);
        }

        return neighbours;
    }
    
    public static List<Vector3Int> GetNeighbourCoordinatesList(Vector3Int cubeCoordinates)
    {
        List<Vector3Int> neighbours = new List<Vector3Int>();

        Vector3Int[] neighbourCoords = new Vector3Int[]
        {
            new Vector3Int(1, -1, 0),
            new Vector3Int(1, 0,-1),
            new Vector3Int(0, 1,-1),
            new Vector3Int(-1, 1,0),
            new Vector3Int(-1, 0,1),
            new Vector3Int(0, -1,1),
        };

        foreach (var coord in neighbourCoords)
        {
            Vector3Int tileCoordinate = cubeCoordinates + coord;
            neighbours.Add(tileCoordinate);
        }

        return neighbours;
    }

    public static Vector3 CubeSubtract(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static int  CubeDistance(Vector3 a, Vector3 b)
    {
        var vec = CubeSubtract(a, b);
        return (int)(Mathf.Abs(vec.x) + Mathf.Abs(vec.y) + Mathf.Abs(vec.z)) / 2;
    }

    public static int AxialDistance(Vector2Int a, Vector2Int b)
    {
        var aCube = AxialToCube(a);
        var bCube = AxialToCube(b);
        
        return CubeDistance(aCube, bCube);
    }

    // Monde qui fait le tour (cases à sommet pointu, lignes décalées) : ramène une case axiale dans les colonnes
    // [0, width). Décaler q d'une largeur décale la colonne d'une largeur, la ligne ne change pas.
    public static Vector2Int WrapAxial(Vector2Int a, int width)
    {
        int col = a.x + (a.y - (a.y & 1)) / 2;
        int wrapped = ((col % width) + width) % width;
        return new Vector2Int(a.x + wrapped - col, a.y);
    }

    // Monde qui fait le tour : décalage (multiple de worldWidth) qui place une colonne d'abscisse x
    // à la copie la plus proche de `nearX` (la caméra).
    public static float WrapShift(float x, float nearX, float worldWidth) =>
        Mathf.Round((nearX - x) / worldWidth) * worldWidth;

    // Distance dans un monde qui fait le tour : la plus courte vers b ou ses copies à une largeur à l'est ou à l'ouest.
    public static int WrappedDistance(Vector2Int a, Vector2Int b, int width)
    {
        var shift = new Vector2Int(width, 0);
        return Mathf.Min(AxialDistance(a, b), Mathf.Min(AxialDistance(a, b + shift), AxialDistance(a, b - shift)));
    }

    public static float OffsetDistance(Vector2Int a, Vector2Int b,  HexOrientation orientation)
    {
        var ac = OffsetToAxial((int)a.x, (int)a.y, orientation);
        var bc =  OffsetToAxial((int)b.x, (int)b.y, orientation);

        return AxialDistance(ac, bc);
    }


}
