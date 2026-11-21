using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Menu de la partie (Échap ou bouton Menu) : reprendre, revenir au menu principal, quitter.
// Pendant qu'il est ouvert, les commandes du jeu sont coupées (actions, survol, défilement par les bords).
// Ce n'est pas une pause du temps : une unité en marche finit son trajet.
[RequireComponent(typeof(UIDocument))]
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public bool IsOpen { get; private set; }

    [SerializeField] private string mainMenuScene = "MainMenu";

    private VisualElement veil, pausePanel;
    private Button menuButton, resume, pauseOptions, mainMenu, quitGame;
    private OptionsPanel optionsPanel;
    private int openedFrame;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        veil = root.Q<VisualElement>("pauseMenu");
        menuButton = root.Q<Button>("menuButton");
        resume = root.Q<Button>("resume");
        pauseOptions = root.Q<Button>("pauseOptions");
        mainMenu = root.Q<Button>("mainMenu");
        quitGame = root.Q<Button>("quitGame");
        pausePanel = root.Q<VisualElement>("pausePanel");
        optionsPanel = new OptionsPanel(root.Q<VisualElement>("optionsPanel"));

        menuButton.clicked += Open;
        resume.clicked += Close;
        pauseOptions.clicked += OpenOptions;
        mainMenu.clicked += ToMainMenu;
        quitGame.clicked += Quit;
        optionsPanel.Closed += OnOptionsClosed;

        WriteTexts();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        menuButton.clicked -= Open;
        resume.clicked -= Close;
        pauseOptions.clicked -= OpenOptions;
        mainMenu.clicked -= ToMainMenu;
        quitGame.clicked -= Quit;
        optionsPanel.Closed -= OnOptionsClosed;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OpenOptions()
    {
        pausePanel.style.display = DisplayStyle.None;
        optionsPanel.Open();
    }

    private void OnOptionsClosed() => pausePanel.style.display = DisplayStyle.Flex;

    private void OnLocaleChanged(UnityEngine.Localization.Locale _) => WriteTexts();

    private void WriteTexts()
    {
        menuButton.text = L10n.Get("Menu", "menu.open");
        resume.text = L10n.Get("Menu", "menu.resume");
        pauseOptions.text = L10n.Get("Menu", "menu.options");
        mainMenu.text = L10n.Get("Menu", "menu.main_menu");
        quitGame.text = L10n.Get("Menu", "menu.quit");
        optionsPanel.WriteTexts();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        openedFrame = Time.frameCount;
        veil.RemoveFromClassList("hidden");
        GameInput.SetEnabled(false);
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (optionsPanel.IsOpen) optionsPanel.Close();
        IsOpen = false;
        veil.AddToClassList("hidden");
        GameInput.SetEnabled(true);
    }

    // Échap referme d'abord les Options (retour au menu de la partie), sinon le menu
    // (pas dans l'image où il s'est ouvert : c'est la même pression).
    private void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame || Keyboard.current == null
            || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (optionsPanel.IsOpen) optionsPanel.Close();
        else Close();
    }

    private void ToMainMenu()
    {
        GameSetup.Current = null;
        SceneManager.LoadScene(mainMenuScene);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
