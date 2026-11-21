using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

// Écran des Options (OptionsPanel.uxml), partagé par le menu principal et le menu de la partie.
// Enregistrer applique tout de suite (UserOptionsStore.Save) ; la langue changée réécrit les textes.
public class OptionsPanel
{
    public event Action Closed;   // Annuler, Échap ou Enregistrer
    public bool IsOpen { get; private set; }

    private readonly VisualElement container;
    private readonly Label title;
    private readonly DropdownField languageField;
    private readonly Toggle fullscreenField, edgeScrollField, invertWheelField;
    private readonly Button cancel, save;
    // Codes des langues proposées, dans l'ordre de la liste ; "" = langue du système.
    private List<string> languageCodes = new List<string> { "" };

    // `container` : l'instance du modèle (fermée au départ).
    public OptionsPanel(VisualElement container)
    {
        this.container = container;
        title = container.Q<Label>("optionsTitle");
        languageField = container.Q<DropdownField>("languageField");
        fullscreenField = container.Q<Toggle>("fullscreenField");
        edgeScrollField = container.Q<Toggle>("edgeScrollField");
        invertWheelField = container.Q<Toggle>("invertWheelField");
        cancel = container.Q<Button>("optionsCancel");
        save = container.Q<Button>("optionsSave");
        cancel.clicked += Close;
        save.clicked += Save;
        container.style.display = DisplayStyle.None;
        WriteTexts();
    }

    public void Open()
    {
        UserOptions o = UserOptionsStore.Current;
        languageField.index = Mathf.Max(0, languageCodes.IndexOf(o.Language));
        fullscreenField.value = o.Fullscreen;
        edgeScrollField.value = o.EdgeScroll;
        invertWheelField.value = o.InvertWheel;
        container.style.display = DisplayStyle.Flex;
        IsOpen = true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        container.style.display = DisplayStyle.None;
        IsOpen = false;
        Closed?.Invoke();
    }

    private void Save()
    {
        UserOptionsStore.Save(new UserOptions
        {
            Language = languageCodes[Mathf.Max(0, languageField.index)],
            Fullscreen = fullscreenField.value,
            EdgeScroll = edgeScrollField.value,
            InvertWheel = invertWheelField.value,
        });
        Close();
    }

    public void WriteTexts()
    {
        title.text = L10n.Get("Menu", "options.title");
        languageField.label = L10n.Get("Menu", "options.language");
        fullscreenField.label = L10n.Get("Menu", "options.fullscreen");
        edgeScrollField.label = L10n.Get("Menu", "options.edge_scroll");
        invertWheelField.label = L10n.Get("Menu", "options.invert_wheel");
        cancel.text = L10n.Get("Menu", "setup.cancel");
        save.text = L10n.Get("Menu", "setup.save");
        // Chaque langue sous son propre nom (Français, English).
        var locales = LocalizationSettings.AvailableLocales.Locales;
        languageCodes = new List<string> { "" }.Concat(locales.Select(l => l.Identifier.Code)).ToList();
        SetChoices(languageField, new[] { L10n.Get("Menu", "options.system_language") }
            .Concat(locales.Select(l => Capitalize(l.Identifier.CultureInfo?.NativeName ?? l.Identifier.Code))));
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    // Choix traduits ; le choix en cours est gardé au changement de langue.
    public static void SetChoices(DropdownField field, IEnumerable<string> choices)
    {
        int index = Mathf.Max(field.index, 0);
        field.choices = choices.ToList();
        field.index = Mathf.Min(index, field.choices.Count - 1);
    }
}
