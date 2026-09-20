using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;
using WankulCrazyPlugin.inventory;
using WankulCrazyPlugin.utils;
using static UnityEngine.GraphicsBuffer;

namespace WankulCrazyPlugin.patch
{
    public class CardOpening
    {
        public static int totalExpGained = 0;
        public static List<int> LegendaryBoosters = new List<int>();
        public static List<int> URBoosters = new List<int>();
        public static List<int> RareBoosters = new List<int>();
        public static int boosterSize = 10;
        private static readonly int randomGoldBoosterSeedBase = 10;
        private static int randomGoldBoosterSeed = 10;

        public static void UpdatePreFix(ref List<CardData> ___m_RolledCardDataList, CardOpeningSequence __instance)
        {
            if (__instance.m_StateIndex == 11 && totalExpGained > 0)
            {
                ___m_RolledCardDataList.Clear();
                CEventManager.QueueEvent(new CEventPlayer_AddShopExp(totalExpGained));
                totalExpGained = 0;
            }
        }

        public static void OpenScreenPrefix(CardOpeningSequence __instance, ECollectionPackType collectionPackType, bool isMultiPack)
        {
            try
            {
                CheckBoosterSize(__instance);
                Plugin.Logger.LogInfo($"[CardOpening] === NOUVEAU BOOSTER OUVERT === boosterSize={boosterSize}, Card3dUIList.Count={__instance.m_Card3dUIList.Count}, CardAnimList.Count={__instance.m_CardAnimList.Count}, ShowAllCardPosList.Count={__instance.m_ShowAllCardPosList.Count}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[OpenScreenPrefix] Erreur: {ex}");
            }
        }

        /// <summary>
        /// Affiche uniquement la carte active et celle juste derrière (activeIndex+1).
        /// Toutes les autres cartes sont désactivées (SetActive false).
        /// 
        /// Règle Unity Canvas : le dernier enfant dans la hiérarchie est rendu PAR-DESSUS les autres.
        /// On pousse donc d'abord la carte « derrière », puis la carte active EN DERNIER
        /// pour garantir qu'elle reste au premier plan, même si elle a été ajoutée après
        /// les cartes 0-7 (ce qui causerait sinon un bug d'ordre de rendu).
        /// </summary>
        private static void ShowCardStack(CardOpeningSequence __instance, int activeIndex)
        {
            if (__instance.m_Card3dUIList == null) return;

            int count = __instance.m_Card3dUIList.Count;

            // 1. Désactiver toutes les cartes qui ne sont ni l'active ni celle de derrière
            for (int i = 0; i < count; i++)
            {
                if (__instance.m_Card3dUIList[i] == null) continue;
                bool shouldBeActive = (i == activeIndex) || (i == activeIndex + 1 && i < boosterSize);
                __instance.m_Card3dUIList[i].gameObject.SetActive(shouldBeActive);
            }

            // 2. Ordonnancement strict dans la hiérarchie CanvasWorldspace :
            // Dans Unity UI / Canvas, le dernier enfant (LastSibling) est rendu PAR-DESSUS tous les autres.
            // On s'assure donc que la carte de derrière est dessinée avant, et la carte active EN DERNIER (au premier plan).
            if (activeIndex + 1 < count && activeIndex + 1 < boosterSize && __instance.m_Card3dUIList[activeIndex + 1] != null)
            {
                __instance.m_Card3dUIList[activeIndex + 1].transform.SetAsLastSibling();
            }

            if (activeIndex < count && __instance.m_Card3dUIList[activeIndex] != null)
            {
                __instance.m_Card3dUIList[activeIndex].transform.SetAsLastSibling();
            }
        }

