using CMF;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using WankulCrazyPlugin.utils;


namespace WankulCrazyPlugin.patch
{
    public static class InteractionPlayerControllerPatch
    {
        const int MaxPacks = 24;
        const int PacksPerColumn = 12;

        private static Transform cachedEnvironmentGrpTransform;

        private static readonly FieldInfo s_HoldItemListField = AccessTools.Field(typeof(InteractionPlayerController), "m_HoldItemList");
        private static readonly FieldInfo s_InputTooltipListDisplayField = AccessTools.Field(typeof(InteractionPlayerController), "m_InputTooltipListDisplay");
        private static readonly FieldInfo s_OpenCardBoxMeshFilterField = AccessTools.Field(typeof(InteractionPlayerController), "m_OpenCardBoxMeshFilter");
        private static readonly FieldInfo s_OpenCardBoxMeshField = AccessTools.Field(typeof(InteractionPlayerController), "m_OpenCardBoxMesh");
        private static readonly FieldInfo s_OpenCardBoxInnerMeshField = AccessTools.Field(typeof(InteractionPlayerController), "m_OpenCardBoxInnerMesh");
        private static readonly FieldInfo s_OpenCardBoxSpawnCardPackPosListField = AccessTools.Field(typeof(InteractionPlayerController), "m_OpenCardBoxSpawnCardPackPosList");
        private static readonly FieldInfo s_HoldCardPackPosListField = AccessTools.Field(typeof(InteractionPlayerController), "m_HoldCardPackPosList");
        private static readonly FieldInfo s_IsOpeningCardBoxField = AccessTools.Field(typeof(InteractionPlayerController), "m_IsOpeningCardBox");

        private static readonly MethodInfo s_CanOpenPackMethod = AccessTools.Method(typeof(InteractionPlayerController), "CanOpenPack");
        private static readonly MethodInfo s_RemoveHoldItemMethod = AccessTools.Method(typeof(InteractionPlayerController), "RemoveHoldItem");
        private static readonly MethodInfo s_CanOpenCardBoxMethod = AccessTools.Method(typeof(InteractionPlayerController), "CanOpenCardBox");
        private static readonly MethodInfo s_CardBoxToCardPackMethod = AccessTools.Method(typeof(InteractionPlayerController), "CardBoxToCardPack");
        private static readonly MethodInfo s_RemoveToolTipMethod = AccessTools.Method(typeof(InteractionPlayerController), "RemoveToolTip");

        private static HashSet<EItemType> s_OpenablePacks;
        private static HashSet<EItemType> s_OpenableCardBoxes;
        private static Dictionary<EItemType, EItemType> s_BoxToPackMap;

        private static readonly Vector3[] s_PackPositions = new Vector3[]
        {
            new Vector3(-0.05f, 0.1f - 0.08f, 0),
            new Vector3(0.05f, 0.1f - 0.08f, 0),
            new Vector3(-0.05f, 0.096f - 0.08f, 0),
            new Vector3(0.05f, 0.096f - 0.08f, 0),
            new Vector3(-0.05f, 0.092f - 0.08f, 0),
            new Vector3(0.05f, 0.092f - 0.08f, 0),
            new Vector3(-0.05f, 0.088f - 0.08f, 0),
            new Vector3(0.05f, 0.088f - 0.08f, 0),
            new Vector3(-0.05f, 0.084f - 0.08f, 0),
            new Vector3(0.05f, 0.084f - 0.08f, 0),
            new Vector3(-0.05f, 0.08f - 0.08f, 0),
            new Vector3(0.05f, 0.08f - 0.08f, 0),
            new Vector3(-0.05f, 0.076f - 0.08f, 0),
            new Vector3(0.05f, 0.076f - 0.08f, 0),
            new Vector3(-0.05f, 0.072f - 0.08f, 0),
            new Vector3(0.05f, 0.072f - 0.08f, 0),
            new Vector3(-0.05f, 0.068f - 0.08f, 0),
            new Vector3(0.05f, 0.068f - 0.08f, 0),
            new Vector3(-0.05f, 0.064f - 0.08f, 0),
            new Vector3(0.05f, 0.064f - 0.08f, 0),
            new Vector3(-0.05f, 0.060f - 0.08f, 0),
            new Vector3(0.05f, 0.060f - 0.08f, 0),
            new Vector3(-0.05f, 0.056f - 0.08f, 0),
            new Vector3(0.05f, 0.056f - 0.08f, 0),
        };

