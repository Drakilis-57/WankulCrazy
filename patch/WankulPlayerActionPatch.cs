using HarmonyLib;
using UnityEngine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.duel;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Intercepte les actions joueur sur la table de jeu vanilla (clic sur slot élémentaire)
    /// et les redirige vers WankulDuelController pour la logique Wankul TCG.
    ///
    /// Mapping slots vanilla → slots Wankul :
    ///   Slot 0 → Terrain Wankul 0
    ///   Slot 1 → Terrain Wankul 1
    ///   Slot 2 → Terrain Wankul 2
    ///   Slot 3 → ignoré (pas de 4e terrain dans les règles Wankul)
    /// </summary>
    public static class WankulPlayerActionPatch
    {
        /// <summary>
        /// Prefix sur OnClickElementButtonArea (joueur clique sur un slot).
        /// Si le duel Wankul est actif, on bypass complètement la logique Tetramon
        /// et on dispatch vers PlaceTerrain ou PlaceCharacter.
        /// </summary>
        [HarmonyPatch(typeof(PlayCardSet), "OnClickElementButtonArea")]
        [HarmonyPrefix]
        public static bool OnClickElementButtonAreaPrefix(PlayCardSet __instance, int index, bool ___m_IsPlayer, InteractableCard3d ___m_CurrentSelectedCard)
        {
            try
            {
                var ctrl = WankulDuelController.Instance;
                if (ctrl == null || !ctrl.IsWankulDuelActive || !ctrl.IsGameSetupCompleted) return true;

                // On n'agit que pour le joueur humain (pas l'IA)
                if (!___m_IsPlayer) return false;

                // Slot 3 non utilisé en Wankul
                int wankulSlot = index;
                if (wankulSlot >= WankulBoardState.MAX_TERRAIN_SLOTS) return false;

                // Aucune carte sélectionnée → afficher infos (comportement vanilla)
                if (___m_CurrentSelectedCard == null) return true;

                var cardData = ___m_CurrentSelectedCard.m_Card3dUI?.m_CardUI?.GetCardData();
                if (cardData == null) return false;

                var wCard = WankulCardsData.Instance?.GetFromMonster(cardData, true);
                if (wCard == null)
                {
                    Plugin.Logger.LogWarning($"[WankulPlayerActionPatch] Carte introuvable dans WankulCardsData pour slot {index}.");
                    return false;
                }

                if (wCard is TerrainCardData terrainData)
                {
                    // --- Pose d'un Terrain ---
                    bool placed = ctrl.PlaceTerrain(wankulSlot, cardData, terrainData);
                    if (placed)
                    {
                        Plugin.Logger.LogInfo($"[WankulPlayerActionPatch] Joueur pose le Terrain '{terrainData.Title}' sur slot {wankulSlot}.");
                        
                        var cardToPlay = ___m_CurrentSelectedCard;
                        __instance.DeselectCardOnHand();
                        ctrl.RemoveCard3dFromHandPublic(__instance, cardToPlay);

                        // Animation de pose 3D sur la table
                        __instance.QueueCardPlacement(cardToPlay, ECardDrawQueueType.ToCenterShow, 0.1f);
                        __instance.QueueCardPlacement(cardToPlay, ECardDrawQueueType.RemoveFromCenterShow, 0.01f);
                        __instance.QueueCardPlacement(cardToPlay, ECardDrawQueueType.ToElementArea, 0.35f, wankulSlot);
                    }
                    else
                    {
                        Plugin.Logger.LogWarning($"[WankulPlayerActionPatch] Terrain slot {wankulSlot} déjà occupé ou invalide.");
                    }
                }
                else if (wCard is EffigyCardData effigy)
                {
                    // --- Pose d'un Personnage ---
                    var targetTerrain = ctrl.BoardState.Terrains[wankulSlot];
                    if (!targetTerrain.HasTerrain)
                    {
                        Plugin.Logger.LogWarning($"[WankulPlayerActionPatch] Action impossible : Le slot {wankulSlot} n'a pas de Terrain ! Posez d'abord une carte Terrain avant d'y placer '{effigy.Title}'.");
                        return false;
                    }
                    if (!ctrl.CanPlayCharacter(true))
                    {
                        Plugin.Logger.LogWarning($"[WankulPlayerActionPatch] Limite atteinte : Vous avez déjà posé le maximum de {WankulBoardState.MAX_CHARACTERS_PER_TURN} personnages ce tour-ci !");
                        return false;
                    }

                    var cardToPlay = ___m_CurrentSelectedCard;
                    __instance.DeselectCardOnHand();

                    bool placed = ctrl.PlaceCharacter(wankulSlot, effigy, true, __instance, cardToPlay);
                    if (placed)
                    {
                        Plugin.Logger.LogInfo($"[WankulPlayerActionPatch] Joueur pose '{effigy.Title}' (Force={effigy.Force}) sur le Terrain {wankulSlot} ({targetTerrain.TerrainData?.Title}).");
                        
                        // Animation de pose 3D sur la table
                        __instance.QueueCardPlacement(cardToPlay, ECardDrawQueueType.ToCenterShow, 0.1f);
                        __instance.QueueCardPlacement(cardToPlay, ECardDrawQueueType.RemoveFromCenterShow, 0.01f);
                        __instance.QueueCardPlacement(cardToPlay, ECardDrawQueueType.ToElementArea, 0.35f, wankulSlot);
                    }
                }
                else
                {
                    Plugin.Logger.LogWarning($"[WankulPlayerActionPatch] Type de carte non reconnu pour la pose : {wCard.GetType().Name}");
                }

                return false; // Bypass total de la logique Tetramon
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulPlayerActionPatch] Erreur dans OnClickElementButtonAreaPrefix: {ex}");
                return true; // En cas d'erreur, laisser passer le vanilla
            }
        }

        /// <summary>
        /// Affiche le bouton "Fin de tour" en mode Wankul.
        /// Prefix sur OnPressEndTurn pour enregistrer la fin de tour joueur et déclencher SwitchTurn.
        /// </summary>
        [HarmonyPatch(typeof(PlayCardSet), "OnPressEndTurn")]
        [HarmonyPrefix]
        public static bool OnPressEndTurnPrefix(PlayCardSet __instance, bool ___m_IsPlayer)
        {
            try
            {
                var ctrl = WankulDuelController.Instance;
                if (ctrl == null || !ctrl.IsWankulDuelActive || !ctrl.IsGameSetupCompleted) return true;
                if (!___m_IsPlayer) return false;

                Plugin.Logger.LogInfo("[WankulPlayerActionPatch] Joueur termine son tour.");
                // Déclencher SwitchTurn vanilla → notre SwitchTurnPrefix appellera HandleTurnStart(false)
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[WankulPlayerActionPatch] Erreur dans OnPressEndTurnPrefix: {ex}");
                return true;
            }
        }

        /// <summary>
        /// Garde contre l'IndexOutOfRangeException dans SelectCardOnHand si l'UI de la main
        /// a un bouton pour un index qui n'existe plus dans m_HoldCard3dList.
        /// </summary>
        [HarmonyPatch(typeof(PlayCardSet), "SelectCardOnHand")]
        [HarmonyPrefix]
        public static bool SelectCardOnHandPrefix(PlayCardSet __instance, int index)
        {
            if (__instance == null || __instance.m_HoldCard3dList == null) return false;
            if (index < 0 || index >= __instance.m_HoldCard3dList.Count)
            {
                Plugin.Logger.LogWarning($"[WankulPlayerActionPatch] SelectCardOnHand ignoré pour index hors limites: {index} (main={__instance.m_HoldCard3dList.Count})");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Garde contre l'IndexOutOfRangeException dans StartDragCardOnHand.
        /// </summary>
        [HarmonyPatch(typeof(PlayCardSet), "StartDragCardOnHand")]
        [HarmonyPrefix]
        public static bool StartDragCardOnHandPrefix(PlayCardSet __instance, int index)
        {
            if (__instance == null || __instance.m_HoldCard3dList == null) return false;
            if (index < 0 || index >= __instance.m_HoldCard3dList.Count)
            {
                Plugin.Logger.LogWarning($"[WankulPlayerActionPatch] StartDragCardOnHand ignoré pour index hors limites: {index} (main={__instance.m_HoldCard3dList.Count})");
                return false;
            }
            return true;
        }
    }
}

