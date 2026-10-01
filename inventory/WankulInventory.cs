using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.inventory
{

    public class WankulInventory : Singleton<WankulInventory>
    {
        public Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> wankulCards = [];


        private static void LogError(string message)
        {
            try
            {
                if (Plugin.Logger != null) Plugin.Logger.LogError(message);
                else Console.WriteLine("[Error] " + message);
            }
            catch (Exception)
            {
                Console.WriteLine("[Error] " + message);
            }
        }

        public static Season ConvertPackTypeToSeason(ECollectionPackType packType)
        {
            // Récupère la valeur dynamique de "Stellar" et "Legacy" / "Ascension"
            ECollectionPackType stellarPack = EnumExtensions.SafeParseECollectionPackType("Stellar");
            ECollectionPackType stellarPackTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");
            ECollectionPackType legacyPack = EnumExtensions.SafeParseECollectionPackType("Legacy");
            ECollectionPackType legacyPackTaux = EnumExtensions.SafeParseECollectionPackType("LegacyTaux");
            ECollectionPackType ascensionPack = EnumExtensions.SafeParseECollectionPackType("AscensionCardPack");

            if (packType == ECollectionPackType.BasicCardPack || packType == ECollectionPackType.DestinyBasicCardPack)
                return Season.S01;
            else if (packType == ECollectionPackType.RareCardPack || packType == ECollectionPackType.DestinyRareCardPack)
                return Season.S02;
            else if (packType == ECollectionPackType.EpicCardPack || packType == ECollectionPackType.DestinyEpicCardPack)
                return Season.S03;
            else if (packType == stellarPack || packType == stellarPackTaux)
                return Season.S04;
            else if (packType == legacyPack || packType == legacyPackTaux || (ascensionPack != (ECollectionPackType)0 && packType == ascensionPack))
                return Season.S05;
            else
                return Season.HS;
        }

        public static WankulCardData DropCard(ECollectionPackType packType, List<WankulCardData> alreadySelectedCards, bool isTerrain = false, bool isMinRare = false, bool isMinUR = false, bool isMinLegendary = false, bool isRare = false)
        {
            ECollectionPackType stellarPackTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");
            ECollectionPackType legacyPackTaux = EnumExtensions.SafeParseECollectionPackType("LegacyTaux");

            bool increaseRarity = false;
            Season season = ConvertPackTypeToSeason(packType);

            if (
                packType == ECollectionPackType.DestinyBasicCardPack ||
                packType == ECollectionPackType.DestinyRareCardPack ||
                packType == ECollectionPackType.DestinyEpicCardPack ||
                packType == ECollectionPackType.DestinyLegendaryCardPack ||
                packType == stellarPackTaux ||
                packType == legacyPackTaux
            )
            {
                increaseRarity = true;
            }

            List<WankulCardData> allCards = WankulCardsData.Instance.cards;
            List<WankulCardData> seasonalCard;

            if (season != Season.HS)
            {
                seasonalCard = allCards.FindAll(card => card.Season == season);
            }
            else
            {
                seasonalCard = allCards;
            }

            if (isTerrain)
            {
                List<WankulCardData> terrainCards = seasonalCard.FindAll(card => card is TerrainCardData);
                if (terrainCards.Count > 0)
                {
                    seasonalCard = terrainCards;
                }
                else
                {
                    seasonalCard = seasonalCard.FindAll(card => card is not TerrainCardData);
                }
            }
            else
            {
                seasonalCard = seasonalCard.FindAll(card => card is not TerrainCardData);
            }

            if (!isTerrain && (isMinRare || isMinLegendary || isMinUR || isRare))
            {
                List<EffigyCardData> effigyCardsData = seasonalCard
                    .FindAll(card => card is EffigyCardData)
                    .ConvertAll(card => (EffigyCardData)card);

                if (isMinRare)
                {
                    seasonalCard = effigyCardsData.FindAll(card =>
                        card.Rarity >= Rarity.R || (RaritiesManager.GetRarity(card.RarityId)?.IsEligibleForMinRare ?? false))
                        .ConvertAll(card => (WankulCardData)card);
                }
                else if (isMinUR)
                {
                    seasonalCard = effigyCardsData.FindAll(card => card.Rarity >= Rarity.UR1)
                        .ConvertAll(card => (WankulCardData)card);
                }
                else if (isMinLegendary)
                {
                    seasonalCard = effigyCardsData.FindAll(card => card.Rarity >= Rarity.LB)
                        .ConvertAll(card => (WankulCardData)card);
                }

                if (isRare)
                {
                    seasonalCard = effigyCardsData.FindAll(card =>
                        card.Rarity == Rarity.R || (RaritiesManager.GetRarity(card.RarityId)?.IsEligibleForMinRare ?? false))
                        .ConvertAll(card => (WankulCardData)card);
                }

                List<WankulCardData> specialCardsData = allCards
                    .FindAll(card => card is SpecialCardData);
                seasonalCard.AddRange(specialCardsData);
            }
            else if (!isTerrain && !isMinRare)
            {
                seasonalCard = seasonalCard.FindAll(card =>
                    !(card is EffigyCardData effigyCard && (effigyCard.Rarity >= Rarity.R || (RaritiesManager.GetRarity(effigyCard.RarityId)?.IsEligibleForMinRare ?? false)))
                );
            }

            if (seasonalCard.Count == 0)
            {
                LogError("No available cards to drop");
                return null;
            }

            List<WankulCardData> uniqueCards = seasonalCard.Where(card => !alreadySelectedCards.Contains(card)).ToList();
            if (uniqueCards.Count > 0)
            {
                seasonalCard = uniqueCards;
            }

            float totalDropChance = 0f;
            foreach (var card in seasonalCard)
            {
                totalDropChance += WeightedCardDropService.GetCardEffectiveDrop(card, increaseRarity, season);
            }

            float randomValue = RandomUtils.Range(0f, totalDropChance);

            WankulCardData selectedCard = WeightedCardDropService.SelectCard(seasonalCard, randomValue, increaseRarity, season);
            if (selectedCard != null)
            {
                alreadySelectedCards.Add(selectedCard);
            }
            return selectedCard;
        }

        public static WankulCardData DropCardGold(ECollectionPackType packType, List<WankulCardData> alreadySelectedCards)
        {
            ECollectionPackType stellarPackTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");
            ECollectionPackType legacyPackTaux = EnumExtensions.SafeParseECollectionPackType("LegacyTaux");
            bool increaseRarity = false;
            Season season = ConvertPackTypeToSeason(packType);

            if (
                packType == ECollectionPackType.DestinyBasicCardPack ||
                packType == ECollectionPackType.DestinyRareCardPack ||
                packType == ECollectionPackType.DestinyEpicCardPack ||
                packType == ECollectionPackType.DestinyLegendaryCardPack ||
                packType == stellarPackTaux ||
                packType == legacyPackTaux
            )
            {
                increaseRarity = true;
            }

            List<WankulCardData> allCards = WankulCardsData.Instance.cards;


            allCards = allCards.FindAll(card => card is not TerrainCardData);

            List<WankulCardData> seasonalCard;

            List<int> BattleGoldCards = [
                300175, 300176, 300177, 300178, 300179, 300180, // Nouveaux index S03 (LA, LO)
                357, 358, 359, 360, 361, 362, 363, 364 // Rétrocompatibilité anciens index
            ];

            List<int> StellardGoldCards = [
                400175, 400176, 400177, 400178, 400179, 400180, // Nouveaux index S04 (LA, LO)
                734, 735, 736, 737, 738, 739, 740, 741 // Rétrocompatibilité anciens index
            ];

            List<int> LegacyGoldCards = [
                500170, // Rage LA
                500171, // Rage LA
                500172, // Dieu de la guerre LA
                500173, // Demi-dieu nordique LA
                500174, // Fin stratège LO
                500175, // Fin stratège LO
            ];

            if (season == Season.S03)
            {
                seasonalCard = allCards.FindAll(card => BattleGoldCards.Contains(card.Index));
            }
            else if (season == Season.S04)
            {
                seasonalCard = allCards.FindAll(card => StellardGoldCards.Contains(card.Index));
            }
            else if (season == Season.S05)
            {
                seasonalCard = allCards.FindAll(card => LegacyGoldCards.Contains(card.Index));
            }
            else
            {
                LogError($"DropCardGold: Season {season} not supported for gold cards, fallback to high rarity");
                seasonalCard = allCards.FindAll(card => card.Season == season && card is EffigyCardData effigy && (effigy.RarityId == "LO" || effigy.RarityId == "LA"));
            }

            // Fallback si la liste par index est vide : chercher les cartes LO/LA de la saison
            if (seasonalCard.Count == 0)
            {
                seasonalCard = allCards.FindAll(card => card.Season == season && card is EffigyCardData effigy && (effigy.RarityId == "LO" || effigy.RarityId == "LA"));
            }

            // Si toujours vide, fallback vers n'importe quelle carte de la saison
            if (seasonalCard.Count == 0)
            {
                LogError("No available gold cards to drop, falling back to seasonal cards");
                seasonalCard = allCards.FindAll(card => card.Season == season);
            }

            if (seasonalCard.Count == 0)
            {
                LogError("No cards found at all, falling back to AJETER");
                return WankulCardsData.GetAJETER();
            }

            // Filtrer les cartes déjà sélectionnées pour éviter les doublons si possible
            var uniqueSeasonalCard = seasonalCard.Where(card => !alreadySelectedCards.Contains(card)).ToList();
            if (uniqueSeasonalCard.Count > 0)
            {
                seasonalCard = uniqueSeasonalCard;
            }

            float totalDropChance = 0f;
            foreach (var card in seasonalCard)
            {
                float increaseFactor = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity, season);
                totalDropChance += card.Drop * increaseFactor;
            }

            float randomValue = RandomUtils.Range(0f, totalDropChance);
            float cumulativeDropChance = 0f;

            foreach (var card in seasonalCard)
            {
                float increaseFactor = RarityIncreaseCalculator.GetIncreaseFactor(card, increaseRarity, season);

                cumulativeDropChance += card.Drop * increaseFactor;
                if (randomValue <= cumulativeDropChance)
                {
                    // Ajouter la carte sélectionnée aux cartes déjà sélectionnées pour éviter un doublon
                    alreadySelectedCards.Add(card);
                    return card;
                }
            }

            if (seasonalCard.Count > 0)
            {
                WankulCardData fallbackCard = seasonalCard[0];
                alreadySelectedCards.Add(fallbackCard);
                return fallbackCard;
            }

            LogError("Failed to drop a card, falling back to AJETER");
            return WankulCardsData.GetAJETER();
        }


        public static WankulCardData randFromPackType(ECollectionPackType packType)
        {
            // Filtrage par saison en O(1) amorti (index précalculé) au lieu d'un
            // List.FindAll sur toutes les cartes à chaque tirage (jusqu'à 10x/booster).
            Season season = ConvertPackTypeToSeason(packType);
            List<WankulCardData> seasonalCard = WankulCardsData.GetCardsBySeasonFast(season);

            if (seasonalCard == null || seasonalCard.Count == 0)
            {
                // Fallback : si la saison spécifique n'a pas de cartes chargées, prendre parmi toutes les cartes disponibles
                seasonalCard = WankulCardsData.Instance.cards;
            }

            if (seasonalCard == null || seasonalCard.Count == 0)
            {
                LogError("No available cards to drop");
                return null;
            }

            int randomValue = RandomUtils.Range(0, seasonalCard.Count);

            return seasonalCard[randomValue];
        }


        public static void AddCard(WankulCardData wankulCardData, CardData cardData, int amount)
        {
            if (wankulCardData == null)
            {
                LogError("Cannot AddCard: wankulCardData is null");
                return;
            }

            if (!Instance.wankulCards.ContainsKey(wankulCardData.Index))
            {
                Instance.wankulCards[wankulCardData.Index] = (wankulCardData, cardData, amount);
            }
            else
            {
                (WankulCardData, CardData, int) inventoryWankulCard = Instance.wankulCards[wankulCardData.Index];
                inventoryWankulCard.Item3 = inventoryWankulCard.Item3 + amount;
                Instance.wankulCards[wankulCardData.Index] = inventoryWankulCard;
            }
        }

        public static void RemoveCard(WankulCardData wankulCardData, int amount)
        {
            if (Instance.wankulCards.ContainsKey(wankulCardData.Index))
            {
                (WankulCardData, CardData, int) inventoryWankulCard = Instance.wankulCards[wankulCardData.Index];
                inventoryWankulCard.Item3 = inventoryWankulCard.Item3 - amount;
                if (inventoryWankulCard.Item3 <= 0)
                {
                    Instance.wankulCards.Remove(wankulCardData.Index);
                }
                else
                {
                    Instance.wankulCards[wankulCardData.Index] = inventoryWankulCard;
                }
            }
        }

        public static Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> GetCardsBySeason(Season season)
        {
            return Instance.wankulCards.Where(card => card.Value.wankulcard.Season == season).ToDictionary(card => card.Key, card => card.Value);
        }

        public static float GetMaxPrice()
        {
            float maxPrice = 0f;
            foreach (var card in Instance.wankulCards)
            {
                if (card.Value.Item1.MarketPrice > maxPrice)
                {
                    maxPrice = card.Value.wankulcard.MarketPrice;
                }
            }
            return maxPrice;
        }

        public static float GetAveragePrice()
        {
            float totalPrice = 0f;
            foreach (var card in Instance.wankulCards)
            {
                totalPrice += card.Value.Item1.MarketPrice;
            }
            return totalPrice / Instance.wankulCards.Count;
        }

        public static float GetTotalPrice()
        {
            float totalPrice = 0f;
            foreach (var card in Instance.wankulCards)
            {
                totalPrice += card.Value.Item1.MarketPrice * card.Value.Item3;
            }
            return totalPrice;
        }

        public static float GetTotalPriceBySeason(Season season)
        {
            float totalPrice = 0f;
            foreach (var card in Instance.wankulCards)
            {
                if (card.Value.Item1.Season == season)
                {
                    totalPrice += card.Value.Item1.MarketPrice * card.Value.Item3;
                }
            }
            return totalPrice;
        }

        public static (WankulCardData wankulcard, CardData card, int amount) GetWankulCardFormGameCard(CardData cardData)
        {
            string key = $"{cardData.monsterType}_{cardData.borderType}_{cardData.expansionType}";
            return Instance.wankulCards.Values.FirstOrDefault(card => $"{card.card.monsterType}_{card.card.borderType}_{card.card.expansionType}" == key);
        }

        public static bool isNewWankulCard(WankulCardData wankulCardData)
        {
            if (wankulCardData == null)
            {
                return false;
            }
            Instance.wankulCards.TryGetValue(wankulCardData.Index, out var card);
            if (card.wankulcard == null)
            {
                return true;
            }
            return card.amount == 0;
        }


        public static (WankulCardData wankulcard, CardData card, int amount) GetWankulCardDataForTradeOffer()
        {
            List<ECollectionPackType> dropableExpansion = [
                ECollectionPackType.BasicCardPack
            ];

            EItemType stellarCardPack = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType stellarCardPackTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            ECollectionPackType stellarCardExpansion = EnumExtensions.SafeParseECollectionPackType("Stellar");
            ECollectionPackType stellarCardExpansionTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");

            EItemType legacyCardPack = EnumExtensions.SafeParseEItemType("BoosterLegacy");
            EItemType legacyCardPackTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
            ECollectionPackType legacyCardExpansion = EnumExtensions.SafeParseECollectionPackType("Legacy");
            ECollectionPackType legacyCardExpansionTaux = EnumExtensions.SafeParseECollectionPackType("LegacyTaux");

            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(EItemType.RareCardPack))
            {
                dropableExpansion.Add(ECollectionPackType.RareCardPack);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(EItemType.EpicCardPack))
            {
                dropableExpansion.Add(ECollectionPackType.EpicCardPack);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(stellarCardPack))
            {
                dropableExpansion.Add(stellarCardExpansion);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(EItemType.LegendaryCardPack))
            {
                dropableExpansion.Add(ECollectionPackType.LegendaryCardPack);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(EItemType.DestinyBasicCardPack))
            {
                dropableExpansion.Add(ECollectionPackType.DestinyBasicCardPack);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(EItemType.DestinyRareCardPack))
            {
                dropableExpansion.Add(ECollectionPackType.DestinyRareCardPack);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(EItemType.DestinyEpicCardPack))
            {
                dropableExpansion.Add(ECollectionPackType.DestinyEpicCardPack);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(stellarCardPackTaux))
            {
                dropableExpansion.Add(stellarCardExpansionTaux);
            }
            if (CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(EItemType.DestinyLegendaryCardPack))
            {
                dropableExpansion.Add(ECollectionPackType.DestinyLegendaryCardPack);
            }
            if (legacyCardPack != (EItemType)0 && CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(legacyCardPack))
            {
                dropableExpansion.Add(legacyCardExpansion);
            }
            if (legacyCardPackTaux != (EItemType)0 && CPlayerData.m_ShopLevel >= InventoryBase.GetUnlockItemLevelRequired(legacyCardPackTaux))
            {
                dropableExpansion.Add(legacyCardExpansionTaux);
            }

            ECollectionPackType selectedPackType = dropableExpansion[RandomUtils.Range(0, dropableExpansion.Count)];
            bool isTerrain = RandomUtils.Range(0, 2) == 1;
            bool isMinRare = RandomUtils.Range(0, 2) == 1;
            bool isMinUR = RandomUtils.Range(0, 100) < 50;
            bool isMinLegendary = RandomUtils.Range(0, 200) < 50;

            WankulCardData wankulCardData = DropCard(selectedPackType, new List<WankulCardData>(), isTerrain, isMinRare, isMinUR, isMinLegendary);

            CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardData);
            if (cardData == null)
            {
                cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                WankulCardsData.Instance.SetFromMonster(cardData, wankulCardData);
            }

            int amount = Instance.wankulCards.ContainsKey(wankulCardData.Index) ? Instance.wankulCards[wankulCardData.Index].amount : 0;

            return (wankulCardData, cardData, amount);
        }

        public static (WankulCardData wankulcard, CardData card, int amount) GetWankulCardDataForTradeOfferByPrice(CardData fromCardData)
        {
            WankulCardData fromWankulCardData = WankulCardsData.Instance.GetFromMonster(fromCardData, true);

            if (fromWankulCardData == null)
            {
                LogError("GetWankulCardDataForTradeOfferByPrice: No wankul card found for this card");
                return GetWankulCardDataForTradeOffer();
            }

            float minFactor = 0.75f;
            float maxFactor = 1.25f;

            float minPrice = fromWankulCardData.MarketPrice * minFactor;
            float maxPrice = fromWankulCardData.MarketPrice * maxFactor;

            List<WankulCardData> inPriceBoundCards = new List<WankulCardData>();
            foreach (var card in WankulCardsData.Instance.cards)
            {
                if (card.Index != fromWankulCardData.Index && card.MarketPrice >= minPrice && card.MarketPrice <= maxPrice)
                {
                    inPriceBoundCards.Add(card);
                }
            }

            WankulCardData wankulCardData = null;
            if (inPriceBoundCards.Count > 0)
            {
                int randomValue = RandomUtils.Range(0, inPriceBoundCards.Count);
                wankulCardData = inPriceBoundCards[randomValue];
            }
            else
            {
                // Fallback si aucune carte dans la fourchette +/- 25% (ex: carte très chère LO / TOR)
                WankulCardData closestCard = null;
                float minDiff = float.MaxValue;

                foreach (var card in WankulCardsData.Instance.cards)
                {
                    if (card.Index != fromWankulCardData.Index)
                    {
                        float diff = Math.Abs(card.MarketPrice - fromWankulCardData.MarketPrice);
                        if (diff < minDiff)
                        {
                            minDiff = diff;
                            closestCard = card;
                        }
                    }
                }

                if (closestCard != null)
                {
                    wankulCardData = closestCard;
                }
                else
                {
                    return GetWankulCardDataForTradeOffer();
                }
            }

            int amount = Instance.wankulCards.ContainsKey(wankulCardData.Index) ? Instance.wankulCards[wankulCardData.Index].amount : 0;

            CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardData);
            if (cardData == null)
            {
                cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                WankulCardsData.Instance.SetFromMonster(cardData, wankulCardData);
            }

            return (wankulCardData, cardData, amount);
        }
    }
}
