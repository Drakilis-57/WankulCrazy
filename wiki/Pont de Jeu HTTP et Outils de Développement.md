# Pont de Jeu HTTP et Outils de Développement

> **Fichiers sources** : `importer/WankulGameBridge.cs`, `importer/WankulDevTools.cs`, `tools/mcp_game_server.py`, `tools/game_bridge.py`

## WankulGameBridge
Serveur HTTP local (`http://127.0.0.1:8765/`) lancé par le plugin pour inspecter et piloter le jeu en direct. Les actions sont exécutées sur le thread principal Unity via une file `ConcurrentQueue`.

| Route | Description |
|---|---|
| `GET /status` | Vérifie que le jeu et le pont tournent |
| `GET /held_item` | Détails de l'objet en main (mesh, matériaux, UVs) |
| `GET /target` | Objet visé au centre de l'écran |
| `POST /reload_textures` | Rechargement à chaud des textures Wankul |
| `POST /screenshot` | Capture d'écran, renvoie le chemin |
| `POST /dump_mesh` | Export `.obj` du mesh + UVs (même non lisible) |
| `POST /dump_texture` | Export PNG de n'importe quelle texture |
| `POST /render_item` | Rendu 3D 4 vues (face, 3/4, dessus, dos) en grille 2x2 |
| `GET /logs` | 200 derniers logs, filtrables par level/keyword |

## Serveur MCP
`tools/mcp_game_server.py` expose ces routes comme outils MCP (client Python : `tools/game_bridge.py`). Le jeu doit être lancé, sinon une erreur « le jeu ne répond pas » est retournée.

## WankulDevTools
Outils in-game (raccourcis F8, F9, F10 ; inspecteur avec Alt gauche) pour le hot-reload et l'inspection.

## Scripts d'outillage (`tools/`)
- `convert_layout_107.py` : convertit les textures Displays/Deckboxes vers le gabarit en croix 2048×2048 de la v1.07.
- `generate_uv_blueprint.py` : génère des plans UV.
- `generate_pricetag_atlas.py` : injecte les icônes Wankul dans l'atlas d'étiquettes. Voir [Compatibilité v1.07](Compatibilité-v1.07-et-Pack-Opener).
