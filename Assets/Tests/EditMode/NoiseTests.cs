using NUnit.Framework;
using UnityEngine;

public class NoiseTests
{
    // Écart moyen entre la colonne `a` et la colonne `b`, sur toutes les lignes.
    private static float MeanGap(float[,] map, int a, int b)
    {
        float sum = 0f;
        for (int y = 0; y < map.GetLength(1); y++) sum += Mathf.Abs(map[a, y] - map[b, y]);
        return sum / map.GetLength(1);
    }

    [Test]
    public void Wrap_EdgesJoinLikeNeighbours()
    {
        var map = Noise.GenerateNoiseMap(40, 25, 10f, 1234, 4, 0.5f, 2f, Vector2.zero, wrapX: true);
        float inner = 0f;
        for (int x = 0; x < 39; x++) inner += MeanGap(map, x, x + 1);
        inner /= 39;

        // Le bord est (39) et le bord ouest (0) sont voisins comme deux colonnes intérieures.
        Assert.Less(MeanGap(map, 39, 0), 2f * inner);
    }
}
