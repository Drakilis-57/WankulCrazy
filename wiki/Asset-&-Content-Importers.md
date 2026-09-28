# Asset & Content Importers

> **Relevant source files**
> * [importer/CustomItemsImporter.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/CustomItemsImporter.cs)
> * [importer/OBJImporter.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs)
> * [importer/WankulLoadingScreen.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/WankulLoadingScreen.cs)

Overview of importer pipelines that inject custom items, meshes, textures, and loading UI.

## Introduction

The asset and content importer subsystem is responsible for bootstrapping and injecting custom assets into the game's runtime state. This involves loading custom 3D models (`.obj`), textures, sprites, custom items, and shop configurations from the mod's `data/` directory, while managing a frame-budgeted loading screen (`WankulLoadingScreen`) to prevent hitching during initialization.

Sources: `importer/CustomItemsImporter.cs:1-133`, `importer/OBJImporter.cs:1-156`, `importer/WankulLoadingScreen.cs:27-140`

### Importer Architecture Diagram

```

```

Sources: `importer/CustomItemsImporter.cs:20-98`, `importer/OBJImporter.cs:109-156`, `importer/WankulLoadingScreen.cs:27-44`

## 5.1 Custom Items and Shop Registration

For details, see [Custom Items and Shop Registration](/Drakilis-57/WankulCrazy/5.1-custom-items-and-shop-registration).

The `CustomItemsImporter` handles loading custom item definitions, restock data, and mesh bindings from JSON schemas located in `data/customitems/`. It interfaces directly with `InventoryBase` to inject items and restock options into the game's stock item database (`m_StockItemData_SO`) [importer/CustomItemsImporter.cs L20-L25](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/CustomItemsImporter.cs#L20-L25)

 It handles deduplication and positions custom normal and `Taux` variants into the appropriate shop categories and restock slots [importer/CustomItemsImporter.cs L31-L95](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/CustomItemsImporter.cs#L31-L95)

Sources: `importer/CustomItemsImporter.cs:13-98`

## 5.2 Mesh, Texture and Sprite Replacement

For details, see [Mesh, Texture and Sprite Replacement](/Drakilis-57/WankulCrazy/5.2-mesh-texture-and-sprite-replacement).

The `OBJImporter` utility scans `data/meshes/`, `data/sprites/`, and `data/names/` to load and cache custom 3D `.obj` meshes and textures at startup [importer/OBJImporter.cs L18-L156](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L18-L156)

 It maintains lookup dictionaries and fallback structures for podium meshes and textures, allowing vanilla visual assets to be dynamically replaced with custom Wankul models and sprites [importer/OBJImporter.cs L26-L95](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L26-L95)

Sources: `importer/OBJImporter.cs:18-156`

## 5.3 Loading Screen and Debug UI

For details, see [Loading Screen and Debug UI](/Drakilis-57/WankulCrazy/5.3-loading-screen-and-debug-ui).

The `WankulLoadingScreen` provides a frame-budgeted coroutine loading system (`LoadCardsCoroutine`) that processes card textures progressively without dropping frames [importer/WankulLoadingScreen.cs L11-L140](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/WankulLoadingScreen.cs#L11-L140)

 It manages a custom UI canvas created via `WankulUiKit`, tracks load progress, and reports slow cards or missing assets while blocking gameplay inputs during initial resource loading [importer/WankulLoadingScreen.cs L57-L140](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/WankulLoadingScreen.cs#L57-L140)

Sources: `importer/WankulLoadingScreen.cs:11-140`

### Loading & Rendering Pipeline Diagram

```

```

Sources: `importer/WankulLoadingScreen.cs:27-167`