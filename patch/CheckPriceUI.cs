using System.Collections.Generic;
using System;
using UnityEngine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.inventory;
using System.Linq;
using System.Threading;
using UnityEngine.UIElements;
using System.Reflection;

namespace WankulCrazyPlugin.patch
{
    public class CheckPriceUI
    {
        //public static Dictionary<int, int> indexesAssociation = new();
        public static List<WankulCardData> wankulCardsSet = new List<WankulCardData>();
        public static bool isFromCheckPriceList = false;

        // Tableau d'enum mis en cache une seule fois (au lieu de Enum.GetValues à chaque appel).
        private static readonly Season[] CachedSeasons = (Season[])Enum.GetValues(typeof(Season));
        public static bool EvaluateCardPanelUI(int cardPageIndex, CheckPriceScreen __instance)
        {
            // Champs résolus via un cache (Plugin.GetPProperty) au lieu d'un AccessTools.Field
            // recalculé à chaque appel de cette méthode.
            var m_PosX = (float)Plugin.GetPProperty(__instance, "m_PosX");
            var m_LerpPosX = (float)Plugin.GetPProperty(__instance, "m_LerpPosX");
            var m_CurrentExpansionType = (ECardExpansionType)Plugin.GetPProperty(__instance, "m_CurrentExpansionType");
            var m_CardPageMaxIndex = (int)Plugin.GetPProperty(__instance, "m_CardPageMaxIndex");
            var m_ScrollEndPosParent = (GameObject)Plugin.GetPProperty(__instance, "m_ScrollEndPosParent");
            var m_CardPageIndex = (int)Plugin.GetPProperty(__instance, "m_CardPageIndex");

            Season currentSeason = CachedSeasons[ExpansionScreen.currentExpensionIndex];
            string currentSeasonText = SeasonsContainer.Seasons[currentSeason];
            List<WankulCardData> wankulCards = WankulCardsData.GetCardsFromSeason(currentSeason);

            m_PosX = 0f;
            m_LerpPosX = 0f;
            for (int i = 0; i < __instance.m_CheckPricePanelUIList.Count; i++)
            {
                __instance.m_CheckPricePanelUIList[i].SetActive(isActive: false);
            }

            __instance.m_CardExpansionText.text = currentSeasonText;
            int cardIndexOffset = cardPageIndex * __instance.m_MaxCardUICountPerPage;
            int totalCardsCount = wankulCards.Count();

            m_CardPageMaxIndex = Mathf.CeilToInt((float)totalCardsCount / (float)__instance.m_MaxCardUICountPerPage) - 1;

            for (int j = 0; j < __instance.m_MaxCardUICountPerPage; j++)
            {
                if (cardIndexOffset >= totalCardsCount)
                {
                    __instance.m_CheckPricePanelUIList[j].SetActive(isActive: false);
                    continue;
                }

                // Assurez-vous que l'index est valide avant d'ins�rer
                if (cardIndexOffset < wankulCardsSet.Count)
                {
                    wankulCardsSet[cardIndexOffset] = wankulCards[cardIndexOffset];
                }
                else
                {
                    // Ajoutez des �l�ments null jusqu'� ce que l'index soit valide
                    while (wankulCardsSet.Count <= cardIndexOffset)
                    {
                        wankulCardsSet.Add(null);
                    }
                    wankulCardsSet[cardIndexOffset] = wankulCards[cardIndexOffset];
                }

                __instance.m_CheckPricePanelUIList[j].InitCard(__instance, cardIndexOffset, m_CurrentExpansionType, false, 0);
                __instance.m_CheckPricePanelUIList[j].SetActive(isActive: true);
                __instance.m_ScrollEndParent.transform.parent = __instance.m_CheckPricePanelUIList[j].transform;
                __instance.m_ScrollEndParent.transform.position = __instance.m_CheckPricePanelUIList[j].transform.position;
                Vector3 position = __instance.m_ScrollEndParent.transform.position;
                position.y += __instance.m_CardScrollOffsetPosEnd.position.y - __instance.m_CardScrollOffsetPosStart.position.y;
                __instance.m_ScrollEndParent.transform.position = position;
                m_ScrollEndPosParent = __instance.m_ScrollEndParent;
                cardIndexOffset++;
            }

            __instance.m_PageText.text = m_CardPageIndex + 1 + " / " + (m_CardPageMaxIndex + 1);
            __instance.m_CardPageOptionGrp.SetActive(value: true);

            Plugin.SetPProperty(__instance, "m_PosX", m_PosX);
            Plugin.SetPProperty(__instance, "m_LerpPosX", m_LerpPosX);
            Plugin.SetPProperty(__instance, "m_CurrentExpansionType", m_CurrentExpansionType);
            Plugin.SetPProperty(__instance, "m_CardPageMaxIndex", m_CardPageMaxIndex);
            Plugin.SetPProperty(__instance, "m_ScrollEndPosParent", m_ScrollEndPosParent);
            Plugin.SetPProperty(__instance, "m_CardPageIndex", m_CardPageIndex);

            return false;
        }
        public static bool CheckPricePanelInitCard(CheckPriceScreen checkPriceScreen, int cardIndex, ECardExpansionType expansionType, bool isDestiny, CheckPricePanelUI __instance)
        {
            var m_IsItem = (bool)Plugin.GetPProperty(__instance, "m_IsItem");
            var m_IsCard = (bool)Plugin.GetPProperty(__instance, "m_IsCard");
            var m_CheckPriceScreen = (CheckPriceScreen)Plugin.GetPProperty(__instance, "m_CheckPriceScreen");
            var m_CardIndex = (int)Plugin.GetPProperty(__instance, "m_CardIndex");
            var m_CardExpansionType = (ECardExpansionType)Plugin.GetPProperty(__instance, "m_CardExpansionType");
            var m_IsDestiny = (bool)Plugin.GetPProperty(__instance, "m_IsDestiny");
            var m_TotalPrice = (float)Plugin.GetPProperty(__instance, "m_TotalPrice");

            WankulCardData wankulCardData = wankulCardsSet.ElementAt(cardIndex);

            CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardData);

