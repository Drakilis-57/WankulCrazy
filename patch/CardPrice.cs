using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WankulCrazyPlugin.cards;
using System.Reflection;
using WankulCrazyPlugin.inventory;

namespace WankulCrazyPlugin.patch
{
    [System.Serializable]
    public class CardPrice
    {

        public static float generateMarketPrice(WankulCardData wankulCardData)
        {
            // Augmenter la plage de variation aléatoire entre -2% et +2%
            float variation = UnityEngine.Random.Range(-0.02f, 0.02f);

            float priceFactor = 1f;
            if (wankulCardData.Season == Season.S01)
            {
                priceFactor = 1f;
            }
            else if (wankulCardData.Season == Season.S02)
            {
                priceFactor = 1.25f;
            }
            else if (wankulCardData.Season == Season.S03)
            {
                priceFactor = 1.5f;
            }
            else if (wankulCardData.Season == Season.S04)
            {
                priceFactor = 1.75f;
            }
            else if (wankulCardData.Season == Season.HS)
            {
                priceFactor = 2f;
            }

            // Calculer le prix maximum en fonction du segment de la carte
            float priceRangeMax;
            float priceRangeMin;

            if (wankulCardData is SpecialCardData specialCardData)
            {
                if (specialCardData.Special == Specials.TOR)
                {
                    priceRangeMin = 10000f;
                    priceRangeMax = 100000f;
                }
                else
                {
                    priceRangeMin = 0.01f;
                    priceRangeMax = 0.5f;
                }
            }
            else if (wankulCardData is TerrainCardData)
            {
                priceRangeMin = 0.5f;
                priceRangeMax = 1.9f;
            }
            else if (wankulCardData is EffigyCardData effigyCard)
            {
                var rarityData = RaritiesManager.GetRarity(effigyCard.RarityId);
                if (string.Equals(effigyCard.RarityId, "DUO", System.StringComparison.OrdinalIgnoreCase))
                {
                    priceRangeMin = rarityData != null ? rarityData.PriceRangeMin : 500f;
                    priceRangeMax = rarityData != null ? rarityData.PriceRangeMax : 1000f;
                }
                else if (rarityData != null && rarityData.PriceRangeMax > 0.5f && effigyCard.Rarity == Rarity.C)
                {
                    priceRangeMin = rarityData.PriceRangeMin;
                    priceRangeMax = rarityData.PriceRangeMax;
                }
                else
                {
                    switch (effigyCard.Rarity)
                    {
                        case Rarity.C:
                            priceRangeMin = 0.01f;
                            priceRangeMax = 0.5f;
                            break;
                    case Rarity.UC:
                        priceRangeMin = 0.5f;
                        priceRangeMax = 1f;
                        break;
                    case Rarity.R:
                        priceRangeMin = 3.5f;
                        priceRangeMax = 10f;
                        break;
                    case Rarity.UR1:
                        priceRangeMin = 10f;
                        priceRangeMax = 50f;
                        break;
                    case Rarity.UR2:
                        priceRangeMin = 50f;
                        priceRangeMax = 150f;
                        break;
                    case Rarity.LB:
                        priceRangeMin = 150;
                        priceRangeMax = 500;
                        break;
                    case Rarity.LA:
                        priceRangeMin = 500;
                        priceRangeMax = 1000;
                        break;
                    case Rarity.LO:
                        priceRangeMin = 1000;
                        priceRangeMax = 2500f;
                        break;
                    case Rarity.TOR: // Gagnant Ticket Or
                        priceRangeMin = 2500f;
                        priceRangeMax = 4000f;
                        break;
                    default:
                        // Raretés spéciales (PGW, Noël, Starter Packs, etc.) : toutes très rares, Drop inutilisable
                        priceRangeMin = 50f;
                        priceRangeMax = 1000f;
                        break;
                    }
                }
            }
            else
            {
                // Fallback global
                if (wankulCardData.Drop >= 0.45f)
                {
                    priceRangeMin = 0.01f;
                    priceRangeMax = 0.5f;
                }
                else if (wankulCardData.Drop >= 0.3f)
                {
                    priceRangeMin = 0.5f;
                    priceRangeMax = 1f;
                }
                else if (wankulCardData.Drop >= 0.1f)
                {
                    priceRangeMin = 3.5f;
                    priceRangeMax = 10f;
                }
                else if (wankulCardData.Drop >= 0.0224f)
                {
                    priceRangeMin = 10f;
                    priceRangeMax = 50f;
                }
                else if (wankulCardData.Drop >= 0.016f)
                {
                    priceRangeMin = 50f;
                    priceRangeMax = 150f;
                }
                else if (wankulCardData.Drop >= 0.008f)
                {
                    priceRangeMin = 150f;
                    priceRangeMax = 500f;
                }
                else if (wankulCardData.Drop >= 0.0028f)
                {
                    priceRangeMin = 500f;
                    priceRangeMax = 1000f;
                }
                else if (wankulCardData.Drop >= 0.0008f)
                {
                    priceRangeMin = 1000f;
                    priceRangeMax = 2500f;
                }
                else if (wankulCardData.Drop >= 0.0005f)
                {
                    priceRangeMin = 2500f;
                    priceRangeMax = 4000f;
                }
                else if (wankulCardData.Drop >= 0.0001f)
                {
                    priceRangeMin = 10000f;
                    priceRangeMax = 100000f;
                }
                else
                {
                    priceRangeMin = 0.01f;
                    priceRangeMax = 0.5f;
                }
            }

            // Calculer le prix avec la variation aléatoire (influencée par le prix max du segment)
            float minPrice = priceRangeMin * (1 + variation);
            float maxPrice = priceRangeMax * (1 + variation);
            return UnityEngine.Random.Range(minPrice, maxPrice) * priceFactor;
        }

