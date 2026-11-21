using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Menu principal (scène MainMenu, première du build).
[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Game";

    private Button newGame, load, options, quit;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        newGame = root.Q<Button>("newGame");
        load = root.Q<Button>("load");
        options = root.Q<Button>("options");
        quit = root.Q<Button>("quit");

        newGame.clicked += OnNewGame;
        quit.clicked += OnQuit;
        // Pas encore faits : sauvegarde et options.
        load.SetEnabled(false);
        options.SetEnabled(false);

        WriteTexts();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        newGame.clicked -= OnNewGame;
        quit.clicked -= OnQuit;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _) => WriteTexts();

    private void WriteTexts()
    {
        newGame.text = L10n.Get("Menu", "menu.new_game");
        load.text = L10n.Get("Menu", "menu.load");
        options.text = L10n.Get("Menu", "menu.options");
        quit.text = L10n.Get("Menu", "menu.quit");
    }

    // En attendant l'écran de création (étape 3) : réglages par défaut (Petite, Continents, 5 joueurs).
    private void OnNewGame()
    {
        GameSetup.Current = new GameSettings();
        SceneManager.LoadScene(gameScene);
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
