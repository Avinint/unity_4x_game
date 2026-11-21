using System;
using System.IO;
using UnityEngine;
using UnityEngine.Localization.Settings;

// Préférences du joueur (pas d'une partie) : langue, plein écran, commandes.
[Serializable]
public class UserOptions
{
    public string Language = "";      // code de la langue (fr, en) ; vide : langue du système
    public bool Fullscreen = true;
    public bool EdgeScroll = true;    // défilement par les bords
    public bool InvertWheel = false;  // non coché : molette vers le haut = éloigner
}

// Options enregistrées (options.json), lues et appliquées au lancement du jeu.
public static class UserOptionsStore
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "options.json");

    public static UserOptions Current { get; private set; } = new UserOptions();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadAtStartup()
    {
        try { Current = File.Exists(FilePath) ? FromJson(File.ReadAllText(FilePath)) : new UserOptions(); }
        catch (Exception e)
        {
            Debug.LogWarning($"Options illisibles ({e.Message}) : valeurs par défaut.");
            Current = new UserOptions();
        }
        Apply(Current);
    }

    public static void Save(UserOptions options)
    {
        Current = options;
        File.WriteAllText(FilePath, ToJson(options));
        Apply(options);
    }

    public static string ToJson(UserOptions o) => JsonUtility.ToJson(o, true);

    // Champs absents (ancienne version) : valeurs par défaut ; fichier abîmé : tout par défaut.
    public static UserOptions FromJson(string json)
    {
        var options = new UserOptions();
        if (string.IsNullOrWhiteSpace(json)) return options;
        try { JsonUtility.FromJsonOverwrite(json, options); }
        catch (ArgumentException) { return new UserOptions(); }
        options.Language ??= "";
        return options;
    }

    private static void Apply(UserOptions o)
    {
        Screen.fullScreenMode = o.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        // La langue se choisit toujours par SelectedLocale, une fois Localization prêt.
        var init = LocalizationSettings.InitializationOperation;
        if (init.IsDone) SelectLanguage(o.Language);
        else init.Completed += _ => SelectLanguage(o.Language);
    }

    private static void SelectLanguage(string code)
    {
        var locales = LocalizationSettings.AvailableLocales;
        var locale = string.IsNullOrEmpty(code)
            ? locales.GetLocale(Application.systemLanguage) ?? locales.GetLocale("fr")   // langue du système, sinon français
            : locales.GetLocale(code);
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
    }
}
