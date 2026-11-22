using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

// Console de débogage (Editor et builds de développement seulement) : ² ouvre / ferme, Échap ferme,
// Entrée exécute, ↑ / ↓ reprennent les commandes précédentes. Les raccourcis F9 / F10 / F12 n'agissent
// que console ouverte, pour ne pas les déclencher par erreur. Outil de développement : textes en anglais,
// non traduits.
[RequireComponent(typeof(UIDocument))]
public class DebugConsole : MonoBehaviour
{
    private const string Shortcuts =
        "F9 teleport selected unit to hovered tile   F10 restore its movement   F12 next language   (² close)";
    private const int MaxLines = 200;

    private readonly ConsoleCommands commands = new ConsoleCommands();
    private readonly List<string> history = new List<string>();
    private int historyIndex;

    private VisualElement frame;
    private ScrollView log;
    private TextField input;
    private bool isOpen;

    private void Awake()
    {
        commands.Register("help", "list commands and shortcuts",
            _ => string.Join("\n", commands.HelpLines().Prepend(Shortcuts)));
        commands.Register("clear", "clear the console", _ => { log.Clear(); return ""; });
        commands.Register("reveal", "reveal the whole map", _ =>
        {
            GameManager.Instance.RevealMap();
            return "Map revealed.";
        });
        commands.Register("moves", "restore movement of the selected unit (moves all: all your units)", Moves);
        commands.Register("tp", "teleport the selected unit to the hovered tile (like F9)",
            _ => SelectionController.Instance.DebugTeleport());
        commands.Register("lang", "next language, or lang <code> (fr, en)", Lang);
        commands.Register("seed", "show the map seed", _ =>
        {
            var generator = FindAnyObjectByType<MapGenerator>();
            return generator != null ? $"Seed: {generator.Seed}" : "No map generator.";
        });
    }

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        frame = root.Q<VisualElement>("debugConsole");
        log = root.Q<ScrollView>("consoleLog");
        input = root.Q<TextField>("consoleInput");
        root.Q<Label>("consoleShortcuts").text = Shortcuts;
        frame.RemoveFromClassList("hidden");
        frame.style.display = DisplayStyle.None;

        input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        input.RegisterCallback<FocusInEvent>(OnFocusIn);
        input.RegisterCallback<FocusOutEvent>(OnFocusOut);
    }

    private void OnDisable()
    {
        input.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        input.UnregisterCallback<FocusInEvent>(OnFocusIn);
        input.UnregisterCallback<FocusOutEvent>(OnFocusOut);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (!Debug.isDebugBuild || keyboard == null) return;

        // ² : touche à gauche du 1 en AZERTY, lue par sa position physique (backquote en QWERTY).
        if (keyboard.backquoteKey.wasPressedThisFrame) { if (isOpen) Close(); else Open(); return; }
        if (!isOpen) return;
        if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }

        if (keyboard.f9Key.wasPressedThisFrame) Print(SelectionController.Instance.DebugTeleport());
        if (keyboard.f10Key.wasPressedThisFrame) Print(Moves(new string[0]));
        if (keyboard.f12Key.wasPressedThisFrame) Print(Lang(new string[0]));
    }

    private void Open()
    {
        isOpen = true;
        frame.style.display = DisplayStyle.Flex;
        input.value = "";
        input.schedule.Execute(() => input.Focus());   // après l'image : sinon le ² tapé s'écrit dans le champ
    }

    private void Close()
    {
        isOpen = false;
        input.Blur();
        frame.style.display = DisplayStyle.None;
        GameInput.SetEnabled(true);
    }

    // Saisie en cours : commandes du jeu coupées (sinon « a » zoomerait, Retour arrière désélectionnerait).
    // Un clic sur la carte retire le focus et rend la main au jeu (survol à jour pour F9).
    private void OnFocusIn(FocusInEvent _) => GameInput.SetEnabled(false);
    private void OnFocusOut(FocusOutEvent _) => GameInput.SetEnabled(true);

    private void OnKeyDown(KeyDownEvent e)
    {
        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
        {
            Execute(input.value);
            input.value = "";
            e.StopPropagation();
        }
        else if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
        {
            if (history.Count == 0) return;
            historyIndex = Mathf.Clamp(historyIndex + (e.keyCode == KeyCode.UpArrow ? -1 : 1), 0, history.Count);
            input.value = historyIndex < history.Count ? history[historyIndex] : "";
            e.StopPropagation();
        }
        else if (e.character == '²' || e.character == '`')
        {
            e.StopPropagation();   // la touche qui ferme la console ne s'écrit pas
        }
    }

    private void Execute(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        history.Add(line.Trim());
        historyIndex = history.Count;
        Print("> " + line.Trim());
        Print(commands.Run(line));
    }

    private void Print(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (string line in text.Split('\n'))
            log.Add(new Label(line) { name = "consoleText" }.WithClass("console-line"));
        while (log.contentContainer.childCount > MaxLines) log.contentContainer.RemoveAt(0);
        log.schedule.Execute(() => log.scrollOffset = new Vector2(0, float.MaxValue));
    }

    private static string Moves(string[] args)
    {
        if (args.Length > 0 && args[0].ToLowerInvariant() == "all")
        {
            Player player = GameManager.Instance.LocalPlayer;
            if (player == null) return "No local player.";
            var units = player.units.Select(go => go != null ? go.GetComponent<Unit>() : null).Where(u => u != null).ToList();
            foreach (var unit in units) unit.ResetMovementPoints();
            return $"Movement restored for {units.Count} units.";
        }
        Unit selected = SelectionController.Instance.SelectedUnit;
        if (selected == null) return "No unit selected.";
        selected.ResetMovementPoints();
        return "Movement restored.";
    }

    private static string Lang(string[] args)
    {
        if (args.Length == 0)
        {
            L10n.NextLocale();
            return $"Language: {LocalizationSettings.SelectedLocale?.Identifier.Code}";
        }
        var locale = LocalizationSettings.AvailableLocales.GetLocale(args[0]);
        if (locale == null)
            return "Unknown language. Available: " +
                   string.Join(", ", LocalizationSettings.AvailableLocales.Locales.Select(l => l.Identifier.Code));
        LocalizationSettings.SelectedLocale = locale;
        return $"Language: {locale.Identifier.Code}";
    }
}

static class VisualElementExtensions
{
    // Ajoute une classe USS et renvoie l'élément (création en une ligne).
    public static T WithClass<T>(this T element, string className) where T : VisualElement
    {
        element.AddToClassList(className);
        return element;
    }
}
