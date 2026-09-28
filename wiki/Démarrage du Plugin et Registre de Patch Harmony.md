# Registre des plugins Bootstrap et Harmony Patch

> **Fichiers sources pertinents**
> * [Plugin.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs)
> * [patch/DebugFilterPatch.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs)
> * [patch/GameStarting.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/GameStarting.cs)
> * [patch/PlayCardSetUIPatch.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs)
> * [patch/Saves.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Saves.cs)
> * [patch/SceneLifecyclePatches.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/SceneLifecyclePatches.cs)

## Objectif et portée

Cette page détaille la séquence d'initialisation, l'amorçage BepInEx, l'infrastructure de correctifs Harmony basée sur la réflexion et les correctifs de correction de bogues d'exécution implémentés dans `Plugin.cs` et les classes de correctifs prenant en charge.Le processus d'amorçage établit l'environnement de mod, configure la journalisation détaillée, active les écrans de débogage et enregistre en toute sécurité des dizaines de correctifs Harmony sur les principaux systèmes de jeu.

Sources : [Plugin.cs L1-L176](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L1-L176)

, [patch/GameStarting.cs L1-L29](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/GameStarting.cs#L1-L29)

---

## Plugin Bootstrap Lifecycle (Awake)

Le point d'entrée du mod est la classe `Plugin`, qui hérite du `BaseUnityPlugin` de BepInEx et est décorée avec l'attribut `[BepInPlugin]` [Plugin.cs L14-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L14-L16)

.Lorsque le jeu se charge, BepInEx instancie le plugin et exécute sa méthode de cycle de vie `Awake()` [Plugin.cs L36](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L36-L36)

.

### Flux d'initialisation

```

```

Sources : [Plugin.cs L36-L46](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L36-L46)

, [patch/GameStarting.cs L8-L17](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/GameStarting.cs#L8-L17)

Pendant `Awake()`, la séquence suivante s'exécute :

1. **Affectation de l'enregistreur** : la référence de l'enregistreur BepInEx est capturée dans un champ statique interne `[Plugin.cs:39-39]`.
2. **Liaison de configuration** : lie l'option de configuration `Debug.EnableVerboseLogs` (par défaut à `false`) pour contrôler la sortie de débogage `[Plugin.cs:40-40]`.
3. **Amorçage de l'écran de débogage** : appelle `WankulDebugScreen.Initialize()` pour configurer l'interface utilisateur d'interception de crash et de blocage `[Plugin.cs:44-44]`.
4. **Création d'instance Harmony** : instancie un nouveau routeur `Harmony` à l'aide de `PluginInfo.PLUGIN_GUID` `[Plugin.cs:46-46]`.
5. **Exécution du registre de correctifs** : applique les correctifs manuels via la fonction d'assistance locale `TryPatch` [Plugin.cs L49-L176](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L176).

---

## L'Utilitaire TryPatch et le Modèle de Sécurité (Safety Pattern)

Pour éviter les exceptions non gérées ou les plantages matériels lorsque les mises à jour du jeu modifient les signatures de méthode ou les symboles de bande, `Plugin.cs` définit une fonction d'assistance interne robuste appelée `TryPatch` [Plugin.cs L49-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L71)

.

```
void TryPatch(string targetName, MethodInfo original, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null){    if (original == null)    {        Logger.LogWarning($"[HarmonyPatch] Méthode cible originale introuvable pour '{targetName}'. Patch ignoré.");        return;    }     try    {        harmony.Patch(            original,            prefix != null ? new HarmonyMethod(prefix) : null,            postfix != null ? new HarmonyMethod(postfix) : null,            transpiler != null ? new HarmonyMethod(transpiler) : null        );        Logger.LogInfo($"[HarmonyPatch] Patch appliqué avec succès sur '{targetName}'.");    }    catch (Exception ex)    {        Logger.LogError($"[HarmonyPatch] Erreur lors du patch de '{targetName}': {ex}");    }}
```

Sources : [Plugin.cs L49-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L71)

### Aspects clés de la conception de TryPatch

* **Null Method Guards** : vérifie que `original` `MethodInfo` récupéré via `AccessTools.Method` n'est pas nul avant de tenter l'application de correctifs, en enregistrant un avertissement plutôt que de lancer une exception [Plugin.csL51-L55](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L51-L55) .
* **Isolement des exceptions** : enveloppe `harmony.Patch()` dans un bloc `try-catch` pour capturer gracieusement les erreurs de réflexion ou de tissage IL [Plugin.cs L57-L70](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L57-L70) .

Sources : [Plugin.cs L49-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L49-L71)

---

## Catalogue du registre des correctifs Harmony

Le registre gère les correctifs sur les sous-systèmes principaux, notamment les énumérations, les gestionnaires de jeux, les prix, les mécanismes d'ouverture de cartes et les routines de sauvegarde/chargement.

```

```

Sources: [Plugin.cs L73-L161](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/Plugin.cs#L73-L161)

### Tableau de mappage des correctifs enregistrés

|Nom de la méthode cible |Type et méthode de cible |Gestionnaires de correctifs |Objectif |
|--- |--- |--- |--- |
|`Enum.GetName` |`System.Enum.GetName(Type, object)` |`Patch_Enum_GetName.Prefix` |Recherche de nom d'énumération sécurisée |
|`Enum.IsDefined` |`System.Enum.IsDefined(Type, object)` |`Patch_Enum_IsDefined.Prefix` |Vérification de la définition de l'énumération sécurisée |
|`Enum.Parse` |`System.Enum.Parse(Type, string, bool)` |`Patch_Enum_Parse.Prefix` |Solution de secours pour l'analyse d'énumération sécurisée |
|`CGameManager.OnLevelFinishedLoading` |`CGameManager.OnLevelFinishedLoading()` |`GameStarting.OnLevelFinishedLoading` |Initialisation du contenu et nettoyage des actifs |
|`PriceChangeManager.OnDayStarted` |`PriceChangeManager.OnDayStarted()` |`CardPrice.OnDayStarted` |Recalcul quotidien du prix de la carte |
|`CardUI.SetCardUI` |`CardUI.SetCardUI()` |`ReplacingCards.SetCardUIPrefix`, `ReplacingCards.SetCardUIPostFix` |Remplacez les visuels des cartes et les textures des feuilles |
| `CardOpeningSequence.OpenScreen` | `CardOpeningSequence.OpenScreen()` | `CardOpening.OpenScreenPrefix` | Booster opening sequence initialization |
|`CSaveLoad.Save` / `Load` |`CSaveLoad.Save()`, `CSaveLoad.Load()` |`Saves.Save`, `Saves.Load` |Intercepter la persistance de l’état du mod |

Sources : `[Plugin.cs:73-160]`, `[patch/GameStarting.cs:8-28]`, [patch/Saves.cs L9-L17](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/Saves.cs#L9-L17)

---

## Corrections de bugs d'exécution et intercepteurs défensifs

Pour garantir la stabilité lors des mises à jour à haute fréquence, des transitions de scène et des sorties de journaux corrompues, plusieurs correctifs défensifs interceptent l'exécution du jeu et nettoient les erreurs au niveau du moteur.

### 1. Filtrage des journaux (DebugFilterPatch)

La classe `DebugFilterPatch` filtre les avertissements et erreurs Unity bruyants ou non exploitables de la sortie de la console (tels que les avertissements de soulignement des ressources de police, les plaintes racine `DontDestroyOnLoad`, les collisionneurs de boîtes d'échelle négative et les mises à jour de pourcentage de l'interface utilisateur) [patch/DebugFilterPatch.csL8-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs#L8-L16)

.

```java
private static bool ShouldIgnore(string text){    if (string.IsNullOrEmpty(text)) return false;    return text.Contains("The character used for Underline is not available in font asset")        || text.Contains("DontDestroyOnLoad only works for root GameObjects")        || text.Contains("Parent of RectTransform is being set with parent property")        || text.Contains("BoxCollider does not support negative scale or size")        || text.Contains("percentDone");}
```

Sources : [patch/DebugFilterPatch.cs L8-L16](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs#L8-L16)

### 2. Gardes de référence nulle de l'interface utilisateur (PlayCardSetUIPatch)

`PlayCardSetUIPatch` empêche `PlayCardSetUI.Update` et `LateUpdatePrefix` de déclencher `NullReferenceException` lorsque des tables ou des jeux de cartes sont détruits ou non initialisés pendant les boucles de jeu [patch/PlayCardSetUIPatch.csL12-L43](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs#L12-L43)

.

```
public static bool Prefix(PlayCardSetUI __instance, PlayCardSet ___m_PlayCardSet){    if (__instance == null) return false;    if (___m_PlayCardSet == null || ___m_PlayCardSet.m_PlayTableGame == null) return false;    return true;}
```

Sources : [patch/PlayCardSetUIPatch.cs L12-L27](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs#L12-L27)

### 3. Sécurité du cycle de vie de la scène (SceneLifecyclePatches)

Empêche les plantages prématurés d'accès singleton dans `LoadingScreen.CloseScreen` et `ScreenRatioScaler.Init` pendant les premières phases de démarrage ou d'arrêt [patch/SceneLifecyclePatches.cs L7-L49](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/SceneLifecyclePatches.cs#L7-L49)

.

Sources : `[patch/SceneLifecyclePatches.cs:7-49]`, [patch/DebugFilterPatch.cs L1-L72](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/DebugFilterPatch.cs#L1-L72)

, [patch/PlayCardSetUIPatch.cs L1-L44](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/PlayCardSetUIPatch.cs#L1-L44)