using Xunit;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.Tests
{
    public class RarityIncreaseCalculatorTests
    {
        [Theory]
        [InlineData(Rarity.R, 0.25f)]
        [InlineData(Rarity.UR1, 1f)]
        [InlineData(Rarity.UR2, 1f)]
        [InlineData(Rarity.LB, 2f)]
        [InlineData(Rarity.LA, 2f)]
        [InlineData(Rarity.LO, 2f)]
        [InlineData(Rarity.C, 1f)]
        public void GetIncreaseFactor_WithIncreaseRarity_ReturnsCorrectFactor(Rarity rarity, float expectedFactor)
        {
            var card = new EffigyCardData { Rarity = rarity };

            float result = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity: true, Season.S01);

            Assert.Equal(expectedFactor, result);
        }

        [Theory]
        [InlineData(Rarity.R)]
        [InlineData(Rarity.LB)]
        public void GetIncreaseFactor_WithoutIncreaseRarity_ReturnsOne(Rarity rarity)
        {
            var card = new EffigyCardData { Rarity = rarity };

            float result = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity: false, Season.S01);

            Assert.Equal(1f, result);
        }

        [Theory]
        [InlineData(Rarity.PGW23, 2f)] // Rarity.PGW23 = 10
        [InlineData(Rarity.PGW24, 2f)] // Rarity.PGW24 = 11
        [InlineData(Rarity.C, 1f)]     // Rarity.C = 0
        public void GetIncreaseFactor_SeasonHS_ReturnsCorrectFactor(Rarity rarity, float expectedFactor)
        {
            var card = new EffigyCardData { Rarity = rarity };

            float result = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity: false, Season.HS);

            Assert.Equal(expectedFactor, result);
        }

        [Fact]
        public void GetIncreaseFactor_SeasonHS_WithIncreaseRarity_CumulatesCorrectly()
        {
            // Un cas particulier : si increaseRarity est vrai ET qu'on est en HS
            // Le code original :
            // factor = (switch rarity...) puis if (season == HS && rarity >= PGW23) factor = 2;
            // Cela ecrase le factor precedent. Verifions ce comportement qui est celui d'origine.

            var card = new EffigyCardData { Rarity = Rarity.PGW23 };

            float result = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity: true, Season.HS);

            Assert.Equal(2f, result); // Car Season.HS avec >= PGW23 force a 2
        }

        [Fact]
        public void GetIncreaseFactor_NotEffigyCard_ReturnsOne()
        {
            var card = new TerrainCardData();

            float result = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity: true, Season.HS);

            Assert.Equal(1f, result);
        }
    }
}
