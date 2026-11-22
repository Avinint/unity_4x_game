using UnityEngine;
using UnityEngine.UIElements;

// La souris est-elle sur un élément d'interface qui reçoit les clics (bouton, console, menu) ?
// Les conteneurs en picking-mode="Ignore" (bande du HUD, fenêtre de la case) laissent passer vers la carte.
public static class UIPointer
{
    public static bool IsOver(Vector2 screen)
    {
        foreach (var doc in Object.FindObjectsByType<UIDocument>())
        {
            IPanel panel = doc.rootVisualElement?.panel;
            if (panel == null) continue;
            // Écran : y vers le haut ; panneau : y vers le bas.
            Vector2 p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            if (panel.Pick(p) != null) return true;
        }
        return false;
    }
}
