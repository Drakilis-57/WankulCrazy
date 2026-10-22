using System;
using System.Collections.Generic;
using HarmonyLib;

namespace WankulCrazyPlugin.patch
{
    public static class InteractableAutoPackOpenerPatch
    {
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
                    AccessTools.Field(typeof(InteractableAutoPackOpener), "m_IsProcessing")?.SetValue(__instance, false);
                    AccessTools.Field(typeof(InteractableAutoPackOpener), "m_PackOpenedCount")?.SetValue(__instance, 0);
                    __instance.m_CurrentState = 0;

                    var uiField = AccessTools.Field(typeof(InteractableAutoPackOpener), "m_AutoCardOpenerUI");
                    var ui = uiField?.GetValue(__instance) as AutoCardOpenerUI;
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
    }
}
