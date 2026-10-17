using System;
using UnityEngine;
using BepInEx.Logging;
using Logger = HarmonyLib.Tools.Logger;
using System.Threading;
using WankulCrazyPlugin.cards;
using Newtonsoft.Json;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using WankulCrazyPlugin.inventory;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.patch;

public class ReplacingCards
{
    public static bool SetCardUIPrefix(CardData cardData, CardUI __instance)
    {
        WankulCardsData cardsData = WankulCardsData.Instance;

        // fixing broken saves
        if (cardData == null)
        {
            Plugin.Logger.LogWarning("gameCardData is null");

            cardData = cardsData.GetUnassciatedCardData();
            WankulCardData debugwankulCardData = WankulCardsData.GetAJETER();

            string key = $"{cardData.monsterType}_{cardData.borderType}_{cardData.expansionType}";
            Plugin.Logger.LogWarning($"gameCardData is null, adding to inventory {debugwankulCardData.Title} {debugwankulCardData.Index}, {key}");

            cardsData.SetFromMonster(cardData, debugwankulCardData);
            CPlayerData.AddCard(cardData, 1);
        }

        CardData gameCardData = cardData;

        WankulCardData wankulCardData = cardsData.GetFromMonster(gameCardData, true);
        if (wankulCardData == null)
        {
            // Si la carte n'a pas de MonsterData vanilla valide (ex: slot corrompu ou au-delà des monstres jeu),
            // la méthode originale du jeu crasherait avec une NullReferenceException en faisant monsterData.something.
            // On empêche ce crash en interceptant l'exécution.
            if (InventoryBase.GetMonsterData(gameCardData.monsterType) == null)
            {
                Plugin.SetPProperty(__instance, "m_CardData", gameCardData);
                return false;
            }
            return true;
        }

        gameCardData.isFoil = false;
        gameCardData.isChampionCard = false;

        if (wankulCardData is EffigyCardData effigyCard)
        {
            var rarityData = RaritiesManager.GetRarity(effigyCard.RarityId);
            if ((rarityData != null && rarityData.IsEligibleForFoil) || effigyCard.Rarity >= Rarity.UR1)
            {
                gameCardData.isFoil = true;
            }
        }
        Plugin.LogInfo($"[CardUI.SetCardUIPrefix] Carte '{wankulCardData.Title}' (Index={wankulCardData.Index}, Type={wankulCardData.GetType().Name}) -> isFoil={gameCardData.isFoil}, HasSpriteMask={(wankulCardData.SpriteMask != null)}");

        // Pour toute carte Wankul valide, on configure complètement la carte
        // et on retourne TOUJOURS false pour empêcher l'exécution de CardUI.SetCardUI vanilla
        // qui crashe avec une NullReferenceException sur les extensions personnalisées.
        Plugin.SetPProperty(__instance, "m_CardData", gameCardData);
        try
        {
            CardUISetting cardUISetting = InventoryBase.GetCardUISetting(gameCardData.expansionType);
            if (cardUISetting != null)
            {
                Plugin.SetPProperty(__instance, "m_CardUISetting", cardUISetting);
                var settingData = cardUISetting.GetCardUISettingData(gameCardData.GetCardBorderType(), gameCardData.isDestiny);
                Plugin.SetPProperty(__instance, "m_CardUISettingData", settingData);
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"[SetCardUIPrefix] Impossible d'assigner CardUISetting: {ex.Message}");
        }
        ApplyWankulCardVisuals(__instance, gameCardData, wankulCardData);
        if (gameCardData.cardGrade > 0)
        {
            Card3dUIGroup card3dUIGroup = Plugin.GetPProperty(__instance, "m_Card3dUIGroup") as Card3dUIGroup;
            if (card3dUIGroup != null)
            {
                card3dUIGroup.EvaluateCardGrade(gameCardData);
            }
            if (CSingleton<InventoryBase>.Instance?.m_MonsterData_SO != null && __instance.m_GradedCardTextureImage != null)
            {
                __instance.m_GradedCardTextureImage.sprite = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.GetGradedCardScratchTexture(gameCardData.cardGrade);
            }
            __instance.ShowGradedCardCase(__instance.m_Show2DGradedCase);

            string cardTitle = wankulCardData.Title ?? "";
            string cardSub = (wankulCardData is EffigyCardData eff) ? $"{eff.SeasonId} - {eff.RarityId}" : $"{wankulCardData.SeasonId}";
            if (__instance.m_GradeNameText != null)
            {
                __instance.m_GradeNameText.text = cardTitle;
            }
            if (__instance.m_GradeExpansionRarityText != null)
            {
                __instance.m_GradeExpansionRarityText.text = cardSub;
            }
            if (card3dUIGroup != null)
            {
                if (card3dUIGroup.m_GradeNameText != null)
                {
                    card3dUIGroup.m_GradeNameText.text = cardTitle;
                }
                if (card3dUIGroup.m_GradeExpansionRarityText != null)
                {
                    card3dUIGroup.m_GradeExpansionRarityText.text = cardSub;
                }
            }
        }
        else
        {
            if (__instance.m_GradedCardCaseGrp != null)
            {
                __instance.m_GradedCardCaseGrp.SetActive(false);
            }
            if (__instance.m_GradedCardTextureImage != null)
            {
                __instance.m_GradedCardTextureImage.enabled = false;
                __instance.m_GradedCardTextureImage.gameObject.SetActive(false);
                __instance.m_GradedCardTextureImage.sprite = null;
            }
            Card3dUIGroup card3dUIGroup = Plugin.GetPProperty(__instance, "m_Card3dUIGroup") as Card3dUIGroup;
            if (card3dUIGroup != null && card3dUIGroup.m_GradedCardGrp != null)
            {
                card3dUIGroup.m_GradedCardGrp.SetActive(false);
            }
        }
        return false;
    }
    