        // FieldInfo mis en cache : AccessTools.Field a chaque appel coutait tres cher
        // (appele jusqu'a 30 x 910 cartes au chargement d'une sauvegarde).
        private static FieldInfo priceChangeMinField;
        private static FieldInfo priceChangeMaxField;

        public static void UpdateCardPricePercent(WankulCardData wankulCardData)
        {
            var priceChangeManager = PriceChangeManager.Instance;
            if (priceChangeMinField == null || priceChangeMaxField == null)
            {
                priceChangeMinField = AccessTools.Field(priceChangeManager.GetType(), "m_PriceChangeMin");
                priceChangeMaxField = AccessTools.Field(priceChangeManager.GetType(), "m_PriceChangeMax");
            }
            var m_PriceChangeMin = (float)priceChangeMinField.GetValue(priceChangeManager);
            var m_PriceChangeMax = (float)priceChangeMaxField.GetValue(priceChangeManager);

            float percentChange = Random.Range(m_PriceChangeMin, m_PriceChangeMax);
            float increaseFactor = UnityEngine.Random.Range(0, 2) == 0 ? -1f : 1f;

            wankulCardData.Percentage += (percentChange * increaseFactor);
            if (wankulCardData.Percentage < -80f)
            {
                wankulCardData.Percentage = -80f;
            }

            if (wankulCardData.Percentage > 200f)
            {
                wankulCardData.Percentage = 200f;
            }

            wankulCardData.PastPercent.Add(wankulCardData.Percentage);
            if (wankulCardData.PastPercent.Count > 30)
            {
                wankulCardData.PastPercent.RemoveAt(0);
            }
        }


        public static void UpdateAllCardsMarketPrice()
        {
            WankulCardsData wankulCardsData = WankulCardsData.Instance;

            foreach (WankulCardData wankulCardData in wankulCardsData.cards)
            {
                UpdateCardPricePercent(wankulCardData);
            }
        }

        public static void OnDayStarted()
        {
            UpdateAllCardsMarketPrice();
        }

