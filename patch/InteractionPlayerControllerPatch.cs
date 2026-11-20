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

        // 24 emplacements debout (2 colonnes x 12 rangées d'avant en arrière) à l'intérieur du display vertical (échelle 0.10)
        private static readonly Vector3[] s_PackPositions = new Vector3[]
        {
            new Vector3(-0.030f, -0.040f,  0.030f),
            new Vector3( 0.036f, -0.040f,  0.030f),
            new Vector3(-0.030f, -0.040f,  0.025f),
            new Vector3( 0.036f, -0.040f,  0.025f),
            new Vector3(-0.030f, -0.040f,  0.019f),
            new Vector3( 0.036f, -0.040f,  0.019f),
            new Vector3(-0.030f, -0.040f,  0.014f),
            new Vector3( 0.036f, -0.040f,  0.014f),
            new Vector3(-0.030f, -0.040f,  0.008f),
            new Vector3( 0.036f, -0.040f,  0.008f),
            new Vector3(-0.030f, -0.040f,  0.003f),
            new Vector3( 0.036f, -0.040f,  0.003f),
            new Vector3(-0.030f, -0.040f, -0.003f),
            new Vector3( 0.036f, -0.040f, -0.003f),
            new Vector3(-0.030f, -0.040f, -0.008f),
            new Vector3( 0.036f, -0.040f, -0.008f),
            new Vector3(-0.030f, -0.040f, -0.014f),
            new Vector3( 0.036f, -0.040f, -0.014f),
            new Vector3(-0.030f, -0.040f, -0.019f),
            new Vector3( 0.036f, -0.040f, -0.019f),
            new Vector3(-0.030f, -0.040f, -0.025f),
            new Vector3( 0.036f, -0.040f, -0.025f),
            new Vector3(-0.030f, -0.040f, -0.030f),
            new Vector3( 0.036f, -0.040f, -0.030f),
        };

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
            bool result = false;

            List<Item> m_HoldItemList = (List<Item>)s_HoldItemListField.GetValue(__instance);
            if (m_HoldItemList != null && m_HoldItemList.Count > 0)
            {
                Item item = m_HoldItemList[0];
                if (item != null)
                {
                    result = CustomItemTypes.IsOpenablePack(item.GetItemType());
                }
            }
            __result = result;
            return false;
        }

        public static bool IsCardPackType(EItemType itemType, ref bool __result)
        {
            __result = CustomItemTypes.IsOpenablePack(itemType);
            return false;
        }

        public static bool CanOpenCardBox(ref bool __result, InteractionPlayerController __instance)
        {
            bool result = false;

            List<Item> m_HoldItemList = (List<Item>)s_HoldItemListField.GetValue(__instance);
            if (m_HoldItemList != null && m_HoldItemList.Count > 0)
            {
                Item item = m_HoldItemList[0];
                if (item != null)
                {
                    result = CustomItemTypes.IsOpenableDisplay(item.GetItemType());
                }
            }
            __result = result;
            return false;
        }

        public static bool CardBoxToCardPack(EItemType cardBoxItemType, ref EItemType __result)
        {
            __result = CustomItemTypes.GetBoosterForDisplay(cardBoxItemType);
            return false;
        }

        // Nouvelle méthode RemoveToolTip
        public static void RemoveToolTip(EGameAction action)
        {
            var inputTooltipListDisplay = s_InputTooltipListDisplayField?.GetValue(CSingleton<InteractionPlayerController>.Instance) as InputTooltipListDisplay;
            inputTooltipListDisplay?.RemoveTooltip(action);
        }

        // Nouvelle méthode EvaluateOpenCardPack
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
                Dictionary<EItemType, string> itemTexturePaths = WankulCrazyPlugin.importer.PatchTexturesImporter.GetBoxTexturePaths();
                EItemType boxType = holdItem != null ? holdItem.GetItemType() : EItemType.None;
                Quaternion spawnPackLocalRot = Quaternion.identity;
                Quaternion cardBoxStaticLocalRot = Quaternion.Euler(0f, 180f, 0f);
                Quaternion staticMeshLocalRot = Quaternion.Euler(0f, 180f, 0f);
                Vector3 spawnPackLocalScale = new Vector3(0.55f, 0.55f, 0.55f);

                // Initialisation unique de la boîte verticale ouverte, de son couvercle et des 24 emplacements internes
                if (__instance.m_HoldCardPackPosList != null)
                {
                    Transform cardBoxInnerMesh = null;
                    for (int posIdx = 0; posIdx < __instance.m_HoldCardPackPosList.Count; posIdx++)
                    {
                        if (__instance.m_HoldCardPackPosList[posIdx] == null) continue;
                        cardBoxInnerMesh = __instance.m_HoldCardPackPosList[posIdx].Find("CardBoxInnerMesh");
                        if (cardBoxInnerMesh != null) break;
                    }

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
                                    child.localScale = spawnPackLocalScale;
                                    packIndex++;
                                }
                            }
                            else if (child.name.StartsWith("CardBoxStaticMesh"))
                            {
                                child.localPosition = new Vector3(0.003f, 0.0238f, 0f);
                                child.localRotation = cardBoxStaticLocalRot;
                                child.localScale = new Vector3(0.10f, 0.0001f, 0.10f);

                                ApplyBoxMaterialToChild(child, boxType, itemTexturePaths, itemMeshData1?.material);
                                StartChildAnimation(child);
                            }
                            else if (child.name.StartsWith("StaticMesh"))
                            {
                                child.localPosition = new Vector3(0.003f, -0.035f, 0f);
                                child.localRotation = staticMeshLocalRot;
                                child.localScale = new Vector3(0.10f, 0.10f, 0.10f);

                                MeshFilter mf = child.GetComponent<MeshFilter>();
                                if (mf != null)
                                {
                                    mf.sharedMesh = GetOrCreateOpenDisplayBoxMesh();
                                }

                                ApplyBoxMaterialToChild(child, boxType, itemTexturePaths, itemMeshData1?.material);
                            }
                            else if (child.name.StartsWith("CardPackStaticMesh"))
                            {
                                ApplyBoxMaterialToChild(child, cardPack, itemTexturePaths, itemMeshData2?.material);
                            }
                        }
                    }
                }

                for (int index = 0; index < openCardBoxSpawnCardPackPosList.Count; ++index)
                {
                    if (index < m_HoldItemList.Count)
                    {
                        Item obj = ItemSpawnManager.GetItem(openCardBoxSpawnCardPackPosList[index]);
                        if (obj != null)
                        {
                            obj.SetMesh(itemMeshData2.mesh, itemMeshData2.material, cardPack, itemMeshData2.meshSecondary, itemMeshData2.materialSecondary);
                            obj.transform.position = openCardBoxSpawnCardPackPosList[index].position;
                            obj.transform.rotation = openCardBoxSpawnCardPackPosList[index].rotation;
                            obj.transform.parent = openCardBoxSpawnCardPackPosList[index];
                            obj.transform.localScale = openCardBoxSpawnCardPackPosList[index].localScale;
                            obj.gameObject.SetActive(true);

                            m_HoldItemList[index] = obj; // Remplacer l'élément fictif par le véritable objet
                            CPlayerData.m_HoldItemTypeList.Add(obj.GetItemType());

                            if (holdCardPackPosList != null && index < holdCardPackPosList.Count && holdCardPackPosList[index] != null)
                            {
                                holdCardPackPosList[index].localScale = Vector3.one;
                            }

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
            if (tex.LoadImage(fileData, false))
            {
                if (tex.width % 4 == 0 && tex.height % 4 == 0)
                {
                    tex.Compress(false);
                }
                tex.Apply(false, true);
                _textureCache[path] = tex;
                return tex;
            }
            UnityEngine.Object.Destroy(tex);
            return null;
        }
        private static Mesh s_OpenDisplayBoxMesh;

        /// <summary>
        /// Construit et met en cache le mesh d'un display ouvert sur le dessus (+Y)
        /// aux dimensions et coordonnées UV exactes du layout Display 1.07 (CardBoxMesh_low).
        /// </summary>
        private static Mesh GetOrCreateOpenDisplayBoxMesh()
        {
            if (s_OpenDisplayBoxMesh != null)
            {
                return s_OpenDisplayBoxMesh;
            }

            const float xMin = -0.781f;
            const float xMax = 0.766f;
            const float yMin = -0.535f;
            const float yMax = 0.588f;
            const float zFront = -0.472f;
            const float zBack = 0.457f;
            const float innerInset = 0.995f;

            var vertices = new List<Vector3>(40);
            var normals = new List<Vector3>(40);
            var uvs = new List<Vector2>(40);
            var triangles = new List<int>(60);

            Vector2 innerUv0 = new Vector2(0.06f, 0.06f);
            Vector2 innerUv1 = new Vector2(0.06f, 0.44f);
            Vector2 innerUv2 = new Vector2(0.28f, 0.44f);
            Vector2 innerUv3 = new Vector2(0.28f, 0.06f);

            void AddWall(
                Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
                Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3,
                Vector3 outwardNormal)
            {
                int outerBase = vertices.Count;
                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);
                for (int i = 0; i < 4; i++) normals.Add(outwardNormal);
                uvs.Add(uv0);
                uvs.Add(uv1);
                uvs.Add(uv2);
                uvs.Add(uv3);
                triangles.Add(outerBase);
                triangles.Add(outerBase + 1);
                triangles.Add(outerBase + 2);
                triangles.Add(outerBase);
                triangles.Add(outerBase + 2);
                triangles.Add(outerBase + 3);

                // Paroi intérieure légèrement en retrait pour éviter tout z-fighting,
                // avec normales/winding inversés et UV sur la zone unie de l'atlas.
                int innerBase = vertices.Count;
                vertices.Add(p0 * innerInset);
                vertices.Add(p1 * innerInset);
                vertices.Add(p2 * innerInset);
                vertices.Add(p3 * innerInset);
                Vector3 inwardNormal = -outwardNormal;
                for (int i = 0; i < 4; i++) normals.Add(inwardNormal);
                uvs.Add(innerUv0);
                uvs.Add(innerUv1);
                uvs.Add(innerUv2);
                uvs.Add(innerUv3);
                triangles.Add(innerBase);
                triangles.Add(innerBase + 2);
                triangles.Add(innerBase + 1);
                triangles.Add(innerBase);
                triangles.Add(innerBase + 3);
                triangles.Add(innerBase + 2);
            }

            // Avant (-Z) : illustration principale
            AddWall(
                new Vector3(xMin, yMin, zFront),
                new Vector3(xMin, yMax, zFront),
                new Vector3(xMax, yMax, zFront),
                new Vector3(xMax, yMin, zFront),
                new Vector2(0.315f, 0.232f),
                new Vector2(0.315f, 0.500f),
                new Vector2(0.684f, 0.500f),
                new Vector2(0.684f, 0.232f),
                Vector3.back
            );

            // Arrière (+Z) : dos
            AddWall(
                new Vector3(xMin, yMax, zBack),
                new Vector3(xMin, yMin, zBack),
                new Vector3(xMax, yMin, zBack),
                new Vector3(xMax, yMax, zBack),
                new Vector2(0.315f, 0.722f),
                new Vector2(0.315f, 0.990f),
                new Vector2(0.684f, 0.990f),
                new Vector2(0.684f, 0.722f),
                Vector3.forward
            );

            // Dessous (-Y)
            AddWall(
                new Vector3(xMin, yMin, zBack),
                new Vector3(xMin, yMin, zFront),
                new Vector3(xMax, yMin, zFront),
                new Vector3(xMax, yMin, zBack),
                new Vector2(0.315f, 0.010f),
                new Vector2(0.315f, 0.232f),
                new Vector2(0.684f, 0.232f),
                new Vector2(0.684f, 0.010f),
                Vector3.down
            );

            // Côté droit (+X)
            AddWall(
                new Vector3(xMax, yMin, zFront),
                new Vector3(xMax, yMax, zFront),
                new Vector3(xMax, yMax, zBack),
                new Vector3(xMax, yMin, zBack),
                new Vector2(0.952f, 0.500f),
                new Vector2(0.684f, 0.500f),
                new Vector2(0.684f, 0.722f),
                new Vector2(0.952f, 0.722f),
                Vector3.right
            );

            // Côté gauche (-X)
            AddWall(
                new Vector3(xMin, yMin, zBack),
                new Vector3(xMin, yMax, zBack),
                new Vector3(xMin, yMax, zFront),
                new Vector3(xMin, yMin, zFront),
                new Vector2(0.047f, 0.722f),
                new Vector2(0.315f, 0.722f),
                new Vector2(0.315f, 0.500f),
                new Vector2(0.047f, 0.500f),
                Vector3.left
            );

            s_OpenDisplayBoxMesh = new Mesh
            {
                name = "WankulOpenDisplayBoxMesh"
            };
            s_OpenDisplayBoxMesh.SetVertices(vertices);
            s_OpenDisplayBoxMesh.SetNormals(normals);
            s_OpenDisplayBoxMesh.SetUVs(0, uvs);
            s_OpenDisplayBoxMesh.SetTriangles(triangles, 0);
            s_OpenDisplayBoxMesh.RecalculateBounds();
            s_OpenDisplayBoxMesh.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return s_OpenDisplayBoxMesh;
        }

        private static void ApplyBoxMaterialToChild(Transform child, EItemType itemType, Dictionary<EItemType, string> paths, Material fallbackMaterial)
        {
            if (child == null)
            {
                return;
            }

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                Plugin.Logger.LogError($"[InteractionPlayerControllerPatch] Impossible de récupérer le Renderer sur '{child.name}'.");
                return;
            }

            Material targetMaterial = null;
            if (paths != null && paths.TryGetValue(itemType, out string texturePath))
            {
                targetMaterial = WankulCrazyPlugin.importer.PatchTexturesImporter.GetOrCreateBoxMaterial(itemType, texturePath, null);
                if (targetMaterial == null)
                {
                    ApplyTextureToChild(child, texturePath);
                    return;
                }
            }
            else if (fallbackMaterial != null && !ShaderUtils.UsesBrokenStandardShader(fallbackMaterial))
            {
                targetMaterial = fallbackMaterial;
            }

            if (targetMaterial != null)
            {
                renderer.sharedMaterial = targetMaterial;
                renderer.material = targetMaterial;
                renderer.SetPropertyBlock(null);
            }
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
    }
}