using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class HydrologyTests
{
    // Carte d'une seule rangée : voisins = gauche et droite.
    private static IEnumerable<Vector2Int> Line(int x, int y)
    {
        yield return new Vector2Int(x - 1, y);
        yield return new Vector2Int(x + 1, y);
    }

    private static float[,] Row(params float[] v)
    {
        var a = new float[v.Length, 1];
        for (int i = 0; i < v.Length; i++) a[i, 0] = v[i];
        return a;
    }

    private static Relief[,] Reliefs(params Relief[] v)
    {
        var a = new Relief[v.Length, 1];
        for (int i = 0; i < v.Length; i++) a[i, 0] = v[i];
        return a;
    }

    private const Relief W = Relief.Water, F = Relief.Flat, M = Relief.Mountain;

    [Test]
    public void Slope_FlowsToSea_AndAccumulates()
    {
        var (down, flow) = Hydrology.Flow(Row(0f, 0.5f, 0.6f, 0.7f), Reliefs(W, F, F, F), Row(0f, 1f, 1f, 1f), Line);

        Assert.AreEqual(new Vector2Int(0, 0), down[1, 0]);
        Assert.AreEqual(new Vector2Int(1, 0), down[2, 0]);
        Assert.AreEqual(3f, flow[1, 0], 1e-4f);   // toute la pluie arrive au bord de la mer
        Assert.AreEqual(1f, flow[3, 0], 1e-4f);   // source
    }

    [Test]
    public void Pit_DrainsThroughLowestPass()
    {
        // La cuvette (0.3) est plus basse que le col (0.6) : elle se déverse quand même vers la mer.
        var (down, flow) = Hydrology.Flow(Row(0f, 0.6f, 0.3f, 0.8f), Reliefs(W, F, F, F), Row(0f, 1f, 1f, 1f), Line);

        Assert.AreEqual(new Vector2Int(1, 0), down[2, 0]);
        Assert.AreEqual(3f, flow[1, 0], 1e-4f);
    }

    [Test]
    public void NoWater_NoDownstream()
    {
        var (down, flow) = Hydrology.Flow(Row(0.5f, 0.6f), Reliefs(F, F), Row(1f, 1f), Line);

        Assert.AreEqual(Hydrology.NoDownstream, down[0, 0]);
        Assert.AreEqual(1f, flow[0, 0], 1e-4f);
    }

    [Test]
    public void Navigable_StrongestFlows_NeverMountainNorWater()
    {
        var s = new MapRulesSettings { NavigableFraction = 0.5f };
        var navigable = Hydrology.Navigable(Row(0f, 4f, 3f, 2f, 1f), Reliefs(W, F, M, F, F), s);

        Assert.IsFalse(navigable[0, 0]);   // eau
        Assert.IsTrue(navigable[1, 0]);    // plus fort débit
        Assert.IsFalse(navigable[2, 0]);   // fort, mais montagne
        Assert.IsFalse(navigable[4, 0]);   // faible
    }

    [Test]
    public void Navigable_DownstreamIsNavigableOrSea()
    {
        var reliefs = Reliefs(W, F, F, F, F, F);
        var (down, flow) = Hydrology.Flow(Row(0f, .1f, .2f, .3f, .4f, .5f), reliefs, Row(0f, 1f, 1f, 1f, 1f, 1f), Line);
        var navigable = Hydrology.Navigable(flow, reliefs, new MapRulesSettings { NavigableFraction = 0.4f });

        int count = 0;
        for (int x = 1; x < 6; x++)
        {
            if (!navigable[x, 0]) continue;
            count++;
            var d = down[x, 0];
            Assert.IsTrue(reliefs[d.x, 0] == Relief.Water || navigable[d.x, 0]);
        }
        Assert.Greater(count, 0);
    }

    private static bool[,] Bools(params bool[] v)
    {
        var a = new bool[v.Length, 1];
        for (int i = 0; i < v.Length; i++) a[i, 0] = v[i];
        return a;
    }

    // Aval de chaque case de la rangée, par numéro ; -1 = aucun.
    private static Vector2Int[,] Downs(params int[] d)
    {
        var a = new Vector2Int[d.Length, 1];
        for (int i = 0; i < d.Length; i++) a[i, 0] = d[i] < 0 ? Hydrology.NoDownstream : new Vector2Int(d[i], 0);
        return a;
    }

    [Test]
    public void KeepLongRivers_ShortRiver_BackToLand()
    {
        // 2 cases navigables (1, 2) avant la mer (0) : trop court pour 3.
        var kept = Hydrology.KeepLongRivers(Bools(false, true, true, false), Downs(-1, 0, 1, 2),
            Bools(false, false, false, false), 3);

        Assert.IsFalse(kept[1, 0]);
        Assert.IsFalse(kept[2, 0]);
    }

    [Test]
    public void KeepLongRivers_LongEnough_Kept()
    {
        var kept = Hydrology.KeepLongRivers(Bools(false, true, true, true), Downs(-1, 0, 1, 2),
            Bools(false, false, false, false), 3);

        Assert.IsTrue(kept[1, 0] && kept[2, 0] && kept[3, 0]);
    }

    [Test]
    public void KeepLongRivers_ShortTributaryOfLongRiver_Kept()
    {
        // Fleuve 3 → 2 → 1 → mer ; affluent d'une case 4 → 1.
        var kept = Hydrology.KeepLongRivers(Bools(false, true, true, true, true), Downs(-1, 0, 1, 2, 1),
            Bools(false, false, false, false, false), 3);

        Assert.IsTrue(kept[4, 0]);
    }

    [Test]
    public void KeepLongRivers_EndsInLake_BackToLand()
    {
        // Fleuve de 3 cases (1, 2, 3) assez long, mais il se jette dans un lac (0).
        var kept = Hydrology.KeepLongRivers(Bools(false, true, true, true), Downs(-1, 0, 1, 2),
            Bools(true, false, false, false), 3);

        Assert.IsFalse(kept[1, 0] || kept[2, 0] || kept[3, 0]);
    }
}
