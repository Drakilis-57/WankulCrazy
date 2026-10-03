using HarmonyLib;
using WankulCrazyPlugin.duel;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Interceptions chirurgicales à la racine :
    /// 1. Bloque la distribution des 4 Guardians Tetramon (aucune carte ne s'empile au centre).
    /// 2. Active immédiatement le tour 1 Wankul dès la fin du Mulligan pour le Joueur OU pour l'IA.
    /// </summary>
    public static class WankulRootDuelPatch
    {
        /// <summary>
        /// Bloque la création et la pioche de cartes "Guardian" (les PV de Tetramon) en mode Wankul.
        /// </summary>
        [HarmonyPatch(typeof(PlayCardSet), "QueueCardDrawFromDeck", new[] { typeof(ECardDrawQueueType), typeof(int), typeof(float), typeof(bool), typeof(float) })]
        [HarmonyPrefix]
        public static bool QueueCardDrawFromDeckPrefix(ECardDrawQueueType queueType)
        {
            if (WankulDuelController.Instance != null && WankulDuelController.Instance.IsWankulDuelActive)
            {
                if (queueType == ECardDrawQueueType.ToGuardian)
                {
                    Plugin.Logger.LogInfo("[WankulRootDuelPatch] Distribution Guardian Tetramon bloquée (Mode Wankul TCG).");
                    return false; // Zéro Guardian sur la table !
                }
            }
            return true;
        }

        /// <summary>
        /// Bloque la coroutine TriggerPlayEffect de Tetramon qui essaie de lire playEffectQueueDataList[0]
        /// et cause un ArgumentOutOfRangeException quand la liste d'effets est vide.
        /// </summary>
        [HarmonyPatch(typeof(PlayTableGame), "TriggerPlayEffect")]
        [HarmonyPrefix]
        public static bool TriggerPlayEffectPrefix(PlayTableGame __instance, ref System.Collections.IEnumerator __result)
        {
            if (WankulDuelController.Instance != null && WankulDuelController.Instance.IsWankulDuelActive)
            {
                __result = EmptyTriggerPlayEffectRoutine(__instance);
                return false; // Court-circuite totalement la coroutine vanilla
            }
            return true;
        }

        private static System.Collections.IEnumerator EmptyTriggerPlayEffectRoutine(PlayTableGame table)
        {
            table.m_IsTriggeringPlayEffect = false;
            table.m_PlayCardSetPlayer?.ResetLastTriggerSearchedAndDiscardedCard();
            table.m_PlayCardSetEnemy?.ResetLastTriggerSearchedAndDiscardedCard();
            yield break;
        }
    }
}
