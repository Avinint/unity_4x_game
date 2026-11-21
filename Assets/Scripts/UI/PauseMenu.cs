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

    private VisualElement veil;
    private Button menuButton, resume, mainMenu, quitGame;
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
        mainMenu = root.Q<Button>("mainMenu");
        quitGame = root.Q<Button>("quitGame");

        menuButton.clicked += Open;
        resume.clicked += Close;
        mainMenu.clicked += ToMainMenu;
        quitGame.clicked += Quit;

        WriteTexts();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        menuButton.clicked -= Open;
        resume.clicked -= Close;
        mainMenu.clicked -= ToMainMenu;
        quitGame.clicked -= Quit;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _) => WriteTexts();

    private void WriteTexts()
    {
        menuButton.text = L10n.Get("Menu", "menu.open");
        resume.text = L10n.Get("Menu", "menu.resume");
        mainMenu.text = L10n.Get("Menu", "menu.main_menu");
        quitGame.text = L10n.Get("Menu", "menu.quit");
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        openedFrame = Time.frameCount;
        veil.RemoveFromClassList("hidden");
        SetGameInput(false);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        veil.AddToClassList("hidden");
        SetGameInput(true);
    }

    // Échap referme le menu (pas dans l'image où il s'est ouvert : c'est la même pression).
    private void Update()
    {
        if (IsOpen && Time.frameCount != openedFrame && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    private void SetGameInput(bool on)
    {
        var input = FindAnyObjectByType<PlayerInput>();
        if (input != null)
        {
            if (on) input.ActivateInput();
            else input.DeactivateInput();
        }
        if (CameraController.Instance != null) CameraController.Instance.IsLocked = !on;
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
