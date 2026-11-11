using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using WankulCrazyPlugin.cards;
using UnityEngine;
using WankulCrazyPlugin.utils;
using System.Linq;
using WankulCrazyPlugin.utils.obj;

namespace WankulCrazyPlugin.importer
{
    public class CustomItemsImporter
    {
        public static List<ItemMeshData> ItemMeshDataList;
        public static List<ItemData> ItemDataList;
        public static List<RestockData> RestockDataList;
        private static bool isImported = false;

        // Décale les licences Taux+ vanilla pour qu'aucun booster/display Taux+ ne se débloque avant la fin des saisons normales (Display Legacy (8) au Niv 31).
        private static readonly Dictionary<(EItemType ItemType, bool IsBigBox), int> VanillaTauxLevelOverrides =
            new Dictionary<(EItemType ItemType, bool IsBigBox), int>
            {
                { (EItemType.DestinyBasicCardPack, false), 32 },
                { (EItemType.DestinyBasicCardPack, true), 34 },
                { (EItemType.DestinyBasicCardBox, false), 36 },
                { (EItemType.DestinyBasicCardBox, true), 38 },
                { (EItemType.DestinyRareCardPack, false), 39 },
                { (EItemType.DestinyRareCardPack, true), 41 },
                { (EItemType.DestinyRareCardBox, false), 43 },
                { (EItemType.DestinyRareCardBox, true), 45 },
                { (EItemType.DestinyEpicCardPack, false), 46 },
                { (EItemType.DestinyEpicCardPack, true), 49 },
                { (EItemType.DestinyEpicCardBox, false), 51 },
                { (EItemType.DestinyEpicCardBox, true), 54 },
                { (EItemType.DestinyLegendaryCardPack, false), 56 },
                { (EItemType.DestinyLegendaryCardPack, true), 60 },
                { (EItemType.DestinyLegendaryCardBox, false), 65 },
                { (EItemType.DestinyLegendaryCardBox, true), 70 },
            };

        /// <summary>
        /// Met à jour en place les niveaux requis des licences Taux+ vanilla pour qu'elles démarrent après toutes les saisons normales.
        /// </summary>
        public static int ApplyVanillaTauxLevelOverrides(IList<RestockData> restockDataList)
        {
            if (restockDataList == null) return 0;

            int updatedCount = 0;
            for (int i = 0; i < restockDataList.Count; i++)
            {
                var entry = restockDataList[i];
                if (entry == null) continue;

                if (VanillaTauxLevelOverrides.TryGetValue((entry.itemType, entry.isBigBox), out int targetLevel))
                {
                    entry.licenseShopLevelRequired = targetLevel;
                    updatedCount++;
                }
            }
            return updatedCount;
        }

        public static void ImportCustomItems()
        {
            if (isImported) return;
            if (InventoryBase.Instance?.m_StockItemData_SO == null) return;

            var so = InventoryBase.Instance.m_StockItemData_SO;

            ItemDataList = DeserializeItemDataListJson();
            RestockDataList = DeserializeRestockDataListJson();
            ItemMeshDataList = DeserializeItemMeshDataList();

            // 1. Déduplication préventive pour éviter les apparitions en double ou en triple
            if (ItemDataList != null && so.m_ItemDataList != null)
            {
                var customItemNames = new HashSet<string>(ItemDataList.Select(i => i.name), StringComparer.OrdinalIgnoreCase);
                so.m_ItemDataList.RemoveAll(i => i != null && customItemNames.Contains(i.name));
                so.m_ItemDataList.AddRange(ItemDataList);
            }

            if (ItemMeshDataList != null && so.m_ItemMeshDataList != null)
            {
                var customMeshNames = new HashSet<string>(ItemMeshDataList.Select(m => m.name), StringComparer.OrdinalIgnoreCase);
                so.m_ItemMeshDataList.RemoveAll(m => m != null && customMeshNames.Contains(m.name));
                so.m_ItemMeshDataList.AddRange(ItemMeshDataList);
            }

            // 2. Positionnement des licences Restock dans l'ordre chronologique (normal après Battle normal, Taux après Battle Taux)
            if (RestockDataList != null && so.m_RestockDataList != null)
            {
                var customRestockTypes = new HashSet<EItemType>(RestockDataList.Select(r => r.itemType));
                so.m_RestockDataList.RemoveAll(r => customRestockTypes.Contains(r.itemType));

                var tauxRestockTypes = new HashSet<EItemType>
                {
                    EnumExtensions.SafeParseEItemType("BoosterStellarTaux"),
                    EnumExtensions.SafeParseEItemType("DisplayStellarTaux"),
                    EnumExtensions.SafeParseEItemType("BoosterLegacyTaux"),
                    EnumExtensions.SafeParseEItemType("DisplayLegacyTaux")
                };

                var normalRestockItems = RestockDataList.Where(r => !tauxRestockTypes.Contains(r.itemType)).ToList();
                var tauxRestockItems = RestockDataList.Where(r => tauxRestockTypes.Contains(r.itemType)).ToList();

                // 2.a Inserer les normaux apres Battle normal (EpicCardBox)
                int battleNormalIdx = so.m_RestockDataList.FindLastIndex(r => 
                    r.itemType == EItemType.EpicCardBox || r.itemType == EItemType.EpicCardPack ||
                    (r.name != null && r.name.IndexOf("Epic", StringComparison.OrdinalIgnoreCase) >= 0 && r.name.IndexOf("Destiny", StringComparison.OrdinalIgnoreCase) < 0));

                if (battleNormalIdx >= 0 && battleNormalIdx + 1 <= so.m_RestockDataList.Count)
                {
                    so.m_RestockDataList.InsertRange(battleNormalIdx + 1, normalRestockItems);
                    Plugin.LogInfo($"[CustomItemsImporter] Restock normal inséré après Battle normal à l'index {battleNormalIdx + 1}.");
                }
                else
                {
                    so.m_RestockDataList.AddRange(normalRestockItems);
                }

                // 2.b Inserer les Taux apres Battle Taux (DestinyEpicCardBox)
                int battleTauxIdx = so.m_RestockDataList.FindLastIndex(r => 
                    r.itemType == EItemType.DestinyEpicCardBox || r.itemType == EItemType.DestinyEpicCardPack ||
                    (r.name != null && (r.name.IndexOf("Destiny Epic", StringComparison.OrdinalIgnoreCase) >= 0 || (r.name.IndexOf("Battle", StringComparison.OrdinalIgnoreCase) >= 0 && r.name.IndexOf("Taux", StringComparison.OrdinalIgnoreCase) >= 0))));

                if (battleTauxIdx >= 0 && battleTauxIdx + 1 <= so.m_RestockDataList.Count)
                {
                    so.m_RestockDataList.InsertRange(battleTauxIdx + 1, tauxRestockItems);
                    Plugin.LogInfo($"[CustomItemsImporter] Restock Taux inséré après Battle Taux à l'index {battleTauxIdx + 1}.");
                }
                else
                {
                    so.m_RestockDataList.AddRange(tauxRestockItems);
                }
            }

            // 2.c Recaler les Taux+ vanilla pour qu'ils commencent au Niv 32 (après l'intégralité des saisons normales)
            if (so.m_RestockDataList != null)
            {
                int updatedTauxCount = ApplyVanillaTauxLevelOverrides(so.m_RestockDataList);
                Plugin.LogInfo($"[CustomItemsImporter] Niveaux Taux+ vanilla recalés sur {updatedTauxCount} licence(s) (>= Niv 32).");
            }

            // 3. Positionnement dans les catégories du shop (m_ShownItemType, m_ShownAccessoryItemType, m_ShownFigurineItemType)
            RegisterCustomItemsToShopCategories(ItemDataList);

            isImported = true;
            Plugin.LogInfo("[CustomItemsImporter] Custom items successfully imported & positioned in shop categories.");

            SyncCustomItemPrices();
        }