        public static float CalculateGradedMarketPrice(float baseMarketPrice, int cardSaveIndex, int cardGrade)
        {
            return GradedCardService.CalculateGradedMarketPrice(baseMarketPrice, cardSaveIndex, cardGrade, CPlayerData.m_GenGradedCardPriceMultiplierList);
        }

        public static void Postfix_GetCardMarketPrice_CardData(CardData cardData, ref float __result)
        {
            if (cardData == null)
            {
                __result = 0f;
                return;
            }

            WankulCardsData wankulCardsData = WankulCardsData.Instance;
            WankulCardData wankulCardData = wankulCardsData.GetFromMonster(cardData, true);

            if (wankulCardData != null)
            {
                float basePrice = wankulCardData.MarketPrice;
                if (cardData.cardGrade > 0)
                {
                    int cardSaveIndex = CPlayerData.GetCardSaveIndex(cardData);
                    __result = CalculateGradedMarketPrice(basePrice, cardSaveIndex, cardData.cardGrade);
                }
                else
                {
                    __result = basePrice;
                }
            }
            else
            {
                __result = 0f;
            }
        }

        public static void Postfix_GetCardMarketPrice_Params(int index, ECardExpansionType expansionType, bool isDestiny, int cardGrade, ref float __result)
        {
            try
            {
                CardData tempCard = new CardData();
                tempCard.monsterType = CPlayerData.GetMonsterTypeFromCardSaveIndex(index, expansionType);
                int cardAmountPerMonster = CPlayerData.GetCardAmountPerMonsterType(expansionType);
                int cardAmountNoFoil = CPlayerData.GetCardAmountPerMonsterType(expansionType, includeFoilCount: false);
                tempCard.isFoil = (cardAmountPerMonster > 0 && (index % cardAmountPerMonster >= cardAmountNoFoil));
                tempCard.borderType = (ECardBorderType)(cardAmountNoFoil > 0 ? (index % cardAmountNoFoil) : 0);
                tempCard.expansionType = expansionType;
                tempCard.isDestiny = isDestiny;
                tempCard.cardGrade = cardGrade;

                WankulCardData wankulCard = WankulCardsData.Instance.GetFromMonster(tempCard, true);
                if (wankulCard != null)
                {
                    float basePrice = wankulCard.MarketPrice;
                    if (cardGrade > 0)
                    {
                        __result = CalculateGradedMarketPrice(basePrice, index, cardGrade);
                    }
                    else
                    {
                        __result = basePrice;
                    }
                    return;
                }
            }
            catch
            {
                // Fallback
            }

            float variation = UnityEngine.Random.Range(-0.3f, 0.3f);
            float marketPrice = 20f;

            switch (expansionType)
            {
                case ECardExpansionType.Tetramon:
                    marketPrice = Mathf.Clamp((1 + variation) * 100f, 5f, 100f);
                    break;
                case ECardExpansionType.Destiny:
                    marketPrice = Mathf.Clamp((1 + variation) * 3000f, 100f, 3000f);
                    break;
                case ECardExpansionType.Ghost:
                    marketPrice = Mathf.Clamp((1 + variation) * 5000f, 1000f, 5000f);
                    break;
                default:
                    marketPrice = Mathf.Clamp((1 + variation) * 100f, 5f, 100f);
                    break;
            }

            if (cardGrade > 0)
            {
                marketPrice = CalculateGradedMarketPrice(marketPrice, index, cardGrade);
            }

            __result = marketPrice;
        }

        public static IEnumerator DelayRemoveCustomerFromQueue(float waitTime, Customer instance)
        {
            yield return new WaitForSeconds(waitTime);
            InteractableCashierCounter m_CurrentQueueCashierCounter = (InteractableCashierCounter)Plugin.GetPProperty(instance, "m_CurrentQueueCashierCounter");
            m_CurrentQueueCashierCounter.RemoveCustomerFromQueue(instance);
            m_CurrentQueueCashierCounter.RemoveCurrentCustomerFromQueue();
        }

