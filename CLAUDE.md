# Fief (Fantasy Fiefdoms)

Jeu 4X heroic fantasy réaliste sous Unity. Langue de travail : français, réponses brèves et précises.

## Environnement
- Unity 6000.6.4f1, Windows. Rendu : URP (`Assets/Settings/URP-HighFidelity.asset`).
- Cinemachine 3 (`Unity.Cinemachine`) : `CinemachineCamera`, `CinemachineOrbitalFollow`, `Lens`.
- Input : ancien `Input` (MouseController) + Input System (`PlayerControls.inputactions`).
- MCP Unity : `unity mcp configure claude-code --project-path "M:\Unity_Projects\Fantasy Fiefdoms"` (Editor ouvert sur le projet, package `com.unity.pipeline`).
- Ne pas éditer `.unity`/`.prefab` à la main si l'Editor est ouvert : passer par les outils MCP.

## Architecture (Assets/Scripts)
- `Singleton<T>` : base des managers (DontDestroyOnLoad seulement si l'objet est à la racine).
- `GameManager` : liste de `Player`, `InitPlayers` appelé sur `HexGrid.OnTileInstancesGenerated`, place les joueurs et unités de départ, événements `GameStart` / `TurnChange`.
- `Grid/MapGenerator` : bruit -> `TerrainType[,]`, événement `OnTerrainMapGenerated`.
- `Grid/HexGrid` : crée les `HexTile` en thread (`Task.Run`), voisins (`SetNeighbours`), instanciation par lots (coroutine), placement des joueurs par quadrant, `TryGetTile`.
- `Grid/HexTile` + `Grid/Tile States` : machine à états (Hidden, Visible, Highlighted, Selected, Focused).
- `MouseController` : raycasts souris -> événements `OnMouseMoving`, `OnLeftMouseClick`, etc.
- `CameraController` (Overhead / Focus) et `OrbitCameraWithTime`.
- `Unit` + `Unit/State/*` : états d'unité (coquilles vides).
- `Grid/PathFinder` : `FindPath` écrit, pas encore appelé.

## État du jeu
- Fait : génération de carte, brouillard (cases Hidden/Visible), placement des joueurs, caméras.
- Pas branché : boucle de tour (`TurnChange` sans abonné, `MaxTurns` inutilisé), déplacement des unités, pathfinding, ressources.

## Points fragiles connus
- `HexGrid` : accès `Tiles[...]` sans test (OnMouseMove ~l.140, GetStartingTile, PlaceFirstPlayer, PlaceSubsequentPlayer) -> utiliser `TryGetValue`.
- `BaseUnitState` est un `Singleton` : un seul état partagé par toutes les unités, à revoir.
- `DispatchPlayers` : au-delà de 5 joueurs, même quadrant réutilisé.
- Avertissements restants : `PathNode`/`Player` (`HexTile` non sérialisable), `StartingArea` dictionnaire sans `[SerializeField]`.

## À modifier après les tests (survol des cases)
- `HexGrid.OnMouseMove` : une souris hors d'une case du jeu (UI, autre objet) est un cas NORMAL : ignorer sans message (pas de LogError). Utiliser `TryGetValue` au lieu de `Tiles[axial]` (~l.140) ; le test `tile != null` actuel est inutile (un Dictionary lève une exception, il ne renvoie pas null).
- Même case survolée : `OnMouseMove` fait Exit puis Enter à chaque mouvement, ce qui relance les animations et les logs. Ne rien faire si la case sous la souris est déjà `_activeTile`.
- `HighlightedState.Exit` : `CameraController.Instance.onCommandAction += tile.OnMoveUnit` doit être `-=` (abonnements cumulés à chaque sortie).
- Sortie de la grille : `MouseController.onMouseExit` est déclaré mais jamais déclenché ; la dernière case reste surélevée. À déclencher quand la souris quitte la grille ou passe sur l'UI.

## Historique migration
Unity 2022.3.46f1 -> Unity 6, Built-in -> URP, Cinemachine 2 -> 3. Notes détaillées dans le projet Claude « Fief » (`claude/migration-unity6.md`).
