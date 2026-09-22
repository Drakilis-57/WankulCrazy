# Documentation du Mod WankulCrazy

Ce document décrit en détail les fonctionnalités intégrées/modifiables par le mod dans **TCG Card Shop Simulator**, ainsi qu'une analyse de l'architecture actuelle et des pistes d'amélioration pour simplifier l'ajout de contenu (cartes, saisons, figurines, etc.).

---

## 1. Que permet d'intégrer et de modifier le mod dans le jeu ?

Le mod **WankulCrazyPlugin** est un mod BepInEx/Harmony qui remplace ou étend le contenu original de TCG Card Shop Simulator par l'univers des cartes **Wankul**.

### 🃏 A. Cartes Wankul & Mécaniques de Collection
* **Cartes Custom (Effigies, Terrains, Spéciales)** :
  * Chargement dynamique à partir d'un fichier JSON (`data/formated_wankul_cards.json`).
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
  * Système de calcul d'expérience basé sur la rareté Wankul et le niveau du magasin du joueur (`WankulCardsData.GetExperienceFromWankulCard`).
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

Malgré la présence de fichiers JSON pour les cartes et les items, plusieurs éléments imposaient jusqu'alors de modifier le code source C# et de recompiler la DLL :

### ❌ Inconvénients / Goulets d'étranglement identifiés :
1. **Types de Saisons hardcodés (`Season.cs` & `SeasonsContainer.cs`)** :
   * Les saisons (`S01`, `S02`, `S03`, `S04`, `HS`) sont définies sous forme d'une `enum C#`.
   * Pour ajouter une "Saison 5" ou un hors-série spécifique, il fallait modifier l'enum C# et recompiler le projet.
2. **Types de Raretés hardcodés (`Rarity.cs`)** :
   * Les raretés (`C`, `UC`, `R`, `UR1`, `UR2`, `LB`, `LA`, `LO`, `PGW23`, `NOEL23`...) sont également des `enum C#`.
3. **Inscription manuelle des items en boutique (`CustomItemsImporter.cs`)** :
   * Les sous-catégories d'affichage en magasin (Boosters, Figurines, Accessoires) nécessitaient des appels `Add(...)` explicites en C#.
4. **Chargement monolithique des cartes** :
   * Toutes les cartes devaient être regroupées dans un seul gros fichier `formated_wankul_cards.json`.

---

## 3. Pistes de simplification & Améliorations intégrées

### ✅ Simplification 1 : Auto-enregistrement dynamique par catégorie (Implémenté)
Au lieu d'ajouter manuellement chaque item custom dans la liste de la boutique par du code C# hardcodé, le mod parcourt désormais dynamiquement la liste `ItemDataList` désérialisée et vérifie la catégorie (`category`) ou le type de chaque item :
* Si `category == EItemCategory.Figurine` $\rightarrow$ Ajout automatique à `m_ShownFigurineItemType`.
* Si `category == EItemCategory.Accessory` $\rightarrow$ Ajout automatique à `m_ShownAccessoryItemType`.
* Si l'item est un booster ou un paquet $\rightarrow$ Ajout automatique à `m_ShownItemType`.

*(Grâce à cela, ajouter une nouvelle figurine ou un nouveau tapis dans `ItemDataList.json` l'affiche immédiatement en magasin sans recompiler la DLL).*

### 💡 Simplification 2 : Évolution vers des Saisons et Raretés dynamiques (Recommandation Future)
Pour affranchir totalement le mod des `enum` C# pour les saisons et raretés :
* Remplacer `enum Season` par un identifiant `string` (ex: `"S01"`, `"S05"`).
* Charger un fichier `seasons.json` contenant la liste des saisons et leurs noms affichés.
* Charger un fichier `rarities.json` définissant les raretés, leurs coefficients d'XP et leurs multiplicateurs de prix.

### 💡 Simplification 3 : Support Multi-Packs / Moddabilité par dossier (Recommandation Future)
Permettre le chargement séparé des données :
```text
data/
  ├── customitems/
  ├── cards/
  │    ├── season1.json
  │    ├── season2.json
  │    └── my_custom_pack.json
```
Le mod fusionnerait tous les fichiers JSON du dossier `data/cards/` au démarrage.
