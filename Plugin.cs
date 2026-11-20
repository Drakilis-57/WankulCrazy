using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WankulCrazyPlugin.patch;
using UnityEngine;
using System;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static bool IsDebug { get; private set; }

    public static void LogInfo(string message)
    {
        if (IsDebug && Logger != null)
        {
            Logger.LogInfo(message);
        }
    }

    public static void LogDebug(string message)
    {
        if (IsDebug && Logger != null)
        {
            Logger.LogDebug(message);
        }
    }

    public static void ReportError(string context, Exception ex)
    {
        if (Logger != null)
        {
            Logger.LogError(ex != null ? $"[{context}] {ex}" : $"[{context}]");
        }

        try
        {
            string message = ex != null ? $"{context} : {ex.Message}" : context;
            string stackTrace = ex != null ? ex.ToString() : null;
            WankulDebugScreen.Show("EXCEPTION CAPTUREE", message, stackTrace);
        }
        catch
        {
            // Sécurité anti-crash si l'appel à l'UI échoue
        }
    }

    private readonly struct PatchDescriptor
    {
        public readonly string DisplayName;
        public readonly Type TargetType;
        public readonly string MethodName;
        public readonly Type[] ParamTypes;
        public readonly Type PrefixType;
        public readonly string PrefixMethod;
        public readonly Type PostfixType;
        public readonly string PostfixMethod;
        public readonly Type TranspilerType;
        public readonly string TranspilerMethod;

        public PatchDescriptor(
            Type targetType,
            string methodName,
            Type[] paramTypes = null,
            (Type type, string method)? prefix = null,
            (Type type, string method)? postfix = null,
            (Type type, string method)? transpiler = null,
            string displayName = null)
        {
            TargetType = targetType;
            MethodName = methodName;
            ParamTypes = paramTypes;
            PrefixType = prefix?.type;
            PrefixMethod = prefix?.method;
            PostfixType = postfix?.type;
            PostfixMethod = postfix?.method;
            TranspilerType = transpiler?.type;
            TranspilerMethod = transpiler?.method;
            DisplayName = displayName ?? $"{targetType.Name}.{methodName}";
        }
    }

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        IsDebug = Config.Bind("Debug", "EnableVerboseLogs", false, "Activer les logs detailles (Debug/Info)").Value;
#if DEBUG
        bool enableDevTools = Config.Bind("Debug", "EnableDevTools", false, "Activer les outils de developpement en jeu (F8: Inspecteur, F9: Capture d'ecran, F10: Hot-Reload)").Value;
        bool enableGameBridge = Config.Bind("Debug", "EnableGameBridge", false, "Activer le pont HTTP local de debug (127.0.0.1:8765)").Value;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! (VerboseLogs: {IsDebug}, DevTools: {enableDevTools}, GameBridge: {enableGameBridge})");
#else
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! (VerboseLogs: {IsDebug})");
#endif

        // Initialisation de l'écran de debug pour intercepter les crashs et blocages
        WankulDebugScreen.Initialize();

#if DEBUG
        // Initialisation des outils de développement (F8: Inspecteur, F9: Screenshots, F10: Hot-Reload)
        if (enableDevTools)
        {
            WankulDevTools.Initialize();
        }

        // Initialisation du pont HTTP local pour l'agent (127.0.0.1:8765)
        if (enableGameBridge)
        {
            WankulGameBridge.Initialize();
        }
