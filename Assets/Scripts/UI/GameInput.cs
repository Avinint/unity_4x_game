using UnityEngine;
using UnityEngine.InputSystem;

// Commandes du jeu (actions, survol, défilement par les bords), coupées par un écran couvrant
// (menu de la partie, écran de chargement).
public static class GameInput
{
    public static void SetEnabled(bool on)
    {
        var input = Object.FindAnyObjectByType<PlayerInput>();
        if (input != null)
        {
            if (on) input.ActivateInput();
            else input.DeactivateInput();
        }
        if (CameraController.Instance != null) CameraController.Instance.IsLocked = !on;
    }
}
