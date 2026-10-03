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
            Plugin.Logger.LogInfo("[WankulDuelController] Initialisé pour un duel Wankul TCG !");
        }

        public void StopDuel()
        {
            IsWankulDuelActive = false;
            IsGameSetupCompleted = false;
            BoardState.Reset();
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
            }
            else
            {
                activeCardSet.QueueCardDrawFromDeck(ECardDrawQueueType.ToHand, 2, 0.2f);
            }

            // Exécution de la vérification des terrains
            StartCoroutine(CheckTerrainsRoutine(isPlayerTurn, activeCardSet));
        }

        /// <summary>
        /// Phase 2 : Vérification des Terrains.
        /// S'il y a 0 ou 1 terrain en jeu, le joueur actif DOIT obligatoirement en poser un.
        /// S'il n'en a pas en main, dépilage du deck jusqu'au premier terrain.
        /// </summary>
        private IEnumerator CheckTerrainsRoutine(bool isPlayerTurn, PlayCardSet cardSet)
        {
            yield return new WaitForSeconds(0.6f);

            if (BoardState.ActiveTerrainCount <= 1)
            {
                Plugin.Logger.LogInfo($"[WankulDuelController] Terrains en jeu: {BoardState.ActiveTerrainCount} <= 1. Pose obligatoire requise.");
                // Si l'IA joue, elle sélectionne ou dépile automatiquement
                if (!isPlayerTurn)
                {
                    WankulEnemyAI.ExecuteTerrainRequirement(this, cardSet);
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
        /// </summary>
        public bool PlaceCharacter(int slotIndex, WankulCardData characterData, bool isPlayer)
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
            return true;
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
