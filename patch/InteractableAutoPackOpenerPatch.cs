using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.patch
{
    public static class InteractableAutoPackOpenerPatch
    {
        private static readonly FieldInfo s_IsProcessingField = AccessTools.Field(typeof(InteractableAutoPackOpener), "m_IsProcessing");
        private static readonly FieldInfo s_PackOpenedCountField = AccessTools.Field(typeof(InteractableAutoPackOpener), "m_PackOpenedCount");
        private static readonly FieldInfo s_AutoCardOpenerUIField = AccessTools.Field(typeof(InteractableAutoPackOpener), "m_AutoCardOpenerUI");
        private static readonly FieldInfo s_TotalExpGainedField = AccessTools.Field(typeof(ShowCardObtainedPage), "m_TotalExpGained");
        private static readonly FieldInfo s_WorkerCardPackListField = AccessTools.Field(typeof(Worker), "m_CardPackItemTypeList");
        private static readonly FieldInfo s_WorkerCardPackEnabledListField = AccessTools.Field(typeof(Worker), "m_CardPackItemTypeEnabledList");
        private static readonly FieldInfo s_OptionScreenPackTypeListField = AccessTools.Field(typeof(WorkerSetPackOpenerTypeOptionScreen), "m_CardPackItemTypeList");
        private static readonly FieldInfo s_OptionScreenPackDataListField = AccessTools.Field(typeof(WorkerSetPackOpenerTypeOptionScreen), "m_CardPackItemDataList");
        private static readonly FieldInfo s_OptionScreenPageMaxIndexField = AccessTools.Field(typeof(WorkerSetPackOpenerTypeOptionScreen), "m_PageMaxIndex");
        private static readonly FieldInfo s_OptionScreenPackPerPageField = AccessTools.Field(typeof(WorkerSetPackOpenerTypeOptionScreen), "m_PackPerPage");

        public static void OpenPackPrefix(InteractableAutoPackOpener __instance)
        {
            try
            {
                var storedItems = __instance.GetStoredItemList();
                if (storedItems != null && storedItems.Count > 0)
                {
                    CardOpening.CurrentMachineOpeningItem = storedItems[0];
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AutoPackOpener] OpenPackPrefix error: {ex}");
            }
        }

        public static void OpenPackPostfix()
        {
            CardOpening.CurrentMachineOpeningItem = null;
        }

        public static bool OnMouseButtonUpPrefix(InteractableAutoPackOpener __instance)
        {
            try
            {
                bool isProcessing = __instance.GetIsProcessing();
                var storedItems = __instance.GetStoredItemList();
                var compactList = __instance.GetCompactCardDataAmountList();

                // Détection de machine bloquée (save corrompue) :
                // La machine est en processing, mais n'a plus d'items à traiter et aucune carte générée
                if (isProcessing && (storedItems == null || storedItems.Count == 0) && (compactList == null || compactList.Count == 0))
                {
                    Plugin.LogInfo("[AutoPackOpener] Récupération automatique d'une machine bloquée : réinitialisation à l'état inactif.");
                    s_IsProcessingField?.SetValue(__instance, false);
                    s_PackOpenedCountField?.SetValue(__instance, 0);
                    __instance.m_CurrentState = 0;

                    var ui = s_AutoCardOpenerUIField?.GetValue(__instance) as AutoCardOpenerUI;
                    if (ui != null)
                    {
                        ui.SetUIState(0);
                        ui.UpdatePackCountText(0, __instance.m_MaxPackCount);
                    }

                    InteractionPlayerController.RemoveToolTip(EGameAction.Collect);
                    InteractionPlayerController.RemoveToolTip(EGameAction.TurnOn);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AutoPackOpener] OnMouseButtonUpPrefix error: {ex}");
            }

            return true;
        }

        /// <summary>
        /// Synchronise la liste des packs autorisés et leurs booléens d'activation sur un employé (Worker)
        /// afin d'éviter un ArgumentOutOfRangeException lorsque des packs custom sont ajoutés à m_CardPackItemTypeList.
        /// </summary>
        public static void SyncWorkerCardPackLists(Worker __instance)
        {
            try
            {
                if (__instance == null) return;
                var so = CSingleton<InventoryBase>.Instance?.m_StockItemData_SO;
                if (so?.m_CardPackItemTypeList == null) return;

                var workerPackList = s_WorkerCardPackListField?.GetValue(__instance) as List<EItemType>;
                var workerEnabledList = s_WorkerCardPackEnabledListField?.GetValue(__instance) as List<bool>;
                if (workerPackList == null || workerEnabledList == null) return;

                for (int i = 0; i < so.m_CardPackItemTypeList.Count; i++)
                {
                    EItemType packType = so.m_CardPackItemTypeList[i];
                    if (!workerPackList.Contains(packType))
                    {
                        workerPackList.Add(packType);
                    }
                }

                while (workerEnabledList.Count < workerPackList.Count)
                {
                    workerEnabledList.Add(true);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AutoPackOpener] SyncWorkerCardPackLists error: {ex}");
            }
        }

        /// <summary>
        /// Met à jour la liste des packs de l'écran d'options de l'employé (WorkerSetPackOpenerTypeOptionScreen)
        /// et garantit que le worker inspecté possède ses listes synchronisées avant l'ouverture du menu.
        /// </summary>
        public static void WorkerSetPackOpenerTypeOptionScreenOpenPrefix(WorkerSetPackOpenerTypeOptionScreen __instance, Worker worker)
        {
            try
            {
                SyncWorkerCardPackLists(worker);

                var so = CSingleton<InventoryBase>.Instance?.m_StockItemData_SO;
                if (__instance == null || so?.m_CardPackItemTypeList == null) return;

                var screenPackList = s_OptionScreenPackTypeListField?.GetValue(__instance) as List<EItemType>;
                var screenDataList = s_OptionScreenPackDataListField?.GetValue(__instance) as List<ItemData>;
                if (screenPackList == null || screenDataList == null) return;

                bool added = false;
                for (int i = 0; i < so.m_CardPackItemTypeList.Count; i++)
                {
                    EItemType packType = so.m_CardPackItemTypeList[i];
                    if (!screenPackList.Contains(packType))
                    {
                        screenPackList.Add(packType);
                        screenDataList.Add(InventoryBase.GetItemData(packType));
                        added = true;
                    }
                }

                if (added && s_OptionScreenPageMaxIndexField != null)
                {
                    int packPerPage = (int)(s_OptionScreenPackPerPageField?.GetValue(__instance) ?? 8);
                    if (packPerPage <= 0) packPerPage = 8;
                    s_OptionScreenPageMaxIndexField.SetValue(__instance, (screenPackList.Count - 1) / packPerPage);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AutoPackOpener] WorkerSetPackOpenerTypeOptionScreenOpenPrefix error: {ex}");
            }
        }

        /// <summary>
        /// Recalcule m_TotalExpGained sur la page récapitulative de l'AutoPackOpener (ShowCardObtainedPage)
        /// en utilisant l'XP Wankul réelle de chaque carte obtenue.
        /// </summary>
        public static void ShowCardObtainedPostfix(ShowCardObtainedPage __instance, List<CompactCardDataAmount> compactCardDataAmountList)
        {
            try
            {
                if (__instance == null || compactCardDataAmountList == null || compactCardDataAmountList.Count == 0)
                    return;

                var wankulCardsData = WankulCardsData.Instance;
                wankulCardsData.EnsureInitialized();

                int totalWankulExp = 0;
                for (int i = 0; i < compactCardDataAmountList.Count; i++)
                {
                    var entry = compactCardDataAmountList[i];
                    CardData cardData = CPlayerData.GetCardData(entry.cardSaveIndex, entry.expansionType, entry.isDestiny);
                    WankulCardData wankulCard = wankulCardsData.GetFromMonster(cardData, allowNull: true);
                    if (wankulCard != null)
                    {
                        totalWankulExp += WankulCardsData.GetExperienceFromWankulCard(wankulCard) * entry.amount;
                    }
                }

                if (totalWankulExp > 0 && s_TotalExpGainedField != null)
                {
                    s_TotalExpGainedField.SetValue(__instance, totalWankulExp);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[AutoPackOpener] ShowCardObtainedPostfix error: {ex}");
            }
        }
    }
}
