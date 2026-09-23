# Documentation du mod WankulCrazy

Ce document reflète l’état actuel du dépôt et décrit les fonctionnalités réellement intégrées dans le mod, ainsi que les points d’architecture et d’optimisation qui ont été ajoutés au fil du développement.

---

## 1. Ce que fait le mod

Le mod WankulCrazyPlugin est un plugin BepInEx/Harmony pour TCG Card Shop Simulator qui enrichit le jeu avec un univers Wankul : cartes custom, items, boosters, figurines, accessoires, adaptation de l’interface et compatibilité avec des données dynamiques chargées depuis des fichiers JSON.

Il interfère principalement avec :
- les cartes du jeu et leurs associations aux monstres,
- les packs / boosters / collections,
- les écrans de prix, de tri et d’album,
- l’import de contenus custom depuis le dossier `data/`,
- les textures et meshes des objets supplémentaires.

---

## 2. Fonctionnalités principales

### 2.1 Cartes Wankul, saisons et raretés dynamiques

Le mod charge des cartes Wankul depuis plusieurs sources, notamment :
- `data/formated_wankul_cards.json` pour le format historique,
- `data/cards/` pour un chargement modulable par fichiers et dossiers.

Les types de cartes supportés incluent :
- `WankulCardData`
- `EffigyCardData`
- `TerrainCardData`
- `SpecialCardData`

Les valeurs de saison et de rareté ne sont plus obligatoirement figées dans des enums C# historiques. Le code supporte désormais :
- `SeasonId` et `RarityId` sous forme de chaîne dynamique,
- gestion par `SeasonsManager` et `RaritiesManager`,
- enregistrement automatique des valeurs nouvelles rencontrées lors du chargement JSON,
- compatibilité rétroactive avec les enums d’origine (`Season`, `Rarity`).

Cela permet d’ajouter de nouvelles saisons ou raretés sans recompilation du plugin.

### 2.2 Import automatisé des items custom

Le chargement des objets custom est centralisé dans `CustomItemsImporter` et `JsonImporter`.

Le mod prend en charge :
- boosters,
- packs,
- displays,
- starters / decks,
- figurines,
- accessoires,
- objets visuels comme tapis, classeurs, posters, etc.

L’auto-enregistrement à la boutique est également activé par catégorie :
- `EItemCategory.Figurine` → ajout aux figurines visibles,
- `EItemCategory.Accessory` → ajout aux accessoires visibles,
- packs/boosters → ajout au catalogue d’items standard.

### 2.3 Association cartes / monstres

`WankulCardsData` construit une association entre les cartes Wankul et les cartes du jeu de base :
- `association` : clé `monsterType_borderType_expansionType` → carte Wankul,
- `reverseAssociation` : index de carte Wankul → `CardData` du jeu,
- validation de clés de monstre / expansion,
- support de plages de monstres par expansion pour éviter les parcours inutiles.

Cela permet un mapping multiforme entre l’univers Wankul et les données du jeu original.

### 2.4 Prix, collection, classeur, album et opening

Le mod patche plusieurs écrans et mécanismes du jeu afin d’intégrer les éléments Wankul :
- `CheckPriceUI`
- `CardPrice`
- `SortUI`
- `ReplacingCards`
- `CardOpening`
- `WorkbenchPatch`
- `CustomerTradeCardScreenPatch`
- `UI_CashCounterScreenPatch`
- `WindowsPosters`

Le but est de rendre compatibles :
- l’ouverture de boosters custom,
- le calcul de valeur de marché,
- la collection et le tri par saison/rarité/numéro/type,
- la vente et la caisse,
- les emplois du temps de travail / reconditionnement.

---

## 3. Architecture actuelle

### 3.1 Fichiers clés

Principaux composants du dépôt :
- `Plugin.cs` : point d’entrée, patchs Harmony, caches globaux,
- `cards/` : définition des données, saisons, raretés, mapping,
- `importer/` : chargement JSON, objets 3D, textures,
- `patch/` : hooks sur les écrans et mécanismes du jeu,
- `inventory/` : logique de tirage et de génération de cartes,
- `utils/` : utilitaires, OBJ loader, conversions, helpers,
- `WankulCrazyPlugin.Tests/` : vérifications de comportement et régression.

### 3.2 Chargement JSON et modularité

`JsonImporter` a été rendu plus robuste et plus extensible.

Il supporte désormais :
- des structures JSON de type tableau,
- des objets contenant des propriétés comme `wankuls`, `terrains`, `specials`,
- un chargement récursif depuis plusieurs sous-dossiers,
- compatibilité avec l’ancien fichier monolithique.

Cela permet d’étendre facilement le contenu sans devoir modifier le code C#.

---

## 4. Améliorations et simplifications intégrées

### ✅ Simplification 1 : auto-enregistrement dynamique par catégorie

Avant, les items custom devaient être ajoutés à la boutique de façon manuelle dans le code.

Maintenant, le code parcourt les objets `ItemDataList` chargés, puis les classe automatiquement selon leur catégorie :
- `EItemCategory.Figurine` → boutique figurines,
- `EItemCategory.Accessory` → boutique accessoires,
- packs et boosters → catalogue standard.

Cela permet d’ajouter un nouvel item via le JSON sans recompilation.

### ✅ Simplification 2 : saisons et raretés dynamiques

Le système n’est plus limité aux enums originaux.

