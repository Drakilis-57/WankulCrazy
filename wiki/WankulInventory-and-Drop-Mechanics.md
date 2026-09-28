# WankulInventory and Drop Mechanics

> **Relevant source files**
> * [inventory/WankulInventory.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/inventory/WankulInventory.cs)
> * [patch/Inventory.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Inventory.cs)
> * [utils/RandomUtils.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/RandomUtils.cs)

## Purpose and Scope

This page documents the inventory state-management and card drop/generation pipelines in the WankulCrazy mod. It covers the `WankulInventory` singleton, inventory modification hooks, pack-to-season conversions, and the weighted card drop algorithms (`DropCard`) implemented in `inventory/WankulInventory.cs`, `patch/Inventory.cs`, and `utils/RandomUtils.cs`.

Sources: `inventory/WankulInventory.cs:1-165()`, `patch/Inventory.cs:1-35()`, `utils/RandomUtils.cs:1-33()`

---

## Architecture and Singleton Inventory

The inventory system revolves around the `WankulInventory` class, which inherits from the shared `Singleton<T>` pattern. It maintains a primary dictionary (`wankulCards`) mapping card IDs to a tuple containing the custom `WankulCardData`, vanilla `CardData`, and the owned quantity.

```

```

The `WankulInventory` class manages internal logging fallback via `LogError`, routing to `Plugin.Logger` if initialized or falling back to `Console.WriteLine`.

Sources: `inventory/WankulInventory.cs:1-28()`

---

## Inventory Mutation Patches (AddCard and RemoveCard)

Game events that alter a player's card count (such as purchasing, opening packs, or trading) are intercepted by the patch layer (`patch/Inventory.cs`).

When vanilla game code invokes an addition or removal, the patch resolves the corresponding custom `WankulCardData` using `WankulCardsData.Instance.GetFromMonster()`. If no matching Wankul card is found during an addition, the system falls back to a default fallback card (`AJETER`) and issues a warning.

```

```

Sources: `patch/Inventory.cs:1-34()`

---

## Pack Type to Season Mapping

Card drop logic depends on the specific collection pack type being opened. `WankulInventory.ConvertPackTypeToSeason()` parses vanilla and dynamic pack types (`ECollectionPackType`), converting them into custom `Season` enum values (`S01` through `S05`, or `HS` for high-season/fallback).

* `BasicCardPack`, `DestinyBasicCardPack` $\rightarrow$ `Season.S01`
* `RareCardPack`, `DestinyRareCardPack` $\rightarrow$ `Season.S02`
* `EpicCardPack`, `DestinyEpicCardPack` $\rightarrow$ `Season.S03`
* `Stellar` / `StellarTaux` $\rightarrow$ `Season.S04`
* `Legacy` / `LegacyTaux` / `AscensionCardPack` $\rightarrow$ `Season.S05`
* All other pack types $\rightarrow$ `Season.HS`

Sources: `inventory/WankulInventory.cs:30-51()`

---

## Weighted Drop Pipelines (DropCard)

The `WankulInventory.DropCard()` function handles card generation during pack opening. It filters cards by season, checks terrain status, applies rarity minimums, avoids duplicate cards from the current selection batch, and computes weighted probabilities based on rarity tiers and pack modifiers (such as Destiny or Taux packs).

```

```

The drop selection relies on `utils.RandomUtils` to fetch uniform random floats and integers, maintaining a reliable fallback to `System.Random` if `UnityEngine.Random` throws an exception in non-unity thread contexts.

Sources: `inventory/WankulInventory.cs:53-165()`, `utils/RandomUtils.cs:1-33()`