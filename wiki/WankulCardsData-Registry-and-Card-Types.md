# WankulCardsData Registry and Card Types

> **Relevant source files**
> * [cards/CardType.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/CardType.cs)
> * [cards/SpecialCardData.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/SpecialCardData.cs)
> * [cards/Specials.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/Specials.cs)
> * [cards/TerrainCardData.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/TerrainCardData.cs)
> * [cards/WankulCardData.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs)
> * [cards/WankulCardsData.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs)
> * [patch/CheckPriceUI.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/CheckPriceUI.cs)

## Purpose and Scope

This page documents the core card domain model and registry system in `WankulCrazy`. It covers the `WankulCardsData` singleton, association mapping between native game structures (`CardData`/`MonsterData`) and custom Wankul records, the polymorphic card type hierarchy (`EffigyCardData`, `TerrainCardData`, `SpecialCardData`), and fallback handling including special items like `AJETER`.

Sources: [cards/WankulCardsData.cs L1-L12](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L1-L12)

 [cards/WankulCardData.cs L1-L10](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L1-L10)

---

## 1. WankulCardsData Singleton and Core Architecture

`WankulCardsData` inherits from `Singleton<WankulCardsData>` [cards/WankulCardsData.cs L12-L13](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L12-L13)

 and serves as the central runtime repository for all loaded Wankul cards.

### Initialization and Storage

The registry holds a primary list of cards (`cards`) and a lookup cache organized by season (`cardsBySeason`) [cards/WankulCardsData.cs L14-L36](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L14-L36)

 If the collection is uninitialized when accessed, `EnsureInitialized()` invokes `JsonImporter.ImportJson()` [cards/WankulCardsData.cs L18-L25](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L18-L25)

```css
public void EnsureInitialized(){    if (cards == null || cards.Count == 0)    {        Plugin.LogInfo("[WankulCardsData] Initialisation des cartes déclenchée par EnsureInitialized.");        JsonImporter.ImportJson();    }}```[cards/WankulCardsData.cs:18-25]() ### Season IndexingTo avoid $O(N)$ scans during UI rendering or season filtering, `GetCardsBySeasonIndex()` builds a lazy dictionary mapping season strings or enum values to sub-lists of `WankulCardData` <FileRef file-url="https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L38-L60" min=38 max=60 file-path="cards/WankulCardsData.cs">cards/WankulCardsData.cs:38-60</FileRef> Static helpers `GetCardsBySeasonFast(string seasonId)` and `GetCardsBySeasonFast(Season season)` provide direct access <FileRef file-url="https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L62-L76" min=62 max=76 file-path="cards/WankulCardsData.cs">cards/WankulCardsData.cs:62-76</FileRef> ```mermaidgraph TD    A["WankulCardsData.Instance"] --> B["EnsureInitialized()"]    B --> C["JsonImporter.ImportJson()"]    A --> D["GetCardsBySeasonFast(string seasonId)"]    D --> E["GetCardsBySeasonIndex()"]    E --> F["Dictionary<string, List<WankulCardData>> cardsBySeason"]    F --> G["WankulCardData Collection"]     sub-graph "Code Entity Space"    A    B    C    D    E    F    G    end
```

*Figure 1: WankulCardsData lifecycle and seasonal index mapping.*

Sources: [cards/WankulCardsData.cs L12-L77](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L12-L77)

---

## 2. Association Mapping and Reverse Lookups

The game engine requires native `CardData` instances to render, price, and trade cards. Because custom cards replace native ones, `WankulCardsData` maintains bidirectional mapping structures between native game references and custom `WankulCardData` objects.

### Association Dictionaries

* `association`: Maps composite string keys (`{monsterType}_{borderType}_{expansionType}`) to `WankulCardData` [cards/WankulCardsData.cs L16-L84](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L16-L84)
* `reverseAssociation`: Maps `WankulCardData.Index` back to native `CardData` objects, optimizing reverse queries from $O(N)$ to $O(1)$ [cards/WankulCardsData.cs L28-L156](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L28-L156)

### GetFromMonster Logic

When the game requests a card mapping for a given native `CardData` (referred to as `monster`), `GetFromMonster` checks existing associations first [cards/WankulCardsData.cs L79-L92](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L79-L92)

 If missing and allowed, it evaluates the rarity and expansion type to determine the appropriate `ECollectionPackType`, draws a random card via `WankulInventory.randFromPackType()`, and registers both forward and reverse links [cards/WankulCardsData.cs L107-L146](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L107-L146)

```

