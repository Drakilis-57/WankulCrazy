# Documentation du Mod WankulCrazy

Ce document décrit en détail les fonctionnalités intégrées/modifiables par le mod dans **TCG Card Shop Simulator**, ainsi qu'une analyse de l'architecture actuelle et des pistes d'amélioration pour simplifier l'ajout de contenu (cartes, saisons, figurines, etc.).

---

## 1. Que permet d'intégrer et de modifier le mod dans le jeu ?

Le mod **WankulCrazyPlugin** est un mod BepInEx/Harmony qui remplace ou étend le contenu original de TCG Card Shop Simulator par l'univers des cartes **Wankul**.

### 🃏 A. Cartes Wankul & Mécaniques de Collection
* **Cartes Custom (Effigies, Terrains, Spéciales)** :
  * Chargement dynamique à partir d'un fichier JSON principal (`data/formated_wankul_cards.json`) et de fichiers/dossiers multiples dans `data/cards/`.
  * Prise en charge des textures HD de cartes et de leurs **masques de brillance/foil** (`data/masks/`).
* **Association & Mapping Dynamiques** :
  * Association automatique des cartes Wankul aux monstres originaux du jeu (`EMonsterType`, `ECardBorderType`, `ECardExpansionType`).
  * Indexation inversée (`reverseAssociation`) en mémoire pour des recherches instantanées O(1).
* **Système Économique & Prix du Marché** :
  * Calcul dynamique de la valeur marchande selon le type de carte, la saison, la rareté et le pourcentage d'état de la carte.
  * Patchs des écrans de comparaison de prix (`CheckPriceUI`), des graphiques de prix historiques (`ItemPriceGraphScreen`) et des transactions avec les clients (`CustomerTradeCardScreen`).
* **Booster Opening & Drop Rates** :
  * Algorithme personnalisé d'ouverture de boosters (`CardOpening.cs`).
  * Distribution des cartes Wankul selon le type de pack/extension (ex: Basic, Rare, Epic, Legendary, Destiny, Stellar...).
* **Expérience (XP)** :
  * Système de calcul d'expérience basé sur la rareté Wankul et le niveau du magasin du joueur (`WankulCardsData.GetExperienceFromWankulCard` & `RaritiesManager`).
* **Classeur / Album (`SortUI`, `ReplacingCards`)** :
  * Patch de l'interface de l'album et du classeur pour afficher les cartes Wankul.
  * Options de tri personnalisées (par Saison, par Rareté, par Numéro, par Type).

### 🛒 B. Produits Custom & Magasin (`CustomItemsImporter`)
* **Types d'Articles Personnalisés** :
  * **Boosters & Displays** (ex: *BoosterStellar*, *DisplayStellar*, *BoosterStellarTaux*...).
  * **Starters & Decks** (ex: *StarterApocalypse*, *StarterShowtime*...).
  * **Figurines** (ex: *CaleçonStellar*...).
  * **Accessoires** (ex: *TapisS41*, *TapisS42*, *ClasseurS4*...).
* **Modèles 3D Mesh & Textures** :
  * Importation de fichiers 3D au format Wavefront `.obj` via `OBJImporter`.
  * Duplication/Copie de meshes existants du jeu originel (`CopyItem`) en appliquant une nouvelle texture.
  * Définition des dimensions, colliders, échelles d'icônes et prix d'achat/revente via `ItemDataList.json` et `itemMeshDataList.json`.
* **Licences de Restock & Réapprovisionnement** :
  * Intégration dans l'application de réapprovisionnement du téléphone portable/PC en jeu via `restockDataList.json`.

### 🎨 C. Retexturing & Décoration du Magasin
* **Posters & Vitrines (`WindowsPosters`)** :
  * Remplacement des textures des vitrines et posters muraux par des visuels Wankil.
* **Workbench & Postes de Travail (`WorkbenchPatch`)** :
  * Adaptation de la table de reconditionnement/tri de cartes pour prendre en compte les extensions et raretés Wankul.
* **Caisse Enregistreuse (`UI_CashCounterScreenPatch`)** :
  * Prise en charge des prix et scans des cartes/produits Wankul lors du passage en caisse.

---

## 2. Qu'est-ce qui manquait pour simplifier l'ajout de nouvelles cartes, saisons, figurines, etc. ?

Auparavant, plusieurs éléments imposaient de modifier le code source C# et de recompiler la DLL :
1. Les types de saisons et de raretés étaient strictement limités par des enums C# (`Season`, `Rarity`).
2. L'inscription des items custom en boutique nécessitait un enregistrement manuel.
3. Toutes les cartes devaient résider dans un fichier unique monolithique.

Toutes ces limitations ont désormais été levées grâce aux simplifications et améliorations intégrées ci-dessous.

---

## 3. Pistes de simplification & Améliorations intégrées

### ✅ Simplification 1 : Auto-enregistrement dynamique par catégorie (Implémenté)
Au lieu d'ajouter manuellement chaque item custom dans la liste de la boutique par du code C# hardcodé, le mod parcourt désormais dynamiquement la liste `ItemDataList` désérialisée et vérifie la catégorie (`category`) ou le type de chaque item :
* Si `category == EItemCategory.Figurine` $\rightarrow$ Ajout automatique à `m_ShownFigurineItemType`.
* Si `category == EItemCategory.Accessory` $\rightarrow$ Ajout automatique à `m_ShownAccessoryItemType`.
* Si l'item est un booster ou un paquet $\rightarrow$ Ajout automatique à `m_ShownItemType`.