Le repo actuel utilise :
- `SeasonsManager`
- `RaritiesManager`
- `SeasonData` / `RarityData`
- `SeasonJsonConverter` / `RarityJsonConverter`

Les nouvelles valeurs sont automatiquement enregistrées et accessibles ensuite via `SeasonId` / `RarityId`.

La compatibilité avec les anciennes valeurs d’enum est conservée via des valeurs de repli.

### ✅ Simplification 3 : chargement de cartes multi-fichiers / multi-dossiers

Le mod supporte désormais :

```text
data/
  ├── customitems/
  ├── cards/
  │    ├── season1.json
  │    ├── season2.json
  │    └── my_custom_pack.json
```

Le chargement ne dépend plus d’un unique fichier monolithique.

### ✅ Simplification 4 : cache mémoire de textures et meshes

`OBJImporter` applique des mécanismes de cache pour éviter de relire les mêmes ressources depuis le disque à chaque instanciation :
- `CacheTexturesAtStart`
- `CacheMeshesAtStart`

Les ressources sont réutilisées par référence lors des spawns d’objets.

### ✅ Simplification 5 : cache des accès par réflexion

Le code évite de réévaluer par réflexion à chaque appel dans les chemins critiques.

Dans `Plugin.cs`, deux caches statiques ont été ajoutés :
- `FieldCache`
- `MethodCache`

et des helpers :
- `GetCachedField`
- `GetCachedMethod`

Cela est particulièrement utile pour des méthodes appelées très souvent pendant les animations et l’UI, comme des chemins de mise à jour par frame.

Le correctif important ici est qu’il remonte la hiérarchie de classes (`BaseType`) pour trouver les membres privés hérités, ce qui correspond au comportement attendu de `AccessTools.Field` / `AccessTools.Method`.

### ✅ Simplification 6 : cache des tableaux d’enum

Les conversions répétées sur `Enum.GetValues` ont été remplacées par des tableaux statiques réutilisés.

Exemples présents dans le code :
- `CachedExpansions`
- `CachedBorders`

Cela évite les allocations inutiles dans les boucles de calculs de cartes et d’UI.

### ✅ Simplification 7 : index saison → cartes precalculé

`WankulCardsData` construit un index lazy :
- `cardsBySeason`
- `GetCardsBySeasonFast(string seasonId)`

Au lieu de recalculer des listes à chaque tirage, le mod accède directement à la liste associée à la saison demandée. Cela améliore le temps de tirage des boosters et réduit les scans O(N) répétés.

### ✅ Simplification 8 : compatibilité Enum + patchs système

Le plugin applique des patches de compatibilité sur certaines méthodes du runtime pour que les cartes Wankul et les données dynamiques interagissent mieux avec les enums et les conversions du jeu.

Cela concerne notamment :
- `Enum.GetName`
- `Enum.IsDefined`
- `Enum.Parse`

Ce type de compatibilité est utile pour stabiliser le comportement du jeu avec des valeurs custom non standard.

---

## 5. Validation et tests du repo

Le dépôt contient une suite de tests unitaire dédiée à ces fonctionnalités, notamment :
- `WankulCrazyPlugin.Tests/DocsFeaturesVerificationTests.cs`
- `DynamicSeasonsAndRaritiesTests.cs`
- `EnumExtensionsTests.cs`
- `StringExtensionsTests.cs`

Les tests couvrent :
- chargement multi-fichiers de cartes,
- enregistrement dynamique d’une saison et d’une rareté,
- conversion JSON des objets `Season`/`Rarity`,
- propriétés auto-enregistrées sur `EffigyCardData`,
- cohérence des extensions de chaînes et enums.

---

## 6. Points d’attention / limites actuelles

Le mod est bien avancé, mais il reste des points sensibles :
- beaucoup de logique dépend directement de l’API du jeu original et de sa structure interne,
- certaines mécaniques sont patchées via réflexion et restent sensibles aux changements de version du jeu,
- la robustesse de certaines méthodes dépend des noms exacts d’éléments privés du jeu,
- la documentation technique doit être maintenue à côté du code au cas où des changements de signature ou de patchs sont apportés.

---

## 7. Conclusion

Le mod WankulCrazyPlugin est aujourd’hui structuré autour de trois axes principaux :
1. extension de contenu via JSON dynamique,
2. compatibilité avec les systèmes du jeu originaux via patchs Harmony,
3. optimisation via caches et indexation pour limiter les coûts de calcul et de réflexion.

La logique actuelle montre clairement une volonté d’ouvrir le système à de nouveaux contenus sans recompilation du plugin, tout en conservant une compatibilité forte avec l’architecture du jeu.

---

## 8. Fichiers utiles à consulter

- `Plugin.cs`
- `cards/WankulCardsData.cs`
- `cards/SeasonsManager.cs`
- `cards/RaritiesManager.cs`
- `importer/JsonImporter.cs`
- `importer/CustomItemsImporter.cs`
- `patch/CardOpening.cs`
- `patch/CheckPriceUI.cs`
- `patch/ReplacingCards.cs`
- `patch/workbench/WorkbenchPatch.cs`
- `WankulCrazyPlugin.Tests/DocsFeaturesVerificationTests.cs`

Cette liste est un bon point de départ si tu veux aller plus loin dans l’analyse du code ou une future documentation technique plus détaillée.
