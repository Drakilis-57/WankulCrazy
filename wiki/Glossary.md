# Glossary

> **Relevant source files**
> * [Plugin.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs)
> * [cards/WankulCardsData.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs)
> * [inventory/WankulInventory.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/inventory/WankulInventory.cs)
> * [patch/EItemTypeExtension.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/EItemTypeExtension.cs)
> * [patch/Inventory.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Inventory.cs)

## Purpose and Scope

This page provides comprehensive technical definitions for core concepts, domain-specific terminology, enumerations, architectural patterns, and specialized data structures used across the `WankulCrazy` codebase. Each entry details its implementation role, data flow context, and links to the relevant source code files and line ranges.

---

## 1. Core Framework and Architecture Terms

### BepInEx

The plugin framework and runtime host utilized to inject the `WankulCrazy` mod into the target Unity game (`TCG Card Shop Simulator`). It provides lifecycle management, configuration binding, and logging services.

* **Implementation**: Initialized via the `Plugin` class inheriting from `BaseUnityPlugin` [Plugin.cs L14-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L14-L16)  Verbose debugging flags are bound through BepInEx's configuration system [Plugin.cs L40](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L40-L40)  and diagnostic logs are channeled through `ManualLogSource` [Plugin.cs L17](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L17-L17)

### Harmony Prefix / Postfix

The interception mechanism provided by the `HarmonyLib` library to modify, extend, or replace existing game methods without recompiling game assemblies.

* **Prefix**: Executed prior to the original method, capable of altering arguments or short-circuiting execution.
* **Postfix**: Executed after the original method, commonly used to append custom logic or modify return values.
* **Implementation**: Patches are registered dynamically inside `Plugin.Awake()` using the `TryPatch` helper function [Plugin.cs L49-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L71)  targeting methods such as `CardUI.SetCardUI` [Plugin.cs L107-L111](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L107-L111)  and `CSaveLoad.Save` [Plugin.cs L151-L154](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L151-L154)

### Frame Budget

A performance optimization pattern implemented within asynchronous loaders and UI rendering pipelines (such as `WankulLoadingScreen`) to distribute heavy asset-loading tasks across multiple frames, preventing game stuttering and main thread lockups.

> **Diagram 1: Framework Execution & Patch Flow**
> ```mermaid
> flowchart TD
> A["Plugin.Awake"]
> B["Harmony Instance"]
> C["Original Game Method"]
> D["Harmony Prefix"]
> E["Harmony Postfix"]
> B --> C
> subgraph subGraph1 ["Patch Registry"]
>     C
>     D
>     E
>     C --> D
>     C --> E
> end
> subgraph subGraph0 ["BepInEx Host"]
>     A
>     B
>     A --> B
> end
> ```
> Sources: [Plugin.cs L36-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L36-L71)

---

## 2. Domain Model and Card Terms

### Wankul Card

The core custom domain entity representing cards styled with Wankul content, replacing standard game monsters.

* **Implementation**: Modeled by subclasses under the `cards/` namespace and stored centrally via the `WankulCardsData` singleton registry [cards/WankulCardsData.cs L12-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L12-L16)  Each card links back to standard game equivalents through asset association maps.

### AJETER

A designated fallback card entity utilized when a requested monster card mapping cannot be resolved or yields a null reference during inventory operations.

