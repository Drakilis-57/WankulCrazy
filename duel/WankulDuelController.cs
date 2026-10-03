using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Contrôleur principal pour les duels Wankul TCG.
    /// Orchestre les phases officielles : Pioche (2 cartes), TerrainCheck, Action, Scoring et Fin de tour.
    /// </summary>
    public class WankulDuelController : MonoBehaviour
    {
        public static WankulDuelController Instance { get; private set; }

        public WankulBoardState BoardState = new WankulBoardState();
        public PlayTableGame GameTable { get; private set; }

        public bool IsWankulDuelActive { get; private set; }
        public bool IsGameSetupCompleted { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(PlayTableGame playTable)
        {
            GameTable = playTable;
            BoardState.Reset();
            IsWankulDuelActive = true;
            IsGameSetupCompleted = false;

            // Création de l'interface dédiée Wankul TCG
            WankulDuelUI.Create(this);

            // Nettoyage visuel 3D de la table de jeu (masque les symboles Tetramon au sol)
            CleanVanillaBoardVisuals(playTable.m_PlayCardSetPlayer);
            CleanVanillaBoardVisuals(playTable.m_PlayCardSetEnemy);

            Plugin.Logger.LogInfo("[WankulDuelController] Initialisé pour un duel Wankul TCG !");
        }

        /// <summary>
        /// Masque les 4 symboles élémentaires vanilla projetés au sol sur le tapis,
        /// et ajuste les slots 0, 1, 2 pour les aligner sur les 3 Terrains Wankul.
        /// </summary>
        private void CleanVanillaBoardVisuals(PlayCardSet cardSet)
        {
            if (cardSet == null) return;
            try
            {
                // Masque les animations et visuels des symboles Feu, Terre, Eau, Air
                if (cardSet.m_PlayCardElemAtkAnimList != null)
                {
                    foreach (var anim in cardSet.m_PlayCardElemAtkAnimList)
                    {
                        if (anim != null && anim.gameObject != null)
                        {
                            anim.gameObject.SetActive(false);
                        }
                    }
                }

                // Masque les effets d'évolution Tetramon
                if (cardSet.m_EvoVFXList != null)
                {
                    foreach (var fx in cardSet.m_EvoVFXList)
                    {
                        if (fx != null) fx.SetActive(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelController] Erreur CleanVanillaBoardVisuals: {ex.Message}");
            }
        }

        public void StopDuel()
        {
            IsWankulDuelActive = false;
            IsGameSetupCompleted = false;
            BoardState.Reset();

            if (WankulDuelUI.Instance != null)
            {
                WankulDuelUI.Instance.DestroyUI();
            }

            Plugin.Logger.LogInfo("[WankulDuelController] Duel Wankul terminé.");
        }

        /// <summary>
        /// Règle officielle : début de tour avec pioche de 2 cartes.
        /// </summary>
        public void HandleTurnStart(bool isPlayerTurn)
        {
            if (!IsWankulDuelActive || !IsGameSetupCompleted || GameTable == null) return;

            BoardState.OnTurnStart(isPlayerTurn);
            Plugin.Logger.LogInfo($"[WankulDuelController] Début du tour {(isPlayerTurn ? "Joueur" : "Adversaire")} (Tour {BoardState.TurnCount + 1}).");

            PlayCardSet activeCardSet = isPlayerTurn ? GameTable.m_PlayCardSetPlayer : GameTable.m_PlayCardSetEnemy;

            // Pioche 2 cartes
            int remainingDeck = activeCardSet.GetDeckCardDataList()?.Count ?? 0;
            if (remainingDeck < 2)
            {
                Plugin.Logger.LogInfo($"[WankulDuelController] Meule ! Deck insuffisant pour piocher 2 cartes ({remainingDeck} cartes restantes).");
                GameTable.ReportWinner(!isPlayerTurn, false);
                return;
            }

            if (isPlayerTurn)
            {
                activeCardSet.QueueCardDrawFromDeck(ECardDrawQueueType.ToHand, 2, 0.2f, showCenter: true, 0.4f);
                // Vérification terrain côté Joueur : si <= 1 terrain, pose obligatoire
                EnsurePlayerTerrainRequirement(activeCardSet);
            }
            else
            {
                activeCardSet.QueueCardDrawFromDeck(ECardDrawQueueType.ToHand, 2, 0.2f);
                // Lancement garanti et unique du tour de l'IA après réception des cartes
                StartCoroutine(EnemyAITurnRoutine(activeCardSet));
            }
        }

        private IEnumerator EnemyAITurnRoutine(PlayCardSet enemyCardSet)
        {
            // Attente de l'animation de pioche des cartes dans la main
            yield return new WaitForSeconds(0.8f);

            Plugin.Logger.LogInfo("[WankulDuelController] Lancement du tour de l'Adversaire Wankul !");
            WankulEnemyAI.RunTurn(this, enemyCardSet);
        }

        /// <summary>
        /// Règle officielle Wankul TCG : Si <= 1 terrain sur la table, le joueur DOIT en poser un.
        /// Si le joueur n'en a pas en main, dépilage automatique du deck jusqu'au premier terrain.
        /// </summary>
        private void EnsurePlayerTerrainRequirement(PlayCardSet playerCardSet)
        {
            if (BoardState.ActiveTerrainCount > 1) return;

            // Trouve le premier slot libre
            int freeSlot = -1;
            for (int i = 0; i < WankulBoardState.MAX_TERRAIN_SLOTS; i++)
            {
                if (!BoardState.Terrains[i].HasTerrain)
                {
                    freeSlot = i;
                    break;
                }
            }
            if (freeSlot == -1) return;

            // Vérifie si le joueur a un terrain en main
            var hand = playerCardSet.GetHoldCard3dList();
            bool hasTerrainInHand = false;
            if (hand != null)
            {
                foreach (var card3d in hand)
                {
                    var cd = card3d?.m_Card3dUI?.m_CardUI?.GetCardData();
                    if (cd != null)
                    {
                        var wCard = WankulCardsData.Instance?.GetFromMonster(cd, true);
                        if (wCard is TerrainCardData)
                        {
                            hasTerrainInHand = true;
                            break;
                        }
                    }
                }
            }

            if (hasTerrainInHand)
            {
                Plugin.Logger.LogInfo($"[WankulDuelController] Terrains en jeu: {BoardState.ActiveTerrainCount} <= 1. Vous devez poser un Terrain sur un slot libre !");
            }
            else
            {
                // Pas de terrain en main -> Dépilage automatique du deck
                Plugin.Logger.LogInfo("[WankulDuelController] Aucun terrain en main -> Dépilage automatique de la pioche du joueur...");
                var deckCards = playerCardSet.GetDeckCardDataList();
                if (deckCards != null)
                {
                    for (int i = 0; i < deckCards.Count; i++)
                    {
                        var candidate = WankulCardsData.Instance?.GetFromMonster(deckCards[i], true);
                        if (candidate is TerrainCardData tdFound)
                        {
                            var foundCardData = deckCards[i];
                            deckCards.RemoveAt(i);
                            PlaceTerrain(freeSlot, foundCardData, tdFound);
                            Plugin.Logger.LogInfo($"[WankulDuelController] Terrain trouvé par dépilage pour le joueur : {tdFound.Title} sur slot {freeSlot} !");
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pose un terrain dans l'un des 3 slots disponibles.
        /// </summary>
        public bool PlaceTerrain(int slotIndex, CardData cardData, TerrainCardData terrainData)
        {
            if (slotIndex < 0 || slotIndex >= WankulBoardState.MAX_TERRAIN_SLOTS) return false;
            var slot = BoardState.Terrains[slotIndex];
            if (slot.HasTerrain) return false;

            slot.TerrainData = terrainData;
            slot.RawCardData = cardData;
            slot.PlacedThisTurn = true;
            slot.IsActive = false; // Règle : non scorable le tour où il est posé

            Plugin.Logger.LogInfo($"[WankulDuelController] Terrain posé dans le Slot {slotIndex}: {terrainData.Title} (Incliné ce tour).");
            return true;
        }

        /// <summary>
        /// Vérifie si le joueur ou l'IA peut encore poser un personnage ce tour-ci (max 4).
        /// </summary>
        public bool CanPlayCharacter(bool isPlayer)
        {
            int played = isPlayer ? BoardState.PlayerCharactersPlayedThisTurn : BoardState.EnemyCharactersPlayedThisTurn;
            return played < WankulBoardState.MAX_CHARACTERS_PER_TURN;
        }

        /// <summary>
        /// Enregistre la pose d'un personnage sur un terrain.
        /// Retire optionnellement la carte de la main physiquement (card3d).
        /// Déclenche ResolveDuel si la carte est un Scoreur posé sur un terrain actif.
        /// </summary>
        public bool PlaceCharacter(int slotIndex, WankulCardData characterData, bool isPlayer, PlayCardSet cardSet = null, InteractableCard3d card3d = null)
        {
            if (!CanPlayCharacter(isPlayer))
            {
                Plugin.Logger.LogWarning($"[WankulDuelController] Limite de 4 personnages atteinte ce tour !");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= WankulBoardState.MAX_TERRAIN_SLOTS) return false;
            var slot = BoardState.Terrains[slotIndex];
            if (!slot.HasTerrain) return false;

            if (isPlayer)
            {
                slot.PlayerCharacters.Add(characterData);
                BoardState.PlayerCharactersPlayedThisTurn++;
            }
            else
            {
                slot.EnemyCharacters.Add(characterData);
                BoardState.EnemyCharactersPlayedThisTurn++;
            }

            Plugin.Logger.LogInfo($"[WankulDuelController] Personnage {characterData.Title} posé sur le Terrain {slotIndex} par {(isPlayer ? "Joueur" : "Adversaire")}.");

            // Retrait physique de la main
            if (cardSet != null && card3d != null)
            {
                RemoveCard3dFromHand(cardSet, card3d);
            }

            // Règle Scoreur : si la carte est un Scoreur et le terrain est actif → scoring immédiat
            if (characterData is WankulCrazyPlugin.cards.EffigyCardData effigy && effigy.IsScoreur && slot.IsActive)
            {
                Plugin.Logger.LogInfo($"[WankulDuelController] Scoreur {effigy.Title} posé sur terrain actif {slotIndex} -> Score automatique !");
                ResolveDuel(slotIndex);
            }

            return true;
        }

        /// <summary>
        /// Retire physiquement une carte3d de la main (m_HoldCard3dList) et la déplace vers la défausse.
        /// Utilise RemoveFromHoldCard vanilla pour synchroniser m_PlayCardBtnGrpList, animations et EvaluateHoldCardPos.
        /// </summary>
        public void RemoveCard3dFromHandPublic(PlayCardSet cardSet, InteractableCard3d card3d)
        {
            RemoveCard3dFromHand(cardSet, card3d);
        }

        private static readonly System.Reflection.MethodInfo RemoveFromHoldCardMethod =
            Plugin.GetCachedMethod(typeof(PlayCardSet), "RemoveFromHoldCard");

        private void RemoveCard3dFromHand(PlayCardSet cardSet, InteractableCard3d card3d)
        {
            try
            {
                if (cardSet == null || card3d == null) return;

                var hand = cardSet.m_HoldCard3dList;
                int idx = hand.IndexOf(card3d);
                if (idx >= 0)
                {
                    InteractableCard3d removed = null;
                    if (RemoveFromHoldCardMethod != null)
                    {
                        removed = RemoveFromHoldCardMethod.Invoke(cardSet, new object[] { idx }) as InteractableCard3d;
                    }
                    else
                    {
                        // Fallback au cas où
                        hand.RemoveAt(idx);
                        removed = card3d;
                    }

                    Plugin.Logger.LogInfo($"[WankulDuelController] Carte retirée de la main via RemoveFromHoldCard (index={idx}).");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelController] Erreur RemoveCard3dFromHand: {ex.Message}");
            }
        }



        /// <summary>
        /// Résout le duel sur un terrain actif :
        /// Compare la Force totale Joueur vs Adversaire -> 1 point au gagnant -> défausse des cartes.
        /// </summary>
        public bool ResolveDuel(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= WankulBoardState.MAX_TERRAIN_SLOTS) return false;
            var slot = BoardState.Terrains[slotIndex];

            if (!slot.HasTerrain || !slot.IsActive)
            {
                Plugin.Logger.LogWarning($"[WankulDuelController] Impossible de scorer le Slot {slotIndex} : terrain non actif/incliné.");
                return false;
            }

            int playerForce = slot.GetPlayerTotalForce();
            int enemyForce = slot.GetEnemyTotalForce();

            Plugin.Logger.LogInfo($"[WankulDuelController] Résolution du Duel Terrain {slotIndex} ({slot.TerrainData.Title}) : Joueur={playerForce} vs Adversaire={enemyForce}");

            if (playerForce > enemyForce)
            {
                BoardState.PlayerScore++;
                Plugin.Logger.LogInfo($"[WankulDuelController] Point marqué par le JOUEUR ! Score actuel: {BoardState.PlayerScore} / {WankulBoardState.WINNING_TERRAIN_COUNT}");
            }
            else if (enemyForce > playerForce)
            {
                BoardState.EnemyScore++;
                Plugin.Logger.LogInfo($"[WankulDuelController] Point marqué par l'ADVERSAIRE ! Score actuel: {BoardState.EnemyScore} / {WankulBoardState.WINNING_TERRAIN_COUNT}");
            }
            else
            {
                Plugin.Logger.LogInfo("[WankulDuelController] Égalité parfaite ! Aucun point marqué.");
            }

            // Nettoyage du slot (cartes envoyées au cimetière/défausse)
            slot.Clear();

            // Vérification de la condition de victoire à 5 Terrains
            CheckScoreWinCondition();

            return true;
        }

        private void CheckScoreWinCondition()
        {
            if (BoardState.PlayerScore >= WankulBoardState.WINNING_TERRAIN_COUNT)
            {
                Plugin.Logger.LogInfo($"[WankulDuelController] VICTOIRE DU JOUEUR ! 5 Terrains remportés.");
                GameTable.ReportWinner(true, false);
            }
            else if (BoardState.EnemyScore >= WankulBoardState.WINNING_TERRAIN_COUNT)
            {
                Plugin.Logger.LogInfo($"[WankulDuelController] VICTOIRE DE L'ADVERSAIRE ! 5 Terrains remportés.");
                GameTable.ReportWinner(false, false);
            }
        }
    }
}
