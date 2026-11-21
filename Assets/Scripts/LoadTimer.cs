using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Durées des étapes du chargement d'une partie, résumées en une ligne par l'écran de chargement
// (pour savoir quelle étape coûte avant d'optimiser). Appelé aussi depuis les threads de génération.
public static class LoadTimer
{
    private static readonly List<(string name, double seconds)> steps = new List<(string, double)>();
    private static readonly object gate = new object();

    public static void Reset()
    {
        lock (gate) steps.Clear();
    }

    // Une étape mesurée plusieurs fois (instanciation par lots) s'additionne.
    public static void Add(string name, double seconds)
    {
        lock (gate)
        {
            int i = steps.FindIndex(s => s.name == name);
            if (i >= 0) steps[i] = (name, steps[i].seconds + seconds);
            else steps.Add((name, seconds));
        }
    }

    public static T Measure<T>(string name, Func<T> work)
    {
        var watch = Stopwatch.StartNew();
        try { return work(); }
        finally { Add(name, watch.Elapsed.TotalSeconds); }
    }

    public static void Measure(string name, Action work)
    {
        var watch = Stopwatch.StartNew();
        try { work(); }
        finally { Add(name, watch.Elapsed.TotalSeconds); }
    }

    // « Chargement 12,9 s : génération 0,1 · … · autre 0,4 » ; « autre » = images d'attente, rendu, étapes non mesurées.
    public static string Summary(double total)
    {
        lock (gate)
        {
            double measured = steps.Sum(s => s.seconds);
            var parts = steps.Select(s => $"{s.name} {s.seconds:F1}")
                             .Append($"autre {Math.Max(0, total - measured):F1}");
            return $"Chargement {total:F1} s : " + string.Join(" · ", parts);
        }
    }
}
