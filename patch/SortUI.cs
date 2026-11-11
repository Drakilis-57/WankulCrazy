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
        const int GRADED_HIGHLIGHT_INDEX = 7;

        public static bool inited = false;
        static List<int> SortedCardIndies = [];
        static List<int> SortedGradedIndices = [];
        static SortSeasonType currentSeason = SortSeasonType.ALL;
        static SortType currentSortType = SortType.Price;
        static SortType currentGradedSortType = SortType.Price;
        static int currentGameSortMethod = GradedCardService.DefaultSortMethodIndex;
        static int currentGameExpansionIndex = 0;
        static bool CanFlip = true;
        static List<GameObject> seasonHighlights = new List<GameObject>();
        static GameObject gradedBtnObj = null;
        static TextMeshProUGUI gradedBtnText = null;
        static TextMeshProUGUI amountOrGradeSortText = null;

        private static bool IsGradedCardAlbum(CollectionBinderFlipAnimCtrl instance)
        {
            return instance != null && (bool)(Plugin.GetPProperty(instance, "m_IsGradedCardAlbum") ?? false);
        }

        private static void SetBinderPagesGradedMode(CollectionBinderFlipAnimCtrl instance, bool isGraded)
        {
            if (instance?.m_BinderPageGrpList == null) return;
            for (int i = 0; i < instance.m_BinderPageGrpList.Count; i++)
            {
                instance.m_BinderPageGrpList[i]?.SetGradedCardMode(isGraded);
            }
        }

        public static void UpdateSeasonHighlights(int selectedIdx)
        {
            for (int h = 0; h < seasonHighlights.Count; h++)
            {
                if (seasonHighlights[h] != null)
                {
                    seasonHighlights[h].SetActive(h == selectedIdx);
                }
            }
        }

        public static void OpenSortAlbumScreenPrefix(ref int sortingMethodIndex, ref int currentExpansionIndex, bool isGradedCardAlbum, CollectionBinderUI __instance)
        {
            bool inGraded = isGradedCardAlbum || IsGradedCardAlbum(__instance?.m_CollectionAlbum);
            if (inGraded)
            {
                int gradedMethodIdx = GradedCardService.ClampSortMethodIndex(CPlayerData.m_GradedCollectionSortingMethodIndex);
                currentGradedSortType = GradedCardService.ButtonIndexToSortType(gradedMethodIdx);
                sortingMethodIndex = gradedMethodIdx;
            }
            else
            {
                sortingMethodIndex = GradedCardService.ClampSortMethodIndex(currentGameSortMethod);
            }

            if (__instance?.m_ExpansionBtnList != null && __instance.m_ExpansionBtnList.Count > 0)
            {
                currentExpansionIndex = Mathf.Clamp(currentGameExpansionIndex, 0, __instance.m_ExpansionBtnList.Count - 1);
            }
        }

        public static void OpenSortAlbumScreen(int sortingMethodIndex, int currentExpansionIndex, bool isGradedCardAlbum, CollectionBinderUI __instance)
        {
            Plugin.LogInfo($"[SortUI] OpenSortAlbumScreen appelé. inited={inited}, isGraded={isGradedCardAlbum}, sortingMethodIndex={sortingMethodIndex}, currentExpansionIndex={currentExpansionIndex}");

            if (!inited || gradedBtnObj == null)
            {
                try
                {
                    Plugin.LogInfo($"[SortUI] Début initialisation UI. m_ExpansionBtnList count={__instance.m_ExpansionBtnList?.Count}, m_SortAlbumBtnList count={__instance.m_SortAlbumBtnList?.Count}");

                    Transform Expansion_AnimGrp_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp");
                    Transform Expansion_BG_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/BG");
                    Transform Expansion_Title_BG_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/TitleBG");
                    Transform Expansion_Title_Text_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/TitleText");
                    Transform Expansion_Mask_Transform = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/Mask");

                    Plugin.LogInfo($"[SortUI] Transforms trouvés : AnimGrp={Expansion_AnimGrp_Transform != null}, BG={Expansion_BG_Transform != null}, Mask={Expansion_Mask_Transform != null}");

                    // Log des propriétés de base pour diagnostic
                    RectTransform maskRect = Expansion_Mask_Transform as RectTransform;
                    RectTransform animGrpRect = Expansion_AnimGrp_Transform as RectTransform;
                    RectTransform btn0Rect = __instance.m_ExpansionBtnList[0].GetComponent<RectTransform>();

                    Plugin.LogInfo($"[SortUI] Base: Mask sizeDelta={maskRect?.sizeDelta}, offsetMin={maskRect?.offsetMin}, offsetMax={maskRect?.offsetMax}");
                    Plugin.LogInfo($"[SortUI] Base: Btn0 anchoredPosition={btn0Rect.anchoredPosition}, sizeDelta={btn0Rect.sizeDelta}, anchorMin={btn0Rect.anchorMin}, anchorMax={btn0Rect.anchorMax}");

                    // On s'assure que le Mask ne masque pas tout le bas du panneau (7 saisons + Cartes notées) :
                    if (maskRect != null)
                    {
                        maskRect.offsetMin = new Vector2(-382f, -560f);
                        maskRect.offsetMax = new Vector2(382f, 462f);
                    }

                    string[] seasonNames = new string[]
                    {
                        "Tout",
                        SeasonsContainer.Seasons[Season.S01],
                        SeasonsContainer.Seasons[Season.S02],
                        SeasonsContainer.Seasons[Season.S03],
                        SeasonsContainer.Seasons[Season.S04],
                        SeasonsContainer.Seasons[Season.S05],
                        SeasonsContainer.Seasons[Season.HS]
                    };

                    string[] btnObjectNames = new string[]
                    {
                        "ALL_Button", "S01_Button", "S02_Button", "S03_Button", "S04_Button", "S05_Button", "HS_Button"
                    };

                    // Modèle de bouton : on clone SortAlbumBtnList[0] qui a le format et le style parfait (bouton orange/propre)
                    // et on l'adapte pour les 7 saisons + le bouton Cartes notées.
                    Transform templateSortBtn = __instance.m_SortAlbumBtnList[0];
                    Transform expansionParent = __instance.m_ExpansionBtnList[0].parent;

                    // Désactiver tous les anciens boutons de cartes d'extension vanilla
                    for (int b = 0; b < __instance.m_ExpansionBtnList.Count; b++)
                    {
                        __instance.m_ExpansionBtnList[b].gameObject.SetActive(false);
                    }

                    // Nettoyer d'anciens clones Wankul si déjà créés
                    List<Transform> existingWankulBtns = new List<Transform>();
                    for (int c = 0; c < expansionParent.childCount; c++)
                    {
                        var child = expansionParent.GetChild(c);
                        if (child.name.StartsWith("Wankul_Season_") || child.name == "Wankul_Graded_Button")
                        {
                            existingWankulBtns.Add(child);
                        }
                    }
                    foreach (var eb in existingWankulBtns)
                    {
                        GameObject.Destroy(eb.gameObject);
                    }

                    // Cacher les anciens titres/boutons vanilla qui se superposeraient au panneau custom
                    Transform titleText = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/TitleText");
                    if (titleText != null) titleText.gameObject.SetActive(false);
                    Transform titleBG = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp/Expansion_AnimGrp/TitleBG");
                    if (titleBG != null) titleBG.gameObject.SetActive(false);
                    if (__instance.m_GradedCardBtn != null) __instance.m_GradedCardBtn.gameObject.SetActive(false);
                    if (__instance.m_ExpansionSelectHighlightBtn != null) __instance.m_ExpansionSelectHighlightBtn.SetActive(false);

                    // Supprimer ou désactiver tout composant LayoutGroup sur le parent qui pourrait écraser les positions
                    var layoutGroup = expansionParent.GetComponent<UnityEngine.UI.LayoutGroup>();
                    if (layoutGroup != null)
                    {
                        layoutGroup.enabled = false;
                    }

                    // On s'inspire des boutons de tri vanilla mais on réduit leur échelle (taille)
                    RectTransform sortBtn0 = __instance.m_SortAlbumBtnList[0].GetComponent<RectTransform>();
                    RectTransform sortBtn1 = __instance.m_SortAlbumBtnList[1].GetComponent<RectTransform>();
                    float vanillaSpacing = Mathf.Abs(sortBtn0.anchoredPosition.y - sortBtn1.anchoredPosition.y);
                    if (vanillaSpacing < 50f) vanillaSpacing = 115f;

                    // Échelle réduite à 72% pour des boutons plus compacts et fins
                    float seasonBtnScale = 0.72f;
                    float seasonSpacing = vanillaSpacing * 0.72f; // ~82px
                    float startY = sortBtn0.anchoredPosition.y + 40f; // léger décalage vers le haut

                    Plugin.LogInfo($"[SortUI] Boutons saisons réduits : scale={seasonBtnScale}, spacing={seasonSpacing}, startY={startY}");

                    static GameObject FindButtonHighlight(GameObject btnObj)
                    {
                        Transform hl = btnObj.transform.Find("AnimGrp/BGHighlight");
                        if (hl != null) return hl.gameObject;
                        return btnObj.GetComponentsInChildren<Transform>(true)
                            .Where(t => t.name == "BGHighlight")
                            .Select(t => t.gameObject)
                            .FirstOrDefault();
                    }

                    List<Button> seasonButtons = new List<Button>();
                    seasonHighlights.Clear();

                    for (int i = 0; i < 7; i++)
                    {
                        GameObject newBtnObj = GameObject.Instantiate(templateSortBtn.gameObject, expansionParent);
                        newBtnObj.name = $"Wankul_Season_{i}_{btnObjectNames[i]}";
                        newBtnObj.SetActive(true);

                        RectTransform rt = newBtnObj.GetComponent<RectTransform>();
                        rt.localScale = new Vector3(seasonBtnScale, seasonBtnScale, 1f);
                        rt.anchoredPosition = new Vector2(0, startY - (seasonSpacing * i));

                        var textComp = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
                        if (textComp != null)
                        {
                            textComp.text = seasonNames[i];
                        }

                        Button btn = newBtnObj.GetComponentInChildren<Button>();
                        if (btn != null)
                        {
                            // Remplacer onClick supprime les callbacks persistants copiés de templateSortBtn (ex: tri 0)
                            btn.onClick = new Button.ButtonClickedEvent();
                        }
                        seasonButtons.Add(btn);
                        seasonHighlights.Add(FindButtonHighlight(newBtnObj));

                        Plugin.LogInfo($"[SortUI] Bouton de saison cloné {i} ({seasonNames[i]}) placé à pos={rt.anchoredPosition}, scale={rt.localScale}");
                    }

                    // Bouton dédié "Cartes notées" sous les saisons (index highlight = GRADED_HIGHLIGHT_INDEX = 7)
                    gradedBtnObj = GameObject.Instantiate(templateSortBtn.gameObject, expansionParent);
                    gradedBtnObj.name = "Wankul_Graded_Button";
                    gradedBtnObj.SetActive(true);
                    RectTransform gradedRt = gradedBtnObj.GetComponent<RectTransform>();
                    gradedRt.localScale = new Vector3(seasonBtnScale, seasonBtnScale, 1f);
                    gradedRt.anchoredPosition = new Vector2(0f, startY - (seasonSpacing * 7) - 18f);
                    gradedBtnText = gradedBtnObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (gradedBtnText != null)
                    {
                        gradedBtnText.text = "Cartes notées";
                    }
                    Button gradedBtn = gradedBtnObj.GetComponentInChildren<Button>();
                    if (gradedBtn != null)
                    {
                        gradedBtn.onClick = new Button.ButtonClickedEvent();
                        gradedBtn.onClick.AddListener(() =>
                        {
                            Plugin.LogInfo("[SortUI] Clic sur Cartes notées");
                            UpdateSeasonHighlights(GRADED_HIGHLIGHT_INDEX);
                            __instance.OnPressSwitchToGradedAlbum();
                        });
                    }
                    seasonHighlights.Add(FindButtonHighlight(gradedBtnObj));
                    Plugin.LogInfo($"[SortUI] Bouton Cartes notées placé à pos={gradedRt.anchoredPosition}, scale={gradedRt.localScale}");

                    // Inspection détaillée de toute la hiérarchie pour diagnostic
                    Transform screenGrp = Plugin.GetByPathIn("Canvas", "CollectionBinderUI/ScreenGrp/SortingSelectScreen/Screen_Grp");
                    if (screenGrp != null)
                    {
                        Plugin.LogInfo($"[SortUI] Enfants de Screen_Grp (count={screenGrp.childCount}):");
                        for (int i = 0; i < screenGrp.childCount; i++)
                        {
                            var ch = screenGrp.GetChild(i);
                            Plugin.LogInfo($"  Screen_Grp[{i}] = '{ch.name}', active={ch.gameObject.activeSelf}");
                        }
                    }

                    if (Expansion_AnimGrp_Transform != null)
                    {
                        Plugin.LogInfo($"[SortUI] Enfants de Expansion_AnimGrp (count={Expansion_AnimGrp_Transform.childCount}):");
                        for (int i = 0; i < Expansion_AnimGrp_Transform.childCount; i++)
                        {
                            var ch = Expansion_AnimGrp_Transform.GetChild(i);
                            Plugin.LogInfo($"  Expansion_AnimGrp[{i}] = '{ch.name}', active={ch.gameObject.activeSelf}");
                            if (ch.name != "BG" && ch.name != "Mask")
                            {
                                ch.gameObject.SetActive(false);
                            }
                        }
                    }

                    if (expansionParent != null)
                    {
                        Plugin.LogInfo($"[SortUI] Enfants de expansionParent '{expansionParent.name}' (count={expansionParent.childCount}):");
                        for (int i = 0; i < expansionParent.childCount; i++)
                        {
                            var ch = expansionParent.GetChild(i);
                            Plugin.LogInfo($"  expansionParent[{i}] = '{ch.name}', active={ch.gameObject.activeSelf}");
                            if (!ch.name.StartsWith("Wankul_Season_") && ch.name != "Wankul_Graded_Button")
                            {
                                ch.gameObject.SetActive(false);
                            }
                        }
                    }

                    // Cacher spécifiquement TitleText et TitleBG s'ils existent n'importe où
                    if (titleText != null) titleText.gameObject.SetActive(false);
                    if (titleBG != null) titleBG.gameObject.SetActive(false);

                    __instance.m_SortAlbumBtnList[0].GetComponentInChildren<TextMeshProUGUI>().text = "Numéro de Carte";
                    __instance.m_SortAlbumBtnList[1].GetComponentInChildren<TextMeshProUGUI>().text = "Rareté";
                    __instance.m_SortAlbumBtnList[2].GetComponentInChildren<TextMeshProUGUI>().text = "Prix";
                    amountOrGradeSortText = __instance.m_SortAlbumBtnList[3].GetComponentInChildren<TextMeshProUGUI>();
                    if (amountOrGradeSortText != null) amountOrGradeSortText.text = "Quantité";

                    // Aligner proprement le bouton "Doublon" sans chevauchement avec "Quantité"
                    RectTransform sortBtn2 = __instance.m_SortAlbumBtnList[2].GetComponent<RectTransform>();
                    RectTransform sortBtn3 = __instance.m_SortAlbumBtnList[3].GetComponent<RectTransform>();
                    float sortSpacing = Mathf.Abs(sortBtn2.anchoredPosition.y - sortBtn3.anchoredPosition.y);
                    if (sortSpacing < 40f) sortSpacing = 75f;

                    RectTransform doublonBtnRect = __instance.m_SortAlbumBtnList[4].GetComponent<RectTransform>();
                    doublonBtnRect.anchoredPosition = new Vector2(
                        sortBtn3.anchoredPosition.x,
                        sortBtn3.anchoredPosition.y - sortSpacing
                    );
                    __instance.m_SortAlbumBtnList[4].GetComponentInChildren<TextMeshProUGUI>().text = "Doublon";
                    __instance.m_SortAlbumBtnList[4].gameObject.SetActive(true);

                    __instance.m_SortAlbumBtnList[5].gameObject.SetActive(false);
                    __instance.m_SortAlbumBtnList[6].gameObject.SetActive(false);

                    void OnClickSeasonButton(SortSeasonType season, int expansionIdx)
                    {
                        Plugin.LogInfo($"[SortUI] Clic sur saison : {season} (expansionIdx={expansionIdx})");
                        bool wasGraded = IsGradedCardAlbum(__instance.m_CollectionAlbum);
                        currentSeason = season;
                        currentGameExpansionIndex = expansionIdx;
                        UpdateSeasonHighlights(expansionIdx);
                        CSingleton<InteractionPlayerController>.Instance?.HideCursor();
                        SoundManager.GenericConfirm(1f, 1f);
                        __instance.m_SortAlbumScreen.SetActive(false);
                        if (__instance.m_SortAlbumScreenUIExtension != null)
                        {
                            ControllerScreenUIExtManager.OnCloseScreen(__instance.m_SortAlbumScreenUIExtension);
                        }
                        if (__instance.m_CollectionAlbum != null)
                        {
                            if (wasGraded)
                            {
                                // Quitter l'album gradé en passant par OnPressSwitchExpansion pour relancer
                                // DelayAlbumSort() et restaurer la couverture + échelle des pages normales.
                                if (CPlayerData.m_CollectionSortingMethodIndexList != null && CPlayerData.m_CollectionSortingMethodIndexList.Count > 0)
                                {
                                    CPlayerData.m_CollectionSortingMethodIndexList[0] = currentGameSortMethod;
                                }
                                __instance.m_CollectionAlbum.OnPressSwitchExpansion(0);
                            }
                            else
                            {
                                OnSortingMethodUpdated(true, __instance.m_CollectionAlbum);
                            }
                        }
                    }

                    SortSeasonType[] seasonTargets =
                    {
                        SortSeasonType.ALL,
                        SortSeasonType.S01,
                        SortSeasonType.S02,
                        SortSeasonType.S03,
                        SortSeasonType.S04,
                        SortSeasonType.S05,
                        SortSeasonType.HS
                    };
                    for (int sIdx = 0; sIdx < seasonTargets.Length && sIdx < seasonButtons.Count; sIdx++)
                    {
                        int capturedIdx = sIdx;
                        SortSeasonType capturedSeason = seasonTargets[sIdx];
                        seasonButtons[sIdx]?.onClick.AddListener(() => OnClickSeasonButton(capturedSeason, capturedIdx));
                    }

                    (int btnIdx, string label, SortType sortType)[] sortButtonSpecs =
                    {
                        (0, "Numéro", SortType.Number),
                        (1, "Rareté", SortType.Rarity),
                        (2, "Prix", SortType.Price),
                        (3, "Quantité/Note", SortType.Amount),
                        (4, "Doublon", SortType.Double)
                    };
                    foreach (var spec in sortButtonSpecs)
                    {
                        int capturedBtnIdx = spec.btnIdx;
                        string capturedLabel = spec.label;
                        SortType capturedSort = spec.sortType;
                        Button sortBtn = __instance.m_SortAlbumBtnList[capturedBtnIdx].GetComponentInChildren<Button>();
                        sortBtn?.onClick.AddListener(() =>
                        {
                            Plugin.LogInfo($"[SortUI] Clic tri : {capturedLabel}");
                            bool inGraded = IsGradedCardAlbum(__instance.m_CollectionAlbum);
                            if (inGraded)
                            {
                                currentGradedSortType = capturedSort;
                                CPlayerData.m_GradedCollectionSortingMethodIndex = capturedBtnIdx;
                            }
                            else
                            {
                                currentSortType = capturedSort;
                                currentGameSortMethod = capturedBtnIdx;
                            }
                            if (__instance.m_SortAlbumScreen != null && __instance.m_SortAlbumScreen.activeSelf)
                            {
                                __instance.OnPressSwitchSortingMethod(capturedBtnIdx);
                            }
                        });
                    }

                    // Marquer l'initialisation comme terminée
                    inited = true;
                    Plugin.LogInfo("[SortUI] Initialisation UI terminée avec succès !");
                }
                catch (System.Exception ex)
                {
                    Plugin.Logger?.LogError($"[SortUI] Exception lors de OpenSortAlbumScreen : {ex}");
                }
            }

            // Mettre à jour le compteur, les libellés contextuels et la surbrillance active à chaque ouverture
            bool isGradedActive = isGradedCardAlbum || IsGradedCardAlbum(__instance.m_CollectionAlbum);
            if (gradedBtnText != null)
            {
                int gradedCount = CPlayerData.m_GradedCardInventoryList != null ? CPlayerData.m_GradedCardInventoryList.Count : 0;
                gradedBtnText.text = $"Cartes notées ({gradedCount})";
            }
            if (amountOrGradeSortText != null)
            {
                amountOrGradeSortText.text = isGradedActive ? "Note" : "Quantité";
            }
            if (__instance.m_ExpansionSelectHighlightBtn != null)
            {
                __instance.m_ExpansionSelectHighlightBtn.SetActive(false);
            }
            UpdateSeasonHighlights(isGradedActive ? GRADED_HIGHLIGHT_INDEX : currentGameExpansionIndex);
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
                Season targetSeason = (Season)season;
                string targetSeasonStr = targetSeason.ToString();

                var filteredWankulCards = WankulInventory.Instance.wankulCards
                    .Where(kvp => kvp.Value.wankulcard.Season == targetSeason || string.Equals(kvp.Value.wankulcard.SeasonId, targetSeasonStr, System.StringComparison.OrdinalIgnoreCase))
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

        private static int ResolveSeasonOrder(WankulCardData wankulCard)
        {
            if (wankulCard == null) return int.MaxValue;
            var allSeasons = SeasonsManager.GetAllSeasons();
            if (allSeasons != null && !string.IsNullOrWhiteSpace(wankulCard.SeasonId))
            {
                for (int i = 0; i < allSeasons.Count; i++)
                {
                    if (string.Equals(allSeasons[i].Id, wankulCard.SeasonId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return (int)wankulCard.Season;
        }

        private static int ResolveRarityOrder(WankulCardData wankulCard, CardData cardData)
        {
            if (wankulCard is EffigyCardData eff)
            {
                var allRarities = RaritiesManager.GetAllRarities();
                if (allRarities != null && !string.IsNullOrWhiteSpace(eff.RarityId))
                {
                    for (int i = 0; i < allRarities.Count; i++)
                    {
                        if (string.Equals(allRarities[i].Id, eff.RarityId, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
                return (int)eff.Rarity;
            }
            if (cardData != null)
            {
                var monsterData = InventoryBase.GetMonsterData(cardData.monsterType);
                if (monsterData != null) return (int)monsterData.Rarity;
            }
            return 0;
        }

        private static void ComputeGradedAlbumStats(out float totalPrice, out int gradedCount)
        {
            totalPrice = 0f;
            var gradedList = CPlayerData.m_GradedCardInventoryList;
            if (gradedList == null)
            {
                gradedCount = 0;
                return;
            }

            gradedCount = gradedList.Count;
            for (int i = 0; i < gradedList.Count; i++)
            {
                var entry = gradedList[i];
                if (entry == null) continue;
                entry.amount = GradedCardService.ClampGrade(entry.amount);
                CardData cardData = CPlayerData.GetGradedCardData(entry);
                if (cardData != null)
                {
                    totalPrice += CPlayerData.GetCardMarketPrice(cardData);
                }
            }
        }

        public static void SortGradedCards(CollectionBinderFlipAnimCtrl instance)
        {
            SortedGradedIndices.Clear();
            var gradedList = CPlayerData.m_GradedCardInventoryList;
            if (gradedList == null || gradedList.Count == 0)
            {
                SyncVanillaSortedIndexList(instance, SortedGradedIndices);
                return;
            }

            var entries = new List<GradedAlbumEntry>(gradedList.Count);
            var wankulData = WankulCardsData.Instance;
            for (int i = 0; i < gradedList.Count; i++)
            {
                var raw = gradedList[i];
                if (raw == null) continue;
                raw.amount = GradedCardService.ClampGrade(raw.amount);

                CardData cardData = CPlayerData.GetGradedCardData(raw);
                WankulCardData wankulCard = (wankulData != null && cardData != null) ? wankulData.GetFromMonster(cardData, true) : null;
                float marketPrice = cardData != null ? CPlayerData.GetCardMarketPrice(cardData) : 0f;
                int cardKey = wankulCard != null ? wankulCard.Index : -1 - raw.cardSaveIndex;
                int seasonOrder = ResolveSeasonOrder(wankulCard);
                int cardNumber = wankulCard != null ? wankulCard.NumberInt : raw.cardSaveIndex;
                int rarityOrder = ResolveRarityOrder(wankulCard, cardData);

                entries.Add(new GradedAlbumEntry(
                    inventoryIndex: i,
                    cardKey: cardKey,
                    seasonOrder: seasonOrder,
                    cardNumber: cardNumber,
                    rarityOrder: rarityOrder,
                    marketPrice: marketPrice,
                    grade: raw.amount
                ));
            }

            SortedGradedIndices = GradedCardService.SortGradedEntries(entries, currentGradedSortType);
            SyncVanillaSortedIndexList(instance, SortedGradedIndices);
        }

        private static void SyncVanillaSortedIndexList(CollectionBinderFlipAnimCtrl instance, List<int> source)
        {
            if (instance == null) return;
            if (Plugin.GetPProperty(instance, "m_SortedIndexList") is List<int> vanillaList)
            {
                vanillaList.Clear();
                if (source != null) vanillaList.AddRange(source);
            }
        }

        public static void UpdateBinderGradedCardUI(int binderIndex, int pageIndex, int maxIndex, CollectionBinderFlipAnimCtrl __instance)
        {
            if (__instance?.m_BinderPageGrpList == null || binderIndex < 0 || binderIndex >= __instance.m_BinderPageGrpList.Count)
            {
                return;
            }
            BinderPageGrp pageGrp = __instance.m_BinderPageGrpList[binderIndex];
            if (pageGrp == null || pageGrp.m_CardList == null)
            {
                return;
            }

            for (int i = 0; i < pageGrp.m_CardList.Count; i++)
            {
                pageGrp.SetSingleCard(i, null, 0, ECollectionSortingType.Default);
            }

            if (pageIndex <= 0 || pageIndex > maxIndex)
            {
                return;
            }

            var gradedList = CPlayerData.m_GradedCardInventoryList;
            if (gradedList == null || gradedList.Count == 0)
            {
                return;
            }

            for (int i = 0; i < pageGrp.m_CardList.Count; i++)
            {
                int listPos = (pageIndex - 1) * GradedCardService.CardsPerBinderPage + i;
                if (listPos < 0 || listPos >= SortedGradedIndices.Count)
                {
                    pageGrp.SetSingleCard(i, null, 0, ECollectionSortingType.Default);
                    continue;
                }

                int invIdx = SortedGradedIndices[listPos];
                if (invIdx < 0 || invIdx >= gradedList.Count || gradedList[invIdx] == null)
                {
                    pageGrp.SetSingleCard(i, null, 0, ECollectionSortingType.Default);
                    continue;
                }

                gradedList[invIdx].amount = GradedCardService.ClampGrade(gradedList[invIdx].amount);
                CardData gradedCardData = CPlayerData.GetGradedCardData(gradedList[invIdx]);
                pageGrp.SetSingleCard(i, gradedCardData, 1, ECollectionSortingType.Default);
            }
        }

        private static void UpdateBinderPageUI(int binderIndex, int pageIndex, int maxIndex, CollectionBinderFlipAnimCtrl __instance)
        {
            if (IsGradedCardAlbum(__instance))
            {
                UpdateBinderGradedCardUI(binderIndex, pageIndex, maxIndex, __instance);
            }
            else
            {
                UpdateBinderAllCardUI(binderIndex, pageIndex, maxIndex, __instance);
            }
        }

        public static void UpdateBinderAllCardUI(int binderIndex, int pageIndex, int ___m_MaxIndex, CollectionBinderFlipAnimCtrl __instance)
        {
            if (IsGradedCardAlbum(__instance))
            {
                UpdateBinderGradedCardUI(binderIndex, pageIndex, ___m_MaxIndex, __instance);
                return;
            }

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

                // fixing broken saves / missing associations
                if (cardData == null)
                {
                    Plugin.Logger.LogWarning($"[SortUI] gameCardData is null for Wankul card '{wankulCardData?.Title}' (Index={index})");

                    cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardData);
                    if (cardData == null)
                    {
                        cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                        if (cardData != null)
                        {
                            WankulCardsData.Instance.SetFromMonster(cardData, wankulCardData);
                        }
                    }

                    if (cardData != null)
                    {
                        WankulInventory.Instance.wankulCards[index] = (wankulCardData, cardData, wankulCardTuple.amount);
                        CPlayerData.AddCard(cardData, 1);
                    }
                    else
                    {
                        Plugin.Logger.LogError($"[SortUI] Impossible d'associer un CardData pour '{wankulCardData?.Title}' (Index={index})");
                        __instance.m_BinderPageGrpList[binderIndex].SetSingleCard(i, null, 0, ECollectionSortingType.Default);
                        continue;
                    }
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

            HideCurrentInteractableCard3dList.Invoke(__instance, new object[] { });
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
            UpdateBinderPageUI(binderIndex, pageIndex, m_MaxIndex, __instance);
        }

        public static bool OnSortingMethodUpdated(bool backToFirstPage, CollectionBinderFlipAnimCtrl __instance)
        {
            bool isGraded = IsGradedCardAlbum(__instance);
            SetBinderPagesGradedMode(__instance, isGraded);

            int m_Index = (int)Plugin.GetPProperty(__instance, "m_Index");
            bool m_CanUpdateSort = (bool)Plugin.GetPProperty(__instance, "m_CanUpdateSort");

            if (backToFirstPage)
            {
                m_Index = 1;
            }
            if (m_CanUpdateSort)
            {
                m_CanUpdateSort = false;
                Plugin.SetPProperty(__instance, "m_CanUpdateSort", m_CanUpdateSort);
            }

            if (isGraded)
            {
                int gradedMethodIdx = GradedCardService.ClampSortMethodIndex(CPlayerData.m_GradedCollectionSortingMethodIndex);
                currentGradedSortType = GradedCardService.ButtonIndexToSortType(gradedMethodIdx);
                SortGradedCards(__instance);

                ComputeGradedAlbumStats(out float gradedTotalPrice, out int gradedCount);
                int gradedMaxIndex = GradedCardService.ComputeMaxPages(gradedCount);
                m_Index = GradedCardService.ClampPageIndex(m_Index, gradedMaxIndex);

                Plugin.SetPProperty(__instance, "m_MaxIndex", gradedMaxIndex);
                Plugin.SetPProperty(__instance, "m_Index", m_Index);

                if (__instance.m_CollectionBinderUI != null)
                {
                    __instance.m_CollectionBinderUI.SetMaxPage(gradedMaxIndex);
                    __instance.m_CollectionBinderUI.SetCurrentPage(m_Index);
                    __instance.m_CollectionBinderUI.SetCardCollected(gradedCount, ECardExpansionType.None);
                    __instance.m_CollectionBinderUI.SetTotalValue(gradedTotalPrice);
                }

                UpdateBinderGradedCardUI(0, m_Index, gradedMaxIndex, __instance);
                UpdateBinderGradedCardUI(1, m_Index + 1, gradedMaxIndex, __instance);
                UpdateBinderGradedCardUI(2, m_Index - 1, gradedMaxIndex, __instance);
                return false;
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
            int m_MaxIndex = GradedCardService.ComputeMaxPages(totalSeasonCards);
            m_Index = GradedCardService.ClampPageIndex(m_Index, m_MaxIndex);
            Plugin.SetPProperty(__instance, "m_MaxIndex", m_MaxIndex);
            Plugin.SetPProperty(__instance, "m_Index", m_Index);

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
            bool ___m_IsGradedCardAlbum = IsGradedCardAlbum(__instance);
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


            if (___m_IsBookOpen && ___m_IsHoldingCardCloseUp && !___m_IsExitingCardCloseUp && __instance.m_CurrentSpawnedInteractableCard3d != null)
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

                if (___m_IsGradedCardAlbum)
                {
                    Vector3 targetScale = Vector3.one * InteractionPlayerController.GetCardCloseUpScale() * 0.66f;
                    Vector3 targetPos = new Vector3(0f, -60f, 0f);
                    __instance.m_CurrentSpawnedInteractableCard3d.SetTargetLocalScale(targetScale);
                    __instance.m_CurrentSpawnedInteractableCard3d.SetTargetLocalPos(targetPos);
                }
            }

            if (___m_OpenBinder && !___m_IsBookOpen)
            {
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

                CanFlip = true;
                ___m_CanFlipCoroutine = __instance.StartCoroutine(DelayResetCanFlipBook(CSingleton<InteractionPlayerController>.Instance.m_HideCardAlbumTime + 0.3f, __instance));
                Plugin.SetPProperty(__instance, "m_CanFlipCoroutine", ___m_CanFlipCoroutine);

                if (___m_IsGradedCardAlbum)
                {
                    ComputeGradedAlbumStats(out float gradedTotalPrice, out int gradedCount);
                    ___m_MaxIndex = GradedCardService.ComputeMaxPages(gradedCount);
                    ___m_Index = GradedCardService.ClampPageIndex(___m_Index, ___m_MaxIndex);
                    Plugin.SetPProperty(__instance, "m_MaxIndex", ___m_MaxIndex);
                    Plugin.SetPProperty(__instance, "m_Index", ___m_Index);

                    __instance.m_CollectionBinderUI.SetMaxPage(___m_MaxIndex);
                    __instance.m_CollectionBinderUI.SetCurrentPage(___m_Index);
                    __instance.m_CollectionBinderUI.SetCardCollected(gradedCount, ECardExpansionType.None);
                    __instance.m_CollectionBinderUI.SetTotalValue(gradedTotalPrice);
                    __instance.m_CollectionBinderUI.OpenScreen();

                    int gradedMethodIdx = GradedCardService.ClampSortMethodIndex(CPlayerData.m_GradedCollectionSortingMethodIndex);
                    currentGradedSortType = GradedCardService.ButtonIndexToSortType(gradedMethodIdx);
                    ___m_SortingType = (ECollectionSortingType)gradedMethodIdx;
                    Plugin.SetPProperty(__instance, "m_SortingType", ___m_SortingType);
                }
                else
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

                    List<WankulCardData> wankulCardDatas = WankulCardsData.Instance.cards;
                    float totalPrice = WankulInventory.GetTotalPrice();
                    if (currentSeason != SortSeasonType.ALL)
                    {
                        wankulCardDatas = WankulCardsData.GetCardsFromSeason((Season)currentSeason);
                        totalPrice = WankulInventory.GetTotalPriceBySeason((Season)currentSeason);
                    }

                    int num2 = wankulCardDatas.Count;

                    ___m_MaxIndex = GradedCardService.ComputeMaxPages(num2);
                    ___m_Index = GradedCardService.ClampPageIndex(___m_Index, ___m_MaxIndex);
                    Plugin.SetPProperty(__instance, "m_MaxIndex", ___m_MaxIndex);
                    Plugin.SetPProperty(__instance, "m_Index", ___m_Index);
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
                }

                OnSortingMethodUpdated(false, __instance);
                ___m_MaxIndex = (int)Plugin.GetPProperty(__instance, "m_MaxIndex");
                ___m_Index = (int)Plugin.GetPProperty(__instance, "m_Index");
                UnityAnalytic.OpenAlbum();
                if (!___m_IsGradedCardAlbum && !CPlayerData.m_HasGetGhostCard && (CPlayerData.GetCardCollectedAmount(ECardExpansionType.Ghost, isDimensionCard: false) > 0 || CPlayerData.GetCardCollectedAmount(ECardExpansionType.Ghost, isDimensionCard: true) > 0))
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
                        UpdateBinderPageUI(1, ___m_Index + 1, ___m_MaxIndex, __instance);
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
                        UpdateBinderPageUI(2, ___m_Index - 1, ___m_MaxIndex, __instance);
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
                    UpdateBinderPageUI(0, ___m_Index, ___m_MaxIndex, __instance);
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
                    UpdateBinderPageUI(0, ___m_Index, ___m_MaxIndex, __instance);
                    __instance.StartCoroutine(DelaySetBinderPageCardIndex(1, ___m_Index + 1, __instance));
                    __instance.m_CollectionBinderUI.SetCurrentPage(___m_Index);
                    if (___m_Index > 1)
                    {
                        __instance.StartCoroutine(DelaySetBinderPageCardIndex(2, ___m_Index - 1, __instance));
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
