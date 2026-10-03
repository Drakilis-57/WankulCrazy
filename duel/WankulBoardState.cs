using System.Collections.Generic;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Représente l'état d'un slot de terrain sur le plateau de jeu (3 slots max).
    /// </summary>
    public class WankulTerrainSlot
    {
        public int SlotIndex;
        public TerrainCardData TerrainData;
        public CardData RawCardData;

        // Règle officielle : un terrain posé durant le tour en cours ne peut pas être scoré ce tour-ci
        public bool IsActive; 
        public bool PlacedThisTurn;

        // Cartes personnages engagées sur ce terrain
        public List<WankulCardData> PlayerCharacters = new List<WankulCardData>();
        public List<WankulCardData> EnemyCharacters = new List<WankulCardData>();

        public WankulTerrainSlot(int index)
        {
            SlotIndex = index;
            Clear();
        }

        public bool HasTerrain => TerrainData != null;

        public void Clear()
        {
            TerrainData = null;
            RawCardData = null;
            IsActive = false;
            PlacedThisTurn = false;
            PlayerCharacters.Clear();
            EnemyCharacters.Clear();
        }

        public int GetPlayerTotalForce()
        {
            int total = 0;
            foreach (var card in PlayerCharacters)
            {
                if (card is EffigyCardData effigy)
                {
                    total += effigy.Force;
                }
            }
            return total;
        }

        public int GetEnemyTotalForce()
        {
            int total = 0;
            foreach (var card in EnemyCharacters)
            {
                if (card is EffigyCardData effigy)
                {
                    total += effigy.Force;
                }
            }
            return total;
        }
    }

    /// <summary>
    /// Représente l'état complet du duel Wankul en cours.
    /// </summary>
    public class WankulBoardState
    {
        public const int MAX_TERRAIN_SLOTS = 3;
        public const int WINNING_TERRAIN_COUNT = 5;
        public const int MAX_CHARACTERS_PER_TURN = 4;

        public WankulTerrainSlot[] Terrains = new WankulTerrainSlot[MAX_TERRAIN_SLOTS];

        public int PlayerScore = 0;
        public int EnemyScore = 0;

        public int PlayerCharactersPlayedThisTurn = 0;
        public int EnemyCharactersPlayedThisTurn = 0;

        public bool IsPlayerTurn = true;
        public int TurnCount = 0;

        public WankulBoardState()
        {
            for (int i = 0; i < MAX_TERRAIN_SLOTS; i++)
            {
                Terrains[i] = new WankulTerrainSlot(i);
            }
        }

        public void Reset()
        {
            PlayerScore = 0;
            EnemyScore = 0;
            PlayerCharactersPlayedThisTurn = 0;
            EnemyCharactersPlayedThisTurn = 0;
            TurnCount = 0;
            IsPlayerTurn = true;
            for (int i = 0; i < MAX_TERRAIN_SLOTS; i++)
            {
                Terrains[i].Clear();
            }
        }

        public int ActiveTerrainCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MAX_TERRAIN_SLOTS; i++)
                {
                    if (Terrains[i].HasTerrain) count++;
                }
                return count;
            }
        }

        public void OnTurnStart(bool isPlayerTurn)
        {
            IsPlayerTurn = isPlayerTurn;
            if (isPlayerTurn)
            {
                PlayerCharactersPlayedThisTurn = 0;
                TurnCount++;
            }
            else
            {
                EnemyCharactersPlayedThisTurn = 0;
            }

            // Les terrains posés au tour précédent deviennent actifs/scorables
            for (int i = 0; i < MAX_TERRAIN_SLOTS; i++)
            {
                if (Terrains[i].HasTerrain && Terrains[i].PlacedThisTurn)
                {
                    Terrains[i].PlacedThisTurn = false;
                    Terrains[i].IsActive = true;
                }
            }
        }
    }
}