#endif

        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        // Helper pour sécuriser chaque patch manuel contre les MethodInfo nulls
        void TryPatch(string targetName, MethodInfo original, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null)
        {
            if (original == null)
            {
                Logger.LogWarning($"[HarmonyPatch] Méthode cible originale introuvable pour '{targetName}'. Patch ignoré.");
                return;
            }

            try
            {
                harmony.Patch(
                    original,
                    prefix != null ? new HarmonyMethod(prefix) : null,
                    postfix != null ? new HarmonyMethod(postfix) : null,
                    transpiler != null ? new HarmonyMethod(transpiler) : null
                );
                Logger.LogInfo($"[HarmonyPatch] Patch appliqué avec succès sur '{targetName}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[HarmonyPatch] Erreur lors du patch de '{targetName}': {ex}");
            }
        }

        PatchDescriptor[] patches = new[]
        {
            // Enums custom
            new PatchDescriptor(typeof(Enum), "GetName", new[] { typeof(Type), typeof(object) },
                prefix: (typeof(Patch_Enum_GetName), nameof(Patch_Enum_GetName.Prefix))),
            new PatchDescriptor(typeof(Enum), "IsDefined", new[] { typeof(Type), typeof(object) },
                prefix: (typeof(Patch_Enum_IsDefined), nameof(Patch_Enum_IsDefined.Prefix))),
            new PatchDescriptor(typeof(Enum), "Parse", new[] { typeof(Type), typeof(string), typeof(bool) },
                prefix: (typeof(Patch_Enum_Parse), nameof(Patch_Enum_Parse.Prefix))),

            // Cycle de vie & économie
            new PatchDescriptor(typeof(CGameManager), "OnLevelFinishedLoading",
                postfix: (typeof(GameStarting), nameof(GameStarting.OnLevelFinishedLoading))),
            new PatchDescriptor(typeof(PriceChangeManager), "OnDayStarted",
                postfix: (typeof(CardPrice), nameof(CardPrice.OnDayStarted))),
            new PatchDescriptor(typeof(CSaveLoad), "Save",
                postfix: (typeof(Saves), nameof(Saves.Save))),
            new PatchDescriptor(typeof(CSaveLoad), "Load",
                postfix: (typeof(Saves), nameof(Saves.Load))),

            // Cartes & classeur
            new PatchDescriptor(typeof(CardUI), "SetCardUI",
                prefix: (typeof(ReplacingCards), nameof(ReplacingCards.SetCardUIPrefix)),
                postfix: (typeof(ReplacingCards), "SetCardUIPostFix")),
            new PatchDescriptor(typeof(CollectionBinderFlipAnimCtrl), "EnterViewUpCloseState",
                prefix: (typeof(ReplacingCards), "EnterViewUpCloseStatePrefix"),
                postfix: (typeof(ReplacingCards), "EnterViewUpCloseStatePostfix")),
            new PatchDescriptor(typeof(CollectionBinderFlipAnimCtrl), "UpdateBinderAllCardUI",
                postfix: (typeof(SortUI), nameof(SortUI.UpdateBinderAllCardUI))),
            new PatchDescriptor(typeof(CollectionBinderFlipAnimCtrl), "Update",
                prefix: (typeof(SortUI), nameof(SortUI.Update))),
            new PatchDescriptor(typeof(CollectionBinderFlipAnimCtrl), "OnSortingMethodUpdated",
                prefix: (typeof(SortUI), nameof(SortUI.OnSortingMethodUpdated))),
            new PatchDescriptor(typeof(CollectionBinderFlipAnimCtrl), "OnRightMouseButtonUp",
                prefix: (typeof(ReplacingCards), nameof(ReplacingCards.CollectionBinderFlipAnimCtrlOnRightMouseButtonUp))),
            new PatchDescriptor(typeof(CollectionBinderUI), "OpenSortAlbumScreen",
                prefix: (typeof(SortUI), nameof(SortUI.OpenSortAlbumScreenPrefix)),
                postfix: (typeof(SortUI), nameof(SortUI.OpenSortAlbumScreen))),
            new PatchDescriptor(typeof(MonsterData), "GetIcon",
                prefix: (typeof(ReplacingCards), nameof(ReplacingCards.GetIcon))),
            new PatchDescriptor(typeof(BinderPageGrp), "SetSingleCard",
                prefix: (typeof(ReplacingCards), nameof(ReplacingCards.SetSingleCard))),

            // Ouverture de boosters & machine auto
            new PatchDescriptor(typeof(CardOpeningSequence), "OpenScreen",
                prefix: (typeof(CardOpening), nameof(CardOpening.OpenScreenPrefix))),
            new PatchDescriptor(typeof(CardOpeningSequence), "ReadyingCardPack",
                postfix: (typeof(CardOpening), nameof(CardOpening.ReadyingCardPackPostfix))),
            new PatchDescriptor(typeof(CardOpeningSequence), "GetPackContent",
                postfix: (typeof(CardOpening), nameof(CardOpening.OpenBooster))),
            new PatchDescriptor(typeof(CardOpeningSequence), "GetPackContentCardDataList",
                prefix: (typeof(CardOpening), nameof(CardOpening.GetPackContentCardDataListPrefix))),
            // CardOpeningSequence.Update : deux prefixes distincts conserves dans cet ordre exact
            // (CardOpening.Update retourne bool false pour remplacer l'Update vanilla, UpdatePreFix est void pour le flush XP state=11).
            new PatchDescriptor(typeof(CardOpeningSequence), "Update",
                prefix: (typeof(CardOpening), nameof(CardOpening.Update)),
                displayName: "CardOpeningSequence.Update (CardOpening)"),
            new PatchDescriptor(typeof(CardOpeningSequence), "Update",
                prefix: (typeof(CardOpening), nameof(CardOpening.UpdatePreFix)),
                displayName: "CardOpeningSequence.Update (UpdatePreFix)"),
            new PatchDescriptor(typeof(InteractableAutoPackOpener), "OpenPack",
                prefix: (typeof(InteractableAutoPackOpenerPatch), nameof(InteractableAutoPackOpenerPatch.OpenPackPrefix)),
                postfix: (typeof(InteractableAutoPackOpenerPatch), nameof(InteractableAutoPackOpenerPatch.OpenPackPostfix))),
            new PatchDescriptor(typeof(InteractableAutoPackOpener), "OnMouseButtonUp",
                prefix: (typeof(InteractableAutoPackOpenerPatch), nameof(InteractableAutoPackOpenerPatch.OnMouseButtonUpPrefix))),
            new PatchDescriptor(typeof(ShowCardObtainedPage), "ShowCardObtained",
                postfix: (typeof(InteractableAutoPackOpenerPatch), nameof(InteractableAutoPackOpenerPatch.ShowCardObtainedPostfix))),
            new PatchDescriptor(typeof(Worker), "InitializeCharacter",
                postfix: (typeof(InteractableAutoPackOpenerPatch), nameof(InteractableAutoPackOpenerPatch.SyncWorkerCardPackLists))),
            new PatchDescriptor(typeof(Worker), "LoadWorkerSaveData",
                postfix: (typeof(InteractableAutoPackOpenerPatch), nameof(InteractableAutoPackOpenerPatch.SyncWorkerCardPackLists))),
            new PatchDescriptor(typeof(WorkerSetPackOpenerTypeOptionScreen), "OpenScreen",
                prefix: (typeof(InteractableAutoPackOpenerPatch), nameof(InteractableAutoPackOpenerPatch.WorkerSetPackOpenerTypeOptionScreenOpenPrefix))),

            // Prix, caisse, inventaire & échanges
            new PatchDescriptor(typeof(CPlayerData), "GetCardMarketPrice", new[] { typeof(CardData) },
                prefix: (typeof(CardPrice), nameof(CardPrice.Prefix_GetCardMarketPrice_CardData)),
                displayName: "CPlayerData.GetCardMarketPrice(CardData)"),
            new PatchDescriptor(typeof(CPlayerData), "GetCardMarketPrice", new[] { typeof(int), typeof(ECardExpansionType), typeof(bool), typeof(int) },
                postfix: (typeof(CardPrice), nameof(CardPrice.Postfix_GetCardMarketPrice_Params)),
                displayName: "CPlayerData.GetCardMarketPrice(int, ECardExpansionType, bool, int)"),
            new PatchDescriptor(typeof(CPlayerData), "AddCard",
                postfix: (typeof(Inventory), nameof(Inventory.AddCard))),
            new PatchDescriptor(typeof(CPlayerData), "ReduceCard",
                postfix: (typeof(Inventory), nameof(Inventory.RemoveCard))),
            new PatchDescriptor(typeof(CPlayerData), "GetCardAmount",
                prefix: (typeof(CPlayerDataPatch), nameof(CPlayerDataPatch.GetCardAmount))),
            new PatchDescriptor(typeof(CheckPricePanelUI), "InitCard",
                prefix: (typeof(CheckPriceUI), nameof(CheckPriceUI.CheckPricePanelInitCard))),
            new PatchDescriptor(typeof(CheckPriceScreen), "EvaluateCardPanelUI",
                prefix: (typeof(CheckPriceUI), nameof(CheckPriceUI.EvaluateCardPanelUI))),
            new PatchDescriptor(typeof(CheckPriceScreen), "OnPressOpenCardPriceGraph",
                prefix: (typeof(CheckPriceUI), nameof(CheckPriceUI.OnPressOpenCardPriceGraph))),
            new PatchDescriptor(typeof(ItemPriceGraphScreen), "ShowCardPriceChart",
                prefix: (typeof(CheckPriceUI), nameof(CheckPriceUI.ShowCardPriceChart))),
            new PatchDescriptor(typeof(Customer), "OnPayingDone",
                prefix: (typeof(CardPrice), nameof(CardPrice.OnPayingDone))),
            new PatchDescriptor(typeof(CustomerTradeCardScreen), "SetCustomer",
                prefix: (typeof(CustomerTradeCardScreenPatch), nameof(CustomerTradeCardScreenPatch.SetCustomer))),
            new PatchDescriptor(typeof(UI_CashCounterScreen), "OnCardScanned",
                prefix: (typeof(UI_CashCounterScreenPatch), nameof(UI_CashCounterScreenPatch.OnCardScanned))),

            // Shop, items custom & textures
            new PatchDescriptor(typeof(RestockManager), "GetItemCost", new[] { typeof(EItemType) },
                prefix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetItemCostPrefix))),
            new PatchDescriptor(typeof(RestockManager), "GetItemMarketPrice", new[] { typeof(EItemType) },
                prefix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetItemMarketPricePrefix))),
            new PatchDescriptor(typeof(RestockManager), "GetItemMarketPriceCustomPercent", new[] { typeof(EItemType), typeof(float) },
                prefix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetItemMarketPriceCustomPercentPrefix))),
            new PatchDescriptor(typeof(RestockItemScreen), "EvaluateRestockItemPanelUI", new[] { typeof(int) },
                prefix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.LogShopEvaluation))),
            new PatchDescriptor(typeof(InventoryBase), "ItemTypeToCollectionPackType",
                postfix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.ItemTypeToCollectionPackType))),
            new PatchDescriptor(typeof(InventoryBase), "GetCardExpansionType",
                postfix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetCardExpansionType))),
            new PatchDescriptor(typeof(InventoryBase), "GetItemData", new[] { typeof(EItemType) },
                prefix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetItemDataPrefix))),
            new PatchDescriptor(typeof(InventoryBase), "GetItemMeshData", new[] { typeof(EItemType) },
                prefix: (typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetItemMeshDataPrefix))),
            new PatchDescriptor(typeof(Item), "SetMesh",
                postfix: (typeof(PatchTexturesImporter), "ItemPostfix")),
            new PatchDescriptor(typeof(Item), "SetTexture2DArrayMaterialsForMesh",
                postfix: (typeof(PatchTexturesImporter), "ItemArrayMaterialPostfix")),
            new PatchDescriptor(typeof(Item), "SetTexture2DArrayMaterialsForItem",
                postfix: (typeof(PatchTexturesImporter), "ItemArrayMaterialPostfix")),
            new PatchDescriptor(typeof(UnlockRoomManager), "Init",
                postfix: (typeof(WindowsPosters), nameof(WindowsPosters.Init))),
            new PatchDescriptor(typeof(UI_PriceTag), "SetItemImage", new[] { typeof(EItemType) },
                postfix: (typeof(PriceTagAtlasPatch), nameof(PriceTagAtlasPatch.SetItemImagePostfix))),
            new PatchDescriptor(typeof(PriceTagAtlasMap), "GetItemIconIndex", new[] { typeof(int) },
                prefix: (typeof(PriceTagAtlasPatch), nameof(PriceTagAtlasPatch.GetItemIconIndexPrefix))),

            // Extensions, Workbench & Contrôleur joueur
            new PatchDescriptor(typeof(CardExpansionSelectScreen), "OpenScreen",
                postfix: (typeof(ExpansionScreen), nameof(ExpansionScreen.OpenExpansionScreen))),
            new PatchDescriptor(typeof(CardExpansionSelectScreen), "OnPressButton",
                prefix: (typeof(ExpansionScreen), nameof(ExpansionScreen.OnExpansionPressButton))),
            new PatchDescriptor(typeof(WorkbenchUIScreen), "OnCardExpansionUpdated",
                postfix: (typeof(patch.workbench.WorkbenchPatch), nameof(patch.workbench.WorkbenchPatch.OnCardExpansionUpdated))),
            new PatchDescriptor(typeof(CardRaritySelectScreen), "OpenScreen",
                postfix: (typeof(patch.workbench.WorkbenchPatch), nameof(patch.workbench.WorkbenchPatch.OpenRarityScreen))),
            new PatchDescriptor(typeof(CardRaritySelectScreen), "OnPressButton",
                prefix: (typeof(patch.workbench.WorkbenchPatch), nameof(patch.workbench.WorkbenchPatch.OnRarityPressButton))),
            new PatchDescriptor(typeof(WorkbenchUIScreen), "OnRarityLimitUpdated",
                postfix: (typeof(patch.workbench.WorkbenchPatch), nameof(patch.workbench.WorkbenchPatch.OnRarityLimitUpdated))),
            new PatchDescriptor(typeof(WorkbenchUIScreen), "OpenScreen",
                postfix: (typeof(patch.workbench.WorkbenchPatch), nameof(patch.workbench.WorkbenchPatch.OpenWorkBenchScreen))),
            new PatchDescriptor(typeof(WorkbenchUIScreen), "RunBundleCardBulkFunction",
                prefix: (typeof(patch.workbench.WorkbenchPatch), nameof(patch.workbench.WorkbenchPatch.RunBundleCardBulkFunction))),

            // InteractionPlayerController.EvaluateOpenCardPack : 2 entrees distinctes dans cet ordre exact :
            // 1) CardOpening capture __state.CanOpenCardBox en prefix avant le remplacement, puis distribue les boosters rares/Gold en postfix.
            // 2) InteractionPlayerControllerPatch remplace l'ouverture de display/pack (retourne bool false).
            new PatchDescriptor(typeof(InteractionPlayerController), "EvaluateOpenCardPack",
                prefix: (typeof(CardOpening), nameof(CardOpening.EvaluateOpenCardPackPreFix)),
                postfix: (typeof(CardOpening), nameof(CardOpening.EvaluateOpenCardPackPostFix))),
            new PatchDescriptor(typeof(InteractionPlayerController), "EvaluateOpenCardPack",
                prefix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.EvaluateOpenCardPack)),
                displayName: "InteractionPlayerController.EvaluateOpenCardPack (V2)"),
            new PatchDescriptor(typeof(InteractionPlayerController), "CardBoxToCardPack",
                prefix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.CardBoxToCardPack))),
            new PatchDescriptor(typeof(InteractionPlayerController), "Awake",
                postfix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.AwakePostfix))),
            new PatchDescriptor(typeof(InteractionPlayerController), "RemoveToolTip",
                postfix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.RemoveToolTip))),
            new PatchDescriptor(typeof(InteractionPlayerController), "CanOpenPack",
                prefix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.CanOpenPack))),
            new PatchDescriptor(typeof(InteractionPlayerController), "IsCardPackType",
                prefix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.IsCardPackType))),
            new PatchDescriptor(typeof(InteractionPlayerController), "CanOpenCardBox",
                prefix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.CanOpenCardBox))),
            new PatchDescriptor(typeof(InteractionPlayerController), "DelayLerpSpawnedCardPackToHand",
                postfix: (typeof(InteractionPlayerControllerPatch), nameof(InteractionPlayerControllerPatch.DelayLerpSpawnedCardPackToHandPostfix))),

            // Filtrage logs Unity, chargement de scène & UI PlayCardSet
            new PatchDescriptor(typeof(Debug), "Log", new[] { typeof(object) },
                prefix: (typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogPrefix)),
                displayName: "Debug.Log(object)"),
            new PatchDescriptor(typeof(Debug), "Log", new[] { typeof(object), typeof(UnityEngine.Object) },
                prefix: (typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogContextPrefix)),
                displayName: "Debug.Log(object, Object)"),
            new PatchDescriptor(typeof(Debug), "LogFormat", new[] { typeof(string), typeof(object[]) },
                prefix: (typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogFormatPrefix)),
                displayName: "Debug.LogFormat(string, object[])"),
            new PatchDescriptor(typeof(Debug), "LogWarning", new[] { typeof(object) },
                prefix: (typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogWarningPrefix)),
                displayName: "Debug.LogWarning(object)"),
            new PatchDescriptor(typeof(Debug), "LogWarning", new[] { typeof(object), typeof(UnityEngine.Object) },
                prefix: (typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogWarningContextPrefix)),
                displayName: "Debug.LogWarning(object, Object)"),
            new PatchDescriptor(typeof(Debug), "LogWarningFormat", new[] { typeof(string), typeof(object[]) },
                prefix: (typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogWarningFormatPrefix)),
                displayName: "Debug.LogWarningFormat(string, object[])"),
            new PatchDescriptor(typeof(PlayCardSetUI), "Update",
                prefix: (typeof(PlayCardSetUIPatch), nameof(PlayCardSetUIPatch.Prefix))),
            new PatchDescriptor(typeof(PlayCardSetUI), "LateUpdate",
                prefix: (typeof(PlayCardSetUIPatch), nameof(PlayCardSetUIPatch.LateUpdatePrefix))),
            new PatchDescriptor(typeof(LoadingScreen), "OpenScreen",
                postfix: (typeof(SceneLifecyclePatches), nameof(SceneLifecyclePatches.OpenScreenPostfix))),
            new PatchDescriptor(typeof(SaveLoadGameSlotSelectScreen), "OnPressLoadGame", new[] { typeof(int) },
                prefix: (typeof(SceneLifecyclePatches), nameof(SceneLifecyclePatches.OnPressLoadGamePrefix))),
            new PatchDescriptor(typeof(LoadingScreen), "CloseScreen",
                prefix: (typeof(SceneLifecyclePatches), nameof(SceneLifecyclePatches.CloseScreenPrefix))),
            new PatchDescriptor(typeof(LoadingScreen), "FinishLoading",
                prefix: (typeof(SceneLifecyclePatches), nameof(SceneLifecyclePatches.FinishLoadingPrefix))),
            new PatchDescriptor(typeof(ScreenRatioScaler), "Init",
                prefix: (typeof(SceneLifecyclePatches), nameof(SceneLifecyclePatches.ScreenRatioScalerInitPrefix))),

            // Duel Wankul TCG : récompenses, auto-complétion deck et lancement DuelView3D
            new PatchDescriptor(typeof(PlayTableGame), "EvaluateEndGameGift",
                prefix: (typeof(DuelRewardsPatch), nameof(DuelRewardsPatch.EvaluateEndGameGiftPrefix))),
            new PatchDescriptor(typeof(PlayTableGame), "SetPlayTable",
                prefix: (typeof(DuelOpponentDeckPatch), nameof(DuelOpponentDeckPatch.SetPlayTablePrefix)),
                postfix: (typeof(WankulDuelEntryPatch), nameof(WankulDuelEntryPatch.SetPlayTablePostfix)))
        };

        foreach (var p in patches)
        {
            MethodInfo original = p.ParamTypes != null
                ? AccessTools.Method(p.TargetType, p.MethodName, p.ParamTypes)
                : AccessTools.Method(p.TargetType, p.MethodName);

            MethodInfo ResolveHook(Type hookType, string hookName, string role)
            {
                if (hookType == null) return null;
                MethodInfo m = AccessTools.Method(hookType, hookName);
                if (m == null)
                {
                    Logger.LogWarning($"[HarmonyPatch] Méthode {role} '{hookType.Name}.{hookName}' introuvable pour '{p.DisplayName}'.");
                }
                return m;
            }

            MethodInfo prefix = ResolveHook(p.PrefixType, p.PrefixMethod, "prefix");
            MethodInfo postfix = ResolveHook(p.PostfixType, p.PostfixMethod, "postfix");
            MethodInfo transpiler = ResolveHook(p.TranspilerType, p.TranspilerMethod, "transpiler");

            TryPatch(p.DisplayName, original, prefix, postfix, transpiler);
        }
    }

    public static string GetPluginPath()
    {
        try
        {
            // 1. Détection via l'emplacement réel de la DLL (fonctionne même si extrait dans un sous-dossier)
            string assemblyLocation = typeof(Plugin).Assembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                string assemblyDir = Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(assemblyDir) && Directory.Exists(Path.Combine(assemblyDir, "data")))
                {
                    return assemblyDir;
                }
            }

            // 2. Emplacement standard BepInEx/plugins/WankulCrazy
            if (!string.IsNullOrEmpty(Application.dataPath))
            {
                return Path.Combine(Application.dataPath, "../BepInEx/plugins", PluginInfo.PLUGIN_NAME);
            }
        }
        catch (Exception)
        {
        }
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    // Cache des FieldInfo/MethodInfo résolus par réflexion.
    // Sans ce cache, chaque appel à GetPProperty/SetPProperty (et donc chaque lecture/écriture
    // de CardOpeningHelpers, appelée à CHAQUE FRAME pendant CardOpening.Update) refaisait un
    // Type.GetField coûteux. Un FieldInfo/MethodInfo est stable pour un type donné : on ne le
    // résout qu'une seule fois puis on le réutilise.
    private static readonly Dictionary<(Type, string), FieldInfo> FieldCache = new Dictionary<(Type, string), FieldInfo>();
    private static readonly Dictionary<(Type, string), MethodInfo> MethodCache = new Dictionary<(Type, string), MethodInfo>();
    private const BindingFlags PrivateInstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;

    // type.GetField/GetMethod ne renvoie que les membres déclarés sur le type exact : un champ
    // privé déclaré dans une classe de base du jeu (ex: CollectionBinderFlipAnimCtrl héritant
    // d'un type Unity) n'est PAS trouvé ainsi. AccessTools.Field/Method (Harmony) remonte la
    // hiérarchie ; on reproduit ce comportement ici pour ne pas casser les patches existants
    // (c'est ce qui avait cassé l'affichage de l'album : GetPProperty renvoyait null en boucle).
    private static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(fieldName, PrivateInstanceFlags | BindingFlags.Public);
            if (field != null)
            {
                return field;
            }
        }
        return null;
    }

    private static MethodInfo FindMethodInHierarchy(Type type, string methodName)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            MethodInfo method = current.GetMethod(methodName, PrivateInstanceFlags | BindingFlags.Public);
            if (method != null)
            {
                return method;
            }
        }
        return null;
    }

    private static FieldInfo GetCachedField(Type type, string fieldName)
    {
        var key = (type, fieldName);
        if (!FieldCache.TryGetValue(key, out FieldInfo field))
        {
            field = FindFieldInHierarchy(type, fieldName);
            FieldCache[key] = field; // on cache aussi les échecs (null) pour éviter de refaire la recherche
        }
        return field;
    }

    /// <summary>
    /// Résout et met en cache une MethodInfo privée d'instance pour un type donné (en remontant
    /// la hiérarchie de classes, comme AccessTools.Method). À utiliser à la place de
    /// `instance.GetType().GetMethod(...)` dans les chemins appelés répétitivement (ex: Update par frame).
    /// </summary>
    public static MethodInfo GetCachedMethod(Type type, string methodName)
    {
        var key = (type, methodName);
        if (!MethodCache.TryGetValue(key, out MethodInfo method))
        {
            method = FindMethodInHierarchy(type, methodName);
            MethodCache[key] = method;
        }
        return method;
    }

    public static object GetPProperty(object __instance, string fieldName)
    {
        Type type = __instance.GetType();

        FieldInfo field = GetCachedField(type, fieldName);
        if (field == null)
        {
            Plugin.Logger.LogError($"Field {fieldName} not found");
            return null;
        }
        object value = field.GetValue(__instance);
        return value;
    }

    public static object SetPProperty(object __instance, string fieldName, object value)
    {
        Type type = __instance.GetType();

        FieldInfo field = GetCachedField(type, fieldName);
        if (field == null)
        {
            Plugin.Logger.LogError($"Field {fieldName} not found");
            return value;
        }
        field.SetValue(__instance, value);
        return value;
    }

    public static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    public static Transform FindChildByPath(Transform parent, string path)
    {
        string[] segments = path.Split('/');
        Transform current = parent;

        foreach (string segment in segments)
        {
            current = current.Find(segment);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    public static Transform GetByPathIn(string source, string path)
    {
        return FindChildByPath(GameObject.Find(source).transform, path);
    }

    private static bool _doorObstacleFixed = false;

    private void Update()
    {
        // Correction du bug natif du jeu : BoxCollider Door_Obstacle avec échelle négative qui spamme le moteur physique Unity
        if (!_doorObstacleFixed)
        {
            try
            {
                var obstacle = GameObject.Find("PathfindingColliderAndFloorGrp/Door_Obstacle");
                if (obstacle != null)
                {
                    Vector3 ls = obstacle.transform.localScale;
                    if (ls.x < 0 || ls.y < 0 || ls.z < 0)
                    {
                        obstacle.transform.localScale = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                        Plugin.LogInfo("[Fix] Échelle négative de 'Door_Obstacle' corrigée avec succès (fin du spam BoxCollider).");
                    }
                    _doorObstacleFixed = true;
                }
            }
            catch { }
        }
    }
}