        public static void CheckBoosterSize(CardOpeningSequence __instance)
        {
            Item currentItem = (Item)Plugin.GetPProperty(__instance, "m_CurrentItem");
            if (currentItem == null) {
                return;
            }

            if (currentItem.GetItemType() == EnumExtensions.SafeParseEItemType("BoosterGoldBattle") || currentItem.GetItemType() == EnumExtensions.SafeParseEItemType("BoosterGoldStellar"))
            {
                boosterSize = 4;
            }
            else { 
                boosterSize = 10;
            }

            if (__instance.m_Card3dUIList.Count < boosterSize)
            {
                // Utilisation de l'instance singleton de Card3dUISpawner
                Card3dUISpawner card3dUISpawnerInstance = Card3dUISpawner.m_Instance;

                if (card3dUISpawnerInstance == null)
                {
                    Plugin.Logger.LogError("Card3dUISpawner instance is null.");
                    return;
                }

                MethodInfo addCardPrefabMethod = typeof(Card3dUISpawner).GetMethod("AddCardPrefab", BindingFlags.Instance | BindingFlags.NonPublic);

                if (addCardPrefabMethod == null)
                {
                    Plugin.Logger.LogError("Failed to get AddCardPrefab method.");
                    return;
                }

                int cardsToAdd = boosterSize - __instance.m_Card3dUIList.Count;

                // Mesurer le décalage Z réel entre les cartes d'origine
                float zStep = 0.0001f;
                if (__instance.m_Card3dUIList.Count >= 2 && __instance.m_Card3dUIList[0] != null && __instance.m_Card3dUIList[1] != null)
                {
                    float dZ = __instance.m_Card3dUIList[1].transform.localPosition.z - __instance.m_Card3dUIList[0].transform.localPosition.z;
                    if (Mathf.Abs(dZ) > 0.0000001f)
                    {
                        zStep = dZ;
                    }
                }

                for (int i = 0; i < cardsToAdd; i++)
                {
                    Transform CardOpeningSequence_WorldUIGrp_Transform = Plugin.GetByPathIn("CanvasWorldspace", "CanvasGrp/CardOpeningSequence_WorldUIGrp/CardOpeningGrp");

                    Card3dUIGroup lastCard3dUIGroup = __instance.m_Card3dUIList[__instance.m_Card3dUIList.Count - 1];
                    Card3dUIGroup newCard3dUIGroup = Card3dUISpawner.m_Instance.GetCardUI();
                    newCard3dUIGroup.gameObject.SetActive(false);
                    newCard3dUIGroup.transform.SetParent(CardOpeningSequence_WorldUIGrp_Transform, false);
                    newCard3dUIGroup.transform.rotation = lastCard3dUIGroup.transform.rotation;
                    newCard3dUIGroup.transform.localScale = lastCard3dUIGroup.transform.localScale;

                    Vector3 newLocalPos = lastCard3dUIGroup.transform.localPosition;
                    newLocalPos.z += zStep;
                    newCard3dUIGroup.transform.localPosition = newLocalPos;

                    __instance.m_Card3dUIList.Add(newCard3dUIGroup);

                    Transform AnimGrp_Transform = Plugin.FindChildByPath(newCard3dUIGroup.transform, "AnimGrp");
                    Transform cleanAnimGrp_Transform = Plugin.FindChildByPath(__instance.m_Card3dUIList[0].transform, "AnimGrp");

                    Animation AnimGrp_Animation = AnimGrp_Transform.GetComponent<Animation>();
                    if (AnimGrp_Animation == null)
                    {
                        AnimGrp_Animation = AnimGrp_Transform.gameObject.AddComponent<Animation>();
                    }
                    AnimGrp_Animation.playAutomatically = false;
                    AnimGrp_Animation.Stop();
                    AnimGrp_Transform.localPosition = Vector3.zero;
                    AnimGrp_Transform.localRotation = Quaternion.identity;

                    AnimationCopier.CopyAnimation(cleanAnimGrp_Transform.gameObject, AnimGrp_Transform.gameObject, "OpenCardNewCard");
                    AnimationCopier.CopyAnimation(cleanAnimGrp_Transform.gameObject, AnimGrp_Transform.gameObject, "OpenCardSlideExit");
                    AnimationCopier.CopyAnimation(cleanAnimGrp_Transform.gameObject, AnimGrp_Transform.gameObject, "OpenCardFinalReveal");
                    AnimationCopier.CopyAnimation(cleanAnimGrp_Transform.gameObject, AnimGrp_Transform.gameObject, "OpenCardDefaultPos");

                    AnimGrp_Animation.Play("OpenCardDefaultPos");

                    __instance.m_CardAnimList.Add(AnimGrp_Animation);

                    Transform ShowAllCardPosList_Transform = Plugin.GetByPathIn("CanvasWorldspace", "CanvasGrp/CardOpeningSequence_WorldUIGrp/ShowAllCardPosList");

                    RectTransform existingPos = (RectTransform)__instance.m_ShowAllCardPosList[__instance.m_ShowAllCardPosList.Count - 1];
                    GameObject newGameObject = new GameObject($"ShowAllCardPos ({__instance.m_ShowAllCardPosList.Count + 1})");
                    RectTransform newPos = newGameObject.AddComponent<RectTransform>();
                    newPos.SetParent(ShowAllCardPosList_Transform, false);
                    newPos.localRotation = existingPos.localRotation;
                    newPos.localScale = existingPos.localScale;
                    newPos.localPosition = existingPos.localPosition;
                    newPos.anchorMin = existingPos.anchorMin;
                    newPos.anchorMax = existingPos.anchorMax;
                    newPos.pivot = existingPos.pivot;
                    newPos.sizeDelta = existingPos.sizeDelta;
                    newPos.gameObject.SetActive(true);

                    __instance.m_ShowAllCardPosList.Add(newPos);
                }

                for (int i = 0; i < __instance.m_ShowAllCardPosList.Count; i++)
                {
                    RectTransform rectTransform = (RectTransform)__instance.m_ShowAllCardPosList[i];
                    float t = (float)i / (__instance.m_ShowAllCardPosList.Count - 1); // Interpolation linéaire
                    float xPosition = Mathf.Lerp(-0.1f, 0.11f, t);
                    Vector3 localPosition = rectTransform.localPosition;
                    localPosition.x = xPosition;
                    rectTransform.localPosition = localPosition;
                }
            } else if (__instance.m_Card3dUIList.Count > boosterSize)
            {
                int cardsToRemove = __instance.m_Card3dUIList.Count - boosterSize;

                for (int i = 0; i < cardsToRemove; i++)
                {
                    Card3dUIGroup card3dUIGroup = __instance.m_Card3dUIList[__instance.m_Card3dUIList.Count - 1];
                    __instance.m_Card3dUIList.RemoveAt(__instance.m_Card3dUIList.Count - 1);
                    InventoryBase.Destroy(card3dUIGroup.gameObject);

                    RectTransform rectTransform = (RectTransform)__instance.m_ShowAllCardPosList[__instance.m_ShowAllCardPosList.Count - 1];
                    __instance.m_ShowAllCardPosList.RemoveAt(__instance.m_ShowAllCardPosList.Count - 1);
                    InventoryBase.Destroy(rectTransform.gameObject);

                    __instance.m_CardAnimList.RemoveAt(__instance.m_CardAnimList.Count - 1);
                }

                for (int i = 0; i < __instance.m_ShowAllCardPosList.Count; i++)
                {
                    RectTransform rectTransform = (RectTransform)__instance.m_ShowAllCardPosList[i];
                    float t = (float)i / (__instance.m_ShowAllCardPosList.Count - 1); // Interpolation linéaire
                    float xPosition = Mathf.Lerp(-0.1f, 0.11f, t);
                    Vector3 localPosition = rectTransform.localPosition;
                    localPosition.x = xPosition;
                    rectTransform.localPosition = localPosition;
                }

            }

        }

