using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

// Affiche le tour, le joueur courant et l'unité sélectionnée.
[RequireComponent(typeof(UIDocument))]
public class HUD : MonoBehaviour
{
    private Label turnLabel;
    private VisualElement unitPanel;
    private Label unitNameLabel;
    private Label unitMoveLabel;
    private VisualElement playerSwatch;
    private string lastUnitText;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        turnLabel = root.Q<Label>("turnLabel");
        unitPanel = root.Q("unitPanel");
        unitNameLabel = root.Q<Label>("unitNameLabel");
        unitMoveLabel = root.Q<Label>("unitMoveLabel");
        playerSwatch = root.Q("playerSwatch");
        lastUnitText = null;
        GameManager.Instance.TurnChange += Refresh;
        GameManager.Instance.PlayerTurnStart += OnPlayerTurnStart;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        if (GameManager.Instance == null) return;
        GameManager.Instance.TurnChange -= Refresh;
        GameManager.Instance.PlayerTurnStart -= OnPlayerTurnStart;
    }

    private void OnPlayerTurnStart(Player player) => Refresh();

    // Langue changée : réécrit le tour (le panneau d'unité est recalculé à chaque image).
    private void OnLocaleChanged(Locale locale) => Refresh();

    private void Refresh()
    {
        if (turnLabel == null) return;
        Player player = GameManager.Instance.CurrentPlayer;
        // Couleur du joueur : donnée du jeu, posée par le code (pas de classe USS possible).
        if (playerSwatch != null && player != null) playerSwatch.style.backgroundColor = player.Color;
        int turn = GameManager.Instance.CurrentTurn;
        turnLabel.text = player != null
            ? L10n.Get("UI", "hud.turn_player", turn, PlayerName(player))
            : L10n.Get("UI", "hud.turn", turn);
    }

    // Unité sélectionnée et points restants ; texte réécrit seulement s'il change.
    private void Update()
    {
        if (unitPanel == null) return;
        Unit unit = SelectionController.Instance.SelectedUnit;
        string name = unit == null ? "" : unit.type == null ? unit.name
            : L10n.Get("UI", "hud.unit_with_category", UnitName(unit), unit.type.Category.DisplayName());
        string move = unit == null ? "" :
            L10n.Get("UI", "hud.movement", unit.MovementPoints, unit.type != null ? unit.type.MovementPoints : 0);
        string text = name + "|" + move;
        if (text == lastUnitText) return;
        lastUnitText = text;
        unitNameLabel.text = name;
        unitMoveLabel.text = move;
        unitPanel.EnableInClassList("hidden", unit == null);
    }

    // Nom provisoire traduit, « Joueur 1 »… (numéro dans GameManager.Players), en attendant les noms de nations.
    private static string PlayerName(Player player) =>
        L10n.Get("UI", "player.default_name", GameManager.Instance.Players.IndexOf(player) + 1);

    // Nom traduit : "unit." + nom du type en minuscules (Warrior → unit.warrior).
    private static string UnitName(Unit unit) => L10n.Get("Units", "unit." + unit.type.Name.ToLower());
}
