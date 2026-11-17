using UnityEngine.Localization.Settings;

// Texte traduit : lit `key` dans la table, dans la langue choisie ({0}, {1}… remplis par args).
public static class L10n
{
    public static string Get(string table, string key, params object[] args) =>
        LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
}
