# WankulInventaire et mécanique des dépôts

> **Fichiers sources pertinents**
> * [inventory/WankulInventory.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/inventory/WankulInventory.cs)
> * [patch/Inventory.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Inventory.cs)
> * [utils/RandomUtils.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/RandomUtils.cs)

## Objectif et portée

This page documents the inventory state-management and card drop/generation pipelines in the WankulCrazy mod. It covers the `WankulInventory` singleton, inventory modification hooks, pack-to-season conversions, and the weighted card drop algorithms (`DropCard`) implemented in `inventory/WankulInventory.cs`, `patch/Inventory.cs`, and `utils/RandomUtils.cs`.

Sources : SNIPPET 0, SNIPPET _1, SNIPPET _2

---

## Architecture et inventaire Singleton

The inventory system revolves around the `WankulInventory` class, which inherits from the shared `Singleton<T>` pattern. It maintains a primary dictionary (`wankulCards`) mapping card IDs to a tuple containing the custom `WankulCardData`, vanilla `CardData`, and the owned quantity.

```

```

La classe `WankulInventory` gère le repli de la journalisation interne via `LogError`, le routage vers `Plugin.Logger` s'il est initialisé ou le retour vers `Console.WriteLine`.

Sources : `inventory/WankulInventory.cs:1-28()`

---

## Patchs de mutation d'inventaire (AddCard et RemoveCard)

Les événements de jeu qui modifient le nombre de cartes d'un joueur (tels que l'achat, l'ouverture de packs ou l'échange) sont interceptés par la couche de correctifs (`patch/Inventory.cs`).

Lorsque le code du jeu Vanilla appelle un ajout ou une suppression, le correctif résout le `WankulCardData` personnalisé correspondant à l'aide de `WankulCardsData.Instance.GetFromMonster()`.Si aucune carte Wankul correspondante n'est trouvée lors d'un ajout, le système revient à une carte de secours par défaut (`AJETER`) et émet un avertissement.

```

```

Sources : `patch/Inventory.cs:1-34()`

---

## Type de pack et mappage de saison

La logique de dépôt de la carte dépend du type de pack de collecte spécifique ouvert.`WankulInventory.ConvertPackTypeToSeason()` analyse les types de packs vanille et dynamiques (`ECollectionPackType`), en les convertissant en valeurs d'énumération `Season` personnalisées (`S01` à `S05`, ou `HS` pour la haute saison/de repli).

* `BasicCardPack`, `DestinyBasicCardPack` $\rightarrow$ `Season.S01`
* SNIPPET _0, SNIPPET _1 $\flèche droite$ SNIPPET _2
* `EpicCardPack`, `DestinyEpicCardPack` $\rightarrow$ `Season.S03`
* SNIPPET _0 / SNIPPET _1 $\flèche droite$ SNIPPET _2
* `Legacy` / `LegacyTaux` / `AscensionCardPack` $\rightarrow$ `Season.S05`
* All other pack types $\rightarrow$ `Season.HS`

Sources : `inventory/WankulInventory.cs:30-51()`

---

## Pipelines de dépôt pondérés (DropCard)

La fonction `WankulInventory.DropCard()` gère la génération de la carte lors de l'ouverture du pack.Il filtre les cartes par saison, vérifie l'état du terrain, applique des minimums de rareté, évite les cartes en double du lot de sélection actuel et calcule les probabilités pondérées en fonction des niveaux de rareté et des modificateurs de pack (tels que les packs Destiny ou Taux).

```

```

La sélection de suppression s'appuie sur `utils.RandomUtils` pour récupérer des flottants et des entiers aléatoires uniformes, en maintenant un repli fiable vers `System.Random` si `UnityEngine.Random` lève une exception dans des contextes de thread non-unité.

Sources : `inventory/WankulInventory.cs:53-165()`, `utils/RandomUtils.cs:1-33()`