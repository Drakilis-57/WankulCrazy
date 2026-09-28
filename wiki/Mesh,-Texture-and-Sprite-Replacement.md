# Mesh, Texture and Sprite Replacement

> **Relevant source files**
> * [importer/OBJImporter.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs)
> * [importer/PatchTexturesImporter.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs)
> * [importer/WankulLoadingScreen.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/WankulLoadingScreen.cs)
> * [patch/WindowsPosters.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/WindowsPosters.cs)
> * [utils/MaterialDiagnostics.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs)
> * [utils/ShaderUtils.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs)
> * [utils/TextureUtils.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/TextureUtils.cs)
> * [utils/obj/CharWordReader.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/CharWordReader.cs)
> * [utils/obj/OBJLoader.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoader.cs)
> * [utils/obj/OBJLoaderHelper.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoaderHelper.cs)
> * [utils/obj/OBJObjectBuilder.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJObjectBuilder.cs)
> * [utils/obj/SplitMode.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/SplitMode.cs)
> * [utils/obj/StringExtensions.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/StringExtensions.cs)

## Purpose and Scope

This page details the asset and content injection architecture responsible for replacing vanilla meshes, textures, sprites, and materials in *WankulCrazy*. It covers the directory-scanning asset importer (`OBJImporter`), the custom streaming `.obj` parser stack (`utils/obj`), the texture patch pipeline (`PatchTexturesImporter`), runtime shader safety (`ShaderUtils`), texture manipulation helpers (`TextureUtils`), and material runtime diagnostics (`MaterialDiagnostics`).

---

## 1. OBJ Importer and Asset Caching (OBJImporter.cs)

The `OBJImporter` class manages custom 3D models and textures placed under the mod's data directory. It initializes file paths, scans directories, and caches assets at startup to avoid runtime I/O bottlenecks [importer/OBJImporter.cs L16-L182](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L16-L182)

### Directory Scanning and Storage

`OBJImporter` establishes paths for meshes, names, and sprites relative to `Plugin.GetPluginPath()` [importer/OBJImporter.cs L20-L22](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L20-L22)

:

* `data/meshes/` (`path_mes`): Stores custom `.obj` 3D models.
* `data/sprites/` (`path_spr`): Stores custom `.png` textures and sprites.
* `data/names/` (`path_nam`): Stores metadata or name configurations.

Files are indexed into case-insensitive dictionaries (`filePaths_obj` and `filePaths_tex`) during `InitFiles()` [importer/OBJImporter.cs L26-L156](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L26-L156)

 Textures and meshes are preloaded and cached via `CacheTexturesAtStart()` and `CacheMeshesAtStart()` into `cachedTextures` and `cachedMeshes` [importer/OBJImporter.cs L28-L182](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L28-L182)

### Podium Mesh and Texture Tracking

The importer maintains arrays of vanilla podium entity identifiers (`podium_meshes` and `podium_textures`) to hook into shop display stands and figurine pedestals [importer/OBJImporter.cs L34-L95](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L34-L95)

```

```

**Sources:** `importer/OBJImporter.cs:16-182`

---

## 2. Custom OBJ Loader Stack (utils/obj)

The `utils/obj` namespace provides a high-performance, custom-built Wavefront `.obj` file parser designed to avoid allocation overhead during runtime model loading.

### Key Parser Components

