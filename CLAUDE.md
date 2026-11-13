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
- `GameManager` : liste de `Player`, `InitPlayers` appelé sur `HexGrid.OnTileInstancesGenerated`, place les joueurs et unités de départ, événements `GameStart` / `TurnChange`. Tour par joueur : délégué à `TurnCycle<Player>` (classe pure testée, `TurnCycleTests` : ordre avec un premier joueur, `Advance()` renvoie true en début de manche, `Round`), créé en fin d'`InitPlayers` avec le joueur local en premier, puis `PlayerTurnStart(premier joueur)` levé pour ouvrir la partie ; `TurnOrder`, `CurrentPlayer`, `CurrentTurn` (= `Round`) en lecture. Entrée (`World/ChangeTurn` -> `ChangeTurn`, seulement si `CurrentPlayer == LocalPlayer`) termine le tour : `EndPlayerTurn` passe au suivant et lève `PlayerTurnStart(joueur)` ; après le dernier, nouvelle manche (`CurrentTurn++`, `TurnChange`). Les joueurs sans IA passent leur tour aussitôt (boucle jusqu'au retour du joueur local). `AddStartingUnits` passe chaque unité à `SetupStartingUnit` : ajout à `Player.units`, `Unit.Owner`, puis `Unit.RevealAround(case)` (les unités de départ du joueur local révèlent ce qu'elles voient, au-delà de la zone de départ).
- `UI/HUD` (UI Toolkit) : objet `HUD` de la scène (`UIDocument` + `HUD.cs`), fichiers dans `Assets/UI/HUD/` (`HUD.uxml`, `HUD.uss`, `HUDTheme.tss`, `HUDPanelSettings.asset`, 1920×1080 adapté à l'écran). Bandeau en haut : à gauche l'unité sélectionnée et ses points (« Warrior — Mouvement 1/2 », lu à chaque image dans `Update`, masqué sans sélection), à droite « Tour N — Player 1 » (nom de l'objet du joueur courant), mis à jour sur `TurnChange` et `PlayerTurnStart`. Bande verticale à droite envisagée plus tard (USS seulement).
- `Grid/MapGenerator` : bruit -> `TerrainType[,]`, événement `OnTerrainMapGenerated`.
- `Grid/HexGrid` : crée les `HexTile` en thread (`Task.Run`), voisins (`SetNeighbours`), instanciation par lots (coroutine), placement des joueurs par `DispatchPlayers`, qui délègue le choix à `StartPositionPicker` (fonction pure testée : 1er au hasard, puis un des 10 % de cases de terre les plus éloignées des joueurs déjà placés), `TryGetTile(x, y, out tile)` (coordonnées offset, remplace l'ancien `GetTile`) et `TryGetTileAxial(axial, out tile)`. `RevealAround(center, radius)` découvre les cases dans un rayon (`HexMetrics.AxialRange`, fonction pure testée).
- `Grid/HexTile` : plus de machine à états. Deux drapeaux indépendants : `IsDiscovered` (brouillard, `Discover()` révèle une seule fois) et `IsHovered` (survol, affiché par le marqueur blanc `HexGrid.HoverMarker`, masqué sur la case sélectionnée). Unités sous le brouillard : `AddUnit` et `Discover` appellent `Unit.SetVisible(IsDiscovered)` (renderers masqués, objet actif) ; testé dans `HexTileTests`.
- `SelectionController` (Singleton créé par `GameManager`) : écoute les actions de `CameraController`. Clic gauche : sélectionne la première unité de la case survolée (case vide : désélection). Clic droit (`Interact`) : déplace l'unité sélectionnée par chemin (`PathFinder`, cases de terre découvertes), coupé à ses points de mouvement, seulement pour une unité du joueur courant (celles des autres se sélectionnent mais ne bougent pas) ; elle change de case (celle où elle s'arrête) dès le départ (`HexTile.RemoveUnit`/`AddUnit`) ; commande ignorée pendant le trajet. Échap / Retour arrière : désélection. `HexGrid.Selector` suit l'unité à chaque image (`Update`). Aperçu du trajet au survol (`PathPreview`, enfant créé dans `Init`) : deux `LineRenderer` (shader `Sprites/Default`), clair pour la partie atteignable ce tour-ci, gris pour la suite ; case survolée interdite (eau, montagne) : chemin jusqu'à elle tout en rouge (troisième ligne, `ShowForbidden`), rien si même ce chemin n'existe pas ; masqué sans unité commandable (`CanCommand`) ; recalculé seulement si cible, origine ou points changent. `PathTo` partagé entre l'aperçu et `Command`. Pas de recentrage de la caméra au clic.
- `MouseController` : raycasts souris -> événements `OnMouseMoving`, `OnLeftMouseClick`, etc.
- `CameraController` (Overhead / Focus) et `OrbitCameraWithTime`. Overhead : vue en perspective inclinée, nord en haut (`CinemachineFollow` en `WorldSpace`, décalage `(0, d·sin(pitch), −d·cos(pitch))` posé par `SetDistance`, `HardLookAt` sur `CameraTarget`). Réglages : `pitch` 55°, `fieldOfView` 30°, distance 40–250 (défaut 120), `distanceZoomSpeed` (A/E, continu), `scrollZoomStep` (molette : 15 % de la distance par cran, molette vers le haut = éloigner, choix de l'utilisateur). Le zoom change la distance, plus le FOV. Pan (flèches, axes monde) borné : `ClampTarget` garde `CameraTarget` dans `HexGrid.WorldBounds` (rectangle des centres des cases, calculé par `HexMetrics.GridBounds`, fonction pure testée).
- `Unit` + `Unit/State/*` : états d'unité (Idle / Selected utilisés, les autres vides). `MoveAlong(path)` (liste de `HexTile`) : déplacement animé case par case (`moveSpeed`, drapeau `IsMoving`, sans passer par `MovingUnitState`), coûte 1 point par case ; à chaque case atteinte, une unité du joueur local révèle les cases dans `UnitType.SightRadius` (2 par défaut). `MovementPoints` vient de `UnitType.MovementPoints` (2 par défaut), rechargé par `ResetMovementPoints` au démarrage et au début du tour de son propriétaire (`Owner`, événement `PlayerTurnStart`, abonnement dans `OnEnable`/`OnDisable`).
- `Grid/PathFinder` : A* pur sur coordonnées axiales, `FindPath(origin, destination, isPassable)`, coût 1 par case ; renvoie le chemin sans la case de départ, liste vide si même case, null si inaccessible. Testé (`PathFinderTests`).

## État du jeu
- Fait : génération de carte, brouillard (`IsDiscovered`), placement des joueurs, caméras, survol et sélection d'unité (marqueurs), déplacement par chemin animé, points de mouvement, tour par joueur (ordre fixe, joueur local d'abord, joueurs sans IA passés automatiquement), HUD (numéro de tour, joueur courant, unité sélectionnée et points restants), exploration (vision des unités), aperçu du chemin au survol.
- Ordre prévu : exploration avant la fondation des villes.
- Pas branché : IA des autres joueurs, fin de partie (`MaxTurns` inutilisé), coût selon le terrain, ressources.

## Points fragiles connus
- `BaseUnitState` : classe simple (plus de `Singleton`/`MonoBehaviour`), états créés avec `new`. `Unit` a un `State` et un `ChangeState` (Idle / Selected utilisés, les autres états sont vides).
- `GameManager.Units` (dictionnaire par case) n'est rempli qu'au placement de départ et n'est lu nulle part : ne pas s'y fier.
- `Player` n'est jamais déplacé sur sa case de départ (sa `transform` n'est pas utilisée) : seules les unités le sont. Optionnel : `player.transform.position = tile.CentrePosition` dans `InitPlayers`.
- Avertissements restants : `Player` (`HexTile` non sérialisable), `StartingArea` dictionnaire sans `[SerializeField]`.

## Révélation des zones de départ
- `GameManager.LocalPlayer` (champ `localPlayer`, par défaut le premier de `Players`) et `RevealAllPlayers` (coché par défaut, débogage). Décoché, seule la zone du joueur local est révélée et la caméra démarre sur sa case.
- Les unités posées sur des cases cachées sont masquées (voir `HexTile`).
- Exploration : les unités du joueur local révèlent les cases autour d'elles en se déplaçant (on ne vise et ne traverse encore que des cases découvertes).
- Plus tard : relief et visibilité (ligne de vue). Idée : `TerrainType` reçoit une altitude et un « bloque la vue » (montagne, forêt) ; une unité en hauteur voit plus loin ; fonction pure testée qui trace la ligne hexagonale entre deux cases et s'arrête au premier obstacle plus haut que l'observateur. Remplacera `AxialRange` dans `RevealAround`.
- À faire : distinguer case explorée et case vue en ce moment (une unité ennemie sur une case explorée reste visible pour l'instant).

## À faire (caméra)
- Vue inclinée en perspective choisie (l'orthographique vu de dessus avait été essayé : le marqueur vertical n'y était vu que par sa tranche). À régler en jeu : `pitch`, distances.
- Plus tard, au choix : inclinaison qui varie avec le zoom (plus à plat de près), rotation par pas de 60° (le pan devra suivre l'orientation).
- Revérifier la lisibilité de `Selector` et `hoverMarker`. Si besoin, les remplacer par un contour d'hexagone à plat (`LineRenderer` sur `HexMetrics.Corners`).

## Historique migration
Unity 2022.3.46f1 -> Unity 6, Built-in -> URP, Cinemachine 2 -> 3. Notes détaillées dans le projet Claude « Fief » (`claude/migration-unity6.md`).
