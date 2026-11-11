using System;
using System.Collections.Generic;
using System.Linq;

namespace WankulCrazyPlugin.cards
{
    public readonly struct GradedAlbumEntry
    {
        public readonly int InventoryIndex;
        public readonly int CardKey;
        public readonly int SeasonOrder;
        public readonly int CardNumber;
        public readonly int RarityOrder;
        public readonly float MarketPrice;
        public readonly int Grade;

        public GradedAlbumEntry(
            int inventoryIndex,
            int cardKey,
            int seasonOrder,
            int cardNumber,
            int rarityOrder,
            float marketPrice,
            int grade)
        {
            InventoryIndex = inventoryIndex;
            CardKey = cardKey;
            SeasonOrder = seasonOrder;
            CardNumber = cardNumber;
            RarityOrder = rarityOrder;
            MarketPrice = marketPrice;
            Grade = grade;
        }
    }

    public static class GradedCardService
    {
        public const int MaxCardGrade = 10;
        public const int CardsPerBinderPage = 12;
        public const int DefaultSortMethodIndex = 2; // Prix
        public const int MaxSortButtonIndex = 4;     // Doublon

        private static readonly SortType[] SortTypesByButtonIndex =
        {
            SortType.Number,
            SortType.Rarity,
            SortType.Price,
            SortType.Amount,
            SortType.Double
        };

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

        /// <summary>Borne une note de gradation à l'intervalle valide vanilla [1..10] si positive.</summary>
        public static int ClampGrade(int grade)
        {
            return grade > MaxCardGrade ? MaxCardGrade : grade;
        }

        /// <summary>Borne l'index de bouton de tri [0..4] avec repli sur Prix (2).</summary>
        public static int ClampSortMethodIndex(int methodIndex, int fallback = DefaultSortMethodIndex)
        {
            return (methodIndex >= 0 && methodIndex <= MaxSortButtonIndex) ? methodIndex : fallback;
        }

        /// <summary>Convertit l'index de bouton Wankul [0..4] en SortType correspondant.</summary>
        public static SortType ButtonIndexToSortType(int buttonIndex)
        {
            return (buttonIndex >= 0 && buttonIndex < SortTypesByButtonIndex.Length)
                ? SortTypesByButtonIndex[buttonIndex]
                : SortType.Price;
        }

        /// <summary>Convertit un SortType Wankul en index de bouton [0..4].</summary>
        public static int SortTypeToButtonIndex(SortType sortType)
        {
            for (int i = 0; i < SortTypesByButtonIndex.Length; i++)
            {
                if (SortTypesByButtonIndex[i] == sortType)
                {
                    return i;
                }
            }
            return DefaultSortMethodIndex;
        }

        /// <summary>Calcule le nombre maximal de pages du classeur (au moins 1).</summary>
        public static int ComputeMaxPages(int itemCount, int pageSize = CardsPerBinderPage)
        {
            if (pageSize <= 0) pageSize = CardsPerBinderPage;
            return itemCount <= 0 ? 1 : (itemCount + pageSize - 1) / pageSize;
        }

        /// <summary>Borne l'index de page courant dans [1..maxPages].</summary>
        public static int ClampPageIndex(int pageIndex, int maxPages)
        {
            if (maxPages < 1) maxPages = 1;
            if (pageIndex < 1) return 1;
            if (pageIndex > maxPages) return maxPages;
            return pageIndex;
        }

        /// <summary>Trie les indices d'inventaire des cartes gradées selon le mode de tri actif.</summary>
        public static List<int> SortGradedEntries(IReadOnlyList<GradedAlbumEntry> entries, SortType sortType)
        {
            if (entries == null || entries.Count == 0)
            {
                return new List<int>();
            }

            IEnumerable<GradedAlbumEntry> ordered;
            switch (sortType)
            {
                case SortType.Number:
                    ordered = entries
                        .OrderBy(e => e.SeasonOrder)
                        .ThenBy(e => e.CardNumber)
                        .ThenByDescending(e => e.Grade)
                        .ThenByDescending(e => e.MarketPrice)
                        .ThenBy(e => e.InventoryIndex);
                    break;

                case SortType.Rarity:
                    ordered = entries
                        .OrderByDescending(e => e.RarityOrder)
                        .ThenByDescending(e => e.Grade)
                        .ThenByDescending(e => e.MarketPrice)
                        .ThenBy(e => e.SeasonOrder)
                        .ThenBy(e => e.CardNumber)
                        .ThenBy(e => e.InventoryIndex);
                    break;

                case SortType.Amount:
                    ordered = entries
                        .OrderByDescending(e => e.Grade)
                        .ThenByDescending(e => e.MarketPrice)
                        .ThenBy(e => e.SeasonOrder)
                        .ThenBy(e => e.CardNumber)
                        .ThenBy(e => e.InventoryIndex);
                    break;

                case SortType.Double:
                    var counts = new Dictionary<int, int>(entries.Count);
                    for (int i = 0; i < entries.Count; i++)
                    {
                        int key = entries[i].CardKey;
                        counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
                    }
                    ordered = entries
                        .OrderByDescending(e => counts[e.CardKey] > 1 ? 1 : 0)
                        .ThenByDescending(e => counts[e.CardKey])
                        .ThenByDescending(e => e.MarketPrice)
                        .ThenBy(e => e.CardKey)
                        .ThenByDescending(e => e.Grade)
                        .ThenBy(e => e.InventoryIndex);
                    break;

                case SortType.Price:
                default:
                    ordered = entries
                        .OrderByDescending(e => e.MarketPrice)
                        .ThenByDescending(e => e.Grade)
                        .ThenBy(e => e.SeasonOrder)
                        .ThenBy(e => e.CardNumber)
                        .ThenBy(e => e.InventoryIndex);
                    break;
            }

            return ordered.Select(e => e.InventoryIndex).ToList();
        }
    }
}
