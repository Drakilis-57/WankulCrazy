---
trigger: always_on
---

# Directives d'architecture & Modding TCG Card Shop Simulator

## 1. Gestion des Objets Custom & Catalogue Boutique
- **Idempotence & Anti-doublons** : Toujours vérifier si un `EItemType` ou une licence `RestockData` existe déjà dans les listes (`m_ShownItemType`, `m_RestockDataList`, etc.) avant de l'ajouter. Ne jamais faire de `Add` sans garde `!Contains`.
- **Répartition stricte par onglet** :
  - `m_ShownItemType` : Boosters, Displays, Starters/Precons uniquement.
  - `m_ShownFigurineItemType` : Figurines, Peluches, Caleçons.
  - `m_ShownAccessoryItemType` : Tapis de jeu (Playmats), Classeurs (Binders), Sleeves.
- **Positionnement thématique** :
  - Positionner les objets custom normaux immédiatement après les items normaux du jeu de niveau équivalent (ex: après Battle / `EpicCardBox`).
  - Positionner les variantes Taux de drop immédiatement après les variantes Taux originales (`DestinyEpicCardBox`).
  - Aligner l'ordre de `m_RestockDataList` sur celui du catalogue pour une progression cohérente.

## 2. Configuration des Prix & Niveaux
- **Licences & Réputation** : Configuré dans `data/customitems/restockDataList.json` (`licenseShopLevelRequired` et `licensePrice`).
- **Prix de base fournisseur & marges** : Configuré dans `data/customitems/ItemDataList.json` (`baseCost`, `marketPriceMinPercent`, `marketPriceMaxPercent`).
- **Cartes individuelles (Singles)** : Géré dynamiquement par algorithme dans `patch/CardPrice.cs`.

## 3. Débogage & Rendu
- Si un comportement de shop ou de licence diverge, privilégier des logs exhaustifs de l'ordre des listes réelles au runtime plutôt que de simples suppositions.
- Prêter attention au mapping des textures 3D (ne pas assigner une texture de Display sur un modèle de Booster).

## 4. Performance & Réflexion
- Ne jamais faire `AccessTools.Field/Method` ou `Type.GetField` dans une méthode appelée par frame (`Update`) ou en boucle sur les cartes — toujours passer par le cache existant (`Plugin.GetCachedMethod`, `GetCachedField`) ou en ajouter un si besoin.
- Pas de `Enum.GetValues(typeof(X))` répété à chaque appel : le mettre en `static readonly` (voir `CachedSeasons`, `CachedExpansions`, `CachedBorders`).
- Toute opération sur `WankulCardsData.Instance.cards` (900+ cartes) appelée fréquemment doit passer par un index précalculé (`cardsBySeason`, `parsedKeyCache`, `reverseAssociation`), jamais un `List.FindAll`/`Find` brut sur toute la collection.

## 5. Shaders / Rendu
- Ne jamais faire `new Material(Shader.Find("Standard"))` — ce shader est cassé/strippé en HDRP/URP dans ce build et produit un rendu magenta silencieux (pas d'exception). Toujours passer par `ShaderUtils.CreateSafeMaterial()` / `ShaderUtils.EnsureNotBrokenStandard()`.

## 6. Harmony Patches
- Toujours enregistrer les patches manuels via le helper `TryPatch` dans `Plugin.cs` (garde contre `MethodInfo` null) plutôt qu'un `harmony.Patch(...)` direct — sinon un changement de signature côté jeu de base fait planter tout le chargement du plugin.
- Un prefix qui retourne `false` (remplace totalement la méthode d'origine, ex: `CardOpening.Update`) doit être traité avec une extrême prudence : toute exception non catchée dedans peut bloquer le joueur à chaque frame. Wrapper la logique complexe dans try/catch (cf. `OpenBooster` qui délègue à `OpenBoosterCore` sous try/catch).

## 7. Enums Custom (EItemType, ECollectionPackType, EMonsterType)
- Ne jamais écrire une valeur custom en dur (`EItemType.MaValeur`) — toujours passer par `EnumExtensions.SafeParseEItemType("...")` / `SafeParseECollectionPackType("...")`, car ces valeurs n'existent pas dans l'enum natif du jeu.
- Toute nouvelle valeur custom doit être ajoutée à `EnumExtensions.customEnumValues`, et avoir son alias dans `itemTypeAliases` si le nom utilisé côté JSON diffère du nom interne.

## 8. JSON / Données Dynamiques
- Season/Rarity : ne jamais assumer que l'enum C# (`Season`, `Rarity`) suffit — privilégier `SeasonId`/`RarityId` (string) et passer par `SeasonsManager`/`RaritiesManager`, qui permettent d'ajouter de nouvelles valeurs sans recompilation.
- Toute nouvelle carte/saison ajoutée doit mettre à jour `Docs/inventaire.md` (comptages par saison/rareté) pour éviter la dérive entre données réelles et documentation.

## 9. Tests & CI
- Un test qui dépend d'un fichier hors-repo ou absent en CI doit rester explicitement `[Fact(Skip = "raison")]` avec la raison indiquée, jamais planter silencieusement ou être supprimé.
- Les tests qui touchent un état statique partagé (`SeasonsManager`, `RaritiesManager`) doivent être dans `[Collection("StaticStateTests")]` (`DisableParallelization = true`) pour éviter une corruption d'état entre tests exécutés en parallèle.

## 10. Robustesse Générale
- Toute méthode qui manipule une carte doit gérer le cas "carte introuvable" avec un fallback documenté (`WankulCardsData.GetAJETER()`, `GetUnassciatedCardData()`) plutôt que de laisser une `NullReferenceException` remonter.
- Logger avec du contexte utile (`Plugin.Logger.LogError($"... {détails pertinents}")`) plutôt qu'un message générique — le debug se fait en jeu compilé, sans debugger attaché.

## 11. Inspection & Décompilation du Jeu (MCP game-decompiler)
- Dès qu'une modification touche au comportement du jeu de base, à une DLL (`Assembly-CSharp.dll`), à un patch Harmony ou à une méthode vanilla (ex: `CollectionBinderFlipAnimCtrl`, `InteractionPlayerController`, `CardOpening`, etc.), **utiliser impérativement le MCP `game-decompiler`** (`decompile_method` ou `decompile_class`).
- Ne jamais deviner la logique interne du jeu de base ou se fier à des suppositions quand la méthode vanilla exacte peut être décompilée et inspectée en quelques secondes.

## 12. Gestion des Pull Requests (GitHub CLI - `gh`)
- Dès que le sujet aborde les Pull Requests (lister, analyser, relire, tester, merger ou fermer), **utiliser prioritairement la CLI GitHub (`gh`)** (`gh pr list`, `gh pr diff`, `gh pr view`, `gh pr close`, `gh pr merge`).
- Préférer une revue ciblée directe avec l'agent principal via `gh` plutôt que des commandes multi-agents verbeuses et coûteuses en tokens.