using UnityEngine.Localization.Settings;

// Texte traduit : lit `key` dans la table, dans la langue choisie ({0}, {1}… remplis par args).
public static class L10n
{
    public static string Get(string table, string key, params object[] args) =>
        LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);

    // Débogage, en attendant le menu d'options.
    public static void NextLocale()
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count == 0) return;
        int index = locales.IndexOf(LocalizationSettings.SelectedLocale);
        LocalizationSettings.SelectedLocale = locales[(index + 1) % locales.Count];
    }
}