* **Implementation**: Retrieved via `WankulCardsData.GetAJETER()` and injected as a safety guard within inventory modification wrappers [patch/Inventory.cs L14-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Inventory.cs#L14-L16)

### Taux

A specialized variant modifier applied to booster packs, displays, and shop items indicating enhanced or boosted drop rates and statistical weighting.

* **Implementation**: Custom enum constants like `BoosterStellarTaux` and `DisplayStellarTaux` are registered under custom enum dictionaries with assigned integer IDs [patch/EItemTypeExtension.cs L17-L21](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/EItemTypeExtension.cs#L17-L21)

### Effigy

A card rarity classification or specific card data type (`EffigyCardData`) representing character portraits or statues within seasonal drops.

* **Implementation**: Filtered and evaluated during booster pack opening drops and minimum rarity guarantees [inventory/WankulInventory.cs L104-L128](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/inventory/WankulInventory.cs#L104-L128)

### Season

An organizational grouping representing release waves or seasonal collections (e.g., `S01`, `S02`, `S03`, `S04`, `S05`, and `HS` for High-Season/Special).

* **Implementation**: Mapped directly from collection pack types via `WankulInventory.ConvertPackTypeToSeason()` [inventory/WankulInventory.cs L30-L51](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/inventory/WankulInventory.cs#L30-L51)  and indexed for fast retrieval via `WankulCardsData.GetCardsBySeasonFast()` [cards/WankulCardsData.cs L62-L76](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L62-L76)

### Foil

A special visual finish applied to card renders, controlled via texture overlays and custom shader parameters during card UI rendering updates.

### Podium Textures

Specialized 3D textures and materials used on display podiums and shop counters to showcase high-value Wankul items and custom displays.

> **Diagram 2: Domain Entity Resolution & Inventory Mapping**
> ```mermaid
> flowchart TD
> F["Inventory.AddCard"]
> G["WankulCardsData.GetFromMonster"]
> H["WankulCardData"]
> I["WankulCardsData.GetAJETER"]
> J["WankulInventory.wankulCards"]
> G --> H
> G --> I
> H --> J
> I --> J
> subgraph Storage ["Storage"]
>     J
> end
> subgraph subGraph1 ["Resolution Pipeline"]
>     H
>     I
> end
> subgraph subGraph0 ["Inventory Action"]
>     F
>     G
>     F --> G
> end
> ```
> Sources: [cards/WankulCardsData.cs L79-L147](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/cards/WankulCardsData.cs#L79-L147)
>  [patch/Inventory.cs L8-L19](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Inventory.cs#L8-L19)
>  [inventory/WankulInventory.cs L12-L15](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/inventory/WankulInventory.cs#L12-L15)

---

## 3. Enumerations and Identifiers

### EItemType

The native game enumeration defining inventory and shop item categories, extended dynamically by the mod to support custom Wankul items such as stellar boosters, clothing items, and specialized mats.

* **Implementation**: Extended via `EnumExtensions.customEnumValues` with custom integer mappings (e.g., `BoosterStellar` = `125`, `CaleconStellar` = `129`) [patch/EItemTypeExtension.cs L15-L35](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/EItemTypeExtension.cs#L15-L35)  and parsed robustly via `SafeParseEItemType()` [patch/EItemTypeExtension.cs L128-L160](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/EItemTypeExtension.cs#L128-L160)

### ECollectionPackType

The native enumeration representing collection pack types, expanded to include custom Wankul seasonal packs.

* **Implementation**: Custom pack types like `Stellar` (value `15`) and `Legacy` (value `19`) are defined in `EnumExtensions` [patch/EItemTypeExtension.cs L38-L47](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/EItemTypeExtension.cs#L38-L47)  and parsed using `SafeParseECollectionPackType()` [patch/EItemTypeExtension.cs L162-L172](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/EItemTypeExtension.cs#L162-L172)

### Booster Hash

A unique hash key calculated for booster pack contents or definitions to ensure deterministic drop verification and prevent duplicate state corruption during opening sequences.

### m_HoldCardPackPosList

An internal game data structure/list tracking held card pack positions within player interaction controllers, manipulated during expanded pack slot patches.

### savedebug

A diagnostic routine or save flag used to dump internal inventory states, association dictionaries, and serialization trees for debugging save/load synchronization bugs.

> **Diagram 3: Enum Extension and Parsing Pipeline**
> ```mermaid
> flowchart TD
> K["String/JSON Item Type"]
> L["EnumExtensions.SafeParseEItemType"]
> M["itemTypeAliases lookup"]
> N["Enum.TryParse"]
> O["customEnumValues dictionary"]
> P["EItemType Value"]
> L --> M
> O --> P
> N --> P
> subgraph subGraph2 ["Output Space"]
>     P
> end
> subgraph subGraph1 ["Parsing Logic"]
>     M
>     N
>     O
>     M --> N
>     N --> O
> end
> subgraph subGraph0 ["Input Space"]
>     K
>     L
>     K --> L
> end
> ```
> Sources: [patch/EItemTypeExtension.cs L110-L160](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/EItemTypeExtension.cs#L110-L160)