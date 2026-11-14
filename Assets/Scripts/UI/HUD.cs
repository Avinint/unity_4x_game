using UnityEngine;
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
        Refresh();
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.TurnChange -= Refresh;
        GameManager.Instance.PlayerTurnStart -= OnPlayerTurnStart;
    }

    private void OnPlayerTurnStart(Player player) => Refresh();

    private void Refresh()
    {
        if (turnLabel == null) return;
        Player player = GameManager.Instance.CurrentPlayer;
        // Couleur du joueur : donnée du jeu, posée par le code (pas de classe USS possible).
        if (playerSwatch != null && player != null) playerSwatch.style.backgroundColor = player.Color;
        turnLabel.text = player != null
            ? $"Tour {GameManager.Instance.CurrentTurn} — {player.name}"
            : $"Tour {GameManager.Instance.CurrentTurn}";
    }

    // Unité sélectionnée et points restants ; texte réécrit seulement s'il change.
    private void Update()
    {
        if (unitPanel == null) return;
        Unit unit = SelectionController.Instance.SelectedUnit;
        string name = unit == null ? "" :
            $"{UnitName(unit)}{(unit.type != null ? $" ({unit.type.Category.DisplayName()})" : "")}";
        string move = unit == null ? "" :
            $"Mouvement {unit.MovementPoints}/{(unit.type != null ? unit.type.MovementPoints : 0)}";
        string text = name + "|" + move;
        if (text == lastUnitText) return;
        lastUnitText = text;
        unitNameLabel.text = name;
        unitMoveLabel.text = move;
        unitPanel.EnableInClassList("hidden", unit == null);
    }

    private static string UnitName(Unit unit) =>
        unit.type != null && !string.IsNullOrEmpty(unit.type.Name) ? unit.type.Name : unit.name;
}
