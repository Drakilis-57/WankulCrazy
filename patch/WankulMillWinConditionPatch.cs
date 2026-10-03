using HarmonyLib;
using UnityEngine;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Règle officielle Wankul TCG : Condition de victoire par Meule (Épuisement de deck).
    /// Si un joueur ou l'adversaire doit piocher ou n'a plus aucune carte dans son deck,
    /// la partie se termine immédiatement par une défaite de celui dont le deck est vide.
    /// </summary>
    public static class WankulMillWinConditionPatch
    {
        [HarmonyPatch(typeof(PlayTableGame), "ReportWinner")]
        [HarmonyPrefix]
        public static void CheckDeckOutBeforeWinner(
            PlayTableGame __instance,
            PlayCardSet ___m_PlayCardSetPlayer,
            PlayCardSet ___m_PlayCardSetEnemy,
            ref bool isPlayerWin,
            ref bool isDraw)
        {
            try
            {
                if (___m_PlayCardSetPlayer == null || ___m_PlayCardSetEnemy == null) return;

                // Garde essentielle : ne jamais évaluer la meule pendant la séquence d'introduction / setup
                if (WankulCrazyPlugin.duel.WankulDuelController.Instance != null &&
                    !WankulCrazyPlugin.duel.WankulDuelController.Instance.IsGameSetupCompleted)
                {
                    return;
                }

                int playerDeckCount = ___m_PlayCardSetPlayer.GetDeckCardDataList()?.Count ?? 0;
                int enemyDeckCount = ___m_PlayCardSetEnemy.GetDeckCardDataList()?.Count ?? 0;

                // Si les deux decks sont vides simultanément
                if (playerDeckCount == 0 && enemyDeckCount == 0)
                {
                    Plugin.Logger.LogInfo("[WankulMillWinCondition] Double Meule : Les deux decks sont vides -> Match nul (Draw).");
                    isDraw = true;
                    isPlayerWin = false;
                }
                // Si le deck de l'adversaire est vide -> Victoire du joueur par Meule
                else if (enemyDeckCount == 0)
                {
                    Plugin.Logger.LogInfo("[WankulMillWinCondition] Victoire du Joueur par Meule ! Le deck adverse est épuisé.");
                    isPlayerWin = true;
                    isDraw = false;
                }
                // Si le deck du joueur est vide -> Défaite du joueur par Meule
                else if (playerDeckCount == 0)
                {
                    Plugin.Logger.LogInfo("[WankulMillWinCondition] Défaite du Joueur par Meule ! Votre deck est épuisé.");
                    isPlayerWin = false;
                    isDraw = false;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulMillWinCondition] Erreur dans CheckDeckOutBeforeWinner: {ex}");
            }
        }

        /// <summary>
        /// Vérifie également à la fin de chaque tour si un deck est tombé à 0 carte
        /// pour abréger immédiatement la partie sans attendre que les HP tombent à 0.
        /// </summary>
        public static void CheckDeckOutOnTurnEnded(
            PlayTableGame __instance,
            PlayCardSet ___m_PlayCardSetPlayer,
            PlayCardSet ___m_PlayCardSetEnemy,
            bool ___m_IsPlayerWin,
            bool ___m_IsDraw)
        {
            try
            {
                if (__instance == null || ___m_PlayCardSetPlayer == null || ___m_PlayCardSetEnemy == null) return;

                if (WankulCrazyPlugin.duel.WankulDuelController.Instance != null &&
                    !WankulCrazyPlugin.duel.WankulDuelController.Instance.IsGameSetupCompleted)
                {
                    return;
                }

                int playerDeckCount = ___m_PlayCardSetPlayer.GetDeckCardDataList()?.Count ?? 0;
                int enemyDeckCount = ___m_PlayCardSetEnemy.GetDeckCardDataList()?.Count ?? 0;

                if (enemyDeckCount == 0 || playerDeckCount == 0)
                {
                    bool playerWins = enemyDeckCount == 0 && playerDeckCount > 0;
                    bool draw = enemyDeckCount == 0 && playerDeckCount == 0;
                    Plugin.Logger.LogInfo($"[WankulMillWinCondition] Détection Meule en fin de tour : PlayerDeck={playerDeckCount}, EnemyDeck={enemyDeckCount} -> Victoire Joueur={playerWins}, Draw={draw}");
                    __instance.ReportWinner(playerWins, draw);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulMillWinCondition] Erreur dans CheckDeckOutOnTurnEnded: {ex}");
            }
        }
    }
}