        private static void EnsurePackAndBoxLookupsInitialized()
        {
            if (s_OpenablePacks != null && s_OpenableCardBoxes != null && s_BoxToPackMap != null)
                return;

            EItemType displayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
            EItemType displayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
            EItemType boosterStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType boosterStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            EItemType displayLegacy = EnumExtensions.SafeParseEItemType("DisplayLegacy");
            EItemType displayLegacyTaux = EnumExtensions.SafeParseEItemType("DisplayLegacyTaux");
            EItemType boosterLegacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");
            EItemType boosterLegacyTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
            EItemType boosterGoldBattle = EnumExtensions.SafeParseEItemType("BoosterGoldBattle");
            EItemType boosterGoldStellar = EnumExtensions.SafeParseEItemType("BoosterGoldStellar");
            EItemType boosterGoldLegacy = EnumExtensions.SafeParseEItemType("BoosterGoldLegacy");

            s_OpenablePacks = new HashSet<EItemType>
            {
                EItemType.BasicCardPack,
                EItemType.RareCardPack,
                EItemType.EpicCardPack,
                EItemType.LegendaryCardPack,
                EItemType.DestinyBasicCardPack,
                EItemType.DestinyRareCardPack,
                EItemType.DestinyEpicCardPack,
                EItemType.DestinyLegendaryCardPack,
                EItemType.GhostPack,
                EItemType.MegabotPack,
                EItemType.FantasyRPGPack,
                EItemType.CatJobPack,
                EItemType.AscensionCardPack,
                boosterStellar,
                boosterStellarTaux,
                boosterLegacy,
                boosterLegacyTaux,
                boosterGoldBattle,
                boosterGoldStellar,
                boosterGoldLegacy
            };

            s_OpenableCardBoxes = new HashSet<EItemType>
            {
                EItemType.BasicCardBox,
                EItemType.RareCardBox,
                EItemType.EpicCardBox,
                EItemType.LegendaryCardBox,
                EItemType.DestinyBasicCardBox,
                EItemType.DestinyRareCardBox,
                EItemType.DestinyEpicCardBox,
                EItemType.DestinyLegendaryCardBox,
                EItemType.AscensionCardBox,
                displayStellar,
                displayStellarTaux,
                displayLegacy,
                displayLegacyTaux
            };

            s_BoxToPackMap = new Dictionary<EItemType, EItemType>
            {
                { EItemType.BasicCardBox, EItemType.BasicCardPack },
                { EItemType.RareCardBox, EItemType.RareCardPack },
                { EItemType.EpicCardBox, EItemType.EpicCardPack },
                { EItemType.LegendaryCardBox, EItemType.LegendaryCardPack },
                { EItemType.DestinyBasicCardBox, EItemType.DestinyBasicCardPack },
                { EItemType.DestinyRareCardBox, EItemType.DestinyRareCardPack },
                { EItemType.DestinyEpicCardBox, EItemType.DestinyEpicCardPack },
                { EItemType.DestinyLegendaryCardBox, EItemType.DestinyLegendaryCardPack },
                { EItemType.AscensionCardBox, EItemType.AscensionCardPack },
                { displayStellar, boosterStellar },
                { displayStellarTaux, boosterStellarTaux },
                { displayLegacy, boosterLegacy },
                { displayLegacyTaux, boosterLegacyTaux }
            };
        }

        [HarmonyPatch(typeof(InteractionPlayerController), "Awake")]
        [HarmonyPostfix]
        public static void AwakePostfix(InteractionPlayerController __instance)
        {

            if (__instance.m_HoldCardPackPosList == null)
                return;

            while (__instance.m_HoldCardPackPosList.Count < MaxPacks)
            {
                Transform lastTransform = __instance.m_HoldCardPackPosList.Last();
                Vector3 newPosition = lastTransform.localPosition;

                if (__instance.m_HoldCardPackPosList.Count >= PacksPerColumn)
                {
                    // Seconde Colonne
                    newPosition.x = -0.07f;
                    newPosition.z = __instance.m_HoldCardPackPosList[__instance.m_HoldCardPackPosList.Count % PacksPerColumn].localPosition.z;
                }
                else
                {
                    newPosition.z -= 0.02f;
                }

                GameObject newPackPos = new GameObject($"CardPackPos_{__instance.m_HoldCardPackPosList.Count}");
                newPackPos.transform.SetParent(lastTransform.parent);
                newPackPos.transform.localPosition = newPosition;
                newPackPos.transform.localRotation = lastTransform.localRotation;
                newPackPos.transform.localScale = Vector3.one; // Echelle des packs (1, 1, 1)

                __instance.m_HoldCardPackPosList.Add(newPackPos.transform);
            }
        }

