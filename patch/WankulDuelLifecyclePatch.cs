using HarmonyLib;
using UnityEngine;
using WankulCrazyPlugin.duel;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Connecte le contrôleur WankulDuelController au cycle de vie de la table de jeu.
    /// Intercepte SetPlayTable, EvaluateEnemyAI, et SwitchTurn.
    /// </summary>
    public static class WankulDuelLifecyclePatch
    {
        [HarmonyPatch(typeof(PlayTableGame), "SetPlayTable")]
        [HarmonyPostfix]
        public static void SetPlayTablePostfix(PlayTableGame __instance)
        {
            try
            {
                if (__instance == null) return;

                var controller = __instance.gameObject.GetComponent<WankulDuelController>();
                if (controller == null)
                {
                    controller = __instance.gameObject.AddComponent<WankulDuelController>();
                }

                controller.Initialize(__instance);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelLifecyclePatch] Erreur dans SetPlayTablePostfix: {ex}");
            }
        }

        [HarmonyPatch(typeof(PlayCardSet), "EvaluateEnemyAI")]
        [HarmonyPrefix]
        public static bool EvaluateEnemyAIPrefix(PlayCardSet __instance, bool ___m_IsAIControlled, bool ___m_IsTurnActive, bool ___m_IsWaitingActionResolve)
        {
            try
            {
                // Si le contrôleur Wankul est actif, nous prenons le contrôle de l'IA
                if (WankulDuelController.Instance != null && WankulDuelController.Instance.IsWankulDuelActive)
                {
                    // Ne rien faire tant que le tour de l'IA n'est pas actif ou que l'animation de setup n'est pas terminée
                    if (!WankulDuelController.Instance.IsGameSetupCompleted || !___m_IsTurnActive || ___m_IsWaitingActionResolve)
                    {
                        return false;
                    }

                    if (___m_IsAIControlled)
                    {
                        WankulEnemyAI.RunTurn(WankulDuelController.Instance, __instance);
                    }
                    return false; // Court-circuite complètement les 800 lignes de Tetramon vanilla !
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelLifecyclePatch] Erreur dans EvaluateEnemyAIPrefix: {ex}");
            }

            return true;
        }

        [HarmonyPatch(typeof(PlayTableGame), "OnFinishMulligan")]
        [HarmonyPostfix]
        public static void OnFinishMulliganPostfix(PlayTableGame __instance, bool ___m_HasFinishMulligan)
        {
            try
            {
                if (___m_HasFinishMulligan && WankulDuelController.Instance != null)
                {
                    WankulDuelController.Instance.IsGameSetupCompleted = true;
                    Plugin.Logger.LogInfo("[WankulDuelLifecyclePatch] Setup et Mulligan terminés. Le match Wankul commence réellement !");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelLifecyclePatch] Erreur dans OnFinishMulliganPostfix: {ex}");
            }
        }

        [HarmonyPatch(typeof(PlayTableGame), "SwitchTurn")]
        [HarmonyPrefix]
        public static void SwitchTurnPrefix(PlayTableGame __instance, bool ___m_IsPlayerTurn)
        {
            try
            {
                if (WankulDuelController.Instance != null && WankulDuelController.Instance.IsWankulDuelActive)
                {
                    // Le tour qui va commencer est l'inverse de m_IsPlayerTurn actuel
                    bool nextIsPlayerTurn = !___m_IsPlayerTurn;
                    WankulDuelController.Instance.HandleTurnStart(nextIsPlayerTurn);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelLifecyclePatch] Erreur dans SwitchTurnPrefix: {ex}");
            }
        }

        [HarmonyPatch(typeof(PlayTableGame), "ReportWinner")]
        [HarmonyPostfix]
        public static void ReportWinnerPostfix(PlayTableGame __instance)
        {
            try
            {
                if (WankulDuelController.Instance != null)
                {
                    WankulDuelController.Instance.StopDuel();
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelLifecyclePatch] Erreur dans ReportWinnerPostfix: {ex}");
            }
        }
    }
}
