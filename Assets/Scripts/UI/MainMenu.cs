using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Menu principal (scène MainMenu, première du build) et écran des paramètres de partie (Personnaliser).
// Nouvelle partie lance directement la partie avec les paramètres enregistrés.
[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Game";

    // Ordre affiché des types, celui par défaut (Fractale) en premier.
    private static readonly MapType[] TypeOrder = { MapType.Fractal, MapType.Continents, MapType.Pangaea, MapType.Archipelago };

    private VisualElement mainPanel, setupPanel, optionsPanel;
    private Button newGame, customize, load, options, quit, cancel, save, optionsCancel, optionsSave;
    private Label setupTitle, sizeInfo, optionsTitle;
    private DropdownField sizeField, typeField, playersField, languageField;
    private Toggle wrapField, fullscreenField, edgeScrollField, invertWheelField;
    private TextField seedField;
    private GameSettings saved;
    // Codes des langues proposées, dans l'ordre de la liste ; "" = langue du système.
    private List<string> languageCodes = new List<string> { "" };

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        mainPanel = root.Q<VisualElement>("mainPanel");
        setupPanel = root.Q<VisualElement>("setupPanel");
        newGame = root.Q<Button>("newGame");
        customize = root.Q<Button>("customize");
        load = root.Q<Button>("load");
        options = root.Q<Button>("options");
        quit = root.Q<Button>("quit");
        cancel = root.Q<Button>("cancel");
        save = root.Q<Button>("save");
        setupTitle = root.Q<Label>("setupTitle");
        sizeInfo = root.Q<Label>("sizeInfo");
        sizeField = root.Q<DropdownField>("sizeField");
        typeField = root.Q<DropdownField>("typeField");
        playersField = root.Q<DropdownField>("playersField");
        wrapField = root.Q<Toggle>("wrapField");
        seedField = root.Q<TextField>("seedField");
        optionsPanel = root.Q<VisualElement>("optionsPanel");
        optionsTitle = root.Q<Label>("optionsTitle");
        languageField = root.Q<DropdownField>("languageField");
        fullscreenField = root.Q<Toggle>("fullscreenField");
        edgeScrollField = root.Q<Toggle>("edgeScrollField");
        invertWheelField = root.Q<Toggle>("invertWheelField");
        optionsCancel = root.Q<Button>("optionsCancel");
        optionsSave = root.Q<Button>("optionsSave");

        saved = GameSettingsStore.Load();

        newGame.clicked += OnNewGame;
        customize.clicked += OnCustomize;
        options.clicked += OnOptions;
        quit.clicked += OnQuit;
        cancel.clicked += OnCancel;
        save.clicked += OnSave;
        optionsCancel.clicked += OnCancel;
        optionsSave.clicked += OnSaveOptions;
        sizeField.RegisterValueChangedCallback(OnShapeChanged);
        typeField.RegisterValueChangedCallback(OnShapeChanged);
        // Pas encore faite : sauvegarde de partie.
        load.SetEnabled(false);

        WriteTexts();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        newGame.clicked -= OnNewGame;
        customize.clicked -= OnCustomize;
        options.clicked -= OnOptions;
        quit.clicked -= OnQuit;
        cancel.clicked -= OnCancel;
        save.clicked -= OnSave;
        optionsCancel.clicked -= OnCancel;
        optionsSave.clicked -= OnSaveOptions;
        sizeField.UnregisterValueChangedCallback(OnShapeChanged);
        typeField.UnregisterValueChangedCallback(OnShapeChanged);
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Échap sur un écran (paramètres, options) : retour au menu sans enregistrer.
    private void Update()
    {
        if (mainPanel.ClassListContains("hidden") && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame) OnCancel();
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _) => WriteTexts();

    private void WriteTexts()
    {
        newGame.text = L10n.Get("Menu", "menu.new_game");
        customize.text = L10n.Get("Menu", "menu.customize");
        load.text = L10n.Get("Menu", "menu.load");
        options.text = L10n.Get("Menu", "menu.options");
        quit.text = L10n.Get("Menu", "menu.quit");

        setupTitle.text = L10n.Get("Menu", "setup.title");
        sizeField.label = L10n.Get("Menu", "setup.size");
        typeField.label = L10n.Get("Menu", "setup.map_type");
        playersField.label = L10n.Get("Menu", "setup.players");
        wrapField.label = L10n.Get("Menu", "setup.wrap");
        seedField.label = L10n.Get("Menu", "setup.seed");
        cancel.text = L10n.Get("Menu", "setup.cancel");
        save.text = L10n.Get("Menu", "setup.save");

        // Choix traduits ; le choix en cours est gardé au changement de langue.
        SetChoices(sizeField, new[] { "size.small", "size.medium", "size.large" }.Select(k => L10n.Get("Menu", k)));
        SetChoices(typeField, TypeOrder.Select(t => L10n.Get("Menu", "maptype." + t.ToString().ToLower())));
        SetChoices(playersField, Enumerable.Range(GameSettings.MinPlayers, GameSettings.MaxPlayers - GameSettings.MinPlayers + 1)
            .Select(n => n.ToString()));
        UpdateSizeInfo();

        optionsTitle.text = L10n.Get("Menu", "options.title");
        languageField.label = L10n.Get("Menu", "options.language");
        fullscreenField.label = L10n.Get("Menu", "options.fullscreen");
        edgeScrollField.label = L10n.Get("Menu", "options.edge_scroll");
        invertWheelField.label = L10n.Get("Menu", "options.invert_wheel");
        optionsCancel.text = L10n.Get("Menu", "setup.cancel");
        optionsSave.text = L10n.Get("Menu", "setup.save");
        // Chaque langue sous son propre nom (Français, English).
        var locales = LocalizationSettings.AvailableLocales.Locales;
        languageCodes = new List<string> { "" }.Concat(locales.Select(l => l.Identifier.Code)).ToList();
        SetChoices(languageField, new[] { L10n.Get("Menu", "options.system_language") }
            .Concat(locales.Select(l => Capitalize(l.Identifier.CultureInfo?.NativeName ?? l.Identifier.Code))));
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    private static void SetChoices(DropdownField field, IEnumerable<string> choices)
    {
        int index = Mathf.Max(field.index, 0);
        field.choices = choices.ToList();
        field.index = Mathf.Min(index, field.choices.Count - 1);
    }

    // Copie : la partie ne modifie pas les paramètres enregistrés.
    private void OnNewGame()
    {
        GameSetup.Current = GameSettingsStore.FromJson(GameSettingsStore.ToJson(saved));
        SceneManager.LoadScene(gameScene);
    }

    private void OnCustomize()
    {
        sizeField.index = (int)saved.Size;
        typeField.index = System.Array.IndexOf(TypeOrder, saved.MapType);
        playersField.index = saved.PlayerCount - GameSettings.MinPlayers;
        wrapField.value = saved.WrapWorld;
        seedField.value = saved.Seed.HasValue ? saved.Seed.Value.ToString() : "";
        UpdateSizeInfo();
        ShowPanel(setupPanel);
    }

    private void OnOptions()
    {
        UserOptions o = UserOptionsStore.Current;
        languageField.index = Mathf.Max(0, languageCodes.IndexOf(o.Language));
        fullscreenField.value = o.Fullscreen;
        edgeScrollField.value = o.EdgeScroll;
        invertWheelField.value = o.InvertWheel;
        ShowPanel(optionsPanel);
    }

    // Enregistrer applique tout de suite ; la langue changée réécrit les textes (OnLocaleChanged).
    private void OnSaveOptions()
    {
        UserOptionsStore.Save(new UserOptions
        {
            Language = languageCodes[Mathf.Max(0, languageField.index)],
            Fullscreen = fullscreenField.value,
            EdgeScroll = edgeScrollField.value,
            InvertWheel = invertWheelField.value,
        });
        ShowPanel(mainPanel);
    }

    private void OnCancel() => ShowPanel(mainPanel);

    private void OnSave()
    {
        saved = new GameSettings
        {
            Size = (MapSize)sizeField.index,
            MapType = TypeOrder[typeField.index],
            PlayerCount = playersField.index + GameSettings.MinPlayers,
            WrapWorld = wrapField.value,
            Seed = GameSettings.ParseSeed(seedField.value),
        };
        GameSettingsStore.Save(saved);
        ShowPanel(mainPanel);
    }

    // Un seul des trois cadres à la fois.
    private void ShowPanel(VisualElement panel)
    {
        foreach (var p in new[] { mainPanel, setupPanel, optionsPanel })
            p.EnableInClassList("hidden", p != panel);
    }

    private void OnShapeChanged(ChangeEvent<string> _) => UpdateSizeInfo();

    // La taille dépend aussi du type (Pangée presque carrée).
    private void UpdateSizeInfo()
    {
        if (sizeField.index < 0 || typeField.index < 0) return;
        var (w, h) = GameSettings.Dimensions((MapSize)sizeField.index, TypeOrder[typeField.index]);
        sizeInfo.text = L10n.Get("Menu", "setup.dimensions", w, h);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
