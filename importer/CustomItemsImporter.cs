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
                using (var stream = File.OpenRead(path))
                using (var document = System.Text.Json.JsonDocument.Parse(stream))
                {
                    foreach (var json in document.RootElement.EnumerateArray())
                    {
                        ItemData item = new ItemData
                        {
                            name = json.GetProperty("name").GetString(),
                            category = (EItemCategory)Enum.Parse(typeof(EItemCategory), json.GetProperty("category").GetString()),
                            iconScale = json.GetProperty("iconScale").GetSingle(),
                            baseCost = json.GetProperty("baseCost").GetSingle(),
                            marketPriceMinPercent = json.GetProperty("marketPriceMinPercent").GetSingle(),
                            marketPriceMaxPercent = json.GetProperty("marketPriceMaxPercent").GetSingle(),
                            boxFollowItemPrice = EnumExtensions.SafeParseEItemType(json.GetProperty("boxFollowItemPrice").GetString()),
                            isNotBoosterPack = json.GetProperty("isNotBoosterPack").GetBoolean(),
                            isTallItem = json.GetProperty("isTallItem").GetBoolean(),
                            isHideItemUntilUnlocked = json.GetProperty("isHideItemUntilUnlocked").GetBoolean(),
                            posYOffsetInBox = json.GetProperty("posYOffsetInBox").GetSingle(),
                            scaleOffsetInBox = json.GetProperty("scaleOffsetInBox").GetSingle()
                        };

                        item.affectedPriceChangeType = new List<EPriceChangeType>();
                        foreach (var pct in json.GetProperty("affectedPriceChangeType").EnumerateArray())
                        {
                            item.affectedPriceChangeType.Add((EPriceChangeType)Enum.Parse(typeof(EPriceChangeType), pct.GetString()));
                        }

                        item.itemDimension = ReadVectorSTJ(json.GetProperty("itemDimension"));
                        item.colliderPosOffset = ReadVectorSTJ(json.GetProperty("colliderPosOffset"));
                        item.colliderScale = ReadVectorSTJ(json.GetProperty("colliderScale"));

                        string iconPath = Path.Combine(root, "icons", json.GetProperty("icon").GetString());
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
            }
            catch (Exception ex) { Plugin.Logger.LogError("Failed to deserialize JSON ItemData: " + ex.Message); }
            return result;
        }


        private static Vector3 ReadVectorSTJ(System.Text.Json.JsonElement token) => new Vector3(token.GetProperty("x").GetSingle(), token.GetProperty("y").GetSingle(), token.GetProperty("z").GetSingle());

        public static List<RestockData> DeserializeRestockDataListJson()
        {
            List<RestockData> result = new List<RestockData>();
            string path = Path.Combine(Plugin.GetPluginPath(), "data/customitems/restockDataList.json");
            try
            {
                using (var stream = File.OpenRead(path))
                using (var document = System.Text.Json.JsonDocument.Parse(stream))
                {
                    foreach (var json in document.RootElement.EnumerateArray())
                    {
                        result.Add(new RestockData
                        {
                            index = json.GetProperty("index").GetInt32(),
                            name = json.GetProperty("name").GetString(),
                            isBigBox = json.GetProperty("isBigBox").GetBoolean(),
                            ignoreDoubleImage = json.GetProperty("ignoreDoubleImage").GetBoolean(),
                            amount = json.GetProperty("amount").GetInt32(),
                            licenseShopLevelRequired = json.GetProperty("licenseShopLevelRequired").GetInt32(),
                            licensePrice = json.GetProperty("licensePrice").GetSingle(),
                            itemType = EnumExtensions.SafeParseEItemType(json.GetProperty("itemType").GetString()),
                            prologueShow = json.GetProperty("prologueShow").GetBoolean(),
                            isHideItemUntilUnlocked = json.GetProperty("isHideItemUntilUnlocked").GetBoolean()
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
            try
            {
                using (var stream = File.OpenRead(Path.Combine(root, "itemMeshDataList.json")))
                using (var document = System.Text.Json.JsonDocument.Parse(stream))
                {
                    foreach (var json in document.RootElement.EnumerateArray())
                    {
                        ItemMeshData mesh = new ItemMeshData { name = json.GetProperty("name").GetString() };
                        ItemMeshData source = InventoryBase.GetItemMeshData(EnumExtensions.SafeParseEItemType(json.GetProperty("copyItemType").GetString()));
                        mesh.mesh = source.mesh; mesh.meshSecondary = source.meshSecondary; mesh.materialSecondary = source.materialSecondary;
                        string texturePath = Path.Combine(root, "textures", json.GetProperty("texture").GetString());
                        if (File.Exists(texturePath))
                        {
                            Texture2D texture = TextureUtils.LoadTexture(texturePath);
                            mesh.material = new Material(Shader.Find("Standard")) { mainTexture = texture };
                        }
                        else Plugin.Logger.LogWarning("Test/custom item texture not found yet: " + texturePath);
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

        public static void GetItemMeshDataPostfix(EItemType itemType, ref ItemMeshData __result)
        {
            if (ItemMeshDataList != null)
            {
                foreach (ItemMeshData customMesh in ItemMeshDataList)
                {
                    if (customMesh != null && EnumExtensions.SafeParseEItemType(customMesh.name) == itemType)
                    {
                        __result = customMesh;
                        return;
                    }
                }
            }

            var stock = InventoryBase.Instance?.m_StockItemData_SO?.m_ItemMeshDataList;
            if (stock != null)
            {
                int index = (int)itemType;
                if (index < 0 || index >= stock.Count)
                {
                    // Fallback pour tout autre enum custom ou invalide non trouvé par index
                    __result = new ItemMeshData();
                }
            }
        }

        public static void GetItemDataPostfix(EItemType itemType, ref ItemData __result)
        {
            if (ItemDataList != null)
            {
                foreach (ItemData customItem in ItemDataList)
                {
                    if (customItem != null && EnumExtensions.SafeParseEItemType(customItem.name) == itemType)
                    {
                        __result = customItem;
                        return;
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
                }
            }
        }
    }
}
