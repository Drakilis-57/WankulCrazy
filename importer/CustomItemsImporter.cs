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

        public static void ImportCustomItems()
        {
            if (isImported) return;
            ItemDataList = DeserializeItemDataListJson();
            RestockDataList = DeserializeRestockDataListJson();
            ItemMeshDataList = DeserializeItemMeshDataList();
            InventoryBase.Instance.m_StockItemData_SO.m_ItemDataList.AddRange(ItemDataList);
            InventoryBase.Instance.m_StockItemData_SO.m_RestockDataList.AddRange(RestockDataList);
            InventoryBase.Instance.m_StockItemData_SO.m_ItemMeshDataList.AddRange(ItemMeshDataList);
            RegisterCustomItemsToShopCategories(ItemDataList);
            isImported = true;
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
                    string content = File.ReadAllText(path);
                    JArray array = JArray.Parse(content);
                    foreach (JObject json in array)
                    {
                        ItemData item = new ItemData
                        {
                            name = (string)json["name"],
                            category = (EItemCategory)Enum.Parse(typeof(EItemCategory), (string)json["category"]),
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
                                item.affectedPriceChangeType.Add((EPriceChangeType)Enum.Parse(typeof(EPriceChangeType), (string)pct));
                            }
                        }

                        item.itemDimension = ReadVector(json["itemDimension"]);
                        item.colliderPosOffset = ReadVector(json["colliderPosOffset"]);
                        item.colliderScale = ReadVector(json["colliderScale"]);

                        string iconProp = (string)json["icon"];
                        if (!string.IsNullOrEmpty(iconProp))
                        {
                            string iconPath = Path.Combine(root, "icons", iconProp);
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
                    string content = File.ReadAllText(path);
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
                    string content = File.ReadAllText(path);
                    JArray array = JArray.Parse(content);
                    foreach (JObject json in array)
                    {
                        ItemMeshData mesh = new ItemMeshData { name = (string)json["name"] };
                        ItemMeshData source = InventoryBase.GetItemMeshData(EnumExtensions.SafeParseEItemType((string)json["copyItemType"]));
                        mesh.mesh = source.mesh;
                        mesh.meshSecondary = source.meshSecondary;
                        mesh.materialSecondary = source.materialSecondary;
                        string texProp = (string)json["texture"];
                        if (!string.IsNullOrEmpty(texProp))
                        {
                            string texturePath = Path.Combine(root, "textures", texProp);
                            if (File.Exists(texturePath))
                            {
                                Texture2D texture = TextureUtils.LoadTexture(texturePath);
                                mesh.material = new Material(Shader.Find("Standard")) { mainTexture = texture };
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
            EItemType test32 = EnumExtensions.SafeParseEItemType("TestCardPack32");
            EItemType test64 = EnumExtensions.SafeParseEItemType("TestCardPack64");
            if (itemType == test32) { __result = EnumExtensions.SafeParseECollectionPackType("SeasonTestPack32"); return; }
            if (itemType == test64) { __result = EnumExtensions.SafeParseECollectionPackType("SeasonTestPack64"); return; }

            EItemType boosterStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType displayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
            EItemType boosterStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            EItemType displayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
            EItemType boosterGoldBattle = EnumExtensions.SafeParseEItemType("BoosterGoldBattle");
            EItemType boosterGoldStellar = EnumExtensions.SafeParseEItemType("BoosterGoldStellar");
            ECollectionPackType stellarPack = EnumExtensions.SafeParseECollectionPackType("Stellar");
            ECollectionPackType stellarPackTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");

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
        }

        public static void GetCardExpansionType(ECollectionPackType collectionPackType, ref ECardExpansionType __result)
        {
            if (collectionPackType == EnumExtensions.SafeParseECollectionPackType("SeasonTestPack32") || collectionPackType == EnumExtensions.SafeParseECollectionPackType("SeasonTestPack64")) { __result = ECardExpansionType.Tetramon; }
        }

        private static void RegisterCustomItemsToShopCategories(List<ItemData> items)
        {
            if (items == null || InventoryBase.Instance?.m_StockItemData_SO == null) return;
            foreach (ItemData item in items)
            {
                EItemType type = EnumExtensions.SafeParseEItemType(item.name);
                if (type != EItemType.None && !InventoryBase.Instance.m_StockItemData_SO.m_ShownItemType.Contains(type)) InventoryBase.Instance.m_StockItemData_SO.m_ShownItemType.Add(type);
            }
        }

        public static bool GetItemMeshDataPrefix(EItemType itemType, ref ItemMeshData __result)
        {
            if (ItemMeshDataList != null)
            {
                foreach (ItemMeshData customMesh in ItemMeshDataList)
                {
                    if (customMesh != null && EnumExtensions.SafeParseEItemType(customMesh.name) == itemType)
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
                    // Fallback pour tout autre enum custom ou hors limites pour éviter l'exception d'index
                    __result = new ItemData();
                    return false;
                }
            }

            return true;
        }
    }
}
