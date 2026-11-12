using UnityEngine;
using UnityEngine.UIElements;

// Affiche le numéro de tour ; mis à jour à chaque TurnChange.
[RequireComponent(typeof(UIDocument))]
public class HUD : MonoBehaviour
{
    private Label turnLabel;
    private Label unitLabel;
    private string lastUnitText;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        turnLabel = root.Q<Label>("turnLabel");
        unitLabel = root.Q<Label>("unitLabel");
        lastUnitText = null;
        GameManager.Instance.TurnChange += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.TurnChange -= Refresh;
    }

    private void Refresh()
    {
        if (turnLabel != null) turnLabel.text = $"Tour {GameManager.Instance.CurrentTurn}";
    }

    // Unité sélectionnée et points restants ; texte réécrit seulement s'il change.
    private void Update()
    {
        if (unitLabel == null) return;
        Unit unit = SelectionController.Instance.SelectedUnit;
        string text = unit == null ? "" :
            $"{UnitName(unit)} — Mouvement {unit.MovementPoints}/{(unit.type != null ? unit.type.MovementPoints : 0)}";
        if (text == lastUnitText) return;
        lastUnitText = text;
        unitLabel.text = text;
        unitLabel.EnableInClassList("hidden", unit == null);
    }

    private static string UnitName(Unit unit) =>
        unit.type != null && !string.IsNullOrEmpty(unit.type.Name) ? unit.type.Name : unit.name;
}
