# Fief (Fantasy Fiefdoms)

Jeu 4X heroic fantasy réaliste sous Unity. Langue de travail : français, réponses brèves et précises.

## Méthode de travail
- Avant toute écriture (code, scène), montrer les changements prévus (diff ou extraits avant/après, fichiers concernés) et attendre la validation explicite de l'utilisateur. Pas d'écriture avant le feu vert.
- CLAUDE.md et ajouts à `.gitignore` : écrire directement, sans montrer le diff ; résumer en une ligne ce qui a changé.
- Mettre CLAUDE.md à jour dans la même étape que le changement qu'il décrit (pas après coup), pour qu'il parte dans le même commit.
- Travailler par petites étapes, une à la fois, et vérifier en jeu entre deux étapes.
- Ne jamais commiter : l'utilisateur commite lui-même.

## Environnement
- Unity 6000.6.4f1, Windows. Rendu : URP (`Assets/Settings/URP-HighFidelity.asset`).
- Cinemachine 3 (`Unity.Cinemachine`) : `CinemachineCamera`, `CinemachineOrbitalFollow`, `Lens`.
- Input : ancien `Input` (MouseController) + Input System (`PlayerControls.inputactions`).
- MCP Unity (Editor ouvert sur le projet, package `com.unity.pipeline`), selon l'ordinateur :
  - `M:\Unity_Projects\Fantasy Fiefdoms` : `unity mcp configure claude-code --project-path "M:\Unity_Projects\Fantasy Fiefdoms"`.
  - `C:\Users\avini\UnityProject\unity_4x_game` : entrée locale `claude mcp add --scope local --transport stdio unity-editor-mcp -- unity mcp --project-path "C:\Users\avini\UnityProject\unity_4x_game"` (ne pas utiliser `unity mcp configure` ici : il écrase l'entrée utilisateur du projet FPS).
- Sortir du mode Play avant une recompilation ou un appel MCP : l'Editor ne répond plus pendant ce temps.
- Touches simulées par MCP (`simulate_key`) : n'atteignent pas les actions du jeu (la vue Game n'a pas le focus) ; tester le clavier à la main.
- Tests EditMode (`Assets/Tests/EditMode`) : `unity test` refuse de tourner si l'Editor est ouvert sur le projet ; les lancer depuis le Test Runner ou par MCP (`eval` avec `TestRunnerApi`).
- Ne pas éditer `.unity`/`.prefab` à la main si l'Editor est ouvert : passer par les outils MCP.

## Architecture (Assets/Scripts)
- `Singleton<T>` : base des managers (DontDestroyOnLoad seulement si l'objet est à la racine).
- `GameManager` : liste de `Player`, `InitPlayers` appelé sur `HexGrid.OnTileInstancesGenerated`, place les joueurs et unités de départ, événements `GameStart` / `TurnChange`. `CurrentTurn` (commence à 1) ; Entrée (`World/ChangeTurn` -> `ChangeTurn`) l'incrémente puis lève `TurnChange`.
- `UI/HUD` (UI Toolkit) : objet `HUD` de la scène (`UIDocument` + `HUD.cs`), fichiers dans `Assets/UI/HUD/` (`HUD.uxml`, `HUD.uss`, `HUDTheme.tss`, `HUDPanelSettings.asset`, 1920×1080 adapté à l'écran). Bandeau en haut : à gauche l'unité sélectionnée et ses points (« Warrior — Mouvement 1/2 », lu à chaque image dans `Update`, masqué sans sélection), à droite « Tour N » mis à jour sur `TurnChange`. Bande verticale à droite envisagée plus tard (USS seulement).
- `Grid/MapGenerator` : bruit -> `TerrainType[,]`, événement `OnTerrainMapGenerated`.
- `Grid/HexGrid` : crée les `HexTile` en thread (`Task.Run`), voisins (`SetNeighbours`), instanciation par lots (coroutine), placement des joueurs par `DispatchPlayers`, qui délègue le choix à `StartPositionPicker` (fonction pure testée : 1er au hasard, puis un des 10 % de cases de terre les plus éloignées des joueurs déjà placés), `TryGetTile(x, y, out tile)` (coordonnées offset, remplace l'ancien `GetTile`) et `TryGetTileAxial(axial, out tile)`.
- `Grid/HexTile` : plus de machine à états. Deux drapeaux indépendants : `IsDiscovered` (brouillard, `Discover()` révèle une seule fois) et `IsHovered` (survol, affiché par le marqueur blanc `HexGrid.HoverMarker`, masqué sur la case sélectionnée).
- `SelectionController` (Singleton créé par `GameManager`) : écoute les actions de `CameraController`. Clic gauche : sélectionne la première unité de la case survolée (case vide : désélection). Clic droit (`Interact`) : déplace l'unité sélectionnée par chemin (`PathFinder`, cases de terre découvertes), coupé à ses points de mouvement ; elle change de case (celle où elle s'arrête) dès le départ (`HexTile.RemoveUnit`/`AddUnit`) ; commande ignorée pendant le trajet. Échap / Retour arrière : désélection. `HexGrid.Selector` suit l'unité à chaque image (`Update`). Pas de recentrage de la caméra au clic.
- `MouseController` : raycasts souris -> événements `OnMouseMoving`, `OnLeftMouseClick`, etc.
- `CameraController` (Overhead / Focus) et `OrbitCameraWithTime`. Overhead : vue en perspective inclinée, nord en haut (`CinemachineFollow` en `WorldSpace`, décalage `(0, d·sin(pitch), −d·cos(pitch))` posé par `SetDistance`, `HardLookAt` sur `CameraTarget`). Réglages : `pitch` 55°, `fieldOfView` 30°, distance 40–250 (défaut 120), `distanceZoomSpeed` (A/E, continu), `scrollZoomStep` (molette : 15 % de la distance par cran, molette vers le haut = éloigner, choix de l'utilisateur). Le zoom change la distance, plus le FOV. Pan inchangé (flèches, axes monde).
- `Unit` + `Unit/State/*` : états d'unité (Idle / Selected utilisés, les autres vides). `MoveAlong(points)` : déplacement animé case par case (`moveSpeed`, drapeau `IsMoving`, sans passer par `MovingUnitState`), coûte 1 point par case. `MovementPoints` vient de `UnitType.MovementPoints` (2 par défaut), rechargé par `ResetMovementPoints` au démarrage et sur `TurnChange` (abonnement dans `OnEnable`/`OnDisable`).
- `Grid/PathFinder` : A* pur sur coordonnées axiales, `FindPath(origin, destination, isPassable)`, coût 1 par case ; renvoie le chemin sans la case de départ, liste vide si même case, null si inaccessible. Testé (`PathFinderTests`).

## État du jeu
- Fait : génération de carte, brouillard (`IsDiscovered`), placement des joueurs, caméras, survol et sélection d'unité (marqueurs), déplacement par chemin animé, points de mouvement, passage du tour avec Entrée (toutes les unités rechargées), HUD (numéro de tour, unité sélectionnée et points restants).
- Pas branché : tour par joueur (joueur courant, gardé pour la fin), fin de partie (`MaxTurns` inutilisé), coût selon le terrain, aperçu du chemin, ressources.

## Points fragiles connus
- `BaseUnitState` : classe simple (plus de `Singleton`/`MonoBehaviour`), états créés avec `new`. `Unit` a un `State` et un `ChangeState` (Idle / Selected utilisés, les autres états sont vides).
- `GameManager.Units` (dictionnaire par case) n'est rempli qu'au placement de départ et n'est lu nulle part : ne pas s'y fier.
- `Player` n'est jamais déplacé sur sa case de départ (sa `transform` n'est pas utilisée) : seules les unités le sont. Optionnel : `player.transform.position = tile.CentrePosition` dans `InitPlayers`.
- Avertissements restants : `Player` (`HexTile` non sérialisable), `StartingArea` dictionnaire sans `[SerializeField]`.

## Révélation des zones de départ
- `GameManager.LocalPlayer` (champ `localPlayer`, par défaut le premier de `Players`) et `RevealAllPlayers` (coché par défaut, débogage). Décoché, seule la zone du joueur local est révélée et la caméra démarre sur sa case.
- À faire : les unités des autres joueurs sont créées même sur des cases cachées, leur modèle reste visible au-dessus du brouillard (à masquer plus tard).

## À faire (caméra)
- Vue inclinée en perspective choisie (l'orthographique vu de dessus avait été essayé : le marqueur vertical n'y était vu que par sa tranche). À régler en jeu : `pitch`, distances.
- Plus tard, au choix : inclinaison qui varie avec le zoom (plus à plat de près), rotation par pas de 60° (le pan devra suivre l'orientation), limiter `CameraTarget` aux bords de la carte.
- Revérifier la lisibilité de `Selector` et `hoverMarker`. Si besoin, les remplacer par un contour d'hexagone à plat (`LineRenderer` sur `HexMetrics.Corners`).

## Historique migration
Unity 2022.3.46f1 -> Unity 6, Built-in -> URP, Cinemachine 2 -> 3. Notes détaillées dans le projet Claude « Fief » (`claude/migration-unity6.md`).