*(Grâce à cela, ajouter une nouvelle figurine ou un nouveau tapis dans `ItemDataList.json` l'affiche immédiatement en magasin sans recompiler la DLL).*

### ✅ Simplification 2 : Évolution vers des Saisons et Raretés dynamiques (Implémenté)
Le mod gère désormais les saisons et raretés via des identifiants `string` dynamiques (`SeasonId` et `RarityId`) et deux gestionnaires dédiés (`SeasonsManager` et `RaritiesManager`) :
* Les fichiers JSON optionnels `data/seasons.json` et `data/rarities.json` permettent de définir de nouvelles saisons ou raretés personnalisées avec leurs noms d'affichage, coefficients d'expérience et multiplicateur de prix sans modifier le code C#.
* Des convertisseurs JSON (`SeasonJsonConverter`, `RarityJsonConverter`) et des accesseurs dans `WankulCardData`/`EffigyCardData` enregistrent automatiquement toute nouvelle saison ou rareté rencontrée lors de la désérialisation.
* La rétrocompatibilité avec les enums C# historiques (`Season`, `Rarity`) est entièrement préservée via des valeurs de repli (ex: `Season.HS`, `Rarity.C`).

### ✅ Simplification 4 : Cache mémoire au démarrage (Implémenté)
Les textures et meshes des items custom (`OBJImporter.cs`) sont chargés une seule fois au
démarrage (`CacheTexturesAtStart`, `CacheMeshesAtStart`) et réutilisés par référence à chaque
spawn, plutôt que relus depuis le disque à chaque instanciation.

### ✅ Simplification 5 : Cache des accès par réflexion (Implémenté)
Plusieurs chemins critiques ré-exécutaient une résolution par réflexion (`Type.GetField`,
`Type.GetMethod`) à **chaque appel** plutôt qu'une seule fois — notamment `Plugin.GetPProperty`/
`SetPProperty` (utilisés par `CardOpeningHelpers` à **chaque frame** pendant `CardOpening.Update()`
et par `CollectionBinderFlipAnimCtrl.Update()` dans `SortUI.cs`, le classeur/album), ainsi que des
`AccessTools.Field(__instance.GetType(), "...")` et `GetType().GetMethod(...)` répétés dans
`CheckPriceUI.cs`, `WorkbenchPatch.cs`, `CardPrice.cs`, `ReplacingCards.cs` et
`InteractionPlayerControllerPatch.cs`.

Deux caches statiques ont été ajoutés dans `Plugin.cs` (`GetCachedField`/`GetCachedMethod`, indexés
par `(Type, nom du champ/méthode)`), et tous les appels de ces fichiers ont été migrés dessus.
Un `FieldInfo`/`MethodInfo` est stable pour un type donné : il n'est désormais résolu qu'une seule
fois, puis réutilisé — supprimant un coût de réflexion répété plusieurs fois par frame dans les
écrans d'ouverture de booster et de tri de cartes.

> ⚠️ **Correctif** : la première version de ce cache (`type.GetField`/`GetMethod` sur le type exact
> uniquement) ne trouvait pas les champs privés déclarés dans une classe de base du jeu — contrairement
> à `AccessTools.Field`/`Method` (Harmony) qui remonte la hiérarchie. `GetCachedField`/`GetCachedMethod`
> remontent désormais `BaseType` jusqu'à trouver le membre, exactement comme le faisait `AccessTools`.

### ✅ Simplification 6 : Cache des tableaux d'enum généralisé (Implémenté)
`Season[] seasons = (Season[])Enum.GetValues(typeof(Season))` était réalloué à chaque appel dans
`CheckPriceUI.cs` et `WorkbenchPatch.cs`, alors que le même principe était déjà appliqué à
`ECardExpansionType`/`ECardBorderType` dans `WankulCardsData.cs` (`CachedExpansions`/`CachedBorders`).
Un tableau `Season[]` statique en cache a été ajouté dans les deux fichiers concernés.

### ✅ Simplification 7 : Index Saison → Cartes précalculé (Implémenté)
`WankulInventory.randFromPackType` faisait un `List.FindAll` sur l'ensemble des cartes à chaque
tirage (jusqu'à 10 fois par booster). `WankulCardsData` construit désormais un index
`Dictionary<string, List<WankulCardData>>` de façon paresseuse (une seule fois, la liste de cartes
n'étant jamais modifiée après le chargement JSON initial), exposé via
`WankulCardsData.GetCardsBySeasonFast(seasonId)`. Le tirage passe d'un scan O(N) répété à un accès
O(1) amorti.

### ✅ Simplification 8 : Support Multi-Packs / Moddabilité par dossier (Implémenté)
Le chargement des cartes supporte la modularité par dossiers et fichiers multiples :
```text
data/
  ├── customitems/
  ├── cards/
  │    ├── season1.json
  │    ├── season2.json
  │    └── my_custom_pack.json
```
Au démarrage, `JsonImporter` conserve la prise en charge du fichier historique `data/formated_wankul_cards.json` tout en parcourant de manière récursive le dossier `data/cards/` pour charger, désérialiser et fusionner automatiquement tous les fichiers JSON qu'il contient. Il est ainsi possible d'ajouter de nouveaux packs de cartes de façon modulaire sans altérer le fichier principal.
