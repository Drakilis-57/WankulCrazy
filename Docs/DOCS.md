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

### 3.3 Cheminement et cycle de vie d'une carte (Exemple : Carte Legacy)

Pour comprendre comment le mod intègre une carte du début à la fin, voici le cheminement pas à pas d'une carte comme **ROAD TRIP (`Index: 500001`, `S05`)** :

```text
┌─────────────────────────────────────────────────────────────┐
│ 1. Définition JSON (data/cards/Legacy/legacy.json)          │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Chargement & Désérialisation (JsonImporter.ImportJson)   │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Enregistrement Modèle (WankulCardsData.Instance)         │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Décompression Asynchrone (WankulLoadingScreen)           │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Mapping In-Game (Association CardData du jeu)            │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Exploitation In-Game (Boosters, Classeur, Prix, Vente)   │
└─────────────────────────────────────────────────────────────┘
```

#### Étape 1 : Définition de la carte (`data/cards/Legacy/legacy.json`)
La carte est définie avec ses caractéristiques Wankul :
- `Index`: `500001` (identifiant unique pour la saison Legacy S05)
- `Number`: `"001"`, `Title`: `"ROAD TRIP"`, `CardType`: `"Terrain"`
- `SeasonId`: `"S05"`, `RarityId`: `"C"` (Commune)
- `TexturePath`: `"cards/Legacy/textures/001_ROAD TRIP.png"`

#### Étape 2 : Chargement et Parsing (`JsonImporter.cs`)
1. Au lancement du jeu (hook `GameStarting.OnLevelFinishedLoading`), `WankulCardsData.Instance.EnsureInitialized()` déclenche `JsonImporter.ImportJson()`.
2. Le fichier `seasons.json` associe `"S05"` à la saison **Legacy** (`expansionIndex: 4`).
3. `JsonImporter` scanne récursivement `data/cards/` et désérialise le JSON en objet typé C# `TerrainCardData` (héritant de `WankulCardData`).

#### Étape 3 : Écran de chargement et décompression visuelle (`WankulLoadingScreen.cs`)
1. En jeu, `JsonImporter` transmet la liste des cartes à `WankulLoadingScreen.ShowAndStartLoading(...)`.
2. L'écran de chargement s'affiche en surimpression (`Canvas`, `sortingOrder = 9999`) avec sa barre de progression.
3. Les textures PNG/JPG sont chargées depuis le disque et converties en `Texture2D` et `Sprite` Unity par batch de 12 images par frame (`yield return null`), ce qui évite tout freeze Windows.
4. Les objets `Texture` et `Sprite` sont assignés directement à la propriété de la carte (`card.Texture = texture`, `card.Sprite = sprite`).

#### Étape 4 : Association avec le moteur du jeu (`WankulCardsData.cs`)
Pour que le moteur de *TCG Card Shop Simulator* puisse manipuler la carte Wankul sans casser ses systèmes internes, la carte est mappée à un slot existant du jeu (`CardData`) :
1. `GetUnassciatedCardData()` alloue un emplacement parmi les extensions supportées (`Ghost`, `FantasyRPG`, `Megabot`, `CatJob`...).
2. Le dictionnaire `association` relie la clé de la carte Wankul au `CardData` correspondant, et un dictionnaire inverse `reverseAssociation` permet un accès en $O(1)$.

#### Étape 5 : Exploitation in-game via les patchs Harmony
Une fois le jeu en cours d'exécution, la carte intervient à plusieurs endroits grâce aux patchs :
- **Ouverture de boosters (`patch/CardOpening.cs`)** : Lors de l'ouverture d'un paquet de la saison Legacy, la pioche tire la carte selon ses taux de drop (`Drop`, `Percentage`, rareté `C`).
- **Affichage et album (`patch/ReplacingCards.cs`, `patch/SortUI.cs`)** : Les méthodes d'affichage de texture du jeu (`GetCardTexture`, `GetIcon`) interceptent le rendu pour afficher le sprite Wankul au lieu du visuel de base du jeu.
- **Cotation et Marché (`patch/CheckPriceUI.cs`)** : Le prix de vente et la valeur marchande sont calculés en fonction des stats de la carte Wankul.
- **Posters et vitrines (`patch/WindowsPosters.cs`, `patch/CustomItemsImporter.cs`)** : Les modèles 3D et textures des packagings Legacy sont injectés dans la boutique.

---

## 4. Validation et tests du repo

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

## 5. Points d’attention / limites actuelles

Le mod est bien avancé, mais il reste des points sensibles :
- beaucoup de logique dépend directement de l’API du jeu original et de sa structure interne,
- certaines mécaniques sont patchées via réflexion et restent sensibles aux changements de version du jeu,
- la robustesse de certaines méthodes dépend des noms exacts d’éléments privés du jeu,
- la documentation technique doit être maintenue à côté du code au cas où des changements de signature ou de patchs sont apportés.

---

## 6. Conclusion

Le mod WankulCrazyPlugin est aujourd’hui structuré autour de trois axes principaux :
1. extension de contenu via JSON dynamique,
2. compatibilité avec les systèmes du jeu originaux via patchs Harmony,
3. optimisation via caches et indexation pour limiter les coûts de calcul et de réflexion.

La logique actuelle montre clairement une volonté d’ouvrir le système à de nouveaux contenus sans recompilation du plugin, tout en conservant une compatibilité forte avec l’architecture du jeu.

---

## 7. Fichiers utiles à consulter

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

Idées pour la suite :   

Rajouter un compteur de carte global dans la vue de l'album ( par exemple on a eu X cartes sur les 910 totaux)

Dans l'album de base il génére des boutons bleus extension , plutot que copier les boutons trier , on peut juste remplacer la logique des boutons par les notres.

Particuliarité de la 1.0 : on peut maintenant jouer avec notre propre deck personallisé au jeux de carte : le jeux de base repose sur des points d'éléments ? Si tout fonctionne et qu'on est vraiment stable,faudrait trouver un moyen de pouvoir y jouer.

Particuliarité de la 1.0 ( possible ) : Notation ! on peut envoyer nos cartes pour les faire note de 1 a 10 , ce qui permet de faire augmenter ou baisser sa valeur de vente. A tester si sa marche

Possible bug : la réduction de la taille des boutons dans UI de l'album a peut etre été recopiée sur la taille des cartes
