using System;
using System.IO;
using UnityEngine;

// Paramètres de partie enregistrés sur disque (écran Personnaliser), repris au lancement du jeu.
public static class GameSettingsStore
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "game-settings.json");

    // Fichier absent ou illisible : paramètres par défaut.
    public static GameSettings Load()
    {
        try
        {
            return File.Exists(FilePath) ? FromJson(File.ReadAllText(FilePath)) : new GameSettings();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Paramètres de partie illisibles ({e.Message}) : valeurs par défaut.");
            return new GameSettings();
        }
    }

    public static void Save(GameSettings settings) => File.WriteAllText(FilePath, ToJson(settings));

    // JsonUtility ne sait pas écrire un int? (la graine) : forme à plat.
    [Serializable]
    private class Saved
    {
        public int Size;
        public int MapType;
        public bool WrapWorld;
        public int PlayerCount;
        public bool HasSeed;
        public int Seed;
    }

    public static string ToJson(GameSettings s) => JsonUtility.ToJson(new Saved
    {
        Size = (int)s.Size,
        MapType = (int)s.MapType,
        WrapWorld = s.WrapWorld,
        PlayerCount = s.PlayerCount,
        HasSeed = s.Seed.HasValue,
        Seed = s.Seed ?? 0,
    }, true);

    // Valeurs hors limites (fichier modifié à la main, ancienne version) ramenées dans les bornes.
    public static GameSettings FromJson(string json)
    {
        var settings = new GameSettings();
        if (string.IsNullOrWhiteSpace(json)) return settings;
        Saved saved;
        try { saved = JsonUtility.FromJson<Saved>(json); }
        catch (ArgumentException) { return settings; }
        if (saved == null) return settings;

        if (Enum.IsDefined(typeof(MapSize), saved.Size)) settings.Size = (MapSize)saved.Size;
        if (Enum.IsDefined(typeof(MapType), saved.MapType)) settings.MapType = (MapType)saved.MapType;
        settings.WrapWorld = saved.WrapWorld;
        settings.PlayerCount = Mathf.Clamp(saved.PlayerCount, GameSettings.MinPlayers, GameSettings.MaxPlayers);
        settings.Seed = saved.HasSeed ? saved.Seed : (int?)null;
        return settings;
    }
}