    private const float CardImageScale = 0.88f; // Sert a grandir ou réduire la taille des images des cartes

    public static void ApplyWankulCardVisuals(CardUI __instance, CardData gameCardData, WankulCardData wankulCardData)
    {
        if (__instance == null || gameCardData == null || wankulCardData == null) return;

        if (wankulCardData.Sprite == null)
        {
            Plugin.Logger.LogWarning($"wankulCardData Sprite is null for {wankulCardData.Title} ({wankulCardData.Index})");
            return;
        }
        if (__instance.m_CardBGImage != null)
        {
            __instance.m_CardBGImage.gameObject.SetActive(false);
        }

        if (__instance.m_CardBorderImage != null)
        {
            __instance.m_CardBorderImage.gameObject.SetActive(false);
        }

        if (__instance.m_CardFrontImage != null)
        {
            __instance.m_CardFrontImage.gameObject.SetActive(false);
        }

        if (__instance.m_CardFrontImageTopLayer != null)
        {
            __instance.m_CardFrontImageTopLayer.gameObject.SetActive(false);
        }

        if (__instance.m_CardFullBGImage != null)
        {
            __instance.m_CardFullBGImage.enabled = true;
            __instance.m_CardFullBGImage.gameObject.SetActive(true);

            if (wankulCardData != null && wankulCardData.Sprite != null)
            {
                __instance.m_CardFullBGImage.sprite = (Sprite)wankulCardData.Sprite;
            }
            else
            {
                WankulCardData fallbackCard = WankulCardsData.GetAJETER();
                if (fallbackCard != null && fallbackCard.Sprite != null)
                {
                    __instance.m_CardFullBGImage.sprite = (Sprite)fallbackCard.Sprite;
                }
            }

            __instance.m_CardFullBGImage.preserveAspect = true;

            RectTransform rect = __instance.m_CardFullBGImage.rectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = new Vector3(CardImageScale, CardImageScale, 1f);
                rect.localRotation = Quaternion.identity;
            }
        }

        if (__instance.m_CardFullBGOffsetGrp != null)
        {
            __instance.m_CardFullBGOffsetGrp.SetActive(true);

            RectTransform grpRect = __instance.m_CardFullBGOffsetGrp.GetComponent<RectTransform>();
            if (grpRect != null)
            {
                grpRect.anchorMin = Vector2.zero;
                grpRect.anchorMax = Vector2.one;
                grpRect.offsetMin = Vector2.zero;
                grpRect.offsetMax = Vector2.zero;
                grpRect.localScale = Vector3.one;
                grpRect.localRotation = Quaternion.identity;
            }
        }

