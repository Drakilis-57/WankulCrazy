using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.inventory;

namespace WankulCrazyPlugin.patch
{
    public class SortUI
    {
        public static bool inited = false;
        static List<int> SortedCardIndies = [];
        static SortSeasonType currentSeason = SortSeasonType.ALL;
        static SortType currentSortType = SortType.Price;
        static int currentGameSortMethod = 2;
        static int currentGameExpansionIndex = 0;
        static bool CanFlip = true;

        public static void OpenSortAlbumScreenPrefix(ref int sortingMethodIndex, ref int currentExpansionIndex, CollectionBinderUI __instance)
        {
            sortingMethodIndex = currentGameSortMethod;
            currentExpansionIndex = currentGameExpansionIndex;
        }

        public static void OpenSortAlbumScreen(int sortingMethodIndex, int currentExpansionIndex, CollectionBinderUI __instance)
        {


            if (!inited)
            {
                Transform Expansion_AnimGrp_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp");
                Transform Expansion_BG_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/BG");
                Transform Expansion_Title_BG_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/TitleBG");
                Transform Expansion_Title_Text_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/TitleText");
                Transform Expansion_Mask_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/Mask");

                ((RectTransform)Expansion_BG_Transform).anchoredPosition = new Vector2(
                    0,
                    - 1.5f
                );
                ((RectTransform)Expansion_BG_Transform).anchoredPosition3D = new Vector3(
                    0,
                    -1.5f,
                    0
                );
                ((RectTransform)Expansion_AnimGrp_Transform).sizeDelta = new Vector2(
                    934,
                    1800
                );

                ((RectTransform)Expansion_Mask_Transform).offsetMin = new Vector2(
                    85,
                    370
                );

                float verticalSpacing = __instance.m_ExpansionBtnList[2].GetComponent<RectTransform>().anchoredPosition.y - __instance.m_ExpansionBtnList[3].GetComponent<RectTransform>().anchoredPosition.y;

                __instance.m_ExpansionBtnList[0].gameObject.name = "ALL_Button";
                __instance.m_ExpansionBtnList[0].GetComponentInChildren<TextMeshProUGUI>().text = "Tout";
                __instance.m_ExpansionBtnList[0].GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(
                    0,
                    380
                );

                __instance.m_ExpansionBtnList[1].gameObject.name = "S01_Button";
                __instance.m_ExpansionBtnList[1].GetComponentInChildren<TextMeshProUGUI>().text = SeasonsContainer.Seasons[Season.S01];
                __instance.m_ExpansionBtnList[1].GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    __instance.m_ExpansionBtnList[0].GetComponent<RectTransform>().anchoredPosition.x,
                    __instance.m_ExpansionBtnList[0].GetComponent<RectTransform>().anchoredPosition.y - verticalSpacing
                );

                __instance.m_ExpansionBtnList[2].gameObject.name = "S02_Button";
                __instance.m_ExpansionBtnList[2].GetComponentInChildren<TextMeshProUGUI>().text = SeasonsContainer.Seasons[Season.S02];
                __instance.m_ExpansionBtnList[2].GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    __instance.m_ExpansionBtnList[1].GetComponent<RectTransform>().anchoredPosition.x,
                    __instance.m_ExpansionBtnList[1].GetComponent<RectTransform>().anchoredPosition.y - verticalSpacing
                );

                __instance.m_ExpansionBtnList[3].gameObject.SetActive(true);
                __instance.m_ExpansionBtnList[3].gameObject.name = "S03_Button";
                __instance.m_ExpansionBtnList[3].GetComponentInChildren<TextMeshProUGUI>().text = SeasonsContainer.Seasons[Season.S03];
                __instance.m_ExpansionBtnList[3].GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    __instance.m_ExpansionBtnList[2].GetComponent<RectTransform>().anchoredPosition.x,
                    __instance.m_ExpansionBtnList[2].GetComponent<RectTransform>().anchoredPosition.y - verticalSpacing
                );

