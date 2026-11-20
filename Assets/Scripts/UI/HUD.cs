using UnityEngine;
using UnityEngine.InputSystem;
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

    [SerializeField] private float tileInfoDelay = 0.4f;   // secondes sans changer de case avant d'afficher
    private VisualElement tileInfo;
    private HexTile tileInfoTile;
    private float tileInfoSince;
    private bool tileInfoFilled;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        turnLabel = root.Q<Label>("turnLabel");
        unitPanel = root.Q("unitPanel");
        unitNameLabel = root.Q<Label>("unitNameLabel");
        unitMoveLabel = root.Q<Label>("unitMoveLabel");
        playerSwatch = root.Q("playerSwatch");
        tileInfo = root.Q("tileInfo");
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
    private void OnLocaleChanged(Locale locale)
    {
        Refresh();
        tileInfoFilled = false;
    }

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
        UpdateTileInfo();
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

    // Fenêtre d'informations de la case survolée : case explorée, fenêtre du jeu active, pas de glissé de carte,
    // après tileInfoDelay sans changer de case ; à côté du curseur, gardée dans l'écran.
    private void UpdateTileInfo()
    {
        if (tileInfo == null) return;
        HexGrid grid = GameManager.Instance.HexGrid;
        HexTile tile = grid != null ? grid.HoveredTile : null;
        if (tile != tileInfoTile) { tileInfoTile = tile; tileInfoSince = Time.unscaledTime; tileInfoFilled = false; }

        bool show = tile != null && tile.IsDiscovered && Application.isFocused && Mouse.current != null
                    && !CameraController.Instance.IsDragging && Time.unscaledTime - tileInfoSince >= tileInfoDelay;
        tileInfo.EnableInClassList("hidden", !show);
        if (!show) return;
        if (!tileInfoFilled) { FillTileInfo(grid, tile); tileInfoFilled = true; }

        // Écran (origine en bas à gauche) → panneau (origine en haut à gauche, 1920×1080 mis à l'échelle).
        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector2 p = RuntimePanelUtils.ScreenToPanel(tileInfo.panel, new Vector2(mouse.x, Screen.height - mouse.y));
        var area = tileInfo.parent.layout;
        float width = tileInfo.resolvedStyle.width, height = tileInfo.resolvedStyle.height;
        float left = p.x + 16f, top = p.y + 16f;
        if (left + width > area.width) left = p.x - 16f - width;     // trop à droite : à gauche du curseur
        if (top + height > area.height) top = p.y - 16f - height;    // trop bas : au-dessus du curseur
        tileInfo.style.left = left;
        tileInfo.style.top = top;
    }

    private void FillTileInfo(HexGrid grid, HexTile tile)
    {
        bool river = tile.Neighbours != null
                     && tile.Neighbours.Exists(n => grid.HasRiver(tile.AxialCoordinates, n.AxialCoordinates));
        var lines = TileInfo.Describe(tile.Relief, tile.Biome, tile.MovementCost, river, grid.HasFreshWater(tile));
        tileInfo.Clear();
        for (int i = 0; i < lines.Count; i++)
        {
            var label = new Label(L10n.Get(lines[i].table, lines[i].key, lines[i].args)) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("tile-info-line");
            if (i == 0) label.AddToClassList("tile-info-title");   // première ligne : le terrain
            tileInfo.Add(label);
        }
    }

    // Nom provisoire traduit, « Joueur 1 »… (numéro dans GameManager.Players), en attendant les noms de nations.
    private static string PlayerName(Player player) =>
        L10n.Get("UI", "player.default_name", GameManager.Instance.Players.IndexOf(player) + 1);

    // Nom traduit : "unit." + nom du type en minuscules (Warrior → unit.warrior).
    private static string UnitName(Unit unit) => L10n.Get("Units", "unit." + unit.type.Name.ToLower());
}
