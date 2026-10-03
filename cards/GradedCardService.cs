using System;
using System.Collections.Generic;

namespace WankulCrazyPlugin.cards
{
    public static class GradedCardService
    {
        /// <summary>
        /// Calcule le prix marché d'une carte gradée à partir de son prix de base, de son index de sauvegarde et de sa note (grade 1 à 10).
        /// Reproduit fidèlement la formule du jeu vanilla (MarketPrice.GetMarketPrice) en appliquant le multiplicateur et le bonus de grade.
        /// </summary>
        public static float CalculateGradedMarketPrice(float baseMarketPrice, int cardSaveIndex, int cardGrade, IList<float> multiplierList = null)
        {
            if (cardGrade <= 0)
            {
                return baseMarketPrice;
            }

            float multiplier;
            if (multiplierList != null && multiplierList.Count > 0)
            {
                int index2 = Math.Abs(cardSaveIndex * 10 + (cardGrade - 1)) % multiplierList.Count;
                multiplier = multiplierList[index2];
            }
            else
            {
                // Multiplicateurs de repli calqués sur la distribution vanilla (0.01x à 7x)
                switch (cardGrade)
                {
                    case 10: multiplier = 5.0f; break;
                    case 9: multiplier = 2.5f; break;
                    case 8: multiplier = 1.6f; break;
                    case 7: multiplier = 1.1f; break;
                    case 6: multiplier = 0.8f; break;
                    case 5: multiplier = 0.55f; break;
                    case 4: multiplier = 0.4f; break;
                    case 3: multiplier = 0.3f; break;
                    case 2: multiplier = 0.2f; break;
                    default: multiplier = 0.1f; break;
                }
            }

            float bonus = 0f;
            if (cardGrade >= 10)
            {
                bonus = multiplier * 12f;
            }
            else
            {
                switch (cardGrade)
                {
                    case 9:
                        bonus = multiplier * 8f;
                        break;
                    case 8:
                        bonus = multiplier * 4f;
                        break;
                    case 7:
                        bonus = multiplier * 2f;
                        break;
                }
            }

            return (float)Math.Round(multiplier * baseMarketPrice * 100f) / 100f + bonus;
        }
    }
}