        public static void OpenBooster(List<CardData> ___m_RolledCardDataList, List<float> ___m_CardValueList, ECollectionPackType ___m_CollectionPackType, Item ___m_CurrentItem, List<CardData> ___m_SecondaryRolledCardDataList, CardOpeningSequence __instance)
        {
            if (SavesManager.DebuggingSave)
            {
                return;
            }

            WankulCardsData wankulCardsData = WankulCardsData.Instance;
            ___m_CardValueList.Clear();
            ___m_RolledCardDataList.Clear();
            ___m_SecondaryRolledCardDataList.Clear();

            totalExpGained = 0;

            List<WankulCardData> alreadySelectedCards = new List<WankulCardData>();

            for (int i = 0; i < boosterSize; i++)
            {
                bool isTerrain = i == 0;
                bool isMinRare = i == boosterSize - 1;
                bool isRare = false;
                bool isMinUR = false;
                bool isMinLegendary = false;
                int hash = ___m_CurrentItem.GetHashCode();

                if (isMinRare)
                {
                    foreach (int boosterHash in LegendaryBoosters)
                    {
                        if (boosterHash == hash)
                        {
                            LegendaryBoosters.Remove(boosterHash);
                            isMinLegendary = true;
                            isMinUR = false;
                            isMinRare = false;
                            break;
                        }
                    }

                    foreach (int boosterHash in URBoosters)
                    {
                        if (boosterHash == hash)
                        {
                            URBoosters.Remove(boosterHash);
                            isMinLegendary = false;
                            isMinUR = true;
                            isMinRare = false;
                            break;
                        }
                    }

                    foreach (int boosterHash in RareBoosters)
                    {
                        if (boosterHash == hash)
                        {
                            RareBoosters.Remove(boosterHash);
                            isMinLegendary = false;
                            isMinUR = false;
                            isMinRare = false;
                            isRare = true;
                            break;
                        }
                    }
                }

                Item currentItem = (Item)Plugin.GetPProperty(__instance, "m_CurrentItem");
                WankulCardData wankulCard;
                if (currentItem.GetItemType() == EnumExtensions.SafeParseEItemType("BoosterGoldBattle") || currentItem.GetItemType() == EnumExtensions.SafeParseEItemType("BoosterGoldStellar") && boosterSize == 4)
                {
                    wankulCard = WankulInventory.DropCardGold(___m_CollectionPackType, alreadySelectedCards);
                }
                else
                {
                    wankulCard = WankulInventory.DropCard(___m_CollectionPackType, alreadySelectedCards, isTerrain, isMinRare, isMinUR, isMinLegendary, isRare);
                }
                CardData associatedCard = wankulCardsData.GetCardDataFromWankulCardData(wankulCard);

                if (associatedCard == null)
                {
                    associatedCard = wankulCardsData.GetUnassciatedCardData();
                    ___m_RolledCardDataList.Add(associatedCard);

                    associatedCard.isFoil = false;
                    associatedCard.isChampionCard = false;

                    // Vérification de la rareté pour décider si la carte est foil
                    if (wankulCard is EffigyCardData)
                    {
                        EffigyCardData effigyCard = (EffigyCardData)wankulCard;

                        // Si la carte a une rareté de UR1 ou plus, elle devient foil
                        if (effigyCard.Rarity >= Rarity.UR1)
                        {
                            associatedCard.isFoil = true;
                        }
                    }

                    wankulCardsData.SetFromMonster(associatedCard, wankulCard);
                }
                else
                {
                    ___m_RolledCardDataList.Add(associatedCard);
                }

                if (WankulInventory.isNewWankulCard(wankulCard))
                {
                    ((List<CardData>)Plugin.GetPProperty(__instance, "m_RolledCardDataList"))[i].isNew = true;
                    if (CSingleton<CGameManager>.Instance.m_OpenPackShowNewCard)
                    {
                        ((List<bool>)Plugin.GetPProperty(__instance, "m_IsNewlList")).Add(item: true);
                    }
                    else
                    {
                        ((List<bool>)Plugin.GetPProperty(__instance, "m_IsNewlList")).Add(item: false);
                    }
                }
                else
                {
                    ((List<CardData>)Plugin.GetPProperty(__instance, "m_RolledCardDataList"))[i].isNew = false;
                    ((List<bool>)Plugin.GetPProperty(__instance, "m_IsNewlList")).Add(item: false);
                }

                // Calcul de l'XP gagnée
                totalExpGained += WankulCardsData.GetExperienceFromWankulCard(wankulCard);
                // Ajout de la valeur de la carte dans la liste des prix
                ___m_CardValueList.Add(wankulCard.MarketPrice);
            }

            Plugin.Logger.LogInfo($"[CardOpening] Booster généré: {___m_RolledCardDataList.Count} cartes tirées (boosterSize={boosterSize})");
            for (int k = 0; k < ___m_RolledCardDataList.Count; k++)
            {
                WankulCardData wk = wankulCardsData.GetFromMonster(___m_RolledCardDataList[k], true);
                string cardTitle = wk != null ? wk.Title : "Inconnue";
                Plugin.Logger.LogInfo($"  [Tirage Carte {k}] Nom='{cardTitle}' Prix={___m_CardValueList[k]} isNew={((List<bool>)Plugin.GetPProperty(__instance, "m_IsNewlList"))[k]}");

                // Affecter explicitement les données de la carte sur l'objet 3D correspondant pour garantir son visuel dès le début
                if (k < __instance.m_Card3dUIList.Count && __instance.m_Card3dUIList[k] != null && __instance.m_Card3dUIList[k].m_CardUI != null)
                {
                    __instance.m_Card3dUIList[k].m_CardUI.SetCardUI(___m_RolledCardDataList[k]);
                }
            }
        }

        public class EvaluateOpenCardPack__State
        {
            public bool CanOpenCardBox;
        }

        public static void EvaluateOpenCardPackPreFix(out EvaluateOpenCardPack__State __state, InteractionPlayerController __instance)
        {
            __state = new EvaluateOpenCardPack__State();
            if (__instance.CanOpenCardBox())
            {
                __state.CanOpenCardBox = true;
            }
            else
            {
                __state.CanOpenCardBox = false;
            }
        }

        public static void EvaluateOpenCardPackPostFix(EvaluateOpenCardPack__State __state, InteractionPlayerController __instance)
        {
            if (__state.CanOpenCardBox)
            {
                LegendaryBoosters.Clear();
                URBoosters.Clear();
                RareBoosters.Clear();

                bool shouldGenGoldBooster = false;
                int boosterGoldIndex = -1;

                List<Item> m_HoldItemList = (List<Item>)AccessTools.Field(__instance.GetType(), "m_HoldItemList").GetValue(__instance);
                List<int>availableHash = new List<int>();

                ECollectionPackType collectionPackType = InventoryBase.ItemTypeToCollectionPackType(m_HoldItemList[0].GetItemType());
                if (
                    collectionPackType == ECollectionPackType.EpicCardPack ||
                    collectionPackType == ECollectionPackType.DestinyEpicCardPack ||
                    collectionPackType == EnumExtensions.SafeParseECollectionPackType("Stellar") ||
                    collectionPackType == EnumExtensions.SafeParseECollectionPackType("StellarTaux")
                )
                {
                    int random = UnityEngine.Random.Range(0, randomGoldBoosterSeed);
                    //Plugin.Logger.LogInfo($"Random Gold booster: {random}");
                    //Plugin.Logger.LogInfo($"Random Gold booster seed: {randomGoldBoosterSeed}");
                    shouldGenGoldBooster = random == 0;
                }

                if (shouldGenGoldBooster)
                {
                    boosterGoldIndex = UnityEngine.Random.RandomRangeInt(0, m_HoldItemList.Count);
                    randomGoldBoosterSeed = randomGoldBoosterSeedBase;
                }
                else
                {
                    randomGoldBoosterSeed--;
                }

                for (int i = 0; i < m_HoldItemList.Count; i++)
                {
                    Item item = m_HoldItemList[i];
                    if (shouldGenGoldBooster && i == boosterGoldIndex)
                    {
                        if (collectionPackType == ECollectionPackType.EpicCardPack || collectionPackType == ECollectionPackType.DestinyEpicCardPack)
                        {
                            Plugin.SetPProperty(item, "m_ItemType", EnumExtensions.SafeParseEItemType("BoosterGoldBattle"));

                            ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(item.GetItemType());
                            item.SetMesh(itemMeshData.mesh, itemMeshData.material, EnumExtensions.SafeParseEItemType("BoosterGoldBattle"));
                        }
                        else if (collectionPackType == EnumExtensions.SafeParseECollectionPackType("Stellar") || collectionPackType == EnumExtensions.SafeParseECollectionPackType("StellarTaux"))
                        {
                            Plugin.SetPProperty(item, "m_ItemType", EnumExtensions.SafeParseEItemType("BoosterGoldStellar"));

                            ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(item.GetItemType());
                            item.SetMesh(itemMeshData.mesh, itemMeshData.material, EnumExtensions.SafeParseEItemType("BoosterGoldStellar"));
                        }
                    }
                    else { 
                        availableHash.Add(item.GetHashCode());
                    }
                }

                int randomHash = availableHash[UnityEngine.Random.RandomRangeInt(0, availableHash.Count)];
                availableHash.Remove(randomHash);
                LegendaryBoosters.Add(randomHash);

                for (int i = 0; i < 11; i++)
                {
                    int urrandomHash = availableHash[UnityEngine.Random.RandomRangeInt(0, availableHash.Count)];
                    availableHash.Remove(urrandomHash);
                    URBoosters.Add(urrandomHash);
                }

                for (int i = 0; i < availableHash.Count; i++)
                {
                    int rarerandomHash = availableHash[i];
                    RareBoosters.Add(rarerandomHash);
                }
            }
        }