```

*Figure 2: Association resolution workflow for native cards.*

Sources: [cards/WankulCardsData.cs L16-L165](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L16-L165)

---

## 3. Card Data Hierarchy and Subtypes

`WankulCardData` acts as the base schema for all card definitions, categorized by the `CardType` enum (`Terrain`, `Effigy`, `Special`) [cards/WankulCardData.cs L8-L18](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L8-L18)

 [cards/CardType.cs L3-L8](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/CardType.cs#L3-L8)

| Base Field / Property | Type | Description |
| --- | --- | --- |
| `Index` | `int` | Unique card identifier index. |
| `Number` | `string` | Card catalog number (parsed via `NumberInt`) [cards/WankulCardData.cs L12-L83](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L12-L83) |
| `Title` | `string` | Display title of the card [cards/WankulCardData.cs L14](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L14-L14) |
| `Artist` | `string` | Artwork creator name [cards/WankulCardData.cs L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L16-L16) |
| `CardType` | `CardType` | Categorization enum (`Terrain`, `Effigy`, `Special`) [cards/WankulCardData.cs L18](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L18-L18) |
| `Season` / `SeasonId` | `Season` / `string` | Expansion season mapping with automatic `SeasonsManager` registration [cards/WankulCardData.cs L20-L40](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L20-L40) |
| `TexturePath` | `string` | File path for card texture loading [cards/WankulCardData.cs L42](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L42-L42) |
| `MarketPrice` | `float` | Computed market valuation incorporating dynamic multipliers [cards/WankulCardData.cs L92-L114](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L92-L114) |

### Specialized Card Subtypes

* **TerrainCardData**: Inherits from `WankulCardData` and introduces gameplay effect properties [cards/TerrainCardData.cs L3-L11](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/TerrainCardData.cs#L3-L11) : * `WinningEffect` [cards/TerrainCardData.cs L6](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/TerrainCardData.cs#L6-L6) * `LosingEffect` [cards/TerrainCardData.cs L8](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/TerrainCardData.cs#L8-L8) * `SpecialEffect` [cards/TerrainCardData.cs L10](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/TerrainCardData.cs#L10-L10)
* **SpecialCardData**: Inherits from `WankulCardData` and defines non-standard card behaviors through the `Specials` enum (`AJETER`, `TOR`) [cards/SpecialCardData.cs L3-L7](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/SpecialCardData.cs#L3-L7)  [cards/Specials.cs L3-L7](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/Specials.cs#L3-L7)

Sources: [cards/WankulCardData.cs L8-L115](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L8-L115)

 [cards/CardType.cs L1-L9](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/CardType.cs#L1-L9)

 [cards/SpecialCardData.cs L1-L8](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/SpecialCardData.cs#L1-L8)

 [cards/Specials.cs L1-L8](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/Specials.cs#L1-L8)

 [cards/TerrainCardData.cs L1-L12](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/TerrainCardData.cs#L1-L12)

---

## 4. Market Pricing, Seasons, and AJETER Fallback

### Market Price Calculation

The `MarketPrice` property evaluates lazily [cards/WankulCardData.cs L92-L114](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L92-L114)

 If `generatedMarketPrice` is unassigned, it calls `CardPrice.generateMarketPrice(this)`, scaling the result by the card's current `Percentage` drop/fluctuation factor [cards/WankulCardData.cs L97-L109](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L97-L109)

### Season Registration

Setting `SeasonId` checks for validity, registers the season dynamically via `SeasonsManager.RegisterSeason()`, and attempts a safe parse into the `Season` enum [cards/WankulCardData.cs L25-L40](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L25-L40)

### Unassociated Cards and AJETER Fallback

When rendering check prices or inventories where a card lacks a direct native association, UI patches fall back to an unassociated card lookup [patch/CheckPriceUI.cs L107-L118](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/CheckPriceUI.cs#L107-L118)

 Special card types such as `AJETER` (A jeter / discardable) handle garbage or fallback states within booster and economy pipelines.

Sources: [cards/WankulCardData.cs L25-L114](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardData.cs#L25-L114)

 [cards/Specials.cs L3-L7](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/Specials.cs#L3-L7)

 [patch/CheckPriceUI.cs L107-L118](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/CheckPriceUI.cs#L107-L118)