# Fief (Fantasy Fiefdoms)

Jeu 4X heroic fantasy réaliste sous Unity. Langue de travail : français, réponses brèves et précises.

## Méthode de travail
- Avant toute écriture (code, scène, CLAUDE.md), montrer les changements prévus (diff ou extraits avant/après, fichiers concernés) et attendre la validation explicite de l'utilisateur. Pas d'écriture avant le feu vert.
- Travailler par petites étapes, une à la fois, et vérifier en jeu entre deux étapes.
- Ne jamais commiter : l'utilisateur commite lui-même.

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
- `Grid/HexGrid` : crée les `HexTile` en thread (`Task.Run`), voisins (`SetNeighbours`), instanciation par lots (coroutine), placement des joueurs par `DispatchPlayers` (point le plus éloigné : 1er au hasard, puis un des 10 % de cases de terre les plus éloignées des joueurs déjà placés), `TryGetTile(x, y, out tile)` (remplace l'ancien `GetTile`, supprimé).
- `Grid/HexTile` : plus de machine à états. Deux drapeaux indépendants : `IsDiscovered` (brouillard, `Discover()` révèle une seule fois) et `IsHovered` (survol, animation LeanTween). La sélection (unité, recentrage caméra) est dans `SelectionController`.
- `MouseController` : raycasts souris -> événements `OnMouseMoving`, `OnLeftMouseClick`, etc.
- `CameraController` (Overhead / Focus) et `OrbitCameraWithTime`.
- `Unit` + `Unit/State/*` : états d'unité (coquilles vides).
- `Grid/PathFinder` : `FindPath` écrit, pas encore appelé.

## État du jeu
- Fait : génération de carte, brouillard (cases Hidden/Visible), placement des joueurs, caméras.
- Pas branché : boucle de tour (`TurnChange` sans abonné, `MaxTurns` inutilisé), déplacement des unités, pathfinding, ressources.

## Points fragiles connus
- `BaseUnitState` : classe simple (plus de `Singleton`/`MonoBehaviour`), états créés avec `new` comme les états de case. `Unit` n'a pas encore d'état courant ni de `ChangeState` (à écrire avec le déplacement).
- `Player` n'est jamais déplacé sur sa case de départ (sa `transform` n'est pas utilisée) : seules les unités le sont. Optionnel : `player.transform.position = tile.CentrePosition` dans `InitPlayers`.
- Avertissements restants : `PathNode`/`Player` (`HexTile` non sérialisable), `StartingArea` dictionnaire sans `[SerializeField]`.

## À modifier après les tests (survol des cases)
- `HexGrid.OnMouseMove` : une souris hors d'une case du jeu (UI, autre objet) est un cas NORMAL : ignorer sans message (pas de LogError). (`TryGetValue` déjà en place.)

## À faire (révélation des zones de départ)
- Pour l'instant, tous les joueurs révèlent leur zone (`GameManager.InitPlayers` -> `SetVisibleTiles`, et `tile.Discover()` dans `HexGrid.DispatchPlayers`) : sert à vérifier la répartition des 5 joueurs. En partie normale, seul le joueur local doit révéler sa zone.
- Ajouter `LocalPlayer` dans `GameManager` et une case `RevealAllPlayers` (débogage) : ne révéler que le joueur local sauf si la case est cochée. Les unités et zones des autres joueurs restent créées.

## Historique migration
Unity 2022.3.46f1 -> Unity 6, Built-in -> URP, Cinemachine 2 -> 3. Notes détaillées dans le projet Claude « Fief » (`claude/migration-unity6.md`).
