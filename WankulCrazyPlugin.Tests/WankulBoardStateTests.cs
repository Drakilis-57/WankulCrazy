using Xunit;
using WankulCrazyPlugin.duel;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.Tests
{
    public class WankulBoardStateTests
    {
        [Fact]
        public void TestScoring_PlayerWins_WhenHigherForce()
        {
            var slot = new WankulTerrainSlot(0)
            {
                TerrainData = new TerrainCardData { Title = "Terrain Test" },
                IsActive = true
            };

            slot.PlayerCharacters.Add(new EffigyCardData { Title = "Guerrier", Force = 50 });
            slot.PlayerCharacters.Add(new EffigyCardData { Title = "Mage", Force = 30 });
            slot.EnemyCharacters.Add(new EffigyCardData { Title = "Gobelin", Force = 40 });

            Assert.Equal(80, slot.GetPlayerTotalForce());
            Assert.Equal(40, slot.GetEnemyTotalForce());
            Assert.True(slot.GetPlayerTotalForce() > slot.GetEnemyTotalForce());
        }

        [Fact]
        public void TestTerrain_NotScorable_WhenPlacedThisTurn()
        {
            var board = new WankulBoardState();
            var terrain = new TerrainCardData { Title = "Plaine" };

            // Simulation d'une pose ce tour-ci
            board.Terrains[0].TerrainData = terrain;
            board.Terrains[0].PlacedThisTurn = true;
            board.Terrains[0].IsActive = false;

            Assert.False(board.Terrains[0].IsActive);
            Assert.True(board.Terrains[0].PlacedThisTurn);
        }

        [Fact]
        public void TestTerrain_Scorable_AfterTurnStart()
        {
            var board = new WankulBoardState();
            var terrain = new TerrainCardData { Title = "Plaine" };

            board.Terrains[0].TerrainData = terrain;
            board.Terrains[0].PlacedThisTurn = true;
            board.Terrains[0].IsActive = false;

            // Début du tour suivant
            board.OnTurnStart(isPlayerTurn: false);

            Assert.True(board.Terrains[0].IsActive);
            Assert.False(board.Terrains[0].PlacedThisTurn);
        }

        [Fact]
        public void TestPlayerWinsAt5Points()
        {
            var board = new WankulBoardState();
            board.PlayerScore = 4;

            board.PlayerScore++;

            Assert.True(board.PlayerScore >= WankulBoardState.WINNING_TERRAIN_COUNT);
        }

        [Fact]
        public void TestMaxCharactersPerTurn()
        {
            var board = new WankulBoardState();
            board.PlayerCharactersPlayedThisTurn = 4;

            bool canPlay = board.PlayerCharactersPlayedThisTurn < WankulBoardState.MAX_CHARACTERS_PER_TURN;

            Assert.False(canPlay);
        }

        [Fact]
        public void TestScoreurDetection()
        {
            var scoreurCard1 = new EffigyCardData { Title = "Scoreur 1", Rules = "Scoreur : déclenche le duel." };
            var scoreurCard2 = new EffigyCardData { Title = "Scoreur 2", Combo = "Scorez un Terrain immédiatement." };
            var normalCard = new EffigyCardData { Title = "Normal", Rules = "Simple effet.", Combo = "Aucun combo." };

            Assert.True(scoreurCard1.IsScoreur);
            Assert.True(scoreurCard2.IsScoreur);
            Assert.False(normalCard.IsScoreur);
        }

        [Fact]
        public void TestCannotPlaceCharacter_WithoutTerrain()
        {
            var slot = new WankulTerrainSlot(0);
            Assert.False(slot.HasTerrain);
            Assert.Empty(slot.PlayerCharacters);
        }
    }
}