        public static bool OnPayingDone(Customer __instance)
        {
            Plugin.SetPProperty(__instance, "m_IsAtPayingPosition", false);
            Plugin.SetPProperty(__instance, "m_HasCheckedOut", true);
            Plugin.SetPProperty(__instance, "m_Path", null);
            if (__instance.m_ItemInBagList.Count + __instance.m_CardInBagList.Count > 0)
            {
                __instance.m_ShoppingBagTransform.gameObject.SetActive(value: true);
            }

            __instance.m_Anim.SetBool("HoldingBag", value: true);
            ((InteractableCashierCounter)Plugin.GetPProperty(__instance, "m_CurrentQueueCashierCounter")).SetPlsaticBagVisibility(isShow: false);
            ((InteractableCashierCounter)Plugin.GetPProperty(__instance, "m_CurrentQueueCashierCounter")).UpdateCashierCounterState(ECashierCounterState.Idle);
            ((InteractableCashierCounter)Plugin.GetPProperty(__instance, "m_CurrentQueueCashierCounter")).UpdateCurrentCustomer(null);
            float num = 0f;
            int num2 = 0;
            for (int i = 0; i < __instance.m_ItemInBagList.Count; i++)
            {
                __instance.m_ItemInBagList[i].transform.parent = __instance.m_ShoppingBagTransform;
                __instance.m_ItemInBagList[i].transform.position = __instance.m_ShoppingBagTransform.position;
                __instance.m_ItemInBagList[i].transform.rotation = __instance.m_ShoppingBagTransform.rotation;
                __instance.m_ItemInBagList[i].gameObject.SetActive(value: false);
                __instance.m_ItemInBagList[i].m_Collider.enabled = false;
                __instance.m_ItemInBagList[i].m_Rigidbody.isKinematic = true;
                __instance.m_ItemInBagList[i].m_InteractableScanItem.enabled = false;
                num += __instance.m_ItemInBagList[i].GetItemVolume();
                num2 += InventoryBase.GetUnlockItemLevelRequired(__instance.m_ItemInBagList[i].GetItemType());
            }

            int totalCardExp = 0;
            for (int j = 0; j < __instance.m_CardInBagList.Count; j++)
            {
                __instance.m_CardInBagList[j].transform.parent = __instance.m_ShoppingBagTransform;
                __instance.m_CardInBagList[j].transform.position = __instance.m_ShoppingBagTransform.position;
                __instance.m_CardInBagList[j].transform.rotation = __instance.m_ShoppingBagTransform.rotation;
                __instance.m_CardInBagList[j].m_Card3dUI.gameObject.SetActive(value: false);
                __instance.m_CardInBagList[j].gameObject.SetActive(value: false);
                __instance.m_CardInBagList[j].m_Collider.enabled = false;
                __instance.m_CardInBagList[j].m_Rigidbody.isKinematic = true;

                CardUI cardUi = __instance.m_CardInBagList[j].m_Card3dUI.m_CardUI;
                CardData cardData = (CardData)Plugin.GetPProperty(cardUi, "m_CardData");
                WankulCardData wankulCardData = WankulCardsData.Instance.GetFromMonster(cardData, true);
                if (wankulCardData != null)
                {
                    int exp = WankulCardsData.GetExperienceFromWankulCard(wankulCardData);
                    totalCardExp += exp;
                }
                else
                {
                    Plugin.Logger.LogError("OnPayingDone Carte non trouvée : " + cardData.monsterType + " " + cardData.borderType + " " + cardData.expansionType);
                }

            }

            __instance.StartCoroutine(DelayRemoveCustomerFromQueue(Random.Range(0.25f, 1f), __instance));
            MethodInfo DetermineShopAction = Plugin.GetCachedMethod(__instance.GetType(), "DetermineShopAction");
            DetermineShopAction.Invoke(__instance, new object[] { });
            CEventManager.QueueEvent(new CEventPlayer_AddShopExp(__instance.m_ItemInBagList.Count * 4 + Mathf.RoundToInt(num) + num2 / 2 + totalCardExp));
            return false;
        }
    }
}
