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
                foreach (JObject json in JArray.Parse(File.ReadAllText(path)))
                {
                    ItemData item = new ItemData
                    {
                        name = json["name"].Value<string>(),
                        category = (EItemCategory)Enum.Parse(typeof(EItemCategory), json["category"].Value<string>()),
                        iconScale = json["iconScale"].Value<float>(),
                        baseCost = json["baseCost"].Value<float>(),
                        marketPriceMinPercent = json["marketPriceMinPercent"].Value<float>(),
                        marketPriceMaxPercent = json["marketPriceMaxPercent"].Value<float>(),
                        boxFollowItemPrice = EnumExtensions.SafeParseEItemType(json["boxFollowItemPrice"].Value<string>()),
                        isNotBoosterPack = json["isNotBoosterPack"].Value<bool>(),
                        isTallItem = json["isTallItem"].Value<bool>(),
                        isHideItemUntilUnlocked = json["isHideItemUntilUnlocked"].Value<bool>(),
                        posYOffsetInBox = json["posYOffsetInBox"].Value<float>(),
                        scaleOffsetInBox = json["scaleOffsetInBox"].Value<float>()
                    };
                    item.affectedPriceChangeType = json["affectedPriceChangeType"].ToObject<List<EPriceChangeType>>();
                    item.itemDimension = ReadVector(json["itemDimension"]);
                    item.colliderPosOffset = ReadVector(json["colliderPosOffset"]);
                    item.colliderScale = ReadVector(json["colliderScale"]);
                    string iconPath = Path.Combine(root, "icons", json["icon"].Value<string>());
                    if (File.Exists(iconPath))
                    {
                        Texture2D texture = TextureUtils.LoadTexture(iconPath);
                        item.icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
                        item.icon.name = item.name + "_icon";
                    }
                    else Plugin.Logger.LogWarning("Test/custom item icon not found yet: " + iconPath);
                    result.Add(item);
                }
            }
            catch (Exception ex) { Plugin.Logger.LogError("Failed to deserialize JSON ItemData: " + ex.Message); }
            return result;
        }

        private static Vector3 ReadVector(JToken token) => new Vector3(token["x"].Value<float>(), token["y"].Value<float>(), token["z"].Value<float>());

        public static List<RestockData> DeserializeRestockDataListJson()
        {
            List<RestockData> result = new List<RestockData>();
            string path = Path.Combine(Plugin.GetPluginPath(), "data/customitems/restockDataList.json");
            try
            {
                foreach (JObject json in JArray.Parse(File.ReadAllText(path)))
                {
                    result.Add(new RestockData
                    {
                        index = json["index"].Value<int>(), name = json["name"].Value<string>(),
                        isBigBox = json["isBigBox"].Value<bool>(), ignoreDoubleImage = json["ignoreDoubleImage"].Value<bool>(),
                        amount = json["amount"].Value<int>(), licenseShopLevelRequired = json["licenseShopLevelRequired"].Value<int>(),
                        licensePrice = json["licensePrice"].Value<float>(), itemType = EnumExtensions.SafeParseEItemType(json["itemType"].Value<string>()),
                        prologueShow = json["prologueShow"].Value<bool>(), isHideItemUntilUnlocked = json["isHideItemUntilUnlocked"].Value<bool>()
                    });
                }
            }
            catch (Exception ex) { Plugin.Logger.LogError("Failed to deserialize JSON RestockData: " + ex.Message); }
            return result;
        }

        public static List<ItemMeshData> DeserializeItemMeshDataList()
        {
            List<ItemMeshData> result = new List<ItemMeshData>();
            string root = Path.Combine(Plugin.GetPluginPath(), "data/customitems");
            try
            {
                foreach (JObject json in JArray.Parse(File.ReadAllText(Path.Combine(root, "itemMeshDataList.json"))))
                {
                    ItemMeshData mesh = new ItemMeshData { name = json["name"].Value<string>() };
                    ItemMeshData source = InventoryBase.GetItemMeshData(EnumExtensions.SafeParseEItemType(json["copyItemType"].Value<string>()));
                    mesh.mesh = source.mesh; mesh.meshSecondary = source.meshSecondary; mesh.materialSecondary = source.materialSecondary;
                    string texturePath = Path.Combine(root, "textures", json["texture"].Value<string>());
                    if (File.Exists(texturePath))
                    {
                        Texture2D texture = TextureUtils.LoadTexture(texturePath);
                        mesh.material = new Material(Shader.Find("Standard")) { mainTexture = texture };
                    }
                    else Plugin.Logger.LogWarning("Test/custom item texture not found yet: " + texturePath);
                    result.Add(mesh);
                }
            }
            catch (Exception ex) { Plugin.Logger.LogError("Failed to deserialize JSON ItemMeshData: " + ex.Message); }
            return result;
        }

        public static bool ItemTypeToCollectionPackType(EItemType itemType, ref ECollectionPackType __result)
        {
            EItemType test32 = EnumExtensions.SafeParseEItemType("TestCardPack32");
            EItemType test64 = EnumExtensions.SafeParseEItemType("TestCardPack64");
            if (itemType == test32) { __result = EnumExtensions.SafeParseECollectionPackType("SeasonTestPack32"); return false; }
            if (itemType == test64) { __result = EnumExtensions.SafeParseECollectionPackType("SeasonTestPack64"); return false; }

            EItemType boosterStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType displayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
            EItemType boosterStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            EItemType displayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
            EItemType boosterGoldBattle = EnumExtensions.SafeParseEItemType("BoosterGoldBattle");
            EItemType boosterGoldStellar = EnumExtensions.SafeParseEItemType("BoosterGoldStellar");
            ECollectionPackType stellarPack = EnumExtensions.SafeParseECollectionPackType("Stellar");
            ECollectionPackType stellarPackTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");

            if (itemType == EItemType.BasicCardPack || itemType == EItemType.BasicCardBox) __result = ECollectionPackType.BasicCardPack;
            else if (itemType == EItemType.RareCardPack || itemType == EItemType.RareCardBox) __result = ECollectionPackType.RareCardPack;
            else if (itemType == EItemType.EpicCardPack || itemType == EItemType.EpicCardBox || itemType == boosterGoldBattle) __result = ECollectionPackType.EpicCardPack;
            else if (itemType == EItemType.LegendaryCardPack || itemType == EItemType.LegendaryCardBox) __result = ECollectionPackType.LegendaryCardPack;
            else if (itemType == EItemType.DestinyBasicCardPack || itemType == EItemType.DestinyBasicCardBox) __result = ECollectionPackType.DestinyBasicCardPack;
            else if (itemType == EItemType.DestinyRareCardPack || itemType == EItemType.DestinyRareCardBox) __result = ECollectionPackType.DestinyRareCardPack;
            else if (itemType == EItemType.DestinyEpicCardPack || itemType == EItemType.DestinyEpicCardBox) __result = ECollectionPackType.DestinyEpicCardPack;
            else if (itemType == EItemType.DestinyLegendaryCardPack || itemType == EItemType.DestinyLegendaryCardBox) __result = ECollectionPackType.DestinyLegendaryCardPack;
            else if (itemType == EItemType.GhostPack) __result = ECollectionPackType.GhostPack;
            else if (itemType == EItemType.MegabotPack) __result = ECollectionPackType.MegabotPack;
            else if (itemType == EItemType.FantasyRPGPack) __result = ECollectionPackType.FantasyRPGPack;
            else if (itemType == EItemType.CatJobPack) __result = ECollectionPackType.CatJobPack;
            else if (itemType == boosterStellar || itemType == displayStellar || itemType == boosterGoldStellar) __result = stellarPack;
            else if (itemType == boosterStellarTaux || itemType == displayStellarTaux) __result = stellarPackTaux;
            else __result = ECollectionPackType.None;
            return false;
        }

        public static bool GetCardExpansionType(ECollectionPackType collectionPackType, ref ECardExpansionType __result)
        {
            if (collectionPackType == EnumExtensions.SafeParseECollectionPackType("SeasonTestPack32") || collectionPackType == EnumExtensions.SafeParseECollectionPackType("SeasonTestPack64")) { __result = ECardExpansionType.Tetramon; return false; }
            __result = ECardExpansionType.None;
            return true;
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
                    // Fallback pour tout autre enum custom ou invalide non trouvé par index
                    __result = new ItemData();
                    return false;
                }
            }

            return true;
        }
    }
}
