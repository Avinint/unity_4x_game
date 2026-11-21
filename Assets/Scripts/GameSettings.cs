// Réglages d'une partie, remplis par l'écran de création et lus par la scène Game.
public enum MapSize { Small, Medium, Large }   // nouvelles valeurs à la fin

public class GameSettings
{
    public const int MinPlayers = 2, MaxPlayers = 5;   // les joueurs de la scène Game

    public MapSize Size = MapSize.Small;
    // Fractale : sans navires ni embarquement, une carte en continents ou en îles peut isoler des joueurs.
    public MapType MapType = MapType.Fractal;
    public bool WrapWorld = true;
    public int PlayerCount = MaxPlayers;
    public int? Seed;              // null : graine au hasard

    // Graine saisie : vide ou invalide = au hasard (null) ; nombre entier sinon.
    public static int? ParseSeed(string text) =>
        int.TryParse(text?.Trim(), out int seed) ? seed : (int?)null;

    // Taille de la carte (terres jouables, sans la banquise). La Pangée est presque carrée, les autres types
    // larges, avec à peu près autant de cases : une colonne fait 1,73 de large, une rangée 1,5 de haut.
    public static (int width, int mapHeight) Dimensions(MapSize size, MapType type)
    {
        bool square = type == MapType.Pangaea;
        switch (size)
        {
            case MapSize.Medium: return square ? (46, 50) : (60, 38);
            case MapSize.Large:  return square ? (60, 66) : (80, 50);
            default:             return square ? (30, 34) : (40, 25);
        }
    }
}

// Passage du menu à la scène Game (reste en mémoire au changement de scène).
public static class GameSetup
{
    // null : scène Game lancée directement depuis l'Editor, les valeurs de l'Inspector restent.
    public static GameSettings Current;
}
