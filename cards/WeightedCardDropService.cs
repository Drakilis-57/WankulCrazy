using System.Collections.Generic;
using System.Linq;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.cards
{
    public static class WeightedCardDropService
    {
        public static float GetCardEffectiveDrop(WankulCardData card, bool increaseRarity, Season season)
        {
            float baseDrop = card.Drop;
            if (card is EffigyCardData effigyCard)
            {
                var rarityData = RaritiesManager.GetRarity(effigyCard.RarityId);
                if (rarityData != null)
                {
                    baseDrop *= rarityData.DropMultiplier;
                }
            }
            float increaseFactor = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity, season);
            return baseDrop * increaseFactor;
        }

        public static WankulCardData SelectCard(List<WankulCardData> candidates, float randomValue, bool increaseRarity, Season season)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            float cumulativeDropChance = 0f;

            foreach (var card in candidates)
            {
                cumulativeDropChance += GetCardEffectiveDrop(card, increaseRarity, season);

                if (randomValue <= cumulativeDropChance)
                {
                    return card;
                }
            }

            return candidates[0]; // fallback comme dans l'implémentation originale
        }
    }
}