        private static IEnumerator DelayToState(int stateIndex, float delayTime, CardOpeningSequence __instance)
        {
            __instance.m_StateIndex = -1;
            yield return new WaitForSeconds(delayTime);
            __instance.m_StateIndex = stateIndex;
        }

        /// <summary>
        /// Remplace totalement la méthode Update() du jeu (Harmony prefix retournant false).
        /// Gère deux grandes phases :
        ///   1. ReadyingToOpen : le joueur tient le booster, animation d'approche + annulation possible
        ///   2. States 0-12   : séquence d'ouverture carte par carte
        ///
        /// Les propriétés privées du jeu sont accédées via <see cref="CardOpeningHelpers"/>
        /// pour éviter les strings magiques dans Plugin.GetPProperty / SetPProperty.
        /// </summary>
        public static bool Update(CardOpeningSequence __instance)
        {
            // Performance Optimization: Removed per-frame CheckBoosterSize(__instance) call.
            // CheckBoosterSize performs Reflection and string/enum parsing. Since booster size is constant
            // for the duration of a pack opening sequence, it is initialized once in OpenScreenPrefix.

            MethodInfo InitOpenSequence = __instance.GetType().GetMethod("InitOpenSequence", BindingFlags.Instance | BindingFlags.NonPublic);

            // Réinitialise le flag auto-fire à chaque frame ; il sera re-levé plus bas si besoin
            CardOpeningHelpers.SetIsAutoFire(__instance, false);

            if (!CardOpeningHelpers.GetIsScreenActive(__instance))
            {
                return false;
            }

            if (InputManager.GetKeyDownAction(EGameAction.OpenPack))
            {
                CardOpeningHelpers.SetIsAutoFireKeydown(__instance, true);
            }

            if (InputManager.GetKeyUpAction(EGameAction.OpenPack))
            {
                CardOpeningHelpers.SetIsAutoFireKeydown(__instance, false);
            }

            // --- Auto-fire : si le joueur maintient la touche, déclenche un "feu rapide" ---
            // Un timer accumule le temps d'appui. Dès 0.05s : IsAutoFire = true (avance vite).
            // Quand le joueur relâche (AutoFireTimer > 0 mais KeyDown = false), on fire une
            // dernière fois pour valider la carte en cours avant de s'arrêter.
            if (CardOpeningHelpers.GetIsAutoFireKeydown(__instance))
            {
                CardOpeningHelpers.SetAutoFireTimer(__instance, CardOpeningHelpers.GetAutoFireTimer(__instance) + Time.deltaTime);
                if (CardOpeningHelpers.GetAutoFireTimer(__instance) >= 0.05f)
                {
                    CardOpeningHelpers.SetAutoFireTimer(__instance, 0f);
                    CardOpeningHelpers.SetIsAutoFire(__instance, true);
                }
            }
            else if (CardOpeningHelpers.GetAutoFireTimer(__instance) > 0f)
            {
                CardOpeningHelpers.SetAutoFireTimer(__instance, 0f);
                CardOpeningHelpers.SetIsAutoFire(__instance, true);
            }

            // ═══════════════════════════════════════════════════════════
            // PHASE 1 — ReadyingToOpen
            // Le joueur tient le booster devant lui, avant de l'ouvrir.
            // On gère : l'animation d'approche (lerp), l'annulation (Cancel),
            // et le déclenchement de l'ouverture via auto-fire.
            // ═══════════════════════════════════════════════════════════
            if (CardOpeningHelpers.GetIsReadyingToOpen(__instance))
            {
                if (!CardOpeningHelpers.GetIsReadyToOpen(__instance))
                {
                    if (CardOpeningHelpers.GetIsCanceling(__instance))
                    {
                        CardOpeningHelpers.SetLerpPosTimer(__instance, CardOpeningHelpers.GetLerpPosTimer(__instance) - Time.deltaTime * CardOpeningHelpers.GetLerpPosSpeed(__instance));
                        if (CardOpeningHelpers.GetLerpPosTimer(__instance) < 0f)
                        {
                            CardOpeningHelpers.SetLerpPosTimer(__instance, 0f);
                            CardOpeningHelpers.SetIsReadyToOpen(__instance, false);
                            CardOpeningHelpers.SetIsReadyingToOpen(__instance, false);
                            CardOpeningHelpers.SetIsCanceling(__instance, false);
                            CardOpeningHelpers.SetIsScreenActive(__instance, false);
                            __instance.m_CardPackAnimator.gameObject.SetActive(value: false);
                            CSingleton<InteractionPlayerController>.Instance.ExitLockMoveMode();
                            CSingleton<InteractionPlayerController>.Instance.OnExitOpenPackState();
                            InteractionPlayerController.RestoreHiddenToolTip();
                            CardOpeningHelpers.GetCurrentItem(__instance).gameObject.SetActive(value: true);
                            InteractionPlayerController.SetAllHoldItemVisibility(isVisible: true);
                            CardOpeningHelpers.SetCurrentItem(__instance, null);
                            TutorialManager.SetGameUIVisible(isVisible: true);
                            CenterDot.SetVisibility(isVisible: true);
                            GameUIScreen.ResetEnterGoNextDayIndicatorVisible();
                        }
                    }
                    else
                    {
                        CardOpeningHelpers.SetLerpPosTimer(__instance, CardOpeningHelpers.GetLerpPosTimer(__instance) + Time.deltaTime * CardOpeningHelpers.GetLerpPosSpeed(__instance));
                        if (CardOpeningHelpers.GetLerpPosTimer(__instance) > 1f)
                        {
                            CardOpeningHelpers.SetLerpPosTimer(__instance, 1f);
                            CardOpeningHelpers.SetIsReadyToOpen(__instance, true);
                        }
                    }

                    float lerpT = CardOpeningHelpers.GetLerpPosTimer(__instance);
                    __instance.m_CardPackAnimator.transform.localPosition = Vector3.Lerp(__instance.m_StartLerpTransform.localPosition, Vector3.zero, lerpT);
                    __instance.m_CardPackAnimator.transform.localRotation = Quaternion.Lerp(__instance.m_StartLerpTransform.localRotation, Quaternion.identity, lerpT);
                    __instance.m_CardPackAnimator.transform.localScale = Vector3.Lerp(__instance.m_StartLerpTransform.localScale, Vector3.one, lerpT);
                }
                else if (CardOpeningHelpers.GetIsAutoFire(__instance))
                {
                    CardOpeningHelpers.SetIsReadyingToOpen(__instance, false);
                    ECollectionPackType collectionPackType = InventoryBase.ItemTypeToCollectionPackType(CardOpeningHelpers.GetCurrentItem(__instance).GetItemType());
                    __instance.OpenScreen(collectionPackType, false);
                }
                else if (InputManager.GetKeyDownAction(EGameAction.CancelOpenPack) && !CardOpeningHelpers.GetIsCanceling(__instance))
                {
                    CSingleton<InteractionPlayerController>.Instance.AddHoldItemToFront(CardOpeningHelpers.GetCurrentItem(__instance));
                    CardOpeningHelpers.SetIsCanceling(__instance, true);
                    CardOpeningHelpers.SetIsReadyToOpen(__instance, false);
                    CSingleton<InteractionPlayerController>.Instance.m_BlackBGWorldUIFade.SetFadeOut(3f);
                    InteractionPlayerController.RestoreHiddenToolTip();
                    CSingleton<InteractionPlayerController>.Instance.m_CameraFOVController.StopLerpFOV();
                    SoundManager.GenericPop(1f, 0.9f);
                }
            }
            else
            {
                // ═══════════════════════════════════════════════════════════
                // PHASE 2 — Machine à états d'ouverture (States 0 → 12)
                // ═══════════════════════════════════════════════════════════
                if (!CardOpeningHelpers.GetIsScreenActive(__instance))
                {
                    return false;
                }

                // State 0 : initialisation de la séquence (remet les positions/rotations à zéro)
                if (__instance.m_StateIndex == 0)
                {
                    InitOpenSequence.Invoke(__instance, []);
                    __instance.m_StateIndex++;
                }
                // State 1 : le joueur appuie (ou maintient) pour ouvrir le booster.
                // Animation PackOpenAnim de 0 → 0.3. Quand 0.3 atteint : booster ouvert, VFX + sons.
                else if (__instance.m_StateIndex == 1)
                {
                    CardOpeningHelpers.SetStateTimer(__instance, CardOpeningHelpers.GetStateTimer(__instance) + Time.deltaTime * CardOpeningHelpers.GetMultiplierStateTimer(__instance));
                    if (CardOpeningHelpers.GetStateTimer(__instance) > 0.05f)
                    {
                        CardOpeningHelpers.SetStateTimer(__instance, 0f);
                        int tempIdx = CardOpeningHelpers.GetTempIndex(__instance);
                        if (tempIdx < __instance.m_Card3dUIList.Count)
                        {
                            __instance.m_Card3dUIList[tempIdx].gameObject.SetActive(value: true);
                            CardOpeningHelpers.SetTempIndex(__instance, tempIdx + 1);
                        }
                    }

                    if (CardOpeningHelpers.GetIsAutoFire(__instance) || CardOpeningHelpers.GetIsAutoFireKeydown(__instance) || CSingleton<CGameManager>.Instance.m_OpenPacAutoNextCard)
                    {
                        CardOpeningHelpers.SetSlider(__instance, CardOpeningHelpers.GetSlider(__instance) + 0.0065f * CardOpeningHelpers.GetMultiplierStateTimer(__instance));
                        __instance.m_CardPackAnimator.Play("PackOpenAnim", -1, CardOpeningHelpers.GetSlider(__instance));
                        if (CardOpeningHelpers.GetSlider(__instance) >= 0.3f)
                        {
                            __instance.m_OpenPackVFX.Play();
                            SoundManager.PlayAudio("SFX_OpenPack", 0.6f);
                            SoundManager.PlayAudio("SFX_BoxOpen", 0.5f);
                            __instance.m_StateIndex++;
                        }
                    }
                }
                // State 2 : animation auto de 0 → 1 (fin de l'ouverture du booster).
                // Dès que le slider atteint 1 : on passe à State 3 et on prépare la première carte.
                else if (__instance.m_StateIndex == 2)
                {
                    CardOpeningHelpers.SetSlider(__instance, CardOpeningHelpers.GetSlider(__instance) + Time.deltaTime * 1f * CardOpeningHelpers.GetMultiplierStateTimer(__instance));
                    __instance.m_CardPackAnimator.Play("PackOpenAnim", -1, CardOpeningHelpers.GetSlider(__instance));
                    CardOpeningHelpers.SetStateTimer(__instance, CardOpeningHelpers.GetStateTimer(__instance) + Time.deltaTime);
                    if (CardOpeningHelpers.GetStateTimer(__instance) > 0.05f)
                    {
                        CardOpeningHelpers.SetStateTimer(__instance, 0f);
                        int tempIdx = CardOpeningHelpers.GetTempIndex(__instance);
                        if (tempIdx < __instance.m_Card3dUIList.Count)
                        {
                            __instance.m_Card3dUIList[tempIdx].gameObject.SetActive(value: true);
                            CardOpeningHelpers.SetTempIndex(__instance, tempIdx + 1);
                        }
                    }

                    if (CardOpeningHelpers.GetSlider(__instance) >= 1f)
                    {
                        InteractionPlayerController.RemoveToolTip(EGameAction.OpenPack);
                        CardOpeningHelpers.SetTempIndex(__instance, 0);
                        CardOpeningHelpers.SetStateTimer(__instance, 0f);
                        CardOpeningHelpers.SetSlider(__instance, 0f);
                        __instance.m_StateIndex++;
                        // Activer la première carte et préparer la pile avec la carte suivante visible derrière
                        ShowCardStack(__instance, 0);
                    }
                }
                // State 3 : courte pause (0.15s) avant de retourner la première carte.
                // L'auto-fire peut sauter cette pause directement.
                else if (__instance.m_StateIndex == 3)
                {
                    CardOpeningHelpers.SetSlider(__instance, CardOpeningHelpers.GetSlider(__instance) + Time.deltaTime * 1f * CardOpeningHelpers.GetMultiplierStateTimer(__instance));
                    if (CardOpeningHelpers.GetSlider(__instance) >= 0.15f)
                    {
                        CardOpeningHelpers.SetSlider(__instance, 0f);
                        __instance.m_StateIndex++;
                        __instance.m_CardOpeningRotateToFrontAnim.Play("CardOpenSeq1_RotateToFront");
                    }
                    else if (CardOpeningHelpers.GetIsAutoFire(__instance) || CSingleton<CGameManager>.Instance.m_OpenPacAutoNextCard)
                    {
                        int curIdx = CardOpeningHelpers.GetCurrentOpenedCardIndex(__instance);
                        float pitchOffset = 0.002f * (float)curIdx;
                        float volOffset   = 0.001f * (float)curIdx;
                        SoundManager.PlayAudio("SFX_CardReveal1", 0.6f + volOffset, 1f + pitchOffset);
                        __instance.m_CardOpeningRotateToFrontAnim.Play("CardOpenSeq1_RotateToFront");
                        CardOpeningHelpers.SetSlider(__instance, 0f);
                        CardOpeningHelpers.SetStateTimer(__instance, 0f);
                        __instance.m_StateIndex++;
                    }
                }
                // State 4 : la carte est retournée et visible. On attend 0.8s.
                // Si High Value (rare/UR/Legendary) ou Nouvelle carte → DelayToState(5) + animation spéciale.
                // Sinon → State 5 directement.
                else if (__instance.m_StateIndex == 4)
                {
                    CardOpeningHelpers.SetSlider(__instance, CardOpeningHelpers.GetSlider(__instance) + Time.deltaTime * 1f * CardOpeningHelpers.GetMultiplierStateTimer(__instance));
                    int curIdx    = CardOpeningHelpers.GetCurrentOpenedCardIndex(__instance);
                    float slider  = CardOpeningHelpers.GetSlider(__instance);
                    float threshold = CardOpeningHelpers.GetHighValueCardThreshold(__instance);
                    List<float> cardValues = CardOpeningHelpers.GetCardValueList(__instance);
                    List<bool>  isNewList  = CardOpeningHelpers.GetIsNewlList(__instance);

                    if (!__instance.m_CardOpeningSequenceUI.m_CardValueTextGrp.activeSelf
                        && curIdx < (boosterSize - 1)
                        && slider >= 0.45f
                        && !isNewList[curIdx]
                        && cardValues[curIdx] < threshold)
                    {
                        CardOpeningHelpers.SetTotalCardValue(__instance, CardOpeningHelpers.GetTotalCardValue(__instance) + cardValues[curIdx]);
                        __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(cardValues[curIdx]);
                    }

                    if (slider >= 0.8f)
                    {
                        CardOpeningHelpers.SetSlider(__instance, 0f);
                        if (cardValues[curIdx] >= threshold)
                        {
                            SoundManager.PlayAudio("SFX_FinalizeCard", 0.6f, 1.2f);
                            __instance.m_CardAnimList[curIdx].Play("OpenCardNewCard");
                            __instance.m_HighValueCardIcon.SetActive(value: true);
                            __instance.StartCoroutine(DelayToState(5, 0.9f, __instance));
                            CardOpeningHelpers.SetTotalCardValue(__instance, CardOpeningHelpers.GetTotalCardValue(__instance) + cardValues[curIdx]);
                            __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(cardValues[curIdx]);
                            CardOpeningHelpers.SetIsGetHighValueCard(__instance, true);
                        }
                        else if (isNewList[curIdx])
                        {
                            SoundManager.PlayAudio("SFX_CardReveal0", 0.6f);
                            __instance.m_CardAnimList[curIdx].Play("OpenCardNewCard");
                            __instance.m_NewCardIcon.SetActive(value: true);
                            __instance.StartCoroutine(DelayToState(5, 0.9f, __instance));
                            CardOpeningHelpers.SetTotalCardValue(__instance, CardOpeningHelpers.GetTotalCardValue(__instance) + cardValues[curIdx]);
                            __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(cardValues[curIdx]);
                            CardOpeningHelpers.SetIsGetHighValueCard(__instance, true);
                        }
                        else
                        {
                            __instance.m_StateIndex++;
                        }
                    }
                }
                // State 5 : attente d'un clic / auto-fire du joueur pour passer à la carte suivante.
                // Déclenche l'animation OpenCardSlideExit (glissement de la carte hors écran).
                // ShowCardStack garantit que la carte qui glisse reste AU PREMIER PLAN.
                else if (__instance.m_StateIndex == 5)
                {
                    if (CardOpeningHelpers.GetIsAutoFire(__instance) || (!CardOpeningHelpers.GetIsGetHighValueCard(__instance) && CSingleton<CGameManager>.Instance.m_OpenPacAutoNextCard))
                    {
                        // Consommer immédiatement l'auto-fire / clic pour ne pas le transmettre à la carte suivante
                        CardOpeningHelpers.SetIsAutoFire(__instance, false);

                        int curIndex = CardOpeningHelpers.GetCurrentOpenedCardIndex(__instance);
                        Plugin.Logger.LogInfo($"[CardOpening] [State 5 -> Clic/Suivant] curIndex={curIndex}, lance OpenCardSlideExit sur la carte {curIndex}");

                        int num3 = UnityEngine.Random.Range(0, 3);
                        float num4 = 0.002f * (float)curIndex;
                        float num5 = 0.001f * (float)curIndex;
                        switch (num3)
                        {
                            case 0:
                                SoundManager.PlayAudio("SFX_CardReveal1", 0.6f + num5, 1f + num4);
                                break;
                            case 1:
                                SoundManager.PlayAudio("SFX_CardReveal2", 0.6f + num5, 1f + num4);
                                break;
                            default:
                                SoundManager.PlayAudio("SFX_CardReveal3", 0.6f + num5, 1f + num4);
                                break;
                        }

                        // Cacher immédiatement les icônes et le texte de valeur pour toutes les cartes (y compris la 10ème)
                        __instance.m_NewCardIcon.SetActive(value: false);
                        __instance.m_HighValueCardIcon.SetActive(value: false);
                        __instance.m_CardOpeningSequenceUI.HideSingleCardValue();

                        // Faire glisser la carte pour sortir de façon fluide
                        __instance.m_StateIndex++;
                        __instance.m_CardAnimList[curIndex].Play("OpenCardSlideExit");
                        __instance.m_CardAnimList[curIndex]["OpenCardSlideExit"].speed = 1f * CardOpeningHelpers.GetMultiplierStateTimer(__instance);

                        // S'assurer que la carte en train de glisser reste AU PREMIER PLAN
                        ShowCardStack(__instance, curIndex);

                        CardOpeningHelpers.SetIsGetHighValueCard(__instance, false);
                    }
                }
                // State 6 : attend que le glissement soit à moitié fait (slider >= 0.5).
                // Puis : masque la carte qui vient de partir, incrémente l'index,
                // active la suivante via ShowCardStack, et décide si c'est High Value / New / normale.
                // Si toutes les cartes sont passées → State 7 (récapitulatif).
                else if (__instance.m_StateIndex == 6)
                {
                    int curIndex = CardOpeningHelpers.GetCurrentOpenedCardIndex(__instance);
                    CardOpeningHelpers.SetSlider(__instance, CardOpeningHelpers.GetSlider(__instance) + Time.deltaTime * 1f * CardOpeningHelpers.GetMultiplierStateTimer(__instance));

                    List<float> cardValues = CardOpeningHelpers.GetCardValueList(__instance);
                    List<bool>  isNewList  = CardOpeningHelpers.GetIsNewlList(__instance);
                    float threshold = CardOpeningHelpers.GetHighValueCardThreshold(__instance);

                    if (!__instance.m_CardOpeningSequenceUI.m_CardValueTextGrp.activeSelf
                        && curIndex + 1 < boosterSize
                        && CardOpeningHelpers.GetSlider(__instance) >= 0.3f
                        && !isNewList[curIndex + 1]
                        && cardValues[curIndex + 1] < threshold)
                    {
                        CardOpeningHelpers.SetTotalCardValue(__instance, CardOpeningHelpers.GetTotalCardValue(__instance) + cardValues[curIndex + 1]);
                        __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(cardValues[curIndex + 1]);
                    }

                    if (!(CardOpeningHelpers.GetSlider(__instance) >= 0.5f))
                    {
                        return false;
                    }

                    CardOpeningHelpers.SetSlider(__instance, 0f);

                    // Masquer et remettre à zéro le transform de la carte qui vient de glisser
                    if (__instance.m_Card3dUIList.Count > curIndex)
                    {
                        __instance.m_Card3dUIList[curIndex].gameObject.SetActive(value: false);
                        __instance.m_CardAnimList[curIndex].Stop();
                        __instance.m_CardAnimList[curIndex].transform.localPosition = Vector3.zero;
                    }

                    int nextCardIndex = curIndex + 1;
                    CardOpeningHelpers.SetCurrentOpenedCardIndex(__instance, nextCardIndex);
                    Plugin.Logger.LogInfo($"[CardOpening] [State 6 -> Fini Slide] curIndex={curIndex} masqué. Prochaine carte nextCardIndex={nextCardIndex} / {boosterSize}");

                    if (nextCardIndex >= boosterSize)
                    {
                        Plugin.Logger.LogInfo($"[CardOpening] Toutes les {boosterSize} cartes terminées -> Passage à State 7 (Récapitulatif)");
                        CardOpeningHelpers.SetIsGetHighValueCard(__instance, false);
                        __instance.m_StateIndex = 7;
                        return false;
                    }

                    // Activer la nouvelle carte au premier plan et préparer la suivante derrière
                    ShowCardStack(__instance, nextCardIndex);

                    // Réinitialiser tout auto-fire / clic résiduel pour que la nouvelle carte ne sorte pas immédiatement
                    CardOpeningHelpers.SetIsAutoFire(__instance, false);
                    CardOpeningHelpers.SetAutoFireTimer(__instance, 0f);

                    float cardValue = cardValues[nextCardIndex];
                    bool isNew      = isNewList[nextCardIndex];
                    bool isHighValue = cardValue >= threshold;

                    Plugin.Logger.LogInfo($"[CardOpening] [State 6 -> Carte Suivante] Index={nextCardIndex}, isNew={isNew}, isHighValue={isHighValue}");

                    if (isHighValue)
                    {
                        SoundManager.PlayAudio("SFX_FinalizeCard", 0.6f, 1.2f);
                        __instance.m_CardAnimList[nextCardIndex].Play("OpenCardNewCard");
                        __instance.m_HighValueCardIcon.SetActive(value: true);
                        __instance.StartCoroutine(DelayToState(5, 0.9f, __instance));
                        CardOpeningHelpers.SetTotalCardValue(__instance, CardOpeningHelpers.GetTotalCardValue(__instance) + cardValue);
                        __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(cardValue);
                        CardOpeningHelpers.SetIsGetHighValueCard(__instance, true);
                    }
                    else if (isNew)
                    {
                        SoundManager.PlayAudio("SFX_CardReveal0", 0.6f);
                        __instance.m_CardAnimList[nextCardIndex].Play("OpenCardNewCard");
                        __instance.m_NewCardIcon.SetActive(value: true);
                        __instance.StartCoroutine(DelayToState(5, 0.9f, __instance));
                        CardOpeningHelpers.SetTotalCardValue(__instance, CardOpeningHelpers.GetTotalCardValue(__instance) + cardValue);
                        __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(cardValue);
                        CardOpeningHelpers.SetIsGetHighValueCard(__instance, true);
                    }
                    else
                    {
                        __instance.m_StateIndex = 5;
                    }
                }

                // State 7 : récapitulatif — affiche toutes les cartes dans leurs positions finales
                // (ShowAllCardPosList) en les faisant apparaître une par une (0.05s d'intervalle).
                else if (__instance.m_StateIndex == 7)
                {
                    if (CardOpeningHelpers.GetStateTimer(__instance) == 0f && CardOpeningHelpers.GetSlider(__instance) == 0f)
                    {
                        SoundManager.PlayAudio("SFX_PercStarJingle3", 0.6f);
                        SoundManager.PlayAudio("SFX_Gift", 0.6f);
                    }

                    CardOpeningHelpers.SetSlider(__instance, CardOpeningHelpers.GetSlider(__instance) + Time.deltaTime);
                    if (CardOpeningHelpers.GetSlider(__instance) >= 0.05f)
                    {
                        CardOpeningHelpers.SetSlider(__instance, 0);
                        int timerIndex = (int)CardOpeningHelpers.GetStateTimer(__instance);
                        if (timerIndex < __instance.m_CardAnimList.Count && timerIndex < __instance.m_ShowAllCardPosList.Count)
                        {
                            __instance.m_CardAnimList[timerIndex].transform.position = __instance.m_ShowAllCardPosList[timerIndex].position;
                            __instance.m_CardAnimList[timerIndex].transform.rotation = __instance.m_ShowAllCardPosList[timerIndex].rotation;

                            __instance.m_Card3dUIList[timerIndex].gameObject.SetActive(value: true);
                            __instance.m_CardAnimList[timerIndex].Play("OpenCardFinalReveal");
                        }
                        CardOpeningHelpers.SetStateTimer(__instance, CardOpeningHelpers.GetStateTimer(__instance) + 1f);
                        if (CardOpeningHelpers.GetStateTimer(__instance) >= (float)__instance.m_Card3dUIList.Count)
                        {
                            CardOpeningHelpers.SetStateTimer(__instance, 0f);
                            __instance.m_StateIndex++;
                            __instance.m_CardOpeningSequenceUI.StartShowTotalValue(CardOpeningHelpers.GetTotalCardValue(__instance), CardOpeningHelpers.GetHasFoilCard(__instance));
                        }
                    }
                }
                // State 8 : active les indicateurs "New" (étoile/badge) sur les cartes nouvelles.
                else if (__instance.m_StateIndex == 8)
                {
                    CardOpeningHelpers.SetStateTimer(__instance, CardOpeningHelpers.GetStateTimer(__instance) + Time.deltaTime);
                    if (CardOpeningHelpers.GetStateTimer(__instance) >= 0.02f)
                    {
                        CardOpeningHelpers.SetSlider(__instance, 0f);
                        int idx = (int)CardOpeningHelpers.GetStateTimer(__instance);
                        List<CardData> rolledList = CardOpeningHelpers.GetRolledCardDataList(__instance);
                        __instance.m_Card3dUIList[idx].m_NewCardIndicator.gameObject.SetActive(rolledList[idx].isNew);
                        CardOpeningHelpers.SetStateTimer(__instance, CardOpeningHelpers.GetStateTimer(__instance) + 1f);
                        if (CardOpeningHelpers.GetStateTimer(__instance) >= (float)__instance.m_Card3dUIList.Count)
                        {
                            __instance.m_StateIndex++;
                        }
                    }
                }
                // State 9 : pause de 1 seconde avant que le joueur puisse confirmer.
                else if (__instance.m_StateIndex == 9)
                {
                    CardOpeningHelpers.SetSlider(__instance, CardOpeningHelpers.GetSlider(__instance) + Time.deltaTime);
                    if (CardOpeningHelpers.GetSlider(__instance) >= 1f)
                    {
                        CardOpeningHelpers.SetSlider(__instance, 0f);
                        __instance.m_StateIndex++;
                    }
                }
                // State 10 : attend un dernier clic/auto-fire pour fermer l'écran.
                else if (__instance.m_StateIndex == 10)
                {
                    if (CardOpeningHelpers.GetIsAutoFire(__instance))
                    {
                        __instance.m_StateIndex++;
                    }
                }
                // State 11 : nettoyage final — réinitialise toutes les cartes, déclenche les events
                // (XP, succès, EvaluateOpenCardPack pour le prochain booster), ferme l'UI.
                else if (__instance.m_StateIndex == 11)
                {
                    CardOpeningHelpers.SetStateTimer(__instance, CardOpeningHelpers.GetStateTimer(__instance) + Time.deltaTime * 1f);
                    if (!(CardOpeningHelpers.GetStateTimer(__instance) >= 0.01f))
                    {
                        return false;
                    }

                    CardOpeningHelpers.SetSlider(__instance, 0f);
                    CardOpeningHelpers.SetIsScreenActive(__instance, false);
                    CardOpeningHelpers.SetIsReadyToOpen(__instance, false);
                    __instance.m_CardPackAnimator.gameObject.SetActive(value: false);
                    __instance.m_CardOpeningUIGroup.SetActive(value: false);
                    __instance.m_CardOpeningSequenceUI.HideTotalValue();
                    CSingleton<InteractionPlayerController>.Instance.ExitLockMoveMode();
                    CSingleton<InteractionPlayerController>.Instance.OnExitOpenPackState();
                    Item currentItem = CardOpeningHelpers.GetCurrentItem(__instance);
                    if ((bool)currentItem)
                    {
                        currentItem.DisableItem();
                    }

                    CardOpeningHelpers.SetCurrentItem(__instance, null);
                    int num6 = 0;
                    CardOpeningHelpers.SetTotalCardValue(__instance, 0f);
                    CardOpeningHelpers.SetTotalExpGained(__instance, 0);
                    bool isGet = false;
                    bool isGet2 = false;
                    List<CardData> rolledCards = CardOpeningHelpers.GetRolledCardDataList(__instance);
                    for (int j = 0; j < rolledCards.Count; j++)
                    {
                        int num7 = (int)(rolledCards[j].GetCardBorderType() + 1) * Mathf.CeilToInt((float)(rolledCards[j].borderType + 1) / 2f);
                        if (rolledCards[j].isFoil)
                        {
                            num7 *= 8;
                        }

                        CardOpeningHelpers.SetTotalExpGained(__instance, CardOpeningHelpers.GetTotalExpGained(__instance) + num7);
                        if (rolledCards[j].GetCardBorderType() == ECardBorderType.FullArt && rolledCards[j].isFoil)
                        {
                            isGet = true;
                            if (rolledCards[j].expansionType == ECardExpansionType.Ghost)
                            {
                                isGet2 = true;
                            }
                        }

                        if (rolledCards[j].isNew)
                        {
                            num6++;
                        }
                    }

                    if (CardOpeningHelpers.GetTotalExpGained(__instance) > 0)
                    {
                        CEventManager.QueueEvent(new CEventPlayer_AddShopExp(CardOpeningHelpers.GetTotalExpGained(__instance)));
                    }

                    for (int k = 0; k < __instance.m_CardAnimList.Count; k++)
                    {
                        __instance.m_CardAnimList[k].transform.localPosition = Vector3.zero;
                        __instance.m_CardAnimList[k].transform.localRotation = Quaternion.identity;
                        __instance.m_Card3dUIList[k].m_NewCardIndicator.gameObject.SetActive(value: false);
                        __instance.m_CardAnimList[k].Play("OpenCardDefaultPos");

                        if (k >= 8 && k < __instance.m_Card3dUIList.Count && __instance.m_Card3dUIList.Count >= 8 && __instance.m_Card3dUIList[7] != null)
                        {
                            __instance.m_Card3dUIList[k].transform.localPosition = __instance.m_Card3dUIList[7].transform.localPosition;
                            __instance.m_Card3dUIList[k].transform.localRotation = __instance.m_Card3dUIList[7].transform.localRotation;
                        }
                    }

                    if (CSingleton<InteractionPlayerController>.Instance.GetHoldItemCount() <= 0)
                    {
                        TutorialManager.SetGameUIVisible(isVisible: true);
                        CenterDot.SetVisibility(isVisible: true);
                        GameUIScreen.ResetEnterGoNextDayIndicatorVisible();
                        CSingleton<InteractionPlayerController>.Instance.m_BlackBGWorldUIFade.SetFadeOut(3f);
                        CSingleton<InteractionPlayerController>.Instance.m_CameraFOVController.StopLerpFOV();
                        CardOpeningHelpers.SetIsAutoFireKeydown(__instance, false);
                        CardOpeningHelpers.SetAutoFireTimer(__instance, 0f);
                    }

                    CSingleton<CustomerManager>.Instance.PlayerFinishOpenCardPack();
                    CSingleton<InteractionPlayerController>.Instance.EvaluateOpenCardPack();
                    TutorialManager.AddTaskValue(ETutorialTaskCondition.OpenPack, 1f);
                    CPlayerData.m_GameReportDataCollect.cardPackOpened++;
                    CPlayerData.m_GameReportDataCollectPermanent.cardPackOpened++;
                    AchievementManager.OnCardPackOpened(CPlayerData.m_GameReportDataCollectPermanent.cardPackOpened);
                    AchievementManager.OnGetFullArtFoil(isGet);
                    AchievementManager.OnGetFullArtGhostFoil(isGet2);
                    if (num6 > 0)
                    {
                        AchievementManager.OnCheckAlbumCardCount(CPlayerData.GetTotalCardCollectedAmount());
                    }
                }
                // State 12 : état de sortie propre (IsScreenActive = false).
                else if (__instance.m_StateIndex == 12)
                {
                    CardOpeningHelpers.SetIsScreenActive(__instance, false);
                }
                // State 101 : état de débogage / auto-avance rapide (non utilisé en production).
                else if (__instance.m_StateIndex == 101)
                {
                    _ = CardOpeningHelpers.GetStateTimer(__instance);
                    _ = 0f;
                    CardOpeningHelpers.SetStateTimer(__instance, CardOpeningHelpers.GetStateTimer(__instance) + Time.deltaTime);
                    if (CardOpeningHelpers.GetStateTimer(__instance) >= 0.05f)
                    {
                        int num8 = UnityEngine.Random.Range(0, 3);
                        float num9  = 0.002f * (float)CardOpeningHelpers.GetCurrentOpenedCardIndex(__instance);
                        float num10 = 0.001f * (float)CardOpeningHelpers.GetCurrentOpenedCardIndex(__instance);
                        switch (num8)
                        {
                            case 0:
                                SoundManager.PlayAudio("SFX_CardReveal1", 0.6f + num10, 1f + num9);
                                break;
                            case 1:
                                SoundManager.PlayAudio("SFX_CardReveal2", 0.6f + num10, 1f + num9);
                                break;
                            default:
                                SoundManager.PlayAudio("SFX_CardReveal3", 0.6f + num10, 1f + num9);
                                break;
                        }

                        CardOpeningHelpers.SetCurrentOpenedCardIndex(__instance, CardOpeningHelpers.GetCurrentOpenedCardIndex(__instance) + 1);
                    }
                }
                else
                {
                    _ = __instance.m_StateIndex;
                    _ = -1;
                }
            }

            return false;
        }
    }
}