            if (cardData == null)
            {
                //Plugin.Logger.LogInfo("CardData is null");
                cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                if (cardData == null)
                {
                    //Plugin.Logger.LogInfo("UnassciatedCardData is null");
                }
                WankulCardsData.Instance.SetFromMonster(cardData, wankulCardData);
            }

            cardData.isDestiny = false;
            __instance.m_UIGrp.SetActive(value: true);
            __instance.m_PrologueUIGrp.SetActive(value: false);

            m_IsItem = false;
            m_IsCard = true;
            m_CheckPriceScreen = checkPriceScreen;
            m_CardIndex = cardIndex;
            m_CardExpansionType = expansionType;
            m_IsDestiny = isDestiny;
            __instance.m_CardUI.SetCardUI(cardData);
            m_TotalPrice = wankulCardData.MarketPrice;

            if (wankulCardData is EffigyCardData effigyCard)
            {
                __instance.m_NameText.text = effigyCard.Title + "\n" + effigyCard.Effigy;
                if (effigyCard.Rarity > Rarity.R)
                {
                    cardData.isFoil = true;
                }
            }
            else if (wankulCardData is SpecialCardData)
            {
                __instance.m_NameText.text = wankulCardData.Title + "\n" + "SPECIAL";
            }
            else if (wankulCardData is TerrainCardData)
            {
                __instance.m_NameText.text = wankulCardData.Title + "\n" + "Terrain";
            }
            else
            {
                __instance.m_NameText.text = wankulCardData.Title + "\n" + "Erreur";
            }

            __instance.m_TotalPriceText.text = GameInstance.GetPriceString(m_TotalPrice);
            __instance.m_ItemImage.enabled = false;
            __instance.m_CardUI.gameObject.SetActive(value: true);

            Plugin.SetPProperty(__instance, "m_IsItem", m_IsItem);
            Plugin.SetPProperty(__instance, "m_IsCard", m_IsCard);
            Plugin.SetPProperty(__instance, "m_CheckPriceScreen", m_CheckPriceScreen);
            Plugin.SetPProperty(__instance, "m_CardIndex", m_CardIndex);
            Plugin.SetPProperty(__instance, "m_CardExpansionType", m_CardExpansionType);
            Plugin.SetPProperty(__instance, "m_IsDestiny", m_IsDestiny);
            Plugin.SetPProperty(__instance, "m_TotalPrice", m_TotalPrice);

            List<float> pastCardPricePercentChange = wankulCardData.PastPercent;
            if (pastCardPricePercentChange.Count > 1)
            {
                float cardMarketPriceCustomPercent = wankulCardData.generatedMarketPrice * (pastCardPricePercentChange[pastCardPricePercentChange.Count - 2] / 100);
                float num = m_TotalPrice - cardMarketPriceCustomPercent;
                if (num > 0.005f)
                {
                    __instance.m_UpArrow.SetActive(value: true);
                    __instance.m_DownArrow.SetActive(value: false);
                    __instance.m_NoChangeArrow.SetActive(value: false);
                    __instance.m_PriceChangeText.text = "+" + GameInstance.GetPriceString(num);
                    __instance.m_PriceChangeText.color = m_CheckPriceScreen.m_PositiveColor;
                }
                else if (num < -0.005f)
                {
                    __instance.m_UpArrow.SetActive(value: false);
                    __instance.m_DownArrow.SetActive(value: true);
                    __instance.m_NoChangeArrow.SetActive(value: false);
                    __instance.m_PriceChangeText.text = GameInstance.GetPriceString(num);
                    __instance.m_PriceChangeText.color = m_CheckPriceScreen.m_NegativeColor;
                }
                else
                {
                    __instance.m_UpArrow.SetActive(value: false);
                    __instance.m_DownArrow.SetActive(value: false);
                    __instance.m_NoChangeArrow.SetActive(value: true);
                    __instance.m_PriceChangeText.text = "+" + GameInstance.GetPriceString(0f);
                    __instance.m_PriceChangeText.color = m_CheckPriceScreen.m_NeutralColor;
                }
                return false;
            }
            else
            {
                __instance.m_UpArrow.SetActive(value: false);
                __instance.m_DownArrow.SetActive(value: false);
                __instance.m_NoChangeArrow.SetActive(value: true);
                __instance.m_PriceChangeText.text = "+" + GameInstance.GetPriceString(0f);


                return false;
            }
        }

