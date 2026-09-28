# Plugin Bootstrap and Harmony Patch Registry

> **Relevant source files**
> * [Plugin.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs)
> * [patch/DebugFilterPatch.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs)
> * [patch/GameStarting.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/GameStarting.cs)
> * [patch/PlayCardSetUIPatch.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs)
> * [patch/Saves.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Saves.cs)
> * [patch/SceneLifecyclePatches.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/SceneLifecyclePatches.cs)

## Purpose and Scope

This page details the initialization sequence, BepInEx bootstrapping, reflection-based Harmony patching infrastructure, and runtime bugfix patches implemented in `Plugin.cs` and supporting patch classes. The bootstrap process establishes the mod environment, configures verbose logging, activates debug screens, and safely registers dozens of Harmony patches across core game systems.

Sources: [Plugin.cs L1-L176](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L1-L176)

, [patch/GameStarting.cs L1-L29](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/GameStarting.cs#L1-L29)

---

## Plugin Bootstrap Lifecycle (Awake)

The mod entry point is the `Plugin` class, which inherits from BepInEx's `BaseUnityPlugin` and is decorated with the `[BepInPlugin]` attribute [Plugin.cs L14-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L14-L16)

. When the game loads, BepInEx instantiates the plugin and executes its `Awake()` lifecycle method [Plugin.cs L36](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L36-L36)

.

### Initialization Flow

```

```

Sources: [Plugin.cs L36-L46](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L36-L46)

, [patch/GameStarting.cs L8-L17](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/GameStarting.cs#L8-L17)

During `Awake()`, the following sequence executes:

1. **Logger Assignment**: The BepInEx logger reference is captured into an internal static field `[Plugin.cs:39-39]`.
2. **Configuration Binding**: Binds the configuration option `Debug.EnableVerboseLogs` (defaulting to `false`) to control debug output `[Plugin.cs:40-40]`.
3. **Debug Screen Bootstrapping**: Calls `WankulDebugScreen.Initialize()` to set up crash and blockage interception UI `[Plugin.cs:44-44]`.
4. **Harmony Instance Creation**: Instantiates a new `Harmony` router using `PluginInfo.PLUGIN_GUID` `[Plugin.cs:46-46]`.
5. **Patch Registry Execution**: Applies manual patches via the local `TryPatch` helper function [Plugin.cs L49-L176](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L176) .

---

## The TryPatch Utility and Safety Pattern

To prevent unhandled exceptions or hard crashes when game updates alter method signatures or strip symbols, `Plugin.cs` defines a robust inner helper function called `TryPatch` [Plugin.cs L49-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L71)

.

```
void TryPatch(string targetName, MethodInfo original, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null){    if (original == null)    {        Logger.LogWarning($"[HarmonyPatch] Méthode cible originale introuvable pour '{targetName}'. Patch ignoré.");        return;    }     try    {        harmony.Patch(            original,            prefix != null ? new HarmonyMethod(prefix) : null,            postfix != null ? new HarmonyMethod(postfix) : null,            transpiler != null ? new HarmonyMethod(transpiler) : null        );        Logger.LogInfo($"[HarmonyPatch] Patch appliqué avec succès sur '{targetName}'.");    }    catch (Exception ex)    {        Logger.LogError($"[HarmonyPatch] Erreur lors du patch de '{targetName}': {ex}");    }}
```

Sources: [Plugin.cs L49-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L71)

### Key Design Aspects of TryPatch

* **Null Method Guards**: Validates that `original` `MethodInfo` retrieved via `AccessTools.Method` is not null before attempting patching, logging a warning rather than throwing an exception [Plugin.cs L51-L55](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L51-L55) .
* **Exception Isolation**: Wraps `harmony.Patch()` in a `try-catch` block to capture reflection or IL weaving errors gracefully [Plugin.cs L57-L70](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L57-L70) .

Sources: [Plugin.cs L49-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L71)

---

## Harmony Patch Registry Catalog

The registry manages patches across core subsystems including enums, game managers, pricing, card opening mechanics, and save/load routines.

```

```

Sources: [Plugin.cs L73-L161](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L73-L161)

### Registered Patch Mapping Table

| Target Method Name | Target Type & Method | Patch Handlers | Purpose |
| --- | --- | --- | --- |
| `Enum.GetName` | `System.Enum.GetName(Type, object)` | `Patch_Enum_GetName.Prefix` | Safe enum name lookup |
| `Enum.IsDefined` | `System.Enum.IsDefined(Type, object)` | `Patch_Enum_IsDefined.Prefix` | Safe enum definition check |
| `Enum.Parse` | `System.Enum.Parse(Type, string, bool)` | `Patch_Enum_Parse.Prefix` | Safe enum parsing fallback |
| `CGameManager.OnLevelFinishedLoading` | `CGameManager.OnLevelFinishedLoading()` | `GameStarting.OnLevelFinishedLoading` | Content initialization & asset cleanup |
| `PriceChangeManager.OnDayStarted` | `PriceChangeManager.OnDayStarted()` | `CardPrice.OnDayStarted` | Daily card price recalculation |
| `CardUI.SetCardUI` | `CardUI.SetCardUI()` | `ReplacingCards.SetCardUIPrefix`, `ReplacingCards.SetCardUIPostFix` | Replace card visuals and foil textures |
| `CardOpeningSequence.OpenScreen` | `CardOpeningSequence.OpenScreen()` | `CardOpening.OpenScreenPrefix` | Booster opening sequence initialization |
| `CSaveLoad.Save` / `Load` | `CSaveLoad.Save()`, `CSaveLoad.Load()` | `Saves.Save`, `Saves.Load` | Intercept mod state persistence |

Sources: `[Plugin.cs:73-160]`, `[patch/GameStarting.cs:8-28]`, [patch/Saves.cs L9-L17](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Saves.cs#L9-L17)

---

## Runtime Bugfixes and Defensive Interceptors

To ensure stability during high-frequency updates, scene transitions, and corrupted log output, several defensive patches intercept game execution and clean up engine-level errors.

### 1. Log Filtering (DebugFilterPatch)

The `DebugFilterPatch` class filters out noisy or non-actionable Unity warnings and errors from the console output (such as font asset underline warnings, root `DontDestroyOnLoad` complaints, negative scale box colliders, and UI percentage updates) [patch/DebugFilterPatch.cs L8-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs#L8-L16)

.

```java
private static bool ShouldIgnore(string text){    if (string.IsNullOrEmpty(text)) return false;    return text.Contains("The character used for Underline is not available in font asset")        || text.Contains("DontDestroyOnLoad only works for root GameObjects")        || text.Contains("Parent of RectTransform is being set with parent property")        || text.Contains("BoxCollider does not support negative scale or size")        || text.Contains("percentDone");}
```

Sources: [patch/DebugFilterPatch.cs L8-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs#L8-L16)

### 2. UI Null-Reference Guards (PlayCardSetUIPatch)

`PlayCardSetUIPatch` protects `PlayCardSetUI.Update` and `LateUpdatePrefix` from triggering `NullReferenceException` when tables or card sets are destroyed or uninitialized during gameplay loops [patch/PlayCardSetUIPatch.cs L12-L43](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs#L12-L43)

.

```
public static bool Prefix(PlayCardSetUI __instance, PlayCardSet ___m_PlayCardSet){    if (__instance == null) return false;    if (___m_PlayCardSet == null || ___m_PlayCardSet.m_PlayTableGame == null) return false;    return true;}
```

Sources: [patch/PlayCardSetUIPatch.cs L12-L27](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs#L12-L27)

### 3. Scene Lifecycle Safety (SceneLifecyclePatches)

Prevents premature singleton access crashes in `LoadingScreen.CloseScreen` and `ScreenRatioScaler.Init` during early startup or shutdown phases [patch/SceneLifecyclePatches.cs L7-L49](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/SceneLifecyclePatches.cs#L7-L49)

.

Sources: `[patch/SceneLifecyclePatches.cs:7-49]`, [patch/DebugFilterPatch.cs L1-L72](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs#L1-L72)

, [patch/PlayCardSetUIPatch.cs L1-L44](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs#L1-L44)