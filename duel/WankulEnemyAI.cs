using System.Collections.Generic;
using UnityEngine;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Intelligence Artificielle dédiée au jeu de cartes Wankul TCG.
    /// Remplace intégralement l'EvaluateEnemyAI vanilla de Tetramon.
    /// </summary>
    public static class WankulEnemyAI
    {
        private static WankulCardData ResolveCard(InteractableCard3d card3d)
        {
            if (card3d == null || card3d.m_Card3dUI == null || card3d.m_Card3dUI.m_CardUI == null) return null;
            CardData cd = card3d.m_Card3dUI.m_CardUI.GetCardData();
            if (cd == null) return null;
            return WankulCardsData.Instance?.GetFromMonster(cd, true);
        }

        private static WankulCardData ResolveCard(CardData cd)
        {
            if (cd == null) return null;
            return WankulCardsData.Instance?.GetFromMonster(cd, true);
        }

        /// <summary>
        /// Exécute l'obligation de poser un terrain si <= 1 terrain en jeu.
        /// </summary>
        public static void ExecuteTerrainRequirement(WankulDuelController controller, PlayCardSet enemyCardSet)
        {
            var board = controller.BoardState;
            if (board.ActiveTerrainCount > 1) return;

            // Trouve le premier slot libre
            int freeSlot = -1;
            for (int i = 0; i < WankulBoardState.MAX_TERRAIN_SLOTS; i++)
            {
                if (!board.Terrains[i].HasTerrain)
                {
                    freeSlot = i;
                    break;
                }
            }
            if (freeSlot == -1) return;

            // Recherche un terrain dans la main de l'IA
            var hand = enemyCardSet.GetHoldCard3dList();
            InteractableCard3d terrainCard = null;
            TerrainCardData terrainData = null;

            foreach (var card3d in hand)
            {
                var wCard = ResolveCard(card3d);
                if (wCard is TerrainCardData td)
                {
                    terrainCard = card3d;
                    terrainData = td;
                    break;
                }
            }

            if (terrainCard != null && terrainData != null)
            {
                Plugin.Logger.LogInfo($"[WankulEnemyAI] Pose obligatoire d'un terrain depuis la main : {terrainData.Title} sur slot {freeSlot}");
                controller.PlaceTerrain(freeSlot, terrainCard.m_Card3dUI.m_CardUI.GetCardData(), terrainData);
            }
            else
            {
                // Dépilage automatique du deck jusqu'au premier terrain
                Plugin.Logger.LogInfo("[WankulEnemyAI] Pas de terrain en main -> Dépilage de la pioche...");
                var deckCards = enemyCardSet.GetDeckCardDataList();
                if (deckCards != null)
                {
                    for (int i = 0; i < deckCards.Count; i++)
                    {
                        var candidate = ResolveCard(deckCards[i]);
                        if (candidate is TerrainCardData tdFound)
                        {
                            var foundCardData = deckCards[i];
                            deckCards.RemoveAt(i);
                            controller.PlaceTerrain(freeSlot, foundCardData, tdFound);
                            Plugin.Logger.LogInfo($"[WankulEnemyAI] Terrain trouvé par dépilage : {tdFound.Title} sur slot {freeSlot}");
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tour d'action complet de l'adversaire IA.
        /// </summary>
        public static void RunTurn(WankulDuelController controller, PlayCardSet enemyCardSet)
        {
            if (controller == null || enemyCardSet == null) return;
            var board = controller.BoardState;

            Plugin.Logger.LogInfo("[WankulEnemyAI] Début du tour de jeu de l'IA Wankul.");

            // 0. PRIORITÉ TERRAIN : doit être exécuté SYNCHRONIQUEMENT avant tout, avant la boucle personnages.
            //    (Supprime la race condition avec CheckTerrainsRoutine)
            ExecuteTerrainRequirement(controller, enemyCardSet);

            // 1. Pose de Personnages (jusqu'à 4 max) — itération en reverse pour éviter IndexOutOfRange après suppression
            int charactersPlayed = 0;
            var hand = enemyCardSet.GetHoldCard3dList();

            for (int i = hand.Count - 1; i >= 0 && controller.CanPlayCharacter(false); i--)
            {
                var card3d = hand[i];
                var wCard = ResolveCard(card3d);
                if (wCard is EffigyCardData effigy)
                {
                    int targetSlot = ChooseBestSlotForCharacter(board);
                    if (targetSlot >= 0)
                    {
                        // Passer card3d pour retrait physique de la main
                        controller.PlaceCharacter(targetSlot, effigy, false, enemyCardSet, card3d);
                        charactersPlayed++;
                        // Réactualiser la référence à hand après la suppression
                        hand = enemyCardSet.GetHoldCard3dList();
                        i = Mathf.Min(i, hand.Count); // borne haute sécurisée
                    }
                }
            }

            Plugin.Logger.LogInfo($"[WankulEnemyAI] L'IA a posé {charactersPlayed} personnage(s) ce tour.");

            // 2. Décision de scoring si l'IA a l'avantage sur un terrain actif
            for (int slotIdx = 0; slotIdx < WankulBoardState.MAX_TERRAIN_SLOTS; slotIdx++)
            {
                var slot = board.Terrains[slotIdx];
                if (slot.HasTerrain && slot.IsActive)
                {
                    int enemyForce = slot.GetEnemyTotalForce();
                    int playerForce = slot.GetPlayerTotalForce();

                    // Si l'IA mène en force et a au moins 1 point de force, elle tente de scorer
                    if (enemyForce > playerForce && enemyForce > 0)
                    {
                        Plugin.Logger.LogInfo($"[WankulEnemyAI] L'IA déclenche le duel sur le Terrain {slotIdx} ({slot.TerrainData.Title}) avec avantage {enemyForce} vs {playerForce} !");
                        controller.ResolveDuel(slotIdx);
                        break;
                    }
                }
            }

            // 3. Fin de tour de l'IA
            Plugin.Logger.LogInfo("[WankulEnemyAI] Fin de tour de l'IA.");
            if (controller.GameTable != null)
            {
                controller.GameTable.SwitchTurn();
            }
        }


        private static int ChooseBestSlotForCharacter(WankulBoardState board)
        {
            int bestSlot = -1;
            int maxPlayerThreat = -1;

            // Privilégier les terrains où le joueur a déjà posé de la force
            for (int i = 0; i < WankulBoardState.MAX_TERRAIN_SLOTS; i++)
            {
                if (board.Terrains[i].HasTerrain)
                {
                    int threat = board.Terrains[i].GetPlayerTotalForce();
                    if (threat > maxPlayerThreat)
                    {
                        maxPlayerThreat = threat;
                        bestSlot = i;
                    }
                }
            }

            // Sinon le premier terrain disponible
            if (bestSlot == -1)
            {
                for (int i = 0; i < WankulBoardState.MAX_TERRAIN_SLOTS; i++)
                {
                    if (board.Terrains[i].HasTerrain) return i;
                }
            }

            return bestSlot;
        }
    }
}