        public static bool OnPressOpenCardPriceGraph(int cardIndex, ECardExpansionType expansionType, bool isDestiny, CheckPriceScreen __instance)
        {
            isFromCheckPriceList = true;
            __instance.m_ItemPriceGraphScreen.ShowCardPriceChart(cardIndex, expansionType, isDestiny, 0);

            MethodInfo openChildScreenMethod = Plugin.GetCachedMethod(__instance.GetType(), "OpenChildScreen");
            openChildScreenMethod.Invoke(__instance, new object[] { __instance.m_ItemPriceGraphScreen });

            return false;
        }

        public static bool ShowCardPriceChart(int cardIndex, ECardExpansionType expansionType, bool isDestiny, ItemPriceGraphScreen __instance)
        {
            if (isFromCheckPriceList)
            {
                __instance.m_CurrentScaleLineIndex = 0;
                WankulCardData wankulCardData = wankulCardsSet[cardIndex];
                UpdateCardPriceIfNeeded(wankulCardData);
                List<float> pricesList = new List<float>();

                for (int i = 0; i < wankulCardData.PastPercent.Count; i++)
                {
                    pricesList.Add(wankulCardData.generatedMarketPrice * (wankulCardData.PastPercent[i]) / 100);
                }


                MethodInfo EvaluatePriceChartMethod = Plugin.GetCachedMethod(__instance.GetType(), "EvaluatePriceChart");
                EvaluatePriceChartMethod.Invoke(__instance, new object[] { pricesList });

                CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardData);
                if (cardData == null)
                {
                    cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                    WankulCardsData.Instance.SetFromMonster(cardData, wankulCardData);
                }

                __instance.m_CardName.text = wankulCardData.Title;
                __instance.m_CardUI.SetCardUI(cardData);
                __instance.m_ItemGrp.SetActive(value: false);
                __instance.m_CardGrp.SetActive(value: true);

                isFromCheckPriceList = false;
                return false;
            }


            CardData cardDataSaveIndex = new CardData();
            cardDataSaveIndex.monsterType = CPlayerData.GetMonsterTypeFromCardSaveIndex(cardIndex, expansionType);
            cardDataSaveIndex.isFoil = cardIndex % CPlayerData.GetCardAmountPerMonsterType(expansionType) >= CPlayerData.GetCardAmountPerMonsterType(expansionType, includeFoilCount: false);
            cardDataSaveIndex.borderType = (ECardBorderType)(cardIndex % CPlayerData.GetCardAmountPerMonsterType(expansionType, includeFoilCount: false));
            cardDataSaveIndex.isDestiny = isDestiny;
            cardDataSaveIndex.expansionType = expansionType;

            WankulCardData wankulCardDataSaveIndex = WankulCardsData.Instance.GetFromMonster(cardDataSaveIndex, true);
            if (wankulCardDataSaveIndex != null)
            {
                UpdateCardPriceIfNeeded(wankulCardDataSaveIndex);
                __instance.m_CurrentScaleLineIndex = 0;
                List<float> pricesList = new List<float>();

                for (int i = 0; i < wankulCardDataSaveIndex.PastPercent.Count; i++)
                {
                    pricesList.Add(wankulCardDataSaveIndex.generatedMarketPrice * (wankulCardDataSaveIndex.PastPercent[i]) / 100);
                }

                MethodInfo EvaluatePriceChartMethod = Plugin.GetCachedMethod(__instance.GetType(), "EvaluatePriceChart");
                EvaluatePriceChartMethod.Invoke(__instance, new object[] { pricesList });

                CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardDataSaveIndex);
                if (cardData == null)
                {
                    cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                    WankulCardsData.Instance.SetFromMonster(cardData, wankulCardDataSaveIndex);
                }

                __instance.m_CardName.text = wankulCardDataSaveIndex.Title;
                __instance.m_CardUI.SetCardUI(cardData);
                __instance.m_ItemGrp.SetActive(value: false);
                __instance.m_CardGrp.SetActive(value: true);

                return false;
            }
            else
            {
                return true;
            }
        }

        private static void UpdateCardPriceIfNeeded(WankulCardData card)
        {
            int currentDay = CSaveLoad.m_SavedGame.m_CurrentDay + 1;
            int maxDayHistory = 30;
            while (card.PastPercent.Count < currentDay && card.PastPercent.Count < maxDayHistory)
            {
                CardPrice.UpdateCardPricePercent(card);
            }
        }
    }
}
