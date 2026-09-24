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
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        // Initialisation de l'écran de debug pour intercepter les crashs et blocages
        WankulDebugScreen.Initialize();

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
                Logger.LogError($"[HarmonyPatch] Erreur lors du patch de '{targetName}': {ex.Message}");
            }
        }

        // 🎯 Patch `Enum.GetName()`
        TryPatch(
            "Enum.GetName",
            AccessTools.Method(typeof(Enum), "GetName", new Type[] { typeof(Type), typeof(object) }),
            prefix: AccessTools.Method(typeof(Patch_Enum_GetName), "Prefix")
        );

        // 🎯 Patch `Enum.IsDefined()`
        TryPatch(
            "Enum.IsDefined",
            AccessTools.Method(typeof(Enum), "IsDefined", new Type[] { typeof(Type), typeof(object) }),
            prefix: AccessTools.Method(typeof(Patch_Enum_IsDefined), "Prefix")
        );

        // 🎯 Patch `Enum.Parse()`
        TryPatch(
            "Enum.Parse",
            AccessTools.Method(typeof(Enum), "Parse", new Type[] { typeof(Type), typeof(string), typeof(bool) }),
            prefix: AccessTools.Method(typeof(Patch_Enum_Parse), "Prefix")
        );

        TryPatch(
            "CGameManager.OnLevelFinishedLoading",
            AccessTools.Method(typeof(CGameManager), "OnLevelFinishedLoading"),
            postfix: AccessTools.Method(typeof(GameStarting), "OnLevelFinishedLoading")
        );

        TryPatch(
            "PriceChangeManager.OnDayStarted",
            AccessTools.Method(typeof(PriceChangeManager), "OnDayStarted"),
            postfix: AccessTools.Method(typeof(CardPrice), "OnDayStarted")
        );

        TryPatch(
            "CardUI.SetCardUI",
            AccessTools.Method(typeof(CardUI), "SetCardUI"),
            prefix: AccessTools.Method(typeof(ReplacingCards), "SetCardUIPrefix"),
            postfix: AccessTools.Method(typeof(ReplacingCards), "SetCardUIPostFix")
        );

        TryPatch(
            "CollectionBinderFlipAnimCtrl.EnterViewUpCloseState",
            AccessTools.Method(typeof(CollectionBinderFlipAnimCtrl), "EnterViewUpCloseState"),
            prefix: AccessTools.Method(typeof(ReplacingCards), "EnterViewUpCloseStatePrefix"),
            postfix: AccessTools.Method(typeof(ReplacingCards), "EnterViewUpCloseStatePostfix")
        );

        TryPatch(
            "CardOpeningSequence.OpenScreen",
            AccessTools.Method(typeof(CardOpeningSequence), "OpenScreen"),
            prefix: AccessTools.Method(typeof(CardOpening), "OpenScreenPrefix")
        );

        TryPatch(
            "CardOpeningSequence.GetPackContent",
            AccessTools.Method(typeof(CardOpeningSequence), "GetPackContent"),
            postfix: AccessTools.Method(typeof(CardOpening), "OpenBooster")
        );

        TryPatch(
            "CardOpeningSequence.Update (CardOpening)",
            AccessTools.Method(typeof(CardOpeningSequence), "Update"),
            prefix: AccessTools.Method(typeof(CardOpening), "Update")
        );

        TryPatch(
            "CardOpeningSequence.Update (UpdatePreFix)",
            AccessTools.Method(typeof(CardOpeningSequence), "Update"),
            prefix: AccessTools.Method(typeof(CardOpening), "UpdatePreFix")
        );

        TryPatch(
            "CollectionBinderFlipAnimCtrl.UpdateBinderAllCardUI",
            AccessTools.Method(typeof(CollectionBinderFlipAnimCtrl), "UpdateBinderAllCardUI"),
            postfix: AccessTools.Method(typeof(SortUI), "UpdateBinderAllCardUI")
        );

        TryPatch(
            "CSaveLoad.Save",
            AccessTools.Method(typeof(CSaveLoad), "Save"),
            postfix: AccessTools.Method(typeof(Saves), "Save")
        );

        TryPatch(
            "CSaveLoad.Load",
            AccessTools.Method(typeof(CSaveLoad), "Load"),
            postfix: AccessTools.Method(typeof(Saves), "Load")
        );

        // Récupère la méthode originale à patcher en spécifiant les paramètres
        TryPatch(
            "CPlayerData.GetCardMarketPrice(CardData)",
            AccessTools.Method(typeof(CPlayerData), "GetCardMarketPrice", new[] { typeof(CardData) }),
            postfix: AccessTools.Method(typeof(CardPrice), nameof(CardPrice.Postfix_GetCardMarketPrice_CardData))
        );

        TryPatch(
            "CPlayerData.GetCardMarketPrice(int, ECardExpansionType, bool, int)",
            AccessTools.Method(typeof(CPlayerData), "GetCardMarketPrice", new[] { typeof(int), typeof(ECardExpansionType), typeof(bool), typeof(int) }),
            postfix: AccessTools.Method(typeof(CardPrice), nameof(CardPrice.Postfix_GetCardMarketPrice_Params))
        );

        TryPatch(
            "CheckPricePanelUI.InitCard",
            AccessTools.Method(typeof(CheckPricePanelUI), "InitCard"),
            prefix: AccessTools.Method(typeof(CheckPriceUI), "CheckPricePanelInitCard")
        );

        TryPatch(
            "CheckPriceScreen.EvaluateCardPanelUI",
            AccessTools.Method(typeof(CheckPriceScreen), "EvaluateCardPanelUI"),
            prefix: AccessTools.Method(typeof(CheckPriceUI), "EvaluateCardPanelUI")
        );

        TryPatch(
            "CheckPriceScreen.OnPressOpenCardPriceGraph",
            AccessTools.Method(typeof(CheckPriceScreen), "OnPressOpenCardPriceGraph"),
            prefix: AccessTools.Method(typeof(CheckPriceUI), "OnPressOpenCardPriceGraph")
        );

        TryPatch(
            "ItemPriceGraphScreen.ShowCardPriceChart",
            AccessTools.Method(typeof(ItemPriceGraphScreen), "ShowCardPriceChart"),
            prefix: AccessTools.Method(typeof(CheckPriceUI), "ShowCardPriceChart")
        );

        TryPatch(
            "CPlayerData.AddCard",
            AccessTools.Method(typeof(CPlayerData), "AddCard"),
            postfix: AccessTools.Method(typeof(Inventory), "AddCard")
        );

        TryPatch(
            "CPlayerData.ReduceCard",
            AccessTools.Method(typeof(CPlayerData), "ReduceCard"),
            postfix: AccessTools.Method(typeof(Inventory), "RemoveCard")
        );

        TryPatch(
            "MonsterData.GetIcon",
            AccessTools.Method(typeof(MonsterData), "GetIcon"),
            prefix: AccessTools.Method(typeof(ReplacingCards), "GetIcon")
        );

        TryPatch(
            "CollectionBinderUI.OpenSortAlbumScreen",
            AccessTools.Method(typeof(CollectionBinderUI), "OpenSortAlbumScreen"),
            prefix: AccessTools.Method(typeof(SortUI), "OpenSortAlbumScreenPrefix"),
            postfix: AccessTools.Method(typeof(SortUI), "OpenSortAlbumScreen")
        );

        TryPatch(
            "CollectionBinderFlipAnimCtrl.Update",
            AccessTools.Method(typeof(CollectionBinderFlipAnimCtrl), "Update"),
            prefix: AccessTools.Method(typeof(SortUI), "Update")
        );

        TryPatch(
            "CollectionBinderFlipAnimCtrl.OnSortingMethodUpdated",
            AccessTools.Method(typeof(CollectionBinderFlipAnimCtrl), "OnSortingMethodUpdated"),
            prefix: AccessTools.Method(typeof(SortUI), "OnSortingMethodUpdated")
        );

        TryPatch(
            "CardExpansionSelectScreen.OpenScreen",
            AccessTools.Method(typeof(CardExpansionSelectScreen), "OpenScreen"),
            postfix: AccessTools.Method(typeof(ExpansionScreen), "OpenExpansionScreen")
        );

        TryPatch(
            "CardExpansionSelectScreen.OnPressButton",
            AccessTools.Method(typeof(CardExpansionSelectScreen), "OnPressButton"),
            prefix: AccessTools.Method(typeof(ExpansionScreen), "OnExpansionPressButton")
        );

        TryPatch(
            "WorkbenchUIScreen.OnCardExpansionUpdated",
            AccessTools.Method(typeof(WorkbenchUIScreen), "OnCardExpansionUpdated"),
            postfix: AccessTools.Method(typeof(patch.workbench.WorkbenchPatch), "OnCardExpansionUpdated")
        );

        TryPatch(
            "CardRaritySelectScreen.OpenScreen",
            AccessTools.Method(typeof(CardRaritySelectScreen), "OpenScreen"),
            postfix: AccessTools.Method(typeof(patch.workbench.WorkbenchPatch), "OpenRarityScreen")
        );

        TryPatch(
            "CardRaritySelectScreen.OnPressButton",
            AccessTools.Method(typeof(CardRaritySelectScreen), "OnPressButton"),
            prefix: AccessTools.Method(typeof(patch.workbench.WorkbenchPatch), "OnRarityPressButton")
        );

        TryPatch(
            "WorkbenchUIScreen.OnRarityLimitUpdated",
            AccessTools.Method(typeof(WorkbenchUIScreen), "OnRarityLimitUpdated"),
            postfix: AccessTools.Method(typeof(patch.workbench.WorkbenchPatch), "OnRarityLimitUpdated")
        );

        TryPatch(
            "WorkbenchUIScreen.OpenScreen",
            AccessTools.Method(typeof(WorkbenchUIScreen), "OpenScreen"),
            postfix: AccessTools.Method(typeof(patch.workbench.WorkbenchPatch), "OpenWorkBenchScreen")
        );

        TryPatch(
            "WorkbenchUIScreen.RunBundleCardBulkFunction",
            AccessTools.Method(typeof(WorkbenchUIScreen), "RunBundleCardBulkFunction"),
            prefix: AccessTools.Method(typeof(patch.workbench.WorkbenchPatch), "RunBundleCardBulkFunction")
        );

        TryPatch(
            "BinderPageGrp.SetSingleCard",
            AccessTools.Method(typeof(BinderPageGrp), "SetSingleCard"),
            prefix: AccessTools.Method(typeof(ReplacingCards), "SetSingleCard")
        );

        TryPatch(
            "InteractionPlayerController.EvaluateOpenCardPack",
            AccessTools.Method(typeof(InteractionPlayerController), "EvaluateOpenCardPack"),
            prefix: AccessTools.Method(typeof(CardOpening), "EvaluateOpenCardPackPreFix"),
            postfix: AccessTools.Method(typeof(CardOpening), "EvaluateOpenCardPackPostFix")
        );

        TryPatch(
            "CollectionBinderFlipAnimCtrl.OnRightMouseButtonUp",
            AccessTools.Method(typeof(CollectionBinderFlipAnimCtrl), "OnRightMouseButtonUp"),
            prefix: AccessTools.Method(typeof(ReplacingCards), "CollectionBinderFlipAnimCtrlOnRightMouseButtonUp")
        );

        TryPatch(
            "Customer.OnPayingDone",
            AccessTools.Method(typeof(Customer), "OnPayingDone"),
            prefix: AccessTools.Method(typeof(CardPrice), "OnPayingDone")
        );

        TryPatch(
            "InventoryBase.ItemTypeToCollectionPackType",
            AccessTools.Method(typeof(InventoryBase), "ItemTypeToCollectionPackType"),
            prefix: AccessTools.Method(typeof(CustomItemsImporter), "ItemTypeToCollectionPackType")
        );

        TryPatch(
            "InventoryBase.GetCardExpansionType",
            AccessTools.Method(typeof(InventoryBase), "GetCardExpansionType"),
            prefix: AccessTools.Method(typeof(CustomItemsImporter), "GetCardExpansionType")
        );

        TryPatch(
            "InteractionPlayerController.CardBoxToCardPack",
            AccessTools.Method(typeof(InteractionPlayerController), "CardBoxToCardPack"),
            prefix: AccessTools.Method(typeof(WankulCrazyPlugin.patch.InteractionPlayerControllerPatch), "CardBoxToCardPack")
        );

        TryPatch(
            "InteractionPlayerController.Awake",
            AccessTools.Method(typeof(InteractionPlayerController), "Awake"),
            postfix: AccessTools.Method(typeof(WankulCrazyPlugin.patch.InteractionPlayerControllerPatch), "AwakePostfix")
        );

        TryPatch(
            "InteractionPlayerController.EvaluateOpenCardPack (V2)",
            AccessTools.Method(typeof(InteractionPlayerController), "EvaluateOpenCardPack"),
            prefix: AccessTools.Method(typeof(WankulCrazyPlugin.patch.InteractionPlayerControllerPatch), "EvaluateOpenCardPack")
        );

        TryPatch(
            "InteractionPlayerController.RemoveToolTip",
            AccessTools.Method(typeof(InteractionPlayerController), "RemoveToolTip"),
            postfix: AccessTools.Method(typeof(WankulCrazyPlugin.patch.InteractionPlayerControllerPatch), "RemoveToolTip")
        );

        TryPatch(
            "InteractionPlayerController.CanOpenPack",
            AccessTools.Method(typeof(InteractionPlayerController), "CanOpenPack"),
            prefix: AccessTools.Method(typeof(WankulCrazyPlugin.patch.InteractionPlayerControllerPatch), "CanOpenPack")
        );

        TryPatch(
            "InteractionPlayerController.CanOpenCardBox",
            AccessTools.Method(typeof(InteractionPlayerController), "CanOpenCardBox"),
            prefix: AccessTools.Method(typeof(WankulCrazyPlugin.patch.InteractionPlayerControllerPatch), "CanOpenCardBox")
        );

        TryPatch(
            "InteractionPlayerController.DelayLerpSpawnedCardPackToHand",
            AccessTools.Method(typeof(InteractionPlayerController), "DelayLerpSpawnedCardPackToHand"),
            postfix: AccessTools.Method(typeof(WankulCrazyPlugin.patch.InteractionPlayerControllerPatch), "DelayLerpSpawnedCardPackToHandPostfix")
        );

        TryPatch(
            "Item.SetMesh",
            AccessTools.Method(typeof(Item), "SetMesh"),
            postfix: AccessTools.Method(typeof(WankulCrazyPlugin.importer.PatchTexturesImporter), "ItemPostfix")
        );

        TryPatch(
            "UnlockRoomManager.Init",
            AccessTools.Method(typeof(UnlockRoomManager), "Init"),
            postfix: AccessTools.Method(typeof(WindowsPosters), "Init")
        );

        TryPatch(
            "CustomerTradeCardScreen.SetCustomer",
            AccessTools.Method(typeof(CustomerTradeCardScreen), "SetCustomer"),
            prefix: AccessTools.Method(typeof(CustomerTradeCardScreenPatch), "SetCustomer")
        );

        TryPatch(
            "UI_CashCounterScreen.OnCardScanned",
            AccessTools.Method(typeof(UI_CashCounterScreen), "OnCardScanned"),
            prefix: AccessTools.Method(typeof(UI_CashCounterScreenPatch), "OnCardScanned")
        );

        TryPatch(
            "CPlayerData.GetCardAmount",
            AccessTools.Method(typeof(CPlayerData), "GetCardAmount"),
            prefix: AccessTools.Method(typeof(CPlayerDataPatch), "GetCardAmount")
        );

        TryPatch(
            "Debug.LogWarning(object)",
            AccessTools.Method(typeof(Debug), "LogWarning", new[] { typeof(object) }),
            prefix: AccessTools.Method(typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogWarningPrefix))
        );

        TryPatch(
            "Debug.LogWarning(object, Object)",
            AccessTools.Method(typeof(Debug), "LogWarning", new[] { typeof(object), typeof(UnityEngine.Object) }),
            prefix: AccessTools.Method(typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogWarningContextPrefix))
        );

        TryPatch(
            "Debug.LogWarningFormat(string, object[])",
            AccessTools.Method(typeof(Debug), "LogWarningFormat", new[] { typeof(string), typeof(object[]) }),
            prefix: AccessTools.Method(typeof(DebugFilterPatch), nameof(DebugFilterPatch.LogWarningFormatPrefix))
        );

        TryPatch(
            "PlayCardSetUI.Update",
            AccessTools.Method(typeof(PlayCardSetUI), "Update"),
            prefix: AccessTools.Method(typeof(PlayCardSetUIPatch), nameof(PlayCardSetUIPatch.Prefix))
        );

        TryPatch(
            "PlayCardSetUI.LateUpdate",
            AccessTools.Method(typeof(PlayCardSetUI), "LateUpdate"),
            prefix: AccessTools.Method(typeof(PlayCardSetUIPatch), nameof(PlayCardSetUIPatch.LateUpdatePrefix))
        );

        TryPatch(
            "InventoryBase.GetItemData",
            AccessTools.Method(typeof(InventoryBase), "GetItemData", new[] { typeof(EItemType) }),
            prefix: AccessTools.Method(typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetItemDataPrefix))
        );

        TryPatch(
            "InventoryBase.GetItemMeshData",
            AccessTools.Method(typeof(InventoryBase), "GetItemMeshData", new[] { typeof(EItemType) }),
            prefix: AccessTools.Method(typeof(CustomItemsImporter), nameof(CustomItemsImporter.GetItemMeshDataPrefix))
        );

        TryPatch(
            "LoadingScreen.CloseScreen",
            AccessTools.Method(typeof(LoadingScreen), "CloseScreen"),
            prefix: AccessTools.Method(typeof(SceneLifecyclePatches), nameof(SceneLifecyclePatches.CloseScreenPrefix))
        );

        TryPatch(
            "ScreenRatioScaler.Init",
            AccessTools.Method(typeof(ScreenRatioScaler), "Init"),
            prefix: AccessTools.Method(typeof(SceneLifecyclePatches), nameof(SceneLifecyclePatches.ScreenRatioScalerInitPrefix))
        );
    }

    public static string GetPluginPath()
    {
        try
        {
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

    public static object GetPProperty(object __instance, string fieldName) {
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
}
