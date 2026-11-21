using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Menu principal (scène MainMenu, première du build), écran des paramètres de partie (Personnaliser)
// et Options (OptionsPanel, partagé avec le menu de la partie).
// Nouvelle partie lance directement la partie avec les paramètres enregistrés.
[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Game";

    // Ordre affiché des types, celui par défaut (Fractale) en premier.
    private static readonly MapType[] TypeOrder = { MapType.Fractal, MapType.Continents, MapType.Pangaea, MapType.Archipelago };

    private VisualElement mainPanel, setupPanel;
    private Button newGame, customize, load, options, quit, cancel, save;
    private Label setupTitle, sizeInfo;
    private DropdownField sizeField, typeField, playersField;
    private Toggle wrapField;
    private TextField seedField;
    private OptionsPanel optionsPanel;
    private GameSettings saved;

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
        optionsPanel = new OptionsPanel(root.Q<VisualElement>("optionsPanel"));

        saved = GameSettingsStore.Load();

        newGame.clicked += OnNewGame;
        customize.clicked += OnCustomize;
        options.clicked += OnOptions;
        quit.clicked += OnQuit;
        cancel.clicked += OnCancel;
        save.clicked += OnSave;
        optionsPanel.Closed += OnCancel;
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
        optionsPanel.Closed -= OnCancel;
        sizeField.UnregisterValueChangedCallback(OnShapeChanged);
        typeField.UnregisterValueChangedCallback(OnShapeChanged);
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Échap sur un écran (paramètres, options) : retour au menu sans enregistrer.
    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (optionsPanel.IsOpen) optionsPanel.Close();
        else if (mainPanel.ClassListContains("hidden")) OnCancel();
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

        OptionsPanel.SetChoices(sizeField, new[] { "size.small", "size.medium", "size.large" }.Select(k => L10n.Get("Menu", k)));
        OptionsPanel.SetChoices(typeField, TypeOrder.Select(t => L10n.Get("Menu", "maptype." + t.ToString().ToLower())));
        OptionsPanel.SetChoices(playersField, Enumerable.Range(GameSettings.MinPlayers, GameSettings.MaxPlayers - GameSettings.MinPlayers + 1)
            .Select(n => n.ToString()));
        UpdateSizeInfo();
        optionsPanel.WriteTexts();
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
        ShowPanel(null);
        optionsPanel.Open();
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

    // Un seul cadre à la fois (null : aucun, pendant les Options).
    private void ShowPanel(VisualElement panel)
    {
        foreach (var p in new[] { mainPanel, setupPanel })
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