* **`OBJLoader.cs`**: Reads input streams line-by-line, processing vertices (`v`), normals (`vn`), texture coordinates (`vt`), and faces (`f`) based on a selected `SplitMode` (`Object`, `Material`, or `None`) [utils/obj/OBJLoader.cs L9-L197](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoader.cs#L9-L197)  [utils/obj/SplitMode.cs L7-L13](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/SplitMode.cs#L7-L13)
* **`CharWordReader.cs`**: A zero-allocation character and word streaming reader that processes file buffers directly using character pointers and index tracking (`MoveNext`, `ReadVector`, `ReadFloat`, `ReadInt`) [utils/obj/CharWordReader.cs L9-L182](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/CharWordReader.cs#L9-L182)
* **`OBJLoaderHelper.cs`**: Provides low-level mathematical helpers including `FastFloatParse` and `FastIntParse` for fast numeric conversion, material transparency configuration (`EnableMaterialTransparency`), and safe material fallback generation [utils/obj/OBJLoaderHelper.cs L10-L100](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoaderHelper.cs#L10-L100)
* **`StringExtensions.cs`**: Extends string manipulation with a `Clean()` method to sanitize `.obj` line content by removing extra whitespace and tabs [utils/obj/StringExtensions.cs L7-L17](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/StringExtensions.cs#L7-L17)

```

```

**Sources:** `utils/obj/OBJLoader.cs:9-198`, `utils/obj/CharWordReader.cs:9-182`, `utils/obj/OBJLoaderHelper.cs:10-100`, `utils/obj/SplitMode.cs:7-13`, `utils/obj/StringExtensions.cs:7-17`

---

## 3. Patch Textures Importer (PatchTexturesImporter.cs)

`PatchTexturesImporter` applies texture replacements dynamically to vanilla game resources based on texture quality levels [importer/PatchTexturesImporter.cs L10-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L10-L71)

### Replacement and Conversion Pipeline

When `ReplaceGameTextures(string textureLevel)` executes, it scans `data/patchtextures/<textureLevel>` for replacement image files [importer/PatchTexturesImporter.cs L12-L18](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L12-L18)

For each matching texture found in Unity's loaded resources (`Resources.FindObjectsOfTypeAll<Texture>`), `ReplaceTexture(Texture2D original, Texture2D replacement)` is invoked [importer/PatchTexturesImporter.cs L21-L45](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L21-L45)

 It handles:

* **Empty/Zero-dimension originals:** Generates and copies data to a new texture instance [importer/PatchTexturesImporter.cs L72-L92](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L72-L92)
* **Dimension validation:** Rejects mismatches where `original.width != replacement.width` or height [importer/PatchTexturesImporter.cs L94-L98](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L94-L98)
* **Mipmap and Format Adjustments:** Automatically adjusts mipmap counts (`AdjustMipMapLevels`) and converts texture formats (`ConvertTextureFormat` for DXT1/DXT5 compression constraints) [importer/PatchTexturesImporter.cs L103-L113](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L103-L113)
* **Rendering Enhancement:** Forces trilinear filtering, anisotropic level 16, and negative mipmap bias (`-0.5f`) on replaced textures for high-clarity surface rendering [importer/PatchTexturesImporter.cs L119-L123](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L119-L123)

Known custom texture prefixes (e.g., `Texture_Display_`, `Texture_Booster_`, `MonsterStatue_`, `T_CardSleeve`) are bypassed safely if they are not present in vanilla resources [importer/PatchTexturesImporter.cs L50-L64](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L50-L64)

```

```

**Sources:** `importer/PatchTexturesImporter.cs:10-141`

---

## 4. Shader and Texture Utilities (ShaderUtils.cs, TextureUtils.cs)

To prevent rendering pipeline incompatibilities and the dreaded full-screen magenta shader bug across Built-in, URP, and HDRP render pipelines, specialized utility classes manage shaders and textures.

### Shader Safety (ShaderUtils.cs)

Calling `Shader.Find("Standard")` fails or strips variants in non-Built-in pipelines, resulting in magenta materials [utils/ShaderUtils.cs L7-L12](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L7-L12)

 `ShaderUtils` resolves this by:

* **`GetFallbackLitShader()`**: Detects active scriptable render pipelines (`GraphicsSettings.currentRenderPipeline`) and returns `HDRP/Lit`, `Universal Render Pipeline/Lit`, or safe Built-in fallbacks like `Unlit/UnlitWithShadowcaster` or `Sprites/Default` [utils/ShaderUtils.cs L18-L62](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L18-L62)
* **`UsesBrokenStandardShader(Material m)`**: Identifies if a material relies on the broken `"Standard"` shader [utils/ShaderUtils.cs L70-L73](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L70-L73)
* **`EnsureNotBrokenStandard(Material m)`**: Clones and migrates broken standard materials to safe shaders while preserving texture references (`_MainTex`, `_BaseColorMap`, `_BaseMap`) [utils/ShaderUtils.cs L80-L98](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L80-L98)
* **`CreateSafeMaterial()`**: Instantiates materials safely using pipeline-validated shaders [utils/ShaderUtils.cs L106-L115](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L106-L115)

### Texture Utilities (TextureUtils.cs)

* **`MakeTextureReadable(Texture2D texture)`**: Uses a temporary `RenderTexture` via `Graphics.Blit` to copy GPU-locked textures into CPU-readable `Texture2D` instances without destroying mipmaps or read permissions [utils/TextureUtils.cs L10-L29](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/TextureUtils.cs#L10-L29)
* **`LoadTexture(string path)`**: Reads raw image bytes from disk and loads them into a `Texture2D` object [utils/TextureUtils.cs L31-L38](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/TextureUtils.cs#L31-L38)

**Sources:** `utils/ShaderUtils.cs:1-117`, `utils/TextureUtils.cs:1-40`

---

## 5. Material Diagnostics (MaterialDiagnostics.cs)

`MaterialDiagnostics` serves as a runtime verification tool to debug shader assignment and rendering issues [utils/MaterialDiagnostics.cs L5-L11](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L5-L11)

The primary function `DumpRenderers(GameObject root, string context)` performs a recursive inspection of all `Renderer` components under a target `GameObject` [utils/MaterialDiagnostics.cs L13-L56](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L13-L56)

 For each material slot, it logs:

* Material name and shader reference [utils/MaterialDiagnostics.cs L36-L43](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L36-L43)
* Shader support status (`shader.isSupported`) [utils/MaterialDiagnostics.cs L37-L43](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L37-L43)
* Render queue index [utils/MaterialDiagnostics.cs L44](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L44-L44)
* Texture property assignment checks for `_BaseColorMap`, `_BaseMap`, and `_MainTex` [utils/MaterialDiagnostics.cs L38-L44](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L38-L44)
* Active graphics render pipeline type [utils/MaterialDiagnostics.cs L54](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L54-L54)

If a shader is null or unsupported, an error log flags the renderer slot as a magenta rendering candidate [utils/MaterialDiagnostics.cs L47-L50](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L47-L50)

```

```

**Sources:** `utils/MaterialDiagnostics.cs:1-57`