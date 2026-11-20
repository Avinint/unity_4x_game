using System.Collections.Generic;

// Lignes de la fenêtre d'informations d'une case explorée : (table, clé, paramètres), traduites par l'appelant.
public static class TileInfo
{
    private static readonly object[] None = new object[0];

    public static List<(string table, string key, object[] args)> Describe(
        Relief relief, Biome biome, Feature feature, int movementCost, bool river, bool freshWater)
    {
        var lines = new List<(string, string, object[])>();
        // Montagne : son modèle cache le sol, on n'affiche que le relief.
        if (relief == Relief.Mountain) lines.Add(("Terrain", "relief.mountain", None));
        else
        {
            lines.Add(("Terrain", "biome." + biome.ToString().ToLower(), None));
            if (feature != Feature.None) lines.Add(("Terrain", "feature." + feature.ToString().ToLower(), None));
            if (relief == Relief.Hill) lines.Add(("Terrain", "relief.hill", None));
            if (relief == Relief.NavigableRiver) lines.Add(("Terrain", "relief.navigable_river", None));
        }
        bool impassable = !MapRules.IsPassable(relief);
        lines.Add(impassable ? ("UI", "tooltip.impassable", None) : ("UI", "tooltip.movement_cost", new object[] { movementCost }));
        if (river) lines.Add(("UI", "tooltip.river", None));
        if (freshWater) lines.Add(("UI", "tooltip.fresh_water", None));
        return lines;
    }
}