        public static bool CanOpenPack(ref bool __result, InteractionPlayerController __instance)
        {
            EnsurePackAndBoxLookupsInitialized();
            bool result = false;

            List<Item> m_HoldItemList = (List<Item>)s_HoldItemListField.GetValue(__instance);
            if (m_HoldItemList != null && m_HoldItemList.Count > 0)
            {
                Item item = m_HoldItemList[0];
                if (item != null)
                {
                    result = s_OpenablePacks.Contains(item.GetItemType());
                }
            }
            __result = result;
            return false;
        }

        public static bool IsCardPackType(EItemType itemType, ref bool __result)
        {
            EnsurePackAndBoxLookupsInitialized();
            __result = s_OpenablePacks.Contains(itemType);
            return false;
        }

        public static bool CanOpenCardBox(ref bool __result, InteractionPlayerController __instance)
        {
            EnsurePackAndBoxLookupsInitialized();
            bool result = false;

            List<Item> m_HoldItemList = (List<Item>)s_HoldItemListField.GetValue(__instance);
            if (m_HoldItemList != null && m_HoldItemList.Count > 0)
            {
                Item item = m_HoldItemList[0];
                if (item != null)
                {
                    result = s_OpenableCardBoxes.Contains(item.GetItemType());
                }
            }
            __result = result;
            return false;
        }

        public static bool CardBoxToCardPack(EItemType cardBoxItemType, ref EItemType __result)
        {
            EnsurePackAndBoxLookupsInitialized();
            if (!s_BoxToPackMap.TryGetValue(cardBoxItemType, out __result))
            {
                __result = EItemType.None;
            }

            return false;
        }

        // Nouvelle méthode RemoveToolTip
        public static void RemoveToolTip(EGameAction action)
        {
            var inputTooltipListDisplay = s_InputTooltipListDisplayField?.GetValue(CSingleton<InteractionPlayerController>.Instance) as InputTooltipListDisplay;
            inputTooltipListDisplay?.RemoveTooltip(action);
        }

