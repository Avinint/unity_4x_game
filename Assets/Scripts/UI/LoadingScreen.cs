using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

// Écran couvrant pendant la génération et l'instanciation des cases : on ne voit pas la carte se créer.
// Il disparaît quand la partie est prête (GameManager.MapReady), vue déjà centrée sur les unités du joueur local.
[RequireComponent(typeof(UIDocument))]
public class LoadingScreen : MonoBehaviour
{
    private VisualElement screen, fill;
    private Label label;
    private HexGrid grid;
    private float progress;
    private float startTime;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        screen = root.Q<VisualElement>("loadingScreen");
        fill = root.Q<VisualElement>("loadingFill");
        label = root.Q<Label>("loadingLabel");

        grid = FindAnyObjectByType<HexGrid>();
        if (grid != null) grid.OnTileBatchGenerated += OnBatch;
        if (GameManager.Instance != null) GameManager.Instance.MapReady += OnMapReady;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        startTime = Time.realtimeSinceStartup;
        LoadTimer.Reset();
        Show(0f);
    }

    private void OnDisable()
    {
        if (grid != null) grid.OnTileBatchGenerated -= OnBatch;
        if (GameManager.Instance != null) GameManager.Instance.MapReady -= OnMapReady;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Après les OnEnable : PlayerInput réactive ses actions dans le sien.
    private void Start() => GameInput.SetEnabled(false);

    private void OnBatch(float value) => Show(value);

    private void OnLocaleChanged(UnityEngine.Localization.Locale _) => Show(progress);

    private void Show(float value)
    {
        progress = value;
        fill.style.width = Length.Percent(value * 100f);
        label.text = L10n.Get("UI", "loading.map", Mathf.RoundToInt(value * 100f));
    }

    private void OnMapReady() => StartCoroutine(Hide());

    // Deux images d'attente : la caméra vient de bouger, les colonnes du monde qui boucle et le survol
    // se recalculent dans LateUpdate ; sans elles, on voyait la carte se replacer.
    private IEnumerator Hide()
    {
        Show(1f);
        yield return null;
        yield return null;
        screen.style.display = DisplayStyle.None;
        GameInput.SetEnabled(true);
        Debug.Log(LoadTimer.Summary(Time.realtimeSinceStartup - startTime));
    }
}
