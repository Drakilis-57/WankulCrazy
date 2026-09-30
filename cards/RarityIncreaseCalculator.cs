using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.cards
{
    public static class RarityIncreaseCalculator
    {
        public static float GetIncreaseFactor(WankulCardData card, bool increaseRarity, Season season)
        {
            float increaseFactor = 1f;

            if (card is EffigyCardData effigyCard)
            {
                if (increaseRarity)
                {
                    switch (effigyCard.Rarity)
                    {
                        case Rarity.R:
                            increaseFactor = 0.25f;
                            break;
                        case Rarity.UR1:
                        case Rarity.UR2:
                            increaseFactor = 1f;
                            break;
                        case Rarity.LB:
                        case Rarity.LA:
                        case Rarity.LO:
                            increaseFactor = 2f;
                            break;
                        default:
                            increaseFactor = 1f;
                            break;
                    }
                }

                if (season == Season.HS && effigyCard.Rarity >= Rarity.PGW23)
                {
                    increaseFactor = 2f;
                }
            }

            return increaseFactor;
        }
    }
}
