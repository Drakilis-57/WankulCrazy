# Duel Wankul TCG

> **Fichiers sources pertinents** : `duel/engine/*`, `duel/view/*`, `duel/*.cs`, `patch/Wankul*Patch.cs`, `patch/Duel*Patch.cs`, `data/customdecks/duel_card_effects.json`, `Docs/TCG_Wankul.md`

## Objectif
Remplace le mini-jeu vanilla (Tetramon) par un duel **Wankul TCG** : deck de 50 cartes (10 Terrains + 40 Personnages), 3 terrains max en jeu, victoire à **5 duels gagnés** ou par **meule** (pioche adverse vide). Règles complètes : `Docs/TCG_Wankul.md`.

## Architecture (3 couches)
| Couche | Dossier | Rôle |
|---|---|---|
| Moteur | `duel/engine/` | C# pur (aucun `UnityEngine`/Harmony). `DuelState` = seul état de vérité, `DuelEngine` = règles, `IEffect` = effets, `DuelAI` = IA, `IRandom` = aléatoire injecté, `DuelEvents` = événements émis. |
| Vue | `duel/view/` | `DuelView2D` (rendu 2D), `DuelSfx` (sons). S'abonne aux événements du moteur ; le moteur ignore la vue. |
| Adaptateurs | `duel/` | `CardAdapter` (CardData → DuelCard, effets lus depuis JSON), `WankulDuelController`, `WankulBoardState`, `WankulDuelUI`, `WankulEnemyAI`, `DuelViewLogger`. |

## Effets de cartes
Les mappings carte → effets sont externalisés dans `data/customdecks/duel_card_effects.json` (aucun nom de carte codé en dur dans le moteur). `CardAdapter.ResolveEffectIds` ne lit que ce JSON : **une carte absente du fichier n'a aucun effet en duel** (seul un bonus de force « combo » est déduit par regex du texte). `EffectRegistry` (dans `IEffect.cs`) enregistre 92 effets ; les 58 identifiants du JSON sont tous enregistrés. `MarkerEffect` sert aux effets passifs lus ailleurs dans le moteur.

### Couverture actuelle (cartes avec texte de règle vs cartes mappées)
| Saison | Cartes avec texte | Mappées | Sans mapping |
|---|---|---|---|
| S01 Origins | 178 | 176 | 4 |
| S02 Campus | 138 | 48 | 90 |
| S03 Battle | 166 | 44 | 123 |
| S04 Stellar | 160 | 54 | 109 |
| S05 Legacy | 174 | 47 | 128 |
| HS | 0 | 0 | 0 |

Total : 369 mappings. Seule Origins est quasi complète ; les autres saisons n'ont qu'une partie de leurs effets (terrains, effets de victoire/défaite, personnages à effet ciblé comme « La Hache », « Pharaon », « Hypnotiseur »…). Les cartes concernées sont listées dans le rapport de mise à jour du wiki.

## Patches Harmony liés
- `DuelOpponentDeckPatch` : l'IA reçoit un deck Wankul de 50 cartes, saison choisie selon le niveau du magasin (S01→S05).
- `DuelRewardsPatch` : récompenses de duel.
- `WankulDuelEntryPatch`, `WankulDuelLifecyclePatch`, `WankulRootDuelPatch` : entrée et cycle de vie du duel.
- `WankulPlayCardEffectPatch`, `WankulPlayerActionPatch` : actions et effets joués.
- `WankulMillWinConditionPatch` : victoire par meule.

## Corrections notables
- `PopBottomDeckCard` retire en fin de liste ; `LookTopDeckEffect` lit l'index 0.
- Début de tour : pioche de 2 cartes ; pose forcée d'un terrain si ≤ 1 en jeu.

## Tests
Suites xUnit dans `WankulCrazyPlugin.Tests` (moteur, effets JSON, pop bottom deck). Voir [Suite de Tests](Suite-de-Tests).