        public static List<ItemData> DeserializeItemDataListJson()
        {
            List<ItemData> result = new List<ItemData>();
            string root = Path.Combine(Plugin.GetPluginPath(), "data/customitems");
            string path = Path.Combine(root, "ItemDataList.json");
            try
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    JArray array = JArray.Parse(content);
                    foreach (JObject json in array)
                    {
                        string catStr = (string)json["category"];
                        EItemCategory cat = EItemCategory.None;
                        if (!string.IsNullOrEmpty(catStr))
                        {
                            if (!Enum.TryParse(catStr, true, out cat))
                            {
                                Plugin.Logger.LogWarning($"[CustomItemsImporter] Unknown category '{catStr}', defaulting to None.");
                            }
                        }

                        string itemTypeName = (string)json["itemType"];
                        string displayName = (string)json["name"];
                        ItemData item = new ItemData
                        {
                            name = !string.IsNullOrEmpty(itemTypeName) ? itemTypeName : displayName,
                            category = cat,
                            iconScale = (float)(json["iconScale"] ?? 1f),
                            baseCost = (float)(json["baseCost"] ?? 0f),
                            marketPriceMinPercent = (float)(json["marketPriceMinPercent"] ?? 0f),
                            marketPriceMaxPercent = (float)(json["marketPriceMaxPercent"] ?? 0f),
                            boxFollowItemPrice = EnumExtensions.SafeParseEItemType((string)json["boxFollowItemPrice"]),
                            isNotBoosterPack = (bool)(json["isNotBoosterPack"] ?? false),
                            isTallItem = (bool)(json["isTallItem"] ?? false),
                            isHideItemUntilUnlocked = (bool)(json["isHideItemUntilUnlocked"] ?? false),
                            posYOffsetInBox = (float)(json["posYOffsetInBox"] ?? 0f),
                            scaleOffsetInBox = (float)(json["scaleOffsetInBox"] ?? 0f)
                        };

                        item.affectedPriceChangeType = new List<EPriceChangeType>();
                        if (json["affectedPriceChangeType"] is JArray priceArr)
                        {
                            foreach (var pct in priceArr)
                            {
                                string pctStr = (string)pct;
                                if (!string.IsNullOrEmpty(pctStr) && Enum.TryParse(pctStr, true, out EPriceChangeType parsedPct))
                                {
                                    item.affectedPriceChangeType.Add(parsedPct);
                                }
                                else
                                {
                                    Plugin.Logger.LogWarning($"[CustomItemsImporter] Unknown EPriceChangeType '{pctStr}', ignored.");
                                }
                            }
                        }

                        item.itemDimension = ReadVector(json["itemDimension"]);
                        item.colliderPosOffset = ReadVector(json["colliderPosOffset"]);
                        item.colliderScale = ReadVector(json["colliderScale"]);

                        string iconProp = (string)json["icon"];
                        if (!string.IsNullOrEmpty(iconProp))
                        {
                            string iconPath = Path.Combine(root, "icons", iconProp);
                            if (!File.Exists(iconPath))
                            {
                                string spritesPath = Path.Combine(Plugin.GetPluginPath(), "data", "sprites", iconProp);
                                if (File.Exists(spritesPath))
                                {
                                    iconPath = spritesPath;
                                }
                                else
                                {
                                    string patchTexPath = Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", iconProp);
                                    if (File.Exists(patchTexPath))
                                    {
                                        iconPath = patchTexPath;
                                    }
                                }
                            }

                            if (File.Exists(iconPath))
                            {
                                Texture2D texture = TextureUtils.LoadTexture(iconPath);
                                item.icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
                                item.icon.name = item.name + "_icon";
                            }
                            else Plugin.Logger.LogWarning("Test/custom item icon not found yet: " + iconPath);
                        }

                        result.Add(item);
                    }
                }
            }
            catch (Exception ex) { Plugin.Logger.LogError("Failed to deserialize JSON ItemData: " + ex.Message); }
            return result;
        }

        private static Vector3 ReadVector(JToken token)
        {
            if (token == null) return Vector3.zero;
            return new Vector3((float)(token["x"] ?? 0f), (float)(token["y"] ?? 0f), (float)(token["z"] ?? 0f));
        }

        public static List<RestockData> DeserializeRestockDataListJson()
        {
            List<RestockData> result = new List<RestockData>();
            string path = Path.Combine(Plugin.GetPluginPath(), "data/customitems/restockDataList.json");
            try
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    JArray array = JArray.Parse(content);
                    foreach (JObject json in array)
                    {
                        result.Add(new RestockData
                        {
                            index = (int)(json["index"] ?? 0),
                            name = (string)json["name"],
                            isBigBox = (bool)(json["isBigBox"] ?? false),
                            ignoreDoubleImage = (bool)(json["ignoreDoubleImage"] ?? false),
                            amount = (int)(json["amount"] ?? 0),
                            licenseShopLevelRequired = (int)(json["licenseShopLevelRequired"] ?? 0),
                            licensePrice = (float)(json["licensePrice"] ?? 0f),
                            itemType = EnumExtensions.SafeParseEItemType((string)json["itemType"]),
                            prologueShow = (bool)(json["prologueShow"] ?? false),
                            isHideItemUntilUnlocked = (bool)(json["isHideItemUntilUnlocked"] ?? false)
                        });
                    }
                }
            }
            catch (Exception ex) { Plugin.Logger.LogError("Failed to deserialize JSON RestockData: " + ex.Message); }
            return result;
        }

        public static List<ItemMeshData> DeserializeItemMeshDataList()
        {
            List<ItemMeshData> result = new List<ItemMeshData>();
            string root = Path.Combine(Plugin.GetPluginPath(), "data/customitems");
            string path = Path.Combine(root, "itemMeshDataList.json");
            try
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    JArray array = JArray.Parse(content);
                    foreach (JObject json in array)
                    {
                        string itemTypeStr = (string)json["itemType"];
                        string nameStr = (string)json["name"];
                        ItemMeshData mesh = new ItemMeshData { name = !string.IsNullOrEmpty(itemTypeStr) ? itemTypeStr : nameStr };
                        
                        string importType = (string)json["importType"] ?? "CopyItem";
                        string objProp = (string)json["obj"];

                        if (importType.Equals("ImportObj", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(objProp))
                        {
                            string objPath = Path.Combine(root, "meshes", objProp ?? "Calecon_S4.obj");
                            if (File.Exists(objPath))
                            {
                                try
                                {
                                    GameObject loadedObj = new OBJLoader().Load(objPath);
                                    if (loadedObj != null)
                                    {
                                        List<Mesh> meshList = new List<Mesh>();
                                        MeshFilter[] meshFilters = loadedObj.GetComponentsInChildren<MeshFilter>();
                                        foreach (MeshFilter mf in meshFilters)
                                        {
                                            if (mf.mesh != null)
                                            {
                                                Mesh m = mf.mesh;
                                                Vector3[] vertices = m.vertices;
                                                for (int i = 0; i < vertices.Length; i++)
                                                    vertices[i].z = -vertices[i].z;
                                                int[] triangles = m.triangles;
                                                for (int i = 0; i < triangles.Length; i += 3)
                                                {
                                                    int temp = triangles[i];
                                                    triangles[i] = triangles[i + 2];
                                                    triangles[i + 2] = temp;
                                                }
                                                m.vertices = vertices;
                                                m.triangles = triangles;
                                                meshList.Add(m);
                                            }
                                        }

                                        CombineInstance[] combine = new CombineInstance[meshList.Count];
                                        for (int i = 0; i < meshList.Count; i++)
                                        {
                                            combine[i].mesh = meshList[i];
                                            combine[i].transform = Matrix4x4.identity;
                                        }

                                        Mesh finalMesh = new Mesh();
                                        finalMesh.CombineMeshes(combine, false);
                                        finalMesh.name = mesh.name;
                                        mesh.mesh = finalMesh;

                                        loadedObj.SetActive(false);
                                        UnityEngine.Object.Destroy(loadedObj);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Plugin.Logger.LogError($"[CustomItemsImporter] Error loading OBJ {objPath}: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            string copyType = (string)json["copyItemType"];
                            if (string.IsNullOrEmpty(copyType) && !string.IsNullOrEmpty(itemTypeStr))
                            {
                                copyType = "BasicCardPack";
                            }
                            
                            ItemMeshData source = InventoryBase.GetItemMeshData(EnumExtensions.SafeParseEItemType(copyType));
                            if (source != null)
                            {
                                mesh.mesh = source.mesh;
                                mesh.meshSecondary = source.meshSecondary;
                                mesh.materialSecondary = source.materialSecondary;
                                mesh.material = source.material;
                                mesh.arrayMaterialType = ItemMeshData.ItemShareTexture2DArrayMaterial.None;
                            }
                        }

                        string texProp = (string)json["texture"];
                        if (!string.IsNullOrEmpty(texProp))
                        {
                            string texturePath = Path.Combine(root, "textures", texProp);
                            if (!File.Exists(texturePath))
                            {
                                string patchTexPath = Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", texProp);
                                if (File.Exists(patchTexPath))
                                {
                                    texturePath = patchTexPath;
                                }
                            }

                            if (File.Exists(texturePath))
                            {
                                Texture2D texture = TextureUtils.LoadTexture(texturePath);
                                Material newMat = WankulCrazyPlugin.utils.ShaderUtils.CreateSafeMaterial(mesh.material);
                                if (newMat.HasProperty("_BaseColorMap")) newMat.SetTexture("_BaseColorMap", texture);
                                if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", texture);
                                if (newMat.HasProperty("_MainTex")) newMat.SetTexture("_MainTex", texture);
                                mesh.material = newMat;
                                EItemType currentType = EnumExtensions.SafeParseEItemType(itemTypeStr);
                                if (mesh.meshSecondary != null)
                                {
                                    if (PatchTexturesImporter.IsBoosterPackType(currentType))
                                    {
                                        mesh.materialSecondary = newMat;
                                    }
                                    else
                                    {
                                        EItemType boosterType = PatchTexturesImporter.GetBoosterTypeForDisplay(currentType);
                                        if (boosterType != EItemType.None)
                                        {
                                            var allBoxPaths = PatchTexturesImporter.GetBoxTexturePaths();
                                            if (allBoxPaths.TryGetValue(boosterType, out string bPath) && File.Exists(bPath))
                                            {
                                                Texture2D bTex = TextureUtils.LoadTexture(bPath);
                                                Material boosterMat = WankulCrazyPlugin.utils.ShaderUtils.CreateSafeMaterial(mesh.material);
                                                if (boosterMat.HasProperty("_BaseColorMap")) boosterMat.SetTexture("_BaseColorMap", bTex);
                                                if (boosterMat.HasProperty("_BaseMap")) boosterMat.SetTexture("_BaseMap", bTex);
                                                if (boosterMat.HasProperty("_MainTex")) boosterMat.SetTexture("_MainTex", bTex);
                                                boosterMat.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                                                mesh.materialSecondary = boosterMat;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Un mesh 1.07 sans meshSecondary (1 seul submesh) ne doit jamais recevoir
                                    // materialSecondary : sinon Item.SetMesh met 2 materiaux sur m_Mesh et Unity
                                    // redessine la face avec la texture booster par-dessus celle du display.
                                    mesh.materialSecondary = null;
                                }
                                mesh.arrayMaterialType = ItemMeshData.ItemShareTexture2DArrayMaterial.None;
                            }
                            else Plugin.Logger.LogWarning("Test/custom item texture not found yet: " + texturePath);
                        }
                        result.Add(mesh);
                    }
                }
            }
            catch (Exception ex) { Plugin.Logger.LogError("Failed to deserialize JSON ItemMeshData: " + ex.Message); }
            return result;
        }

        public static void ItemTypeToCollectionPackType(EItemType itemType, ref ECollectionPackType __result)
        {
            EItemType boosterStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType displayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
            EItemType boosterStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            EItemType displayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
            EItemType boosterGoldBattle = EnumExtensions.SafeParseEItemType("BoosterGoldBattle");
            EItemType boosterGoldStellar = EnumExtensions.SafeParseEItemType("BoosterGoldStellar");
            EItemType boosterGoldLegacy = EnumExtensions.SafeParseEItemType("BoosterGoldLegacy");
            ECollectionPackType stellarPack = EnumExtensions.SafeParseECollectionPackType("Stellar");
            ECollectionPackType stellarPackTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");

            EItemType boosterLegacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");
            EItemType displayLegacy = EnumExtensions.SafeParseEItemType("DisplayLegacy");
            EItemType boosterLegacyTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
            EItemType displayLegacyTaux = EnumExtensions.SafeParseEItemType("DisplayLegacyTaux");
            ECollectionPackType legacyPack = EnumExtensions.SafeParseECollectionPackType("Legacy");
            ECollectionPackType legacyPackTaux = EnumExtensions.SafeParseECollectionPackType("LegacyTaux");

            if (itemType == EItemType.BasicCardPack || itemType == EItemType.BasicCardBox) return; // Keep original __result
            else if (itemType == EItemType.RareCardPack || itemType == EItemType.RareCardBox) return; // Keep original __result
            else if (itemType == EItemType.EpicCardPack || itemType == EItemType.EpicCardBox || itemType == boosterGoldBattle) __result = ECollectionPackType.EpicCardPack;
            else if (itemType == EItemType.LegendaryCardPack || itemType == EItemType.LegendaryCardBox) return; // Keep original __result
            else if (itemType == EItemType.DestinyBasicCardPack || itemType == EItemType.DestinyBasicCardBox) return; // Keep original __result
            else if (itemType == EItemType.DestinyRareCardPack || itemType == EItemType.DestinyRareCardBox) return; // Keep original __result
            else if (itemType == EItemType.DestinyEpicCardPack || itemType == EItemType.DestinyEpicCardBox) return; // Keep original __result
            else if (itemType == EItemType.DestinyLegendaryCardPack || itemType == EItemType.DestinyLegendaryCardBox) return; // Keep original __result
            else if (itemType == EItemType.GhostPack) return; // Keep original __result
            else if (itemType == EItemType.MegabotPack) return; // Keep original __result
            else if (itemType == EItemType.FantasyRPGPack) return; // Keep original __result
            else if (itemType == EItemType.CatJobPack) return; // Keep original __result
            else if (itemType == boosterStellar || itemType == displayStellar || itemType == boosterGoldStellar) __result = stellarPack;
            else if (itemType == boosterStellarTaux || itemType == displayStellarTaux) __result = stellarPackTaux;
            else if (itemType == boosterLegacy || itemType == displayLegacy || itemType == boosterGoldLegacy) __result = legacyPack;
            else if (itemType == boosterLegacyTaux || itemType == displayLegacyTaux) __result = legacyPackTaux;
        }

        public static void GetCardExpansionType(ECollectionPackType collectionPackType, ref ECardExpansionType __result)
        {
            ECollectionPackType stellarPack = EnumExtensions.SafeParseECollectionPackType("Stellar");
            ECollectionPackType stellarPackTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");
            ECollectionPackType legacyPack = EnumExtensions.SafeParseECollectionPackType("Legacy");
            ECollectionPackType legacyPackTaux = EnumExtensions.SafeParseECollectionPackType("LegacyTaux");

            if (collectionPackType == stellarPack || collectionPackType == stellarPackTaux ||
                collectionPackType == legacyPack || collectionPackType == legacyPackTaux)
            {
                __result = ECardExpansionType.Tetramon;
            }
        }

        private static void RegisterCustomItemsToShopCategories(List<ItemData> items)
        {
            if (InventoryBase.Instance?.m_StockItemData_SO == null) return;
            var so = InventoryBase.Instance.m_StockItemData_SO;

            // Définition des types custom
            EItemType boosterStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType displayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
            EItemType boosterStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            EItemType displayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
            EItemType boosterLegacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");
            EItemType displayLegacy = EnumExtensions.SafeParseEItemType("DisplayLegacy");
            EItemType boosterLegacyTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
            EItemType displayLegacyTaux = EnumExtensions.SafeParseEItemType("DisplayLegacyTaux");
            EItemType starterApocalypse = EnumExtensions.SafeParseEItemType("StarterApocalypse");
            EItemType starterShowtime = EnumExtensions.SafeParseEItemType("StarterShowtime");
            EItemType caleconStellar = EnumExtensions.SafeParseEItemType("CaleconStellar");
            EItemType tapisS41 = EnumExtensions.SafeParseEItemType("TapisS41");
            EItemType tapisS42 = EnumExtensions.SafeParseEItemType("TapisS42");
            EItemType classeurS4 = EnumExtensions.SafeParseEItemType("ClasseurS4");

            // Nettoyage complet des objets custom dans TOUTES les listes pour éviter qu'un objet se retrouve dans le mauvais onglet
            var allCustom = new List<EItemType>
            {
                boosterStellar, displayStellar, boosterStellarTaux, displayStellarTaux,
                boosterLegacy, displayLegacy, boosterLegacyTaux, displayLegacyTaux,
                starterApocalypse, starterShowtime, caleconStellar, tapisS41, tapisS42, classeurS4
            };

            if (so.m_ShownItemType != null) so.m_ShownItemType.RemoveAll(t => allCustom.Contains(t));
            if (so.m_ShownAccessoryItemType != null) so.m_ShownAccessoryItemType.RemoveAll(t => allCustom.Contains(t));
            if (so.m_ShownFigurineItemType != null) so.m_ShownFigurineItemType.RemoveAll(t => allCustom.Contains(t));

            // Insertion des Packs dans m_ShownItemType :
            // Dans le magasin, les types de packs sont regroupés :
            // 1) Packs normaux : Basic -> Rare -> Epic (Battle) -> Stellar -> Legacy
            // 2) Decks préconstruits : PreconDeck_Wind -> Starters S4
            // 3) Packs Taux : DestinyBasic -> DestinyRare -> DestinyEpic (Battle Taux) -> Stellar Taux -> Legacy Taux
            if (so.m_ShownItemType != null)
            {
                var normalPacks = new List<EItemType> { boosterStellar, displayStellar, boosterLegacy, displayLegacy };
                var s4Starters = new List<EItemType> { starterApocalypse, starterShowtime };
                var tauxPacks = new List<EItemType> { boosterStellarTaux, displayStellarTaux, boosterLegacyTaux, displayLegacyTaux };

                // 1) Normal après Battle normal (EpicCardBox)
                int battleNormalIdx = so.m_ShownItemType.FindLastIndex(t => t == EItemType.EpicCardBox || t == EItemType.EpicCardPack);
                if (battleNormalIdx >= 0 && battleNormalIdx + 1 <= so.m_ShownItemType.Count)
                {
                    so.m_ShownItemType.InsertRange(battleNormalIdx + 1, normalPacks);
                    Plugin.LogInfo($"[CustomItemsImporter] Inserted Normal packs at index {battleNormalIdx + 1} after Battle (EpicCardBox).");
                }
                else
                {
                    so.m_ShownItemType.AddRange(normalPacks);
                }

                // 2) Starters après les decks de départ (PreconDeck_Wind)
                int deckIdx = so.m_ShownItemType.FindLastIndex(t => t == EItemType.PreconDeck_Wind || t == EItemType.PreconDeck_Water || t == EItemType.PreconDeck_Earth || t == EItemType.PreconDeck_Fire);
                if (deckIdx >= 0 && deckIdx + 1 <= so.m_ShownItemType.Count)
                {
                    so.m_ShownItemType.InsertRange(deckIdx + 1, s4Starters);
                    Plugin.LogInfo($"[CustomItemsImporter] Inserted Starters at index {deckIdx + 1} after PreconDeck_Wind.");
                }
                else
                {
                    so.m_ShownItemType.AddRange(s4Starters);
                }

                // 3) Taux après Battle Taux (DestinyEpicCardBox)
                int battleTauxIdx = so.m_ShownItemType.FindLastIndex(t => t == EItemType.DestinyEpicCardBox || t == EItemType.DestinyEpicCardPack);
                if (battleTauxIdx >= 0 && battleTauxIdx + 1 <= so.m_ShownItemType.Count)
                {
                    so.m_ShownItemType.InsertRange(battleTauxIdx + 1, tauxPacks);
                    Plugin.LogInfo($"[CustomItemsImporter] Inserted Taux packs at index {battleTauxIdx + 1} after Battle Taux (DestinyEpicCardBox).");
                }
                else
                {
                    so.m_ShownItemType.AddRange(tauxPacks);
                }
            }

            // Onglet Figurines (CaleconStellar uniquement)
            if (so.m_ShownFigurineItemType != null)
            {
                so.m_ShownFigurineItemType.Add(caleconStellar);
                Plugin.LogInfo("[CustomItemsImporter] Added CaleconStellar to m_ShownFigurineItemType.");
            }

            // Onglet Accessoires (TapisS41, TapisS42, ClasseurS4)
            if (so.m_ShownAccessoryItemType != null)
            {
                var s4Accessories = new List<EItemType> { tapisS41, tapisS42, classeurS4 };
                so.m_ShownAccessoryItemType.AddRange(s4Accessories);
                Plugin.LogInfo($"[CustomItemsImporter] Added {s4Accessories.Count} accessories to m_ShownAccessoryItemType.");
            }

            // Enregistrement des boosters custom dans m_CardPackItemTypeList (utilisé par les employés pour remplir l'AutoPackOpener)
            if (so.m_CardPackItemTypeList != null)
            {
                var customCardPacks = new List<EItemType> { boosterStellar, boosterStellarTaux, boosterLegacy, boosterLegacyTaux };
                foreach (var packType in customCardPacks)
                {
                    if (!so.m_CardPackItemTypeList.Contains(packType))
                    {
                        so.m_CardPackItemTypeList.Add(packType);
                    }
                }
                Plugin.LogInfo($"[CustomItemsImporter] Synced custom packs to m_CardPackItemTypeList (total: {so.m_CardPackItemTypeList.Count}).");

                // Synchronise également tous les employés déjà instanciés en scène
                try
                {
                    if (CSingleton<WorkerManager>.Instance != null)
                    {
                        var workers = WorkerManager.GetWorkerList();
                        if (workers != null)
                        {
                            for (int i = 0; i < workers.Count; i++)
                            {
                                WankulCrazyPlugin.patch.InteractableAutoPackOpenerPatch.SyncWorkerCardPackLists(workers[i]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"[CustomItemsImporter] Worker pack list sync skipped: {ex.Message}");
                }
            }

            // Affichage exhaustif des listes pour debug immédiat dans la console
            Plugin.LogInfo("=== [SHOP DEBUG: m_ShownItemType (Boosters)] ===");
            if (so.m_ShownItemType != null)
            {
                for (int i = 0; i < so.m_ShownItemType.Count; i++)
                {
                    Plugin.LogInfo($"  [{i}] {so.m_ShownItemType[i]}");
                }
            }

            Plugin.LogInfo("=== [SHOP DEBUG: m_ShownAccessoryItemType (Accessoires)] ===");
            if (so.m_ShownAccessoryItemType != null)
            {
                for (int i = 0; i < so.m_ShownAccessoryItemType.Count; i++)
                {
                    Plugin.LogInfo($"  [{i}] {so.m_ShownAccessoryItemType[i]}");
                }
            }

            Plugin.LogInfo("=== [SHOP DEBUG: m_ShownFigurineItemType (Figurines)] ===");
            if (so.m_ShownFigurineItemType != null)
            {
                for (int i = 0; i < so.m_ShownFigurineItemType.Count; i++)
                {
                    Plugin.LogInfo($"  [{i}] {so.m_ShownFigurineItemType[i]}");
                }
            }

            Plugin.LogInfo("=== [SHOP DEBUG: m_RestockDataList (Licences boutique)] ===");
            if (so.m_RestockDataList != null)
            {
                for (int i = 0; i < so.m_RestockDataList.Count; i++)
                {
                    var r = so.m_RestockDataList[i];
                    Plugin.LogInfo($"  [{i}] {r.name} (itemType={r.itemType}, Level={r.licenseShopLevelRequired})");
                }
            }
        }

        public static void LogShopEvaluation(int pageIndex)
        {
            string tabName = pageIndex switch
            {
                0 => "Boosters",
                1 => "Accessoires",
                2 => "Figurines",
                3 => "Tous",
                _ => $"Onglet #{pageIndex}"
            };
            Plugin.LogInfo($"[SHOP EVALUATE] Ouverture de l'onglet: {tabName} (index={pageIndex})");

            var so = CSingleton<InventoryBase>.Instance?.m_StockItemData_SO;
            if (so != null)
            {
                List<EItemType> list = pageIndex switch
                {
                    0 => so.m_ShownItemType,
                    1 => so.m_ShownAccessoryItemType,
                    2 => so.m_ShownFigurineItemType,
                    3 => so.m_ShownAllItemType,
                    _ => null
                };

                if (list != null)
                {
                    Plugin.LogInfo($"[SHOP EVALUATE] {tabName} contient {list.Count} éléments :");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var it = list[i];
                        bool hasRestock = so.m_RestockDataList != null && so.m_RestockDataList.Exists(r => r.itemType == it);
                        Plugin.LogInfo($"  [{i}] {it} (HasRestockData: {hasRestock})");
                    }
                }
            }
        }

        public static bool GetItemMeshDataPrefix(EItemType itemType, ref ItemMeshData __result)
        {
            if (ItemMeshDataList != null)
            {
                string typeStr = itemType.ToString();
                foreach (ItemMeshData customMesh in ItemMeshDataList)
                {
                    if (customMesh != null && (
                        EnumExtensions.SafeParseEItemType(customMesh.name) == itemType ||
                        customMesh.name.Equals(typeStr, StringComparison.OrdinalIgnoreCase) ||
                        customMesh.name.StartsWith(typeStr + "_", StringComparison.OrdinalIgnoreCase)))
                    {
                        __result = customMesh;
                        return false;
                    }
                }
            }

            var stock = InventoryBase.Instance?.m_StockItemData_SO?.m_ItemMeshDataList;
            if (stock != null)
            {
                int index = (int)itemType;
                if (index < 0 || index >= stock.Count)
                {
                    // Pour les boîtes de display custom, utiliser le mesh et material de base d'une boîte de boosters
                    EItemType displayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
                    EItemType displayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
                    EItemType displayLegacy = EnumExtensions.SafeParseEItemType("DisplayLegacy");
                    EItemType displayLegacyTaux = EnumExtensions.SafeParseEItemType("DisplayLegacyTaux");

                    if (itemType == displayStellar || itemType == displayStellarTaux || itemType == displayLegacy || itemType == displayLegacyTaux)
                    {
                        ItemMeshData baseBoxMesh = InventoryBase.GetItemMeshData(EItemType.BasicCardBox);
                        if (baseBoxMesh != null)
                        {
                            EItemType boosterType = PatchTexturesImporter.GetBoosterTypeForDisplay(itemType);
                            ItemMeshData boosterMesh = boosterType != EItemType.None ? InventoryBase.GetItemMeshData(boosterType) : null;
                            __result = new ItemMeshData
                            {
                                name = itemType.ToString(),
                                mesh = baseBoxMesh.mesh,
                                material = baseBoxMesh.material,
                                meshSecondary = baseBoxMesh.meshSecondary,
                                materialSecondary = baseBoxMesh.meshSecondary != null ? (boosterMesh?.material ?? baseBoxMesh.materialSecondary) : null,
                                arrayMaterialType = ItemMeshData.ItemShareTexture2DArrayMaterial.None
                            };
                            return false;
                        }
                    }

                    // Pour les boosters custom, utiliser le mesh et material de base d'un booster de base
                    EItemType boosterStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
                    EItemType boosterStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
                    EItemType boosterLegacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");
                    EItemType boosterLegacyTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");

                    if (itemType == boosterStellar || itemType == boosterStellarTaux || itemType == boosterLegacy || itemType == boosterLegacyTaux)
                    {
                        ItemMeshData basePackMesh = InventoryBase.GetItemMeshData(EItemType.BasicCardPack);
                        if (basePackMesh != null)
                        {
                            __result = new ItemMeshData
                            {
                                name = itemType.ToString(),
                                mesh = basePackMesh.mesh,
                                material = basePackMesh.material,
                                meshSecondary = basePackMesh.meshSecondary,
                                materialSecondary = basePackMesh.materialSecondary
                            };
                            return false;
                        }
                    }

                    // Fallback pour tout autre enum custom ou hors limites pour éviter l'exception d'index
                    __result = new ItemMeshData();
                    return false;
                }
            }

            return true;
        }

        public static bool GetItemDataPrefix(EItemType itemType, ref ItemData __result)
        {
            if (ItemDataList != null)
            {
                foreach (ItemData customItem in ItemDataList)
                {
                    if (customItem != null && EnumExtensions.SafeParseEItemType(customItem.name) == itemType)
                    {
                        __result = customItem;
                        return false;
                    }
                }
            }

            var stock = InventoryBase.Instance?.m_StockItemData_SO?.m_ItemDataList;
            if (stock != null)
            {
                int index = (int)itemType;
                if (index < 0 || index >= stock.Count)
                {
                    EItemType dsStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
                    EItemType dsStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
                    EItemType dsLegacy = EnumExtensions.SafeParseEItemType("DisplayLegacy");
                    EItemType dsLegacyTaux = EnumExtensions.SafeParseEItemType("DisplayLegacyTaux");

                    if (itemType == dsStellar || itemType == dsStellarTaux || itemType == dsLegacy || itemType == dsLegacyTaux)
                    {
                        ItemData baseBoxData = InventoryBase.GetItemData(EItemType.BasicCardBox);
                        if (baseBoxData != null)
                        {
                            string jsonString = UnityEngine.JsonUtility.ToJson(baseBoxData);
                            __result = UnityEngine.JsonUtility.FromJson<ItemData>(jsonString);
                            __result.name = itemType.ToString();
                            return false;
                        }
                    }

                    EItemType bsStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
                    EItemType bsStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
                    EItemType bsLegacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");
                    EItemType bsLegacyTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
                    EItemType bsGoldBattle = EnumExtensions.SafeParseEItemType("BoosterGoldBattle");
                    EItemType bsGoldStellar = EnumExtensions.SafeParseEItemType("BoosterGoldStellar");
                    EItemType bsGoldLegacy = EnumExtensions.SafeParseEItemType("BoosterGoldLegacy");

                    if (itemType == bsStellar || itemType == bsStellarTaux || itemType == bsLegacy || itemType == bsLegacyTaux || itemType == bsGoldBattle || itemType == bsGoldStellar || itemType == bsGoldLegacy)
                    {
                        ItemData basePackData = InventoryBase.GetItemData(EItemType.BasicCardPack);
                        if (basePackData != null)
                        {
                            string jsonString = UnityEngine.JsonUtility.ToJson(basePackData);
                            __result = UnityEngine.JsonUtility.FromJson<ItemData>(jsonString);
                            __result.name = itemType.ToString();
                            return false;
                        }
                    }

                    // Fallback pour tout autre enum custom ou hors limites pour éviter l'exception d'index
                    __result = new ItemData();
                    return false;
                }
            }

            return true;
        }

        public static void SyncCustomItemPrices()
        {
            try
            {
                if (CPlayerData.m_GeneratedCostPriceList == null || CPlayerData.m_GeneratedCostPriceList.Count == 0) return;
                if (ItemDataList == null || ItemDataList.Count == 0) return;

                // 1. Première passe : items normaux (boosters, accessoires, etc.)
                foreach (var customItem in ItemDataList)
                {
                    EItemType itemType = EnumExtensions.SafeParseEItemType(customItem.name);
                    int itemIndex = (int)itemType;
                    if (itemIndex >= 0 && itemIndex < CPlayerData.m_GeneratedCostPriceList.Count)
                    {
                        if (customItem.boxFollowItemPrice == EItemType.None)
                        {
                            float currentCost = CPlayerData.m_GeneratedCostPriceList[itemIndex];
                            // Si le prix généré stocké dans la sauvegarde diverge anormalement du baseCost configuré
                            if (currentCost <= 0f || currentCost > customItem.baseCost * 2f || currentCost < customItem.baseCost * 0.4f)
                            {
                                float newCost = customItem.baseCost * UnityEngine.Random.Range(1f, 1.15f);
                                CPlayerData.m_GeneratedCostPriceList[itemIndex] = newCost;

                                float minP = customItem.marketPriceMinPercent > 0 ? customItem.marketPriceMinPercent : 1.2f;
                                float maxP = customItem.marketPriceMaxPercent > 0 ? customItem.marketPriceMaxPercent : 1.8f;
                                if (itemIndex < CPlayerData.m_GeneratedMarketPriceList.Count)
                                {
                                    CPlayerData.m_GeneratedMarketPriceList[itemIndex] = newCost * UnityEngine.Random.Range(minP, maxP);
                                }
                                Plugin.LogInfo($"[PriceSync] Prix réajusté pour {customItem.name} : Cost={newCost:F2}€ (baseCost={customItem.baseCost})");
                            }
                        }
                    }
                }

                // 2. Deuxième passe : items boîtes / displays (boxFollowItemPrice)
                foreach (var customItem in ItemDataList)
                {
                    EItemType itemType = EnumExtensions.SafeParseEItemType(customItem.name);
                    int itemIndex = (int)itemType;
                    if (itemIndex >= 0 && itemIndex < CPlayerData.m_GeneratedCostPriceList.Count)
                    {
                        if (customItem.boxFollowItemPrice != EItemType.None)
                        {
                            float boosterCost = CPlayerData.GetItemCost(customItem.boxFollowItemPrice);
                            float newCost = boosterCost * 8f * UnityEngine.Random.Range(1f, 1.1f);
                            CPlayerData.m_GeneratedCostPriceList[itemIndex] = newCost;

                            float boosterMarket = CPlayerData.GetItemMarketPrice(customItem.boxFollowItemPrice);
                            float minP = customItem.marketPriceMinPercent > 0 ? customItem.marketPriceMinPercent : 0.9f;
                            float maxP = customItem.marketPriceMaxPercent > 0 ? customItem.marketPriceMaxPercent : 1.1f;
                            if (itemIndex < CPlayerData.m_GeneratedMarketPriceList.Count)
                            {
                                CPlayerData.m_GeneratedMarketPriceList[itemIndex] = boosterMarket * 8f * UnityEngine.Random.Range(minP, maxP);
                            }
                            Plugin.LogInfo($"[PriceSync] Display réajusté pour {customItem.name} : Cost={newCost:F2}€ (suivi de {customItem.boxFollowItemPrice})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[CustomItemsImporter] Erreur SyncCustomItemPrices : {ex}");
            }
        }

        private static Dictionary<EItemType, ItemData> _customItemDataCache;

        public static ItemData GetCustomItemData(EItemType itemType)
        {
            if (ItemDataList == null) return null;
            if (_customItemDataCache == null || _customItemDataCache.Count != ItemDataList.Count)
            {
                _customItemDataCache = new Dictionary<EItemType, ItemData>();
                foreach (var item in ItemDataList)
                {
                    EItemType type = EnumExtensions.SafeParseEItemType(item.name);
                    if (type != EItemType.None && !_customItemDataCache.ContainsKey(type))
                    {
                        _customItemDataCache[type] = item;
                    }
                }
            }
            _customItemDataCache.TryGetValue(itemType, out var result);
            return result;
        }

        public static bool GetItemCostPrefix(EItemType itemType, ref float __result)
        {
            var customItem = GetCustomItemData(itemType);
            if (customItem != null)
            {
                float percentChange = 0f;
                int idx = (int)itemType;
                if (CPlayerData.m_ItemPricePercentChangeList != null && idx >= 0 && idx < CPlayerData.m_ItemPricePercentChangeList.Count)
                {
                    percentChange = CPlayerData.m_ItemPricePercentChangeList[idx];
                }

                if (customItem.boxFollowItemPrice == EItemType.None)
                {
                    __result = (float)Mathf.RoundToInt((customItem.baseCost + customItem.baseCost * (percentChange / 100f)) * 100f) / 100f;
                }
                else
                {
                    float boosterCost = RestockManager.GetItemCost(customItem.boxFollowItemPrice);
                    __result = (float)Mathf.RoundToInt((boosterCost * 8f + (boosterCost * 8f) * (percentChange / 100f)) * 100f) / 100f;
                }
                return false;
            }
            return true;
        }

        public static bool GetItemMarketPricePrefix(EItemType itemType, ref float __result)
        {
            var customItem = GetCustomItemData(itemType);
            if (customItem != null)
            {
                float percentChange = 0f;
                int idx = (int)itemType;
                if (CPlayerData.m_ItemPricePercentChangeList != null && idx >= 0 && idx < CPlayerData.m_ItemPricePercentChangeList.Count)
                {
                    percentChange = CPlayerData.m_ItemPricePercentChangeList[idx];
                }

                float minP = customItem.marketPriceMinPercent > 0 ? customItem.marketPriceMinPercent : 1.2f;
                float maxP = customItem.marketPriceMaxPercent > 0 ? customItem.marketPriceMaxPercent : 1.8f;
                float avgMargin = (minP + maxP) * 0.5f;

                if (customItem.boxFollowItemPrice == EItemType.None)
                {
                    float baseMarket = customItem.baseCost * avgMargin;
                    __result = (float)Mathf.RoundToInt((baseMarket + baseMarket * (percentChange / 100f)) * 100f) / 100f;
                }
                else
                {
                    float boosterMarket = RestockManager.GetItemMarketPrice(customItem.boxFollowItemPrice);
                    float displayMarket = boosterMarket * 8f * avgMargin;
                    __result = (float)Mathf.RoundToInt((displayMarket + displayMarket * (percentChange / 100f)) * 100f) / 100f;
                }
                return false;
            }
            return true;
        }

        public static bool GetItemMarketPriceCustomPercentPrefix(EItemType itemType, float percent, ref float __result)
        {
            var customItem = GetCustomItemData(itemType);
            if (customItem != null)
            {
                float minP = customItem.marketPriceMinPercent > 0 ? customItem.marketPriceMinPercent : 1.2f;
                float maxP = customItem.marketPriceMaxPercent > 0 ? customItem.marketPriceMaxPercent : 1.8f;
                float avgMargin = (minP + maxP) * 0.5f;

                if (customItem.boxFollowItemPrice == EItemType.None)
                {
                    float baseMarket = customItem.baseCost * avgMargin;
                    __result = (float)Mathf.RoundToInt((baseMarket + baseMarket * (percent / 100f)) * 100f) / 100f;
                }
                else
                {
                    float boosterMarket = RestockManager.GetItemMarketPrice(customItem.boxFollowItemPrice);
                    float displayMarket = boosterMarket * 8f * avgMargin;
                    __result = (float)Mathf.RoundToInt((displayMarket + displayMarket * (percent / 100f)) * 100f) / 100f;
                }
                return false;
            }
            return true;
        }
    }
}