        // Nouvelle méthode EvaluateOpenCardPack
        [HarmonyPatch(typeof(InteractionPlayerController), "EvaluateOpenCardPack")]
        public static bool EvaluateOpenCardPack(InteractionPlayerController __instance)
        {
            if (WankulCrazyPlugin.importer.WankulLoadingScreen.IsLoading)
            {
                return false;
            }

            List<Item> m_HoldItemList = (List<Item>)s_HoldItemListField.GetValue(__instance);
            bool canOpenPack = (bool)s_CanOpenPackMethod.Invoke(__instance, Array.Empty<object>());

            if (canOpenPack)
            {
                Item holdItem = m_HoldItemList[0];
                s_RemoveHoldItemMethod.Invoke(__instance, new object[] { holdItem });

                CSingleton<CardOpeningSequence>.Instance.ReadyingCardPack(holdItem);

                Plugin.SetPProperty(__instance, "m_IsHoldingMouseDown", false);
                Plugin.SetPProperty(__instance, "m_IsHoldingRightMouseDown", false);
            }
            else
            {
                bool canOpenCardBox = (bool)s_CanOpenCardBoxMethod.Invoke(__instance, Array.Empty<object>());
                if (!canOpenCardBox)
                {
                    return false;
                }

                s_IsOpeningCardBoxField.SetValue(__instance, true);
                Item holdItem = m_HoldItemList[0];
                EItemType cardPack = (EItemType)s_CardBoxToCardPackMethod.Invoke(__instance, new object[] { holdItem.GetItemType() });

                ItemMeshData itemMeshData1 = InventoryBase.GetItemMeshData(holdItem.GetItemType());

                var openCardBoxMeshFilter = s_OpenCardBoxMeshFilterField.GetValue(__instance) as MeshFilter;
                var openCardBoxMesh = s_OpenCardBoxMeshField.GetValue(__instance) as MeshRenderer;

                openCardBoxMeshFilter.mesh = itemMeshData1.mesh;
                openCardBoxMesh.material = itemMeshData1.material;
                holdItem.gameObject.SetActive(false);
                m_HoldItemList.Clear();
                CPlayerData.m_HoldItemTypeList.Clear();
                s_RemoveToolTipMethod.Invoke(null, new object[] { EGameAction.OpenCardBox });
                SoundManager.PlayAudio("SFX_OpenCardBox", 0.6f);

                var openCardBoxInnerMesh = s_OpenCardBoxInnerMeshField.GetValue(__instance) as Animation;
                openCardBoxInnerMesh.gameObject.SetActive(true);

                var openCardBoxSpawnCardPackPosList = s_OpenCardBoxSpawnCardPackPosListField.GetValue(__instance) as List<Transform>;
                var holdCardPackPosList = s_HoldCardPackPosListField.GetValue(__instance) as List<Transform>;

                if (m_HoldItemList.Count < 24)
                {
                    while (m_HoldItemList.Count < 24)
                    {
                        Item fakeItem = ItemSpawnManager.GetItem(null);
                        m_HoldItemList.Add(fakeItem);
                    }
                }


                // Optimisation: Utiliser un cache pour éviter de rechercher l'objet "Level_Environment_Grp" à chaque fois
                if (cachedEnvironmentGrpTransform == null)
                {
                    cachedEnvironmentGrpTransform = GameObject.Find("Level_Environment_Grp")?.transform;
                }

                Transform someDefaultParentTransform = cachedEnvironmentGrpTransform;

                if (someDefaultParentTransform == null)
                {
                    Plugin.Logger.LogError("Le parent par défaut 'Level_Environment_Grp' n'a pas été trouvé dans la scène !");
                }

                if (openCardBoxSpawnCardPackPosList.Count < 24)
                {
                    // Récupérer le dernier élément valide pour baser les éléments fictifs
                    Transform lastValidTransform = openCardBoxSpawnCardPackPosList[openCardBoxSpawnCardPackPosList.Count - 1];

                    // Vérifier si le dernier élément valide a un parent, sinon définisser un parent par défaut
                    Transform defaultParent = lastValidTransform.parent != null ? lastValidTransform.parent : someDefaultParentTransform;

                    while (openCardBoxSpawnCardPackPosList.Count < 24)
                    {
                        Transform fakeTransform = new GameObject("FakeTransform").transform;
                        fakeTransform.localPosition = new Vector3(0.02f, 0, 0);
                        fakeTransform.localRotation = lastValidTransform.localRotation * Quaternion.Euler(0, 0.5f, 0);
                        fakeTransform.localScale = lastValidTransform.localScale;
                        fakeTransform.name = $"SpawnCardPackPosLoc ({openCardBoxSpawnCardPackPosList.Count})";
                        fakeTransform.parent = defaultParent;
                        openCardBoxSpawnCardPackPosList.Add(fakeTransform);
                        lastValidTransform = fakeTransform;
                    }
                }

                // Pour rattacher les éléments sans parent
                for (int i = 0; i < openCardBoxSpawnCardPackPosList.Count; i++)
                {
                    if (openCardBoxSpawnCardPackPosList[i].parent == null)
                    {
                        openCardBoxSpawnCardPackPosList[i].parent = someDefaultParentTransform;  // Assigner le parent par défaut
                    }
                }

                if (__instance.m_HoldCardPackPosList.Count < 24)
                {
                    // Récupérer le dernier élément valide pour baser les éléments fictifs
                    Transform lastValidTransform = __instance.m_HoldCardPackPosList.Count > 0 ? __instance.m_HoldCardPackPosList[__instance.m_HoldCardPackPosList.Count - 1] : null;

                    // Vérifier si lastValidTransform a un parent, sinon utiliser le parent par défaut
                    Transform defaultParent = lastValidTransform?.parent != null ? lastValidTransform.parent : someDefaultParentTransform;

                    while (__instance.m_HoldCardPackPosList.Count < 24)
                    {
                        Transform fakeTransform = new GameObject("FakeTransform").transform;
                        fakeTransform.localPosition = lastValidTransform != null ? lastValidTransform.localPosition + new Vector3(0.02f, 0, 0) : Vector3.zero;
                        fakeTransform.localRotation = lastValidTransform != null ? lastValidTransform.localRotation * Quaternion.Euler(0, 0.5f, 0) : Quaternion.identity;
                        fakeTransform.localScale = lastValidTransform != null ? lastValidTransform.localScale : Vector3.one;
                        fakeTransform.name = $"HoldCardPackPosLoc ({__instance.m_HoldCardPackPosList.Count})";
                        fakeTransform.parent = defaultParent;
                        __instance.m_HoldCardPackPosList.Add(fakeTransform);
                        lastValidTransform = fakeTransform;
                    }
                }

                ItemMeshData itemMeshData2 = InventoryBase.GetItemMeshData(cardPack);
                Dictionary<EItemType, string> boxPaths = GetBoxTexturePaths();
                Dictionary<EItemType, string> packPaths = GetPackTexturePaths();
                EItemType boxType = holdItem != null ? holdItem.GetItemType() : EItemType.None;
                Quaternion spawnPackLocalRot = Quaternion.Euler(270, 0, 0);
                Quaternion cardBoxStaticLocalRot = Quaternion.Euler(0, 180, 0);
                Quaternion staticMeshLocalRot = Quaternion.Euler(90, 180, 0);

                for (int index = 0; index < openCardBoxSpawnCardPackPosList.Count; ++index)
                {
                    if (index < m_HoldItemList.Count)
                    {
                        Item obj = ItemSpawnManager.GetItem(openCardBoxSpawnCardPackPosList[index]);
                        if (obj != null)
                        {
                            Transform cardBoxInnerMesh = __instance.m_HoldCardPackPosList[index].Find("CardBoxInnerMesh");
                            if (cardBoxInnerMesh != null)
                            {
                                int packIndex = 0;
                                foreach (Transform child in cardBoxInnerMesh)
                                {
                                    if (child.name.StartsWith("SpawnCardPackPosLoc"))
                                    {
                                        if (packIndex < s_PackPositions.Length)
                                        {
                                            child.localPosition = s_PackPositions[packIndex];
                                            child.localRotation = spawnPackLocalRot;
                                            child.localScale = Vector3.one;
                                            packIndex++;
                                        }
                                    }
                                    else if (child.name.StartsWith("CardBoxStaticMesh"))
                                    {
                                        child.localPosition = new Vector3(-0.001f, 0.1107f - 0.08f, 0);
                                        child.localRotation = cardBoxStaticLocalRot;
                                        child.localScale = new Vector3(0.26f, 0.15f, 0.0001f);

                                        if (boxPaths.TryGetValue(boxType, out string texturePath))
                                        {
                                            ApplyTextureToChild(child, texturePath);
                                        }
                                        else if (itemMeshData1 != null && itemMeshData1.material != null)
                                        {
                                            var rend = child.GetComponent<Renderer>();
                                            if (rend != null)
                                            {
                                                rend.material = itemMeshData1.material;
                                            }
                                        }

                                        StartChildAnimation(child);
                                    }
                                    else if (child.name.StartsWith("StaticMesh") || child.name.StartsWith("CardPackStaticMesh"))
                                    {
                                        if (child.name.StartsWith("StaticMesh"))
                                        {
                                            child.localPosition = new Vector3(-0.001f, 0.08f - 0.08f, 0);
                                            child.localRotation = staticMeshLocalRot;
                                            child.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                                        }

                                        // Vérifie si le cardPack a une texture associée dans le dictionnaire
                                        if (packPaths.TryGetValue(cardPack, out string texturePath))
                                        {
                                            ApplyTextureToChild(child, texturePath);
                                        }
                                    }
                                }
                            }

                            obj.SetMesh(itemMeshData2.mesh, itemMeshData2.material, cardPack, itemMeshData2.meshSecondary, itemMeshData2.materialSecondary);
                            obj.transform.position = openCardBoxSpawnCardPackPosList[index].position;
                            obj.transform.rotation = openCardBoxSpawnCardPackPosList[index].rotation;
                            obj.transform.parent = openCardBoxSpawnCardPackPosList[index];
                            obj.transform.localScale = openCardBoxSpawnCardPackPosList[index].localScale;
                            obj.gameObject.SetActive(true);

                            m_HoldItemList[index] = obj; // Remplacer l'élément fictif par le véritable objet
                            CPlayerData.m_HoldItemTypeList.Add(obj.GetItemType());

                            DelayLerpSpawnedCardPackToHandPostfix(__instance, index, (1.25f + 0.05f * (float)index), obj, holdCardPackPosList[index], holdItem);
                        }
                    }
                }
            }

            return false;
        }

