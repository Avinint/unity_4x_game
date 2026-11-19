using System;
using System.Collections.Generic;
using System.Linq;
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

    // Trois lignes : `middle` au centre, de l'eau (0) au-dessus et au-dessous, pour que la ligne testée
    // ne soit pas sur le bord haut ou bas de la carte (jamais de fleuve navigable au bord).
    private static float[,] Framed(params float[] middle)
    {
        var a = new float[middle.Length, 3];
        for (int i = 0; i < middle.Length; i++) a[i, 1] = middle[i];
        return a;
    }

    private static Relief[,] FramedReliefs(params Relief[] middle)
    {
        var a = new Relief[middle.Length, 3];
        for (int i = 0; i < middle.Length; i++) { a[i, 0] = a[i, 2] = Relief.Water; a[i, 1] = middle[i]; }
        return a;
    }

    [Test]
    public void Navigable_StrongestFlows_NeverMountainNorWater()
    {
        var s = new MapRulesSettings { NavigableFraction = 0.5f };
        var navigable = Hydrology.Navigable(Framed(0f, 4f, 3f, 2f, 1f, 0f), FramedReliefs(W, F, M, F, F, W), s);

        Assert.IsFalse(navigable[0, 1]);   // eau
        Assert.IsTrue(navigable[1, 1]);    // plus fort débit
        Assert.IsFalse(navigable[2, 1]);   // fort, mais montagne
        Assert.IsFalse(navigable[4, 1]);   // faible
    }

    [Test]
    public void Navigable_NeverOnMapEdge()
    {
        // La case du bord droit (x = 4) a le plus fort débit, mais reste de la terre.
        var s = new MapRulesSettings { NavigableFraction = 0.5f };
        var navigable = Hydrology.Navigable(Framed(0f, 1f, 2f, 3f, 9f), FramedReliefs(W, F, F, F, F), s);

        Assert.IsFalse(navigable[4, 1]);
        Assert.IsTrue(navigable[3, 1]);
    }

    [Test]
    public void Navigable_DownstreamIsNavigableOrSea()
    {
        var reliefs = FramedReliefs(W, F, F, F, F, F);
        var (down, flow) = Hydrology.Flow(Framed(0f, .1f, .2f, .3f, .4f, .5f), reliefs, Framed(0f, 1f, 1f, 1f, 1f, 1f), Line);
        var navigable = Hydrology.Navigable(flow, reliefs, new MapRulesSettings { NavigableFraction = 0.4f });

        int count = 0;
        for (int x = 1; x < 6; x++)
        {
            if (!navigable[x, 1]) continue;
            count++;
            var d = down[x, 1];
            Assert.IsTrue(reliefs[d.x, d.y] == Relief.Water || navigable[d.x, d.y]);
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

    // Voisins donnés à la main : paires de cases (numéros sur la rangée) qui se touchent.
    private static Func<int, int, IEnumerable<Vector2Int>> Touching(params (int a, int b)[] pairs) =>
        (x, y) => pairs.Where(p => p.a == x || p.b == x).Select(p => new Vector2Int(p.a == x ? p.b : p.a, 0));

    [Test]
    public void SingleWidth_SimpleChain_Kept()
    {
        // 3 → 2 → 1 → mer (0).
        var kept = Hydrology.SingleWidth(Bools(false, true, true, true), Row(9f, 3f, 2f, 1f), Downs(-1, 0, 1, 2),
            Touching((0, 1), (1, 2), (2, 3)));

        Assert.IsTrue(kept[1, 0] && kept[2, 0] && kept[3, 0]);
    }

    [Test]
    public void SingleWidth_TributaryTouchingTwoCells_Removed()
    {
        // Fleuve 3 → 2 → 1 → mer ; affluent 5 → 4 → 2, la case 4 touche aussi 3 : affluent retiré avec son amont.
        var kept = Hydrology.SingleWidth(Bools(false, true, true, true, true, true), Row(9f, 6f, 5f, 2f, 1.5f, 1f),
            Downs(-1, 0, 1, 2, 2, 4), Touching((0, 1), (1, 2), (2, 3), (2, 4), (3, 4), (4, 5)));

        Assert.IsTrue(kept[3, 0]);
        Assert.IsFalse(kept[4, 0] || kept[5, 0]);
    }

    [Test]
    public void SingleWidth_SharpBend_Kept()
    {
        // 3 → 2 → 1 → mer, 3 touche aussi 1 (virage serré) : toléré.
        var kept = Hydrology.SingleWidth(Bools(false, true, true, true), Row(9f, 3f, 2f, 1f), Downs(-1, 0, 1, 2),
            Touching((0, 1), (1, 2), (2, 3), (1, 3)));

        Assert.IsTrue(kept[3, 0]);
    }

    [Test]
    public void PruneShortTributaries_OneCellTributary_Removed()
    {
        // Fleuve 3 → 2 → 1 → mer ; affluent d'une case 4 → 1.
        var kept = Hydrology.PruneShortTributaries(Bools(false, true, true, true, true), Row(9f, 5f, 2f, 1f, 1f),
            Downs(-1, 0, 1, 2, 1), 2);

        Assert.IsTrue(kept[1, 0] && kept[2, 0] && kept[3, 0]);
        Assert.IsFalse(kept[4, 0]);
    }

    [Test]
    public void PruneShortTributaries_LongEnoughTributary_Kept()
    {
        // Fleuve 3 → 2 → 1 → mer ; affluent de deux cases 5 → 4 → 1.
        var kept = Hydrology.PruneShortTributaries(Bools(false, true, true, true, true, true), Row(9f, 6f, 2f, 1f, 2f, 1f),
            Downs(-1, 0, 1, 2, 1, 4), 2);

        Assert.IsTrue(kept[4, 0] && kept[5, 0]);
    }

    [Test]
    public void PruneShortTributaries_SimpleChain_Unchanged()
    {
        var kept = Hydrology.PruneShortTributaries(Bools(false, true, true, true), Row(9f, 3f, 2f, 1f), Downs(-1, 0, 1, 2), 2);

        Assert.IsTrue(kept[1, 0] && kept[2, 0] && kept[3, 0]);
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
