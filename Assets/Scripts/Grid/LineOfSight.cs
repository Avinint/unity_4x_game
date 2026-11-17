using System;
using System.Collections.Generic;
using UnityEngine;

// Ligne de vue sur la grille hexagonale. Ne connaît pas la carte : hauteurs fournies par l'appelant.
public static class LineOfSight
{
    private const float Nudge = 1e-6f;

    // Renvoie les cases de `range` que voit un observateur placé en `origin`, avec un œil à `eyeHeight`.
    // Une case est vue si la ligne droite qui y mène ne traverse aucune case à la fois plus haute que l'œil
    // et au moins aussi haute que le sommet de la case visée (`surfaceHeight`) : on voit ce qui dépasse
    // (une colline derrière une forêt, une montagne derrière une colline), pas ce qui est de même hauteur.
    // On ne compte ni l'observateur ni la case visée : on voit la forêt, pas ce qu'il y a derrière.
    // Si la ligne longe une arête entre deux cases, il suffit que l'un des deux côtés soit dégagé.
    public static List<Vector2Int> Visible(Vector2Int origin, IEnumerable<Vector2Int> range,
                                           Func<Vector2Int, int> obstacleHeight, Func<Vector2Int, int> surfaceHeight,
                                           int eyeHeight)
    {
        var visible = new List<Vector2Int>();
        foreach (var cell in range)
        {
            int target = surfaceHeight(cell);
            if (IsClear(HexMetrics.AxialLine(origin, cell, Nudge), obstacleHeight, eyeHeight, target)
                || IsClear(HexMetrics.AxialLine(origin, cell, -Nudge), obstacleHeight, eyeHeight, target))
                visible.Add(cell);
        }
        return visible;
    }

    private static bool IsClear(List<Vector2Int> line, Func<Vector2Int, int> obstacleHeight, int eyeHeight, int targetHeight)
    {
        for (int i = 1; i < line.Count - 1; i++)   // ni l'observateur ni la case visée
        {
            int h = obstacleHeight(line[i]);
            if (h > eyeHeight && h >= targetHeight) return false;
        }
        return true;
    }
}