                GameObject s04GameObject = GameObject.Instantiate(__instance.m_ExpansionBtnList[0].gameObject);
                s04GameObject.name = "S04_Button";
                s04GameObject.AddComponent<RectTransform>();
                s04GameObject.transform.SetParent(__instance.m_ExpansionBtnList[0].parent);
                s04GameObject.transform.localScale = __instance.m_ExpansionBtnList[0].localScale;
                s04GameObject.transform.localPosition = __instance.m_ExpansionBtnList[0].localPosition;
                s04GameObject.transform.localRotation = __instance.m_ExpansionBtnList[0].localRotation;
                s04GameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    __instance.m_ExpansionBtnList[3].GetComponent<RectTransform>().anchoredPosition.x,
                    __instance.m_ExpansionBtnList[3].GetComponent<RectTransform>().anchoredPosition.y - verticalSpacing  // Espacement vertical
                );
                s04GameObject.GetComponentInChildren<TextMeshProUGUI>().text = SeasonsContainer.Seasons[Season.S04];
                __instance.m_ExpansionBtnList.Add(s04GameObject.transform);


                GameObject hsGameObject = GameObject.Instantiate(__instance.m_ExpansionBtnList[0].gameObject);
                hsGameObject.name = "HS_Button";
                hsGameObject.AddComponent<RectTransform>();
                hsGameObject.transform.SetParent(__instance.m_ExpansionBtnList[0].parent);
                hsGameObject.transform.localScale = __instance.m_ExpansionBtnList[0].localScale;
                hsGameObject.transform.localPosition = __instance.m_ExpansionBtnList[0].localPosition;
                hsGameObject.transform.localRotation = __instance.m_ExpansionBtnList[0].localRotation;
                hsGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    __instance.m_ExpansionBtnList[4].GetComponent<RectTransform>().anchoredPosition.x,
                    __instance.m_ExpansionBtnList[4].GetComponent<RectTransform>().anchoredPosition.y - verticalSpacing  // Espacement vertical
                );
                hsGameObject.GetComponentInChildren<TextMeshProUGUI>().text = SeasonsContainer.Seasons[Season.HS];
                __instance.m_ExpansionBtnList.Add(hsGameObject.transform);


                __instance.m_SortAlbumBtnList[2].GetComponentInChildren<TextMeshProUGUI>().text = "Prix";
                __instance.m_SortAlbumBtnList[1].GetComponentInChildren<TextMeshProUGUI>().text = "Rareté";
                __instance.m_SortAlbumBtnList[0].GetComponentInChildren<TextMeshProUGUI>().text = "Numéro de Carte";
                __instance.m_SortAlbumBtnList[3].GetComponentInChildren<TextMeshProUGUI>().text = "Quantité";


                // Utiliser un facteur pour espacer (par exemple 1.5x la hauteur du bouton)
                float verticalSpacingSort = __instance.m_SortAlbumBtnList[3].GetComponent<RectTransform>().anchoredPosition.y - __instance.m_SortAlbumBtnList[3].GetComponent<RectTransform>().anchoredPosition.y;
                // Positionner le 5ème bouton en dessous du 4ème, avec un espacement cohérent
                __instance.m_SortAlbumBtnList[4].GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    __instance.m_SortAlbumBtnList[3].GetComponent<RectTransform>().anchoredPosition.x,
                    __instance.m_SortAlbumBtnList[3].GetComponent<RectTransform>().anchoredPosition.y - verticalSpacingSort  // Espacement vertical
                );
                __instance.m_SortAlbumBtnList[4].GetComponentInChildren<TextMeshProUGUI>().text = "Doublon";
                __instance.m_SortAlbumBtnList[4].gameObject.SetActive(true);

                __instance.m_SortAlbumBtnList[5].gameObject.SetActive(false);
                __instance.m_SortAlbumBtnList[6].gameObject.SetActive(false);


                void OnClickSeasonButton(SortSeasonType season, int expansionIdx)
                {
                    currentSeason = season;
                    currentGameExpansionIndex = expansionIdx;
                    CSingleton<InteractionPlayerController>.Instance?.HideCursor();
                    SoundManager.GenericConfirm(1f, 1f);
                    __instance.m_SortAlbumScreen.SetActive(false);
                    if (__instance.m_SortAlbumScreenUIExtension != null)
                    {
                        ControllerScreenUIExtManager.OnCloseScreen(__instance.m_SortAlbumScreenUIExtension);
                    }
                    if (__instance.m_CollectionAlbum != null)
                    {
                        OnSortingMethodUpdated(true, __instance.m_CollectionAlbum);
                    }
                }

                __instance.m_ExpansionBtnList[0].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    OnClickSeasonButton(SortSeasonType.ALL, 0);
                });
                __instance.m_ExpansionBtnList[1].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    OnClickSeasonButton(SortSeasonType.S01, 1);
                });
                __instance.m_ExpansionBtnList[2].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    OnClickSeasonButton(SortSeasonType.S02, 2);
                });
                __instance.m_ExpansionBtnList[3].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    OnClickSeasonButton(SortSeasonType.S03, 3);
                });
                __instance.m_ExpansionBtnList[4].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    OnClickSeasonButton(SortSeasonType.S04, 4);
                });
                __instance.m_ExpansionBtnList[5].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    OnClickSeasonButton(SortSeasonType.HS, 5);
                });

                __instance.m_SortAlbumBtnList[2].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    currentSortType = SortType.Price;
                    __instance.OnPressSwitchSortingMethod(2);
                    currentGameSortMethod = 2;
                });
                __instance.m_SortAlbumBtnList[1].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    currentSortType = SortType.Rarity;
                    __instance.OnPressSwitchSortingMethod(1);
                    currentGameSortMethod = 1;
                });
                __instance.m_SortAlbumBtnList[0].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    currentSortType = SortType.Number;
                    __instance.OnPressSwitchSortingMethod(0);
                    currentGameSortMethod = 0;
                });
                __instance.m_SortAlbumBtnList[3].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    currentSortType = SortType.Amount;
                    __instance.OnPressSwitchSortingMethod(3);
                    currentGameSortMethod = 3;
                });

                __instance.m_SortAlbumBtnList[4].GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    currentSortType = SortType.Double;
                    __instance.OnPressSwitchSortingMethod(4);
                    currentGameSortMethod = 4;
                });

                // Marquer l'initialisation comme terminée
                inited = true;
            }
        }

        public static Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> GetWankulCardsBySeason(SortSeasonType season)
        {
            List<WankulCardData> wankulCards = WankulInventory.Instance.wankulCards.Values.Select(x => x.wankulcard).ToList();

            if (season == SortSeasonType.ALL)
            {
                return WankulInventory.Instance.wankulCards;
            }
            else
            {
                var filteredWankulCards = WankulInventory.Instance.wankulCards
                    .Where(kvp => kvp.Value.wankulcard.Season == (Season)season)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return filteredWankulCards;
            }
        }

        public static void SortByPriceAmount()
        {
            SortedCardIndies.Clear();

            Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> wankulCards = GetWankulCardsBySeason(currentSeason);

            SortedCardIndies = wankulCards
                .Select((card) => new { card })
                .OrderByDescending(x => x.card.Value.wankulcard.MarketPrice)
                .Select(x => x.card.Value.wankulcard.Index)
                .ToList();
        }

        public static void SortByRarity()
        {
            SortedCardIndies.Clear();

            Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> wankulCards = GetWankulCardsBySeason(currentSeason);

            SortedCardIndies = wankulCards
                .Select((card) => new { card })
                .OrderByDescending(x =>
                {
                    if (x.card.Value.wankulcard is EffigyCardData)
                    {
                        return ((EffigyCardData)x.card.Value.wankulcard).Rarity;
                    }
                    else
                    {
                        return Rarity.C;
                    }
                })
                .Select(x => x.card.Value.wankulcard.Index)
                .ToList();
        }

        public static void SortBySeasonAndNumber()
        {
            SortedCardIndies.Clear();

            Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> wankulCards = GetWankulCardsBySeason(currentSeason);

            SortedCardIndies = wankulCards
                .Select((card) => new { card })
                .OrderBy(x => x.card.Value.wankulcard.Season)
                .ThenBy(x => x.card.Value.wankulcard.NumberInt)
                .Select(x => x.card.Value.wankulcard.Index)
                .ToList();
        }

        public static void SortByAmount()
        {
            SortedCardIndies.Clear();

            Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> wankulCards = GetWankulCardsBySeason(currentSeason);

            SortedCardIndies = wankulCards
                .Select((card) => new { card })
                .OrderByDescending(x => x.card.Value.amount)
                .Select(x => x.card.Value.wankulcard.Index)
                .ToList();
        }

        public static void SortByDouble()
        {
            SortedCardIndies.Clear();

            Dictionary<int, (WankulCardData wankulcard, CardData card, int amount)> wankulCards = GetWankulCardsBySeason(currentSeason);

            SortedCardIndies = wankulCards
                .Select((card) => new { card })
                .Where(x => x.card.Value.amount > 1)
                .OrderByDescending(x => x.card.Value.wankulcard.MarketPrice)
                .Select(x => x.card.Value.wankulcard.Index)
                .ToList();
        }

        public static void UpdateBinderAllCardUI(int binderIndex, int pageIndex, int ___m_MaxIndex, CollectionBinderFlipAnimCtrl __instance)
        {
            switch (currentSortType)
            {
                case SortType.Price:
                    SortByPriceAmount();
                    break;
                case SortType.Rarity:
                    SortByRarity();
                    break;
                case SortType.Number:
                    SortBySeasonAndNumber();
                    break;
                case SortType.Amount:
                    SortByAmount();
                    break;
                case SortType.Double:
                    SortByDouble();
                    break;
            }

            if (pageIndex <= 0 || pageIndex > ___m_MaxIndex)
            {
                return;
            }
            for (int i = 0; i < __instance.m_BinderPageGrpList[binderIndex].m_CardList.Count; i++)
            {
                int num = (pageIndex - 1) * 12 + i;
                if (num >= SortedCardIndies.Count)
                {
                    __instance.m_BinderPageGrpList[binderIndex].SetSingleCard(i, null, 0, ECollectionSortingType.Default);
                    continue;
                }
                int index = SortedCardIndies[num];

                var wankulCardTuple = WankulInventory.Instance.wankulCards[index];
                WankulCardData wankulCardData = wankulCardTuple.wankulcard;

                CardData cardData = wankulCardTuple.card;

                // fixing broken saves
                if (cardData == null)
                {
                    Plugin.Logger.LogWarning("gameCardData is null");

                    cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                    WankulCardData debugwankulCardData = WankulCardsData.GetAJETER();

                    string debugkey = $"{cardData.monsterType}_{cardData.borderType}_{cardData.expansionType}";
                    Plugin.Logger.LogWarning($"gameCardData is null, adding to inventory {debugwankulCardData.Title} {debugwankulCardData.Index}, {debugkey}");

                    WankulCardsData.Instance.SetFromMonster(cardData, debugwankulCardData);
                    CPlayerData.AddCard(cardData, 1);
                }

                __instance.m_BinderPageGrpList[binderIndex].SetSingleCard(i, cardData, wankulCardTuple.amount, ECollectionSortingType.Default);
            }
        }

        private static IEnumerator DelaySetBinderPageVisibility(bool isVisible, CollectionBinderFlipAnimCtrl __instance)
        {
            yield return new WaitForSeconds(0.5f);
            __instance.m_BinderPageGrpList[1].SetVisibility(isVisible);
            __instance.m_BinderPageGrpList[2].SetVisibility(isVisible);
        }

        private static IEnumerator DelayResetCanFlipBook(float delayTime, CollectionBinderFlipAnimCtrl __instance)
        {
            MethodInfo HideCurrentInteractableCard3dList = Plugin.GetCachedMethod(__instance.GetType(), "HideCurrentInteractableCard3dList");
            MethodInfo UpdateCurrentInteractableCard3dList = Plugin.GetCachedMethod(__instance.GetType(), "UpdateCurrentInteractableCard3dList");
            bool m_CanFlip = (bool)Plugin.GetPProperty(__instance, "m_CanFlip");

            HideCurrentInteractableCard3dList.Invoke(__instance, new object[] {});
            m_CanFlip = false;
            CanFlip = false;
            Plugin.SetPProperty(__instance, "m_CanFlip", m_CanFlip);
            yield return new WaitForSeconds(delayTime);
            m_CanFlip = true;
            CanFlip = true;
            Plugin.SetPProperty(__instance, "m_CanFlip", m_CanFlip);
            yield return new WaitForSeconds(0.1f);
            UpdateCurrentInteractableCard3dList.Invoke(__instance, new object[] { });
        }

        private static IEnumerator DelaySetBinderPageCardIndex(int binderIndex, int pageIndex, CollectionBinderFlipAnimCtrl __instance)
        {
            int m_MaxIndex = (int)Plugin.GetPProperty(__instance, "m_MaxIndex");
            yield return new WaitForSeconds(0.5f);
            UpdateBinderAllCardUI(binderIndex, pageIndex, m_MaxIndex, __instance);
        }

        public static bool OnSortingMethodUpdated(bool backToFirstPage, CollectionBinderFlipAnimCtrl __instance)
        {
            int m_Index = (int)Plugin.GetPProperty(__instance, "m_Index");
            bool m_CanUpdateSort = (bool)Plugin.GetPProperty(__instance, "m_CanUpdateSort");

            if (backToFirstPage)
            {
                m_Index = 1;
                Plugin.SetPProperty(__instance, "m_Index", m_Index);
            }
            if (m_CanUpdateSort)
            {
                m_CanUpdateSort = false;
                Plugin.SetPProperty(__instance, "m_CanUpdateSort", m_CanUpdateSort);
            }

            // Recalculer le nombre de cartes et max index pour la saison active
            List<WankulCardData> wankulCardDatas = WankulCardsData.Instance.cards;
            float totalPrice = WankulInventory.GetTotalPrice();
            if (currentSeason != SortSeasonType.ALL)
            {
                wankulCardDatas = WankulCardsData.GetCardsFromSeason((Season)currentSeason);
                totalPrice = WankulInventory.GetTotalPriceBySeason((Season)currentSeason);
            }

            int totalSeasonCards = wankulCardDatas.Count;
            int m_MaxIndex = Mathf.Max(1, Mathf.CeilToInt((float)totalSeasonCards / 12f));
            Plugin.SetPProperty(__instance, "m_MaxIndex", m_MaxIndex);

            if (m_Index > m_MaxIndex)
            {
                m_Index = m_MaxIndex;
                Plugin.SetPProperty(__instance, "m_Index", m_Index);
            }

            if (__instance.m_CollectionBinderUI != null)
            {
                __instance.m_CollectionBinderUI.SetMaxPage(m_MaxIndex);
                __instance.m_CollectionBinderUI.SetCurrentPage(m_Index);
                __instance.m_CollectionBinderUI.SetMaxCardCollectCount(totalSeasonCards);
                __instance.m_CollectionBinderUI.SetTotalValue(totalPrice);
            }

            UpdateBinderAllCardUI(0, m_Index, m_MaxIndex, __instance);
            UpdateBinderAllCardUI(1, m_Index + 1, m_MaxIndex, __instance);
            UpdateBinderAllCardUI(2, m_Index - 1, m_MaxIndex, __instance);

            if (__instance.m_CollectionBinderUI != null)
            {
                __instance.m_CollectionBinderUI.SetCardCollected(SortedCardIndies.Count, (ECardExpansionType)0);
            }

            return false;
        }

        public static bool Update(
            CollectionBinderFlipAnimCtrl __instance
        )
        {
            bool ___m_IsBookOpen = (bool)Plugin.GetPProperty(__instance, "m_IsBookOpen");
            bool ___m_IsHoldingCardCloseUp = (bool)Plugin.GetPProperty(__instance, "m_IsHoldingCardCloseUp");
            bool ___m_IsExitingCardCloseUp = (bool)Plugin.GetPProperty(__instance, "m_IsExitingCardCloseUp");
            bool ___m_OpenBinder = (bool)Plugin.GetPProperty(__instance, "m_OpenBinder");
            Coroutine ___m_CanFlipCoroutine = (Coroutine)Plugin.GetPProperty(__instance, "m_CanFlipCoroutine");
            ECardExpansionType ___m_ExpansionType = (ECardExpansionType)Plugin.GetPProperty(__instance, "m_ExpansionType");
            int ___m_MaxIndex = (int)Plugin.GetPProperty(__instance, "m_MaxIndex");
            ECollectionSortingType ___m_SortingType = (ECollectionSortingType)Plugin.GetPProperty(__instance, "m_SortingType");
            bool ___m_CloseBinder = (bool)Plugin.GetPProperty(__instance, "m_CloseBinder");
            bool ___m_CanFlip = (bool)Plugin.GetPProperty(__instance, "m_CanFlip");
            bool ___m_GoNext = (bool)Plugin.GetPProperty(__instance, "m_GoNext");
            bool ___m_GoPrevious = (bool)Plugin.GetPProperty(__instance, "m_GoPrevious");
            bool ___m_GoNext10 = (bool)Plugin.GetPProperty(__instance, "m_GoNext10");
            bool ___m_GoPrevious10 = (bool)Plugin.GetPProperty(__instance, "m_GoPrevious10");
            int ___m_Index = (int)Plugin.GetPProperty(__instance, "m_Index");


            if (___m_IsBookOpen && ___m_IsHoldingCardCloseUp && !___m_IsExitingCardCloseUp)
            {
                float x = Mathf.Clamp(CSingleton<InteractionPlayerController>.Instance.m_CameraController.GetViewCardDeltaAngleX() * 1.5f, -15f, 15f);
                float num = Mathf.Clamp(CSingleton<InteractionPlayerController>.Instance.m_CameraController.GetViewCardDeltaAngleY(), -35f, 35f);
                Quaternion localRotation = __instance.m_CurrentSpawnedInteractableCard3d.m_Card3dUI.m_ScaleGrp.transform.localRotation;
                Vector3 eulerAngles = localRotation.eulerAngles;
                eulerAngles.x = x;
                eulerAngles.y = 0f - num;
                eulerAngles.z = 0f;
                localRotation.eulerAngles = eulerAngles;
                __instance.m_CurrentSpawnedInteractableCard3d.SetTargetRotation(localRotation);
            }

            if (___m_OpenBinder && !___m_IsBookOpen)
            {
                switch (currentSortType)
                {
                    case SortType.Price:
                        SortByPriceAmount();
                        break;
                    case SortType.Rarity:
                        SortByRarity();
                        break;
                    case SortType.Number:
                        SortBySeasonAndNumber();
                        break;
                    case SortType.Amount:
                        SortByAmount();
                        break;
                    case SortType.Double:
                        SortByDouble();
                        break;
                }

                __instance.m_ShowHideAnim.gameObject.SetActive(value: true);
                ___m_OpenBinder = false;
                Plugin.SetPProperty(__instance, "m_OpenBinder", ___m_OpenBinder);
                ___m_IsBookOpen = true;
                Plugin.SetPProperty(__instance, "m_IsBookOpen", ___m_IsBookOpen);
                __instance.m_BookAnim.SetTrigger("OpenBinder");
                __instance.m_BinderThicknessAnim.SetTrigger("OpenBinder");
                __instance.m_BinderPageGrpList[0].m_Anim.SetTrigger("OpenBinder");
                __instance.m_BinderPageGrpList[1].m_Anim.SetTrigger("SetHideNextIdle");
                __instance.m_BinderPageGrpList[2].m_Anim.SetTrigger("SetHidePreviousIdle");
                __instance.StartCoroutine(DelaySetBinderPageVisibility(isVisible: true, __instance));
                if (___m_CanFlipCoroutine != null)
                {
                    __instance.StopCoroutine(___m_CanFlipCoroutine);
                }

                ___m_CanFlipCoroutine = __instance.StartCoroutine(DelayResetCanFlipBook(CSingleton<InteractionPlayerController>.Instance.m_HideCardAlbumTime + 0.3f, __instance));
                Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);

                List<WankulCardData> wankulCardDatas = WankulCardsData.Instance.cards;
                float totalPrice = WankulInventory.GetTotalPrice();
                if (currentSeason != SortSeasonType.ALL)
                {
                    wankulCardDatas = WankulCardsData.GetCardsFromSeason((Season)currentSeason);
                    totalPrice = WankulInventory.GetTotalPriceBySeason((Season)currentSeason);
                }

                int num2 = wankulCardDatas.Count;

                ___m_MaxIndex = Mathf.CeilToInt((float)num2 / 12f);
                Plugin.SetPProperty(__instance, "m_MaxIndex", ___m_MaxIndex);
                __instance.m_CollectionBinderUI.SetMaxPage(___m_MaxIndex);
                __instance.m_CollectionBinderUI.SetCurrentPage(___m_Index);
                __instance.m_CollectionBinderUI.SetMaxCardCollectCount(num2);
                __instance.m_CollectionBinderUI.SetCardCollected(SortedCardIndies.Count, ___m_ExpansionType);
                __instance.m_CollectionBinderUI.SetTotalValue(totalPrice);

                __instance.m_CollectionBinderUI.OpenScreen();
                if ((int)___m_ExpansionType >= 0 && (int)___m_ExpansionType < CPlayerData.m_CollectionSortingMethodIndexList.Count)
                {
                    ___m_SortingType = (ECollectionSortingType)CPlayerData.m_CollectionSortingMethodIndexList[(int)___m_ExpansionType];
                    if (___m_SortingType < ECollectionSortingType.Default || ___m_SortingType >= ECollectionSortingType.MAX)
                    {
                        ___m_SortingType = ECollectionSortingType.Price;
                        CPlayerData.m_CollectionSortingMethodIndexList[(int)___m_ExpansionType] = (int)___m_SortingType;
                    }
                    Plugin.SetPProperty(__instance, "m_SortingType", ___m_SortingType);
                }

                OnSortingMethodUpdated(false, __instance);
                UnityAnalytic.OpenAlbum();
                if (!CPlayerData.m_HasGetGhostCard && (CPlayerData.GetCardCollectedAmount(ECardExpansionType.Ghost, isDimensionCard: false) > 0 || CPlayerData.GetCardCollectedAmount(ECardExpansionType.Ghost, isDimensionCard: true) > 0))
                {
                    __instance.m_CollectionBinderUI.OpenGhostCardTutorialScreen();
                }
            }

            if (___m_IsBookOpen)
            {
                if (___m_CloseBinder)
                {
                    ___m_IsBookOpen = false;
                    Plugin.SetPProperty(__instance, "m_IsBookOpen", ___m_IsBookOpen);
                    __instance.m_BookAnim.Play("CollectionBookClose");
                    __instance.m_BinderThicknessAnim.Play("CollectionBookClose");
                    __instance.m_BinderPageGrpList[0].m_Anim.Play("BinderClose");
                    __instance.m_BinderPageGrpList[1].m_Anim.SetTrigger("SetHideNextIdle");
                    __instance.m_BinderPageGrpList[2].m_Anim.SetTrigger("SetHidePreviousIdle");
                    __instance.m_BinderPageGrpList[1].SetVisibility(isVisible: false);
                    __instance.m_BinderPageGrpList[2].SetVisibility(isVisible: false);
                    ___m_CloseBinder = false;
                    Plugin.SetPProperty(__instance, "m_CloseBinder", ___m_CloseBinder);
                    if (___m_CanFlipCoroutine != null)
                    {
                        __instance.StopCoroutine(___m_CanFlipCoroutine);
                    }

                    ___m_CanFlipCoroutine = __instance.StartCoroutine(DelayResetCanFlipBook(0.5f, __instance));
                    Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);
                    SoundManager.PlayAudio("SFX_AlbumFlip", 0.6f);
                }

                ___m_CanFlip = (bool)Plugin.GetPProperty(__instance, "m_CanFlip");
                if (!___m_CanFlip || !CanFlip)
                {
                    ___m_GoNext = false;
                    Plugin.SetPProperty(__instance, "m_GoNext", ___m_GoNext);
                    ___m_GoPrevious = false;
                    Plugin.SetPProperty(__instance, "m_GoPrevious", ___m_GoPrevious);
                    ___m_GoNext10 = false;
                    Plugin.SetPProperty(__instance, "m_GoNext10", ___m_GoNext10);
                    ___m_GoPrevious10 = false;
                    Plugin.SetPProperty(__instance, "m_GoPrevious10", ___m_GoPrevious10);

                    Plugin.SetPProperty(__instance, "m_IsBookOpen", ___m_IsBookOpen);
                    Plugin.SetPProperty(__instance, "m_IsHoldingCardCloseUp", ___m_IsHoldingCardCloseUp);
                    Plugin.SetPProperty(__instance, "m_IsExitingCardCloseUp", ___m_IsExitingCardCloseUp);
                    Plugin.SetPProperty(__instance, "m_OpenBinder", ___m_OpenBinder);
                    Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);
                    Plugin.SetPProperty(__instance, "m_ExpansionType", ___m_ExpansionType);
                    Plugin.SetPProperty(__instance, "m_MaxIndex", ___m_MaxIndex);
                    Plugin.SetPProperty(__instance, "m_SortingType", ___m_SortingType);
                    Plugin.SetPProperty(__instance, "m_CloseBinder", ___m_CloseBinder);
                    Plugin.SetPProperty(__instance, "m_CanFlip", ___m_CanFlip);
                    Plugin.SetPProperty(__instance, "m_GoNext", ___m_GoNext);
                    Plugin.SetPProperty(__instance, "m_GoPrevious", ___m_GoPrevious);
                    Plugin.SetPProperty(__instance, "m_GoNext10", ___m_GoNext10);
                    Plugin.SetPProperty(__instance, "m_GoPrevious10", ___m_GoPrevious10);
                    Plugin.SetPProperty(__instance, "m_Index", ___m_Index);
                    return false;
                }

                if (___m_GoNext && ___m_Index < ___m_MaxIndex)
                {
                    __instance.m_BinderPageGrpList[0].m_Anim.SetTrigger("GoNextPage");
                    __instance.m_BinderPageGrpList[1].m_Anim.SetTrigger("GoNextPage");
                    __instance.m_BinderPageGrpList[2].m_Anim.SetTrigger("SetHideNextIdle");
                    BinderPageGrp item = __instance.m_BinderPageGrpList[0];
                    __instance.m_BinderPageGrpList.RemoveAt(0);
                    __instance.m_BinderPageGrpList.Add(item);
                    ___m_GoNext = false;
                    Plugin.SetPProperty(__instance, "m_GoNext", ___m_GoNext);
                    ___m_Index++;
                    if (___m_CanFlipCoroutine != null)
                    {
                        __instance.StopCoroutine(___m_CanFlipCoroutine);
                    }

                    ___m_CanFlipCoroutine = __instance.StartCoroutine(DelayResetCanFlipBook(0.55f, __instance));
                    Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);
                    __instance.m_CollectionBinderUI.SetCurrentPage(___m_Index);
                    SoundManager.PlayAudio("SFX_AlbumFlip", 0.6f);
                    if (___m_Index < ___m_MaxIndex)
                    {
                        UpdateBinderAllCardUI(1, ___m_Index + 1, ___m_MaxIndex, __instance);
                    }
                }

                if (___m_GoPrevious && ___m_Index > 1)
                {
                    __instance.m_BinderPageGrpList[2].m_Anim.SetTrigger("GoPreviousPage");
                    __instance.m_BinderPageGrpList[1].m_Anim.SetTrigger("SetHidePreviousIdle");
                    __instance.m_BinderPageGrpList[0].m_Anim.SetTrigger("GoPreviousPage");
                    BinderPageGrp item2 = __instance.m_BinderPageGrpList[2];
                    __instance.m_BinderPageGrpList.RemoveAt(2);
                    __instance.m_BinderPageGrpList.Insert(0, item2);
                    ___m_GoPrevious = false;
                    Plugin.SetPProperty(__instance, "m_GoPrevious", ___m_GoPrevious);
                    ___m_Index--;
                    if (___m_CanFlipCoroutine != null)
                    {
                        __instance.StopCoroutine(___m_CanFlipCoroutine);
                    }

                    ___m_CanFlipCoroutine = __instance.StartCoroutine(DelayResetCanFlipBook(0.55f, __instance));
                    Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);
                    __instance.m_CollectionBinderUI.SetCurrentPage(___m_Index);
                    SoundManager.PlayAudio("SFX_AlbumFlip", 0.6f);
                    if (___m_Index > 1)
                    {
                        UpdateBinderAllCardUI(2, ___m_Index - 1, ___m_MaxIndex, __instance);
                    }
                }

                if (___m_GoNext10 && ___m_Index < ___m_MaxIndex)
                {
                    __instance.m_BinderPageGrpList[0].m_Anim.SetTrigger("GoNextPage");
                    __instance.m_BinderPageGrpList[1].m_Anim.SetTrigger("GoNextPage");
                    __instance.m_BinderPageGrpList[2].m_Anim.SetTrigger("SetHideNextIdle");
                    BinderPageGrp item3 = __instance.m_BinderPageGrpList[0];
                    __instance.m_BinderPageGrpList.RemoveAt(0);
                    __instance.m_BinderPageGrpList.Add(item3);
                    ___m_GoNext10 = false;
                    Plugin.SetPProperty(__instance, "m_GoNext10", ___m_GoNext10);
                    ___m_Index += 10;
                    if (___m_Index > ___m_MaxIndex)
                    {
                        ___m_Index = ___m_MaxIndex;
                    }

                    if (___m_CanFlipCoroutine != null)
                    {
                        __instance.StopCoroutine(___m_CanFlipCoroutine);
                    }

                    ___m_CanFlipCoroutine = __instance.StartCoroutine(DelayResetCanFlipBook(0.55f, __instance));
                    Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);
                    UpdateBinderAllCardUI(0, ___m_Index, ___m_MaxIndex, __instance);
                    __instance.StartCoroutine(DelaySetBinderPageCardIndex(2, ___m_Index - 1, __instance));
                    __instance.m_CollectionBinderUI.SetCurrentPage(___m_Index);
                    SoundManager.PlayAudio("SFX_AlbumFlip", 0.6f);
                    if (___m_Index < ___m_MaxIndex)
                    {
                        __instance.StartCoroutine(DelaySetBinderPageCardIndex(1, ___m_Index + 1, __instance));
                    }
                }

                if (___m_GoPrevious10 && ___m_Index > 1)
                {
                    __instance.m_BinderPageGrpList[2].m_Anim.SetTrigger("GoPreviousPage");
                    __instance.m_BinderPageGrpList[1].m_Anim.SetTrigger("SetHidePreviousIdle");
                    __instance.m_BinderPageGrpList[0].m_Anim.SetTrigger("GoPreviousPage");
                    BinderPageGrp item4 = __instance.m_BinderPageGrpList[2];
                    __instance.m_BinderPageGrpList.RemoveAt(2);
                    __instance.m_BinderPageGrpList.Insert(0, item4);
                    ___m_GoPrevious10 = false;
                    Plugin.SetPProperty(__instance, "m_GoPrevious10", ___m_GoPrevious10);
                    ___m_Index -= 10;
                    if (___m_Index < 1)
                    {
                        ___m_Index = 1;
                    }

                    if (___m_CanFlipCoroutine != null)
                    {
                        __instance.StopCoroutine(___m_CanFlipCoroutine);
                    }

                    ___m_CanFlipCoroutine = __instance.StartCoroutine(DelayResetCanFlipBook(0.55f, __instance));
                    Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);
                    UpdateBinderAllCardUI(0, ___m_Index, ___m_MaxIndex, __instance);
                    __instance.StartCoroutine(DelaySetBinderPageCardIndex( 1, ___m_Index + 1, __instance));
                    __instance.m_CollectionBinderUI.SetCurrentPage(___m_Index);
                    if (___m_Index > 1)
                    {
                        __instance.StartCoroutine(DelaySetBinderPageCardIndex( 2, ___m_Index - 1, __instance));
                    }

                    SoundManager.PlayAudio("SFX_AlbumFlip", 0.6f);
                }
            }

            ___m_OpenBinder = false;
            ___m_CloseBinder = false;
            ___m_GoNext = false;
            ___m_GoPrevious = false;
            ___m_GoNext10 = false;
            ___m_GoPrevious10 = false;

            Plugin.SetPProperty(__instance, "m_IsBookOpen", ___m_IsBookOpen);
            Plugin.SetPProperty(__instance, "m_IsHoldingCardCloseUp", ___m_IsHoldingCardCloseUp);
            Plugin.SetPProperty(__instance, "m_IsExitingCardCloseUp", ___m_IsExitingCardCloseUp);
            Plugin.SetPProperty(__instance, "m_OpenBinder", ___m_OpenBinder);
            Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);
            Plugin.SetPProperty(__instance, "m_ExpansionType", ___m_ExpansionType);
            Plugin.SetPProperty(__instance, "m_MaxIndex", ___m_MaxIndex);
            Plugin.SetPProperty(__instance, "m_SortingType", ___m_SortingType);
            Plugin.SetPProperty(__instance, "m_CloseBinder", ___m_CloseBinder);
            Plugin.SetPProperty(__instance, "m_CanFlip", ___m_CanFlip);
            Plugin.SetPProperty(__instance, "m_GoNext", ___m_GoNext);
            Plugin.SetPProperty(__instance, "m_GoPrevious", ___m_GoPrevious);
            Plugin.SetPProperty(__instance, "m_GoNext10", ___m_GoNext10);
            Plugin.SetPProperty(__instance, "m_GoPrevious10", ___m_GoPrevious10);
            Plugin.SetPProperty(__instance, "m_Index", ___m_Index);

            return false;
        }
    }
}