        public static void DelayLerpSpawnedCardPackToHandPostfix(InteractionPlayerController __instance, int index, float waitTime, Item item, Transform targetTransform, Item cardBoxItem)
        {
            __instance.StartCoroutine(HandleDelayLerpSpawnedCardPackToHand(__instance, index, waitTime, item, targetTransform, cardBoxItem));
        }

        private static IEnumerator HandleDelayLerpSpawnedCardPackToHand(InteractionPlayerController __instance, int index, float waitTime, Item item, Transform targetTransform, Item cardBoxItem)
        {
            var m_OpenCardBoxInnerMesh = s_OpenCardBoxInnerMeshField.GetValue(__instance) as Animation;

            yield return new WaitForSeconds(waitTime);
            item.SmoothLerpToTransform(targetTransform, targetTransform);

            if (index == 23 && cardBoxItem != null)
            {
                yield return new WaitForSeconds(0.5f);
                cardBoxItem.DisableItem();
                if (m_OpenCardBoxInnerMesh != null)
                {
                    m_OpenCardBoxInnerMesh.gameObject.SetActive(false);
                }
                s_IsOpeningCardBoxField.SetValue(__instance, false);
            }
        }

        private static AnimationOpeningDisplay animationDisplay;
        public static void InitializeAnimationDisplay()
        {
            GameObject animationDisplayObject = new GameObject("AnimationOpeningDisplay");
            animationDisplay = animationDisplayObject.AddComponent<AnimationOpeningDisplay>();
        }

