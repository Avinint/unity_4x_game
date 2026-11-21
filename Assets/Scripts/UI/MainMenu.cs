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

    private VisualElement mainPanel, setupPanel;
    private Button newGame, customize, load, options, quit, cancel, save;
    private Label setupTitle, sizeInfo;
    private DropdownField sizeField, typeField, playersField;
    private Toggle wrapField;
    private TextField seedField;
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

        saved = GameSettingsStore.Load();

        newGame.clicked += OnNewGame;
        customize.clicked += OnCustomize;
        quit.clicked += OnQuit;
        cancel.clicked += OnCancel;
        save.clicked += OnSave;
        sizeField.RegisterValueChangedCallback(OnShapeChanged);
        typeField.RegisterValueChangedCallback(OnShapeChanged);
        // Pas encore faits : sauvegarde de partie et options.
        load.SetEnabled(false);
        options.SetEnabled(false);

        WriteTexts();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        newGame.clicked -= OnNewGame;
        customize.clicked -= OnCustomize;
        quit.clicked -= OnQuit;
        cancel.clicked -= OnCancel;
        save.clicked -= OnSave;
        sizeField.UnregisterValueChangedCallback(OnShapeChanged);
        typeField.UnregisterValueChangedCallback(OnShapeChanged);
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Échap sur l'écran des paramètres : retour au menu sans enregistrer.
    private void Update()
    {
        if (!setupPanel.ClassListContains("hidden") && Keyboard.current != null
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
    }

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
        ShowSetup(true);
    }

    private void OnCancel() => ShowSetup(false);

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
        ShowSetup(false);
    }

    private void ShowSetup(bool show)
    {
        mainPanel.EnableInClassList("hidden", show);
        setupPanel.EnableInClassList("hidden", !show);
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
