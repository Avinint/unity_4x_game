using UnityEngine;
using UnityEngine.UIElements;

// Affiche le numéro de tour ; mis à jour à chaque TurnChange.
[RequireComponent(typeof(UIDocument))]
public class HUD : MonoBehaviour
{
    private Label turnLabel;

    private void OnEnable()
    {
        turnLabel = GetComponent<UIDocument>().rootVisualElement.Q<Label>("turnLabel");
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
}