        public static void StartChildAnimation(Transform child)
        {
            if (animationDisplay == null)
            {
                InitializeAnimationDisplay();
            }
            animationDisplay.StartAnimation(child);
        }
        static void ChangeTextureColorInUVRectangle(GameObject targetObject, Vector2 uvTopLeft, Vector2 uvBottomRight, Color newColor)
        {
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Le shader "Standard" est strippé/cassé dans ce build (magenta silencieux).
                renderer.sharedMaterial = ShaderUtils.EnsureNotBrokenStandard(renderer.sharedMaterial);

                Material material = renderer.material;
                Texture2D texture = null;
                if (material.HasProperty("_BaseColorMap"))
                    texture = material.GetTexture("_BaseColorMap") as Texture2D;
                else if (material.HasProperty("_BaseMap"))
                    texture = material.GetTexture("_BaseMap") as Texture2D;
                else if (material.HasProperty("_MainTex"))
                    texture = material.GetTexture("_MainTex") as Texture2D;

                if (texture != null)
                {
                    // Rendre la texture modifiable
                    Texture2D readableTexture = TextureUtils.MakeTextureReadable(texture);
                    if (readableTexture != null)
                    {
                        // Convertir les coordonnées UV en coordonnées de pixels
                        int pixelXMin = Mathf.FloorToInt(uvTopLeft.x * readableTexture.width);
                        int pixelYMin = Mathf.FloorToInt(uvBottomRight.y * readableTexture.height);
                        int pixelXMax = Mathf.FloorToInt(uvBottomRight.x * readableTexture.width);
                        int pixelYMax = Mathf.FloorToInt(uvTopLeft.y * readableTexture.height);

                        // Optimisation : utiliser SetPixels plutôt que SetPixel en boucle
                        int blockWidth = pixelXMax - pixelXMin + 1;
                        int blockHeight = pixelYMax - pixelYMin + 1;
                        if (blockWidth > 0 && blockHeight > 0)
                        {
                            Color[] colors = new Color[blockWidth * blockHeight];
                            for (int i = 0; i < colors.Length; i++)
                            {
                                colors[i] = newColor;
                            }
                            readableTexture.SetPixels(pixelXMin, pixelYMin, blockWidth, blockHeight, colors);
                        }

                        // Appliquer les changements à la texture
                        readableTexture.Apply();

                        // Réaffecter la texture modifiée au matériau
                        if (material.HasProperty("_BaseColorMap"))
                            material.SetTexture("_BaseColorMap", readableTexture);
                        if (material.HasProperty("_BaseMap"))
                            material.SetTexture("_BaseMap", readableTexture);
                        if (material.HasProperty("_MainTex"))
                            material.SetTexture("_MainTex", readableTexture);

                        //Debug.Log($"Texture modifiée pour la zone UV ({uvTopLeft}, {uvBottomRight}) sur l'objet {targetObject.name}");
                    }
                    else
                    {
                        Debug.LogError("Impossible de rendre la texture lisible.");
                    }
                }
                else
                {
                    Debug.LogError("Le GameObject n'a pas de Texture2D assignée.");
                }
            }
            else
            {
                Debug.LogError("Le GameObject n'a pas de Renderer.");
            }
        }
        private static System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

