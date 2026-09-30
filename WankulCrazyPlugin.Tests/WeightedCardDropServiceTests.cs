using System.Collections.Generic;
using Xunit;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.Tests
{
    public class WeightedCardDropServiceTests
    {
        [Fact]
        public void SelectCard_NullOrEmptyList_ReturnsNull()
        {
            var resultNull = WeightedCardDropService.SelectCard(null, 0.5f, false, Season.S01);
            Assert.Null(resultNull);

            var resultEmpty = WeightedCardDropService.SelectCard(new List<WankulCardData>(), 0.5f, false, Season.S01);
            Assert.Null(resultEmpty);
        }

        [Fact]
        public void SelectCard_RandomValueZero_ReturnsFirstCard()
        {
            var cards = new List<WankulCardData>
            {
                new EffigyCardData { Index = 1, Drop = 10f, Rarity = Rarity.C },
                new EffigyCardData { Index = 2, Drop = 10f, Rarity = Rarity.C }
            };

            var result = WeightedCardDropService.SelectCard(cards, 0f, false, Season.S01);

            Assert.NotNull(result);
            Assert.Equal(1, result.Index);
        }

        [Fact]
        public void SelectCard_RandomValueExceedsCumulativeDrop_ReturnsLastCard()
        {
            var cards = new List<WankulCardData>
            {
                new EffigyCardData { Index = 1, Drop = 10f, Rarity = Rarity.C },
                new EffigyCardData { Index = 2, Drop = 10f, Rarity = Rarity.C }
            };

            // La chance totale est 20, on met la randomValue proche de 20 (ou même égale, avec les floats c'est capricieux, mais >= 10 suffira)
            var result = WeightedCardDropService.SelectCard(cards, 15f, false, Season.S01);

            Assert.NotNull(result);
            Assert.Equal(2, result.Index);
        }

        [Fact]
        public void SelectCard_TotalDropIsZero_ReturnsFirstCard_AsFallback()
        {
            var cards = new List<WankulCardData>
            {
                new EffigyCardData { Index = 1, Drop = 0f, Rarity = Rarity.C },
                new EffigyCardData { Index = 2, Drop = 0f, Rarity = Rarity.C }
            };

            // Même si le total est 0, on essaye de s'assurer que ça ne crash pas
            var result = WeightedCardDropService.SelectCard(cards, 0f, false, Season.S01);

            Assert.NotNull(result);
            Assert.Equal(1, result.Index);
        }

        [Fact]
        public void SelectCard_SelectsCorrectCardBasedOnWeightAndIncreaseFactor()
        {
            var cardC = new EffigyCardData { Index = 1, Drop = 100f, Rarity = Rarity.C };
            var cardLB = new EffigyCardData { Index = 2, Drop = 50f, Rarity = Rarity.LB };

            var cards = new List<WankulCardData> { cardC, cardLB };

            // Avec increaseRarity = true, cardLB a un facteur de 2f et un DropMultiplier de 0.05f (50 * 0.05 * 2 = 5f).
            // Total = 100(C) + 5(LB) = 105.

            // randomValue = 50f -> Tombe sur cardC (cumul à 100)
            var result1 = WeightedCardDropService.SelectCard(cards, 50f, increaseRarity: true, Season.S01);
            Assert.Equal(1, result1.Index);

            // randomValue = 102f -> Dépasse 100, tombe sur cardLB (cumul à 105)
            var result2 = WeightedCardDropService.SelectCard(cards, 102f, increaseRarity: true, Season.S01);
            Assert.Equal(2, result2.Index);
        }
    }
}