        if (__instance.m_CardFullBGTransparentLayeredOffsetGrp != null)
        {
            __instance.m_CardFullBGTransparentLayeredOffsetGrp.SetActive(false);
        }

        CleanCardVisuals(__instance, gameCardData.isFoil, wankulCardData);
    }

    static void SetCardUIPostFix(CardData cardData, CardUI __instance)
    {
        if (cardData == null)
        {
            Plugin.Logger.LogError("gameCardData is null");
            return;
        }
        CardData gameCardData = cardData;
        WankulCardsData cardsData = WankulCardsData.Instance;

        WankulCardData wankulCardData = cardsData.GetFromMonster(gameCardData, true);
        if (wankulCardData == null)
        {
            return;
        }

        ApplyWankulCardVisuals(__instance, gameCardData, wankulCardData);

        if (gameCardData.cardGrade > 0)
        {
            string cardTitle = wankulCardData.Title ?? "";
            string cardSub = (wankulCardData is EffigyCardData eff) ? $"{eff.SeasonId} - {eff.RarityId}" : $"{wankulCardData.SeasonId}";

            if (__instance.m_GradeNameText != null)
            {
                __instance.m_GradeNameText.text = cardTitle;
            }
            if (__instance.m_GradeExpansionRarityText != null)
            {
                __instance.m_GradeExpansionRarityText.text = cardSub;
            }
            Card3dUIGroup card3dUIGroup = Plugin.GetPProperty(__instance, "m_Card3dUIGroup") as Card3dUIGroup;
            if (card3dUIGroup != null)
            {
                if (card3dUIGroup.m_GradeNameText != null)
                {
                    card3dUIGroup.m_GradeNameText.text = cardTitle;
                }
                if (card3dUIGroup.m_GradeExpansionRarityText != null)
                {
                    card3dUIGroup.m_GradeExpansionRarityText.text = cardSub;
                }
            }
        }
        else
        {
            if (__instance.m_GradedCardCaseGrp != null)
            {
                __instance.m_GradedCardCaseGrp.SetActive(false);
            }
            if (__instance.m_GradedCardTextureImage != null)
            {
                __instance.m_GradedCardTextureImage.enabled = false;
                __instance.m_GradedCardTextureImage.gameObject.SetActive(false);
                __instance.m_GradedCardTextureImage.sprite = null;
            }
            Card3dUIGroup card3dUIGroup = Plugin.GetPProperty(__instance, "m_Card3dUIGroup") as Card3dUIGroup;
            if (card3dUIGroup != null && card3dUIGroup.m_GradedCardGrp != null)
            {
                card3dUIGroup.m_GradedCardGrp.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Nettoie et configure précisément les calques visuels d'une carte Wankul.
    /// Éteint complètement les résidus vanilla (StatGrp, RarityImage, CameraFoilShine*, FadeBar*, etc.)
    /// particulièrement récalcitrants sur les slots 7, 8 et 9 lors de l'ouverture de booster.
    /// </summary>
    public static void CleanCardVisuals(CardUI __instance, bool isFoil, WankulCardData wankulCardData)
    {
        if (__instance == null) return;

        Transform cardBGMask = __instance.m_CardFullBGOffsetGrp != null
            ? __instance.m_CardFullBGOffsetGrp.transform.parent
            : (__instance.m_FoilShowList != null && __instance.m_FoilShowList.Count > 0 ? __instance.m_FoilShowList[0].transform.parent : null);
        Transform foilMaskTrans = cardBGMask?.Find("WankulFoilMask");

        if (wankulCardData != null && wankulCardData.SpriteMask != null && isFoil)
        {
            Plugin.LogInfo($"[CleanCardVisuals] ACTIVATION FOIL pour '{wankulCardData.Title}' (isFoil=True)");
            if (__instance.m_FoilGrp != null)
            {
                __instance.m_FoilGrp.SetActive(true);
            }

            if (cardBGMask != null)
            {
                if (foilMaskTrans == null)
                {
                    GameObject maskObj = new GameObject("WankulFoilMask", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
                    maskObj.transform.SetParent(cardBGMask, false);
                    RectTransform rt = maskObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.localScale = new Vector3(CardImageScale, CardImageScale, 1f);
                    rt.localRotation = Quaternion.identity;

                    var maskComp = maskObj.GetComponent<UnityEngine.UI.Mask>();
                    maskComp.showMaskGraphic = false;

                    foilMaskTrans = maskObj.transform;

                    if (__instance.m_FoilShowList != null)
                    {
                        foreach (var foilImg in __instance.m_FoilShowList)
                        {
                            if (foilImg != null)
                            {
                                foilImg.transform.SetParent(foilMaskTrans, false);
                                RectTransform cRt = foilImg.rectTransform;
                                if (cRt != null)
                                {
                                    cRt.anchorMin = Vector2.zero;
                                    cRt.anchorMax = Vector2.one;
                                    cRt.offsetMin = Vector2.zero;
                                    cRt.offsetMax = Vector2.zero;
                                    cRt.localScale = Vector3.one;
                                    cRt.localRotation = Quaternion.identity;
                                }
                            }
                        }
                    }
                }

                foilMaskTrans.gameObject.SetActive(true);
                var maskImage = foilMaskTrans.GetComponent<UnityEngine.UI.Image>();
                if (maskImage != null)
                {
                    maskImage.enabled = true;
                    maskImage.sprite = (Sprite)wankulCardData.SpriteMask;
                    maskImage.preserveAspect = true;
                }
                var maskComponent = foilMaskTrans.GetComponent<UnityEngine.UI.Mask>();
                if (maskComponent != null)
                {
                    maskComponent.enabled = true;
                }
            }

            if (__instance.m_FoilShowList != null)
            {
                foreach (var foilImg in __instance.m_FoilShowList)
                {
                    if (foilImg != null)
                    {
                        foilImg.gameObject.SetActive(true);
                    }
                }
            }
        }
        else
        {
            if (foilMaskTrans != null)
            {
                foilMaskTrans.gameObject.SetActive(false);
            }
            if (__instance.m_FoilGrp != null)
            {
                __instance.m_FoilGrp.SetActive(false);
            }
            if (__instance.m_FoilShowList != null)
            {
                foreach (var foilImg in __instance.m_FoilShowList)
                {
                    if (foilImg != null) foilImg.gameObject.SetActive(false);
                }
            }
        }

        if (__instance.m_CenterFrameImageGrp != null)
        {
            __instance.m_CenterFrameImageGrp.SetActive(false);
        }

        if (__instance.m_CenterFrameMaskGrp != null)
        {
            __instance.m_CenterFrameMaskGrp.SetActive(false);
        }

        if (__instance.m_StatGrp != null)
        {
            __instance.m_StatGrp.SetActive(false);
        }

        if (__instance.m_EvoAndArtistNameGrp != null)
        {
            __instance.m_EvoAndArtistNameGrp.SetActive(false);
        }

        if (__instance.m_EvoGrp != null)
        {
            __instance.m_EvoGrp.SetActive(false);
        }

        if (__instance.m_EvoBasicGrp != null)
        {
            __instance.m_EvoBasicGrp.SetActive(false);
        }

        if (__instance.m_ArtistGrp != null)
        {
            __instance.m_ArtistGrp.SetActive(false);
        }

        if (__instance.m_DescriptionGrp != null)
        {
            __instance.m_DescriptionGrp.SetActive(false);
        }

        if (__instance.m_FoilBlendedShowList != null)
        {
            foreach (var img in __instance.m_FoilBlendedShowList)
            {
                if (img != null) img.gameObject.SetActive(false);
            }
        }

        if (__instance.m_FoilDarkenImageList != null)
        {
            foreach (var img in __instance.m_FoilDarkenImageList)
            {
                if (img != null) img.gameObject.SetActive(false);
            }
        }

        if (__instance.m_CenterFoilGlitter != null) __instance.m_CenterFoilGlitter.SetActive(false);
        if (__instance.m_CenterFoilGlitterBtm != null) __instance.m_CenterFoilGlitterBtm.SetActive(false);
        if (__instance.m_BorderFoilGlitter != null) __instance.m_BorderFoilGlitter.SetActive(false);
        if (__instance.m_FadeBarTopImage != null) __instance.m_FadeBarTopImage.gameObject.SetActive(false);
        if (__instance.m_FadeBarBtmImage != null) __instance.m_FadeBarBtmImage.gameObject.SetActive(false);
        if (__instance.m_CardBorderMask != null) __instance.m_CardBorderMask.gameObject.SetActive(false);
        if (__instance.m_CardFullTransparentLayerBGImage != null) __instance.m_CardFullTransparentLayerBGImage.gameObject.SetActive(false);

        CardData cData = __instance.GetCardData();
        bool isGraded = cData != null && cData.cardGrade > 0;

        // Désactivation récursive des composants parasites vanilla (glow, shine, monstres, masques, gradation)
        foreach (Transform t in __instance.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            string n = t.name;

            // Toujours désactiver les résidus vanilla non utilisés par Wankul
            if (n.StartsWith("FadeBar") || n.Contains("Glitter") ||
                n == "StatGrp" || n == "RarityImage" || n == "RarityText" ||
                n == "GlowMask" || n == "LegendaryShine" || n == "MonsterMask" || n == "GlowMaskMonster")
            {
                t.gameObject.SetActive(false);
            }
            // Si la carte n'est pas foil ou n'a pas de masque, désactiver FoilGrp et CameraFoilShine
            else if ((!isFoil || wankulCardData?.SpriteMask == null) && (n == "FoilGrp" || n.StartsWith("CameraFoilShine")))
            {
                t.gameObject.SetActive(false);
            }
            // Si la carte n'est pas gradée, désactiver tous les overlays de gradation
            else if (!isGraded && n.StartsWith("GradedCard") && n != "CardBrightnessControl")
            {
                t.gameObject.SetActive(false);
            }
        }

        // Réinitialiser la luminosité à pleine clarté (alpha = 0 sur m_BrightnessControl)
        if (__instance.m_BrightnessControl != null)
        {
            Color c = __instance.m_BrightnessControl.color;
            c.a = 0f;
            __instance.m_BrightnessControl.color = c;
        }

        __instance.m_RarityImage?.gameObject.SetActive(false);
        __instance.m_NumberText?.gameObject.SetActive(false);
        __instance.m_MonsterNameText?.gameObject.SetActive(false);
        __instance.m_RarityText?.gameObject.SetActive(false);
        __instance.m_Stat1Text?.gameObject.SetActive(false);
        __instance.m_Stat2Text?.gameObject.SetActive(false);
        __instance.m_Stat3Text?.gameObject.SetActive(false);
        __instance.m_Stat4Text?.gameObject.SetActive(false);
        __instance.m_DescriptionText?.gameObject.SetActive(false);
        __instance.m_ArtistText?.gameObject.SetActive(false);
        __instance.m_FirstEditionText?.gameObject.SetActive(false);
    }

    class EnterViewUpCloseState__State
    {
        public bool ready;
        public CardData cardData;
        public WankulCardData wankulCardData;
    }

    static void EnterViewUpCloseStatePrefix(out EnterViewUpCloseState__State __state, CollectionBinderFlipAnimCtrl __instance)
    {
        __state = new EnterViewUpCloseState__State();
        if (!__instance.m_IsHoldingCardCloseUp && (bool)__instance.m_CurrentRaycastedInteractableCard3d)
        {
            __state.ready = true;
            __state.cardData = __instance.m_CurrentRaycastedInteractableCard3d.m_Card3dUI.m_CardUI.GetCardData();
            __state.wankulCardData = WankulCardsData.Instance.GetFromMonster(__state.cardData, true);
        }
        else
        {
            __state.ready = false;
        }
    }

    static void EnterViewUpCloseStatePostfix(EnterViewUpCloseState__State __state, CollectionBinderFlipAnimCtrl __instance)
    {
        if (__state.ready)
        {
            if (__state.wankulCardData != null)
            {
                WankulCardData wankulCardData = __state.wankulCardData;
                CardData inGameCard = __state.cardData;
                ECardExpansionType expansionType = inGameCard.expansionType;
                MonsterData monsterData = InventoryBase.GetMonsterData(inGameCard.monsterType);
                EElementIndex elementIndex = monsterData.ElementIndex;
                ERarity rarity = monsterData.Rarity;

                string key = inGameCard.monsterType.ToString() + "_" + inGameCard.borderType.ToString() + "_" + expansionType.ToString() + "_" + elementIndex.ToString() + "_" + rarity.ToString();

                if (__state.wankulCardData is EffigyCardData effigyCard)
                {
                    __instance.m_CollectionBinderUI.m_CardFullRarityNameText.text = RaritiesContainer.Rarities[effigyCard.Rarity];
                    __instance.m_CollectionBinderUI.m_CardNameText.text = effigyCard.Title + "\n" + effigyCard.Effigy;
                }
                else if (__state.wankulCardData is SpecialCardData)
                {
                    __instance.m_CollectionBinderUI.m_CardFullRarityNameText.text = "SPECIAL";
                    __instance.m_CollectionBinderUI.m_CardNameText.text = wankulCardData.Title;
                }
                else if (__state.wankulCardData is TerrainCardData)
                {
                    __instance.m_CollectionBinderUI.m_CardFullRarityNameText.text = "Terrain";
                    __instance.m_CollectionBinderUI.m_CardNameText.text = wankulCardData.Title;
                }
                else
                {
                    __instance.m_CollectionBinderUI.m_CardFullRarityNameText.text = "Erreur";
                    __instance.m_CollectionBinderUI.m_CardNameText.text = "Erreur";
                }
            }
            else
            {
                Plugin.Logger.LogError("EnterViewUpCloseStatePostfix wankulCardData is null");
            }
        }
    }

    public static bool SetSingleCard(int cardIndex, CardData cardData, int cardCount, ECollectionSortingType sortingType, BinderPageGrp __instance)
    {
        if (cardData == null)
        {
            __instance.m_CardList[cardIndex].SetVisibility(isVisible: false);
            return false;
        }

        bool isGradedMode = (bool)(Plugin.GetPProperty(__instance, "m_IsGradedCardMode") ?? false);
        bool isGraded = cardData.cardGrade > 0 || isGradedMode;
        if (isGraded)
        {
            if (sortingType == ECollectionSortingType.DuplicatePrice && !isGradedMode)
            {
                cardCount--;
            }
            if (cardCount <= 0)
            {
                __instance.m_CardList[cardIndex].SetVisibility(isVisible: false);
                return false;
            }
            __instance.m_CardList[cardIndex].m_CardUI.SetCardUI(cardData);
            if (Plugin.GetPProperty(__instance.m_CardList[cardIndex].m_CardUI, "m_CardUISettingData") != null)
            {
                __instance.m_CardList[cardIndex].m_CardUI.SetFoilMaterialListFromSettingData(isWorldView: false);
                __instance.m_CardList[cardIndex].m_CardUI.SetFoilBlendedMaterialListFromSettingData(isWorldView: false);
            }
            __instance.m_CardList[cardIndex].SetVisibility(isVisible: true);
            __instance.m_CardList[cardIndex].SetCardCountText(cardCount, sortingType == ECollectionSortingType.DuplicatePrice);
            __instance.m_CardList[cardIndex].SetCardCountTextVisibility(isVisible: false);
            return false;
        }

        cardCount = WankulInventory.GetWankulCardFormGameCard(cardData).amount;
        if (sortingType == ECollectionSortingType.DuplicatePrice)
        {
            cardCount--;
        }
        if (cardCount <= 0)
        {
            __instance.m_CardList[cardIndex].SetVisibility(isVisible: false);
            return false;
        }
        __instance.m_CardList[cardIndex].m_CardUI.SetCardUI(cardData);
        if (Plugin.GetPProperty(__instance.m_CardList[cardIndex].m_CardUI, "m_CardUISettingData") != null)
        {
            __instance.m_CardList[cardIndex].m_CardUI.SetFoilMaterialListFromSettingData(isWorldView: false);
            __instance.m_CardList[cardIndex].m_CardUI.SetFoilBlendedMaterialListFromSettingData(isWorldView: false);
        }
        __instance.m_CardList[cardIndex].SetVisibility(isVisible: true);
        __instance.m_CardList[cardIndex].SetCardCountText(cardCount, sortingType == ECollectionSortingType.DuplicatePrice);
        __instance.m_CardList[cardIndex].SetCardCountTextVisibility(isVisible: true);

        return false;
    }

    public static bool CollectionBinderFlipAnimCtrlOnRightMouseButtonUp(CollectionBinderFlipAnimCtrl __instance)
    {
        ECollectionSortingType m_SortingType = (ECollectionSortingType)Plugin.GetPProperty(__instance, "m_SortingType");
        if ((__instance.m_IsHoldingCardCloseUp || __instance.m_IsExitingCardCloseUp) && __instance.m_IsHoldingCardCloseUp)
        {
            _ = __instance.m_IsExitingCardCloseUp;
        }
        if (!__instance.m_CurrentRaycastedInteractableCard3d)
        {
            return false;
        }

        // 1. Sélection de carte pour l'éditeur de Deck
        if (CSingleton<InteractionPlayerController>.Instance.IsSelectingCardForEditDeckScreen())
        {
            bool isGradedAlbum = (bool)(Plugin.GetPProperty(__instance, "m_IsGradedCardAlbum") ?? false);
            if (isGradedAlbum)
            {
                return false;
            }
            CardData selectedCard = __instance.m_CurrentRaycastedInteractableCard3d.m_Card3dUI.m_CardUI.GetCardData();
            CSingleton<InteractionPlayerController>.Instance.UpdateSelectedCardData(selectedCard);
            CPlayerData.ReduceCard(selectedCard, 1);
            CSingleton<InteractionPlayerController>.Instance.ExitViewCardAlbumMode();
            return false;
        }

        // 2. Sélection de carte pour la Boîte de dons (Bulk Donation Box)
        if (CSingleton<InteractionPlayerController>.Instance.IsSelectingCardForBulkDonationBoxScreen())
        {
            CardData selectedCard = __instance.m_CurrentRaycastedInteractableCard3d.m_Card3dUI.m_CardUI.GetCardData();
            bool isGradedAlbum = (bool)(Plugin.GetPProperty(__instance, "m_IsGradedCardAlbum") ?? false);
            if (isGradedAlbum)
            {
                if (!CSingleton<InteractionPlayerController>.Instance.CanSelectGradedCardForBulkDonationBoxScreen())
                {
                    return false;
                }
                CSingleton<InteractionPlayerController>.Instance.UpdateSelectedCardData(selectedCard);
                CPlayerData.RemoveGradedCard(selectedCard, false);
                CSingleton<InteractionPlayerController>.Instance.ExitViewCardAlbumMode();
                Plugin.SetPProperty(__instance, "m_CanUpdateSort", true);
                return false;
            }
            CSingleton<InteractionPlayerController>.Instance.UpdateSelectedCardData(selectedCard);
            CPlayerData.ReduceCard(selectedCard, 1);
            CSingleton<InteractionPlayerController>.Instance.ExitViewCardAlbumMode();
            return false;
        }

        // 3. Sélection de carte pour l'application de Gradation (Téléphone)
        if (CSingleton<InteractionPlayerController>.Instance.IsSelectingCardForGradingScreen())
        {
            CardData selectedCard = __instance.m_CurrentRaycastedInteractableCard3d.m_Card3dUI.m_CardUI.GetCardData();
            CSingleton<InteractionPlayerController>.Instance.UpdateSelectedCardData(selectedCard);
            bool isGradedAlbum = (bool)(Plugin.GetPProperty(__instance, "m_IsGradedCardAlbum") ?? false);
            if (isGradedAlbum)
            {
                CPlayerData.RemoveGradedCard(selectedCard, false);
                Plugin.SetPProperty(__instance, "m_CanUpdateSort", true);
            }
            else
            {
                CPlayerData.ReduceCard(selectedCard, 1);
            }
            CSingleton<InteractionPlayerController>.Instance.ExitViewCardAlbumMode();
            return false;
        }

        if (!InteractionPlayerController.HasEnoughSlotToHoldCard())
        {
            NotEnoughResourceTextPopup.ShowText(ENotEnoughResourceText.HandFull);
            return false;
        }
        Card3dUIGroup cardUI = CSingleton<Card3dUISpawner>.Instance.GetCardUI();
        __instance.m_CurrentSpawnedInteractableCard3d = ShelfManager.SpawnInteractableObject(EObjectType.Card3d).GetComponent<InteractableCard3d>();
        cardUI.m_IgnoreCulling = true;
        cardUI.m_CardUI.SetFoilCullListVisibility(isActive: true);
        cardUI.m_CardUI.ResetFarDistanceCull();
        cardUI.m_CardUI.SetFoilMaterialList(CSingleton<Card3dUISpawner>.Instance.m_FoilMaterialWorldView);
        cardUI.m_CardUI.SetFoilBlendedMaterialList(CSingleton<Card3dUISpawner>.Instance.m_FoilBlendedMaterialWorldView);
        cardUI.m_CardUI.SetCardUI(__instance.m_CurrentRaycastedInteractableCard3d.m_Card3dUI.m_CardUI.GetCardData());
        cardUI.transform.position = __instance.m_CurrentRaycastedInteractableCard3d.transform.position;
        cardUI.transform.rotation = __instance.m_CurrentRaycastedInteractableCard3d.transform.rotation;
        __instance.m_CurrentSpawnedInteractableCard3d.transform.position = __instance.m_CurrentRaycastedInteractableCard3d.transform.position;
        __instance.m_CurrentSpawnedInteractableCard3d.transform.rotation = __instance.m_CurrentRaycastedInteractableCard3d.transform.rotation;
        __instance.m_CurrentSpawnedInteractableCard3d.SetCardUIFollow(cardUI);
        __instance.m_CurrentSpawnedInteractableCard3d.SetEnableCollision(isEnable: false);
        CardData cardData = __instance.m_CurrentRaycastedInteractableCard3d.m_Card3dUI.m_CardUI.GetCardData();

        // Même logique que la méthode vanilla : RemoveGradedCard pour les albums gradés, ReduceCard sinon
        bool isGradedAlbumPickup = (bool)(Plugin.GetPProperty(__instance, "m_IsGradedCardAlbum") ?? false);
        if (isGradedAlbumPickup)
        {
            CPlayerData.RemoveGradedCard(cardData, false);
            Plugin.SetPProperty(__instance, "m_CanUpdateSort", true);
        }
        else
        {
            CPlayerData.ReduceCard(cardData, 1);
        }

        // Utiliser CPlayerData.GetCardAmount (source de vérité vanilla) pour le count affiché dans le slot
        int count = CPlayerData.GetCardAmount(cardData);
        // Pour les albums gradés, la carte est retirée définitivement : forcer count=0 comme le fait vanilla
        if (isGradedAlbumPickup)
        {
            count = 0;
        }

        int m_CurrentRaycastedCardIndex = (int)Plugin.GetPProperty(__instance, "m_CurrentRaycastedCardIndex");
        __instance.m_BinderPageGrpList[0].SetSingleCard(m_CurrentRaycastedCardIndex, cardData, count, m_SortingType);
        if (count <= 0)
        {
            __instance.m_CurrentRaycastedInteractableCard3d.m_Card3dUI.gameObject.SetActive(value: false);
            __instance.m_CurrentRaycastedInteractableCard3d.gameObject.SetActive(value: false);
            __instance.m_CurrentRaycastedInteractableCard3d.OnRaycastEnded();
            __instance.m_CurrentRaycastedInteractableCard3d = null;
        }
        InteractionPlayerController.AddHoldCard(__instance.m_CurrentSpawnedInteractableCard3d);
        InteractionPlayerController.RemoveToolTip(EGameAction.ViewAlbumCard);

        return false;
    }

    public static bool GetIcon(ECardExpansionType cardExpansionType, MonsterData __instance, ref Sprite __result)
    {
        
        WankulCardData aJETER = WankulCardsData.GetAJETER();
        if (aJETER?.Sprite != null)
        {
            __result = (Sprite)aJETER.Sprite;
            return false;
        }
        
        // Si aucun sprite custom n'est disponible, laisser la méthode vanilla s'exécuter
        return true;
    }
}