        public static Texture2D LoadTexture(string path)
        {
            if (_textureCache.TryGetValue(path, out Texture2D cachedTex))
            {
                return cachedTex;
            }

            // Exemple de chargement d'une texture à partir d'un fichier
            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            byte[] fileData = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(fileData))
            {
                _textureCache[path] = tex;
                return tex;
            }
            return null;
        }
        public static void ApplyTextureToChild(Transform child, string texturePath)
        {
            Texture2D newTexture = LoadTexture(texturePath);

            if (newTexture != null)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material sourceMat = renderer.sharedMaterial;
                    if (ShaderUtils.UsesBrokenStandardShader(sourceMat))
                    {
                        sourceMat = WankulCrazyPlugin.importer.PatchTexturesImporter.GetSafe2DBaseMaterial();
                    }
                    sourceMat = ShaderUtils.EnsureNotBrokenStandard(sourceMat);

                    Material material = ShaderUtils.CreateSafeMaterial(sourceMat);
                    if (material.HasProperty("_MainTex"))
                        material.SetTexture("_MainTex", newTexture);
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", newTexture);
                    if (material.HasProperty("_BaseColorMap"))
                        material.SetTexture("_BaseColorMap", newTexture);
                    renderer.sharedMaterial = material;
                    renderer.material = material;
                    renderer.SetPropertyBlock(null);
                }
                else
                {
                    Plugin.Logger.LogError("Impossible de récupérer le Renderer.");
                }
            }
            else
            {
                Plugin.Logger.LogError("Échec du chargement de la texture.");
            }
        }

        private static Dictionary<EItemType, string> boxTexturePaths = null;

        private static Dictionary<EItemType, string> GetBoxTexturePaths()
        {
            if (boxTexturePaths == null)
            {
                boxTexturePaths = new Dictionary<EItemType, string>
                {
                    { EItemType.BasicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S1.png") },
                    { EItemType.RareCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S2.png") },
                    { EItemType.EpicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S3.png") },
                    { EItemType.LegendaryCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_HS.png") },
                    { EItemType.DestinyBasicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S1_TauxDrop.png") },
                    { EItemType.DestinyRareCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S2_TauxDrop.png") },
                    { EItemType.DestinyEpicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S3_TauxDrop.png") },
                    { EItemType.DestinyLegendaryCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_HS_TauxDrop.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayStellar"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S4.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayStellarTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S4_TauxDrop.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayLegacy"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S5.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayLegacyTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S5_TauxDrop.png") },
                };
            }
            return boxTexturePaths;
        }

        private static Dictionary<EItemType, string> packTexturePaths = null;

        private static Dictionary<EItemType, string> GetPackTexturePaths()
        {
            if (packTexturePaths == null)
            {
                packTexturePaths = new Dictionary<EItemType, string>
                {
                    { EItemType.BasicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackCommon.png") },
                    { EItemType.RareCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackRare.png") },
                    { EItemType.EpicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackEpic.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterStellar"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S4.png") },
                    { EItemType.LegendaryCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackLegnd.png") },
                    { EItemType.DestinyBasicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyCommon.png") },
                    { EItemType.DestinyRareCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyRare.png") },
                    { EItemType.DestinyEpicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyEpic.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterStellarTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S4_TauxDrop.png") },
                    { EItemType.DestinyLegendaryCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyLegend.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterLegacy"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S5.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterLegacyTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S5_TauxDrop.png") },
                };
            }
            return packTexturePaths;
        }
    }
}