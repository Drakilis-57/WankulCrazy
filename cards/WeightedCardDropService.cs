using System.Collections.Generic;
using System.Linq;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.cards
{
    public static class WeightedCardDropService
    {
        public static WankulCardData SelectCard(List<WankulCardData> candidates, float randomValue, bool increaseRarity, Season season)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            float cumulativeDropChance = 0f;

            foreach (var card in candidates)
            {
                float increaseFactor = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity, season);
                cumulativeDropChance += card.Drop * increaseFactor;

                if (randomValue <= cumulativeDropChance)
                {
                    return card;
                }
            }

            return candidates[0]; // fallback comme dans l'implémentation originale
        }
    }
}
