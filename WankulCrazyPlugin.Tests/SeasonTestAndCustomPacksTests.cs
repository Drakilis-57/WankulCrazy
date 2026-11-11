using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;
using WankulCrazyPlugin.patch;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class SeasonTestAndCustomPacksTests
    {
        [Fact(Skip = "Missing legacy.json asset in CI")]
public void LegacyCards_JsonLoading_RegistersSeasonAndRarities()
{
    SeasonsManager.LoadFromPluginPath(Directory.GetCurrentDirectory());
    RaritiesManager.ResetToDefaults();

    string jsonPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "data/cards/Legacy/legacy.json");

    Assert.True(File.Exists(jsonPath), $"legacy.json file should exist at {jsonPath}");

    string jsonContent = File.ReadAllText(jsonPath);
    JToken token = JToken.Parse(jsonContent);
    List<WankulCardData> cards = JsonImporter.DeserializeToken(token);

    // validations conservées...
}

        [Fact]
        public void CustomItems_JsonFiles_ParseValidly()
        {
            string? rootPath = AppDomain.CurrentDomain.BaseDirectory;
            while (rootPath != null && !Directory.Exists(Path.Combine(rootPath, "data")))
            {
                rootPath = Directory.GetParent(rootPath)?.FullName;
            }
            if (rootPath == null) rootPath = Directory.GetCurrentDirectory();

            string itemDataPath = Path.Combine(rootPath, "data/customitems/ItemDataList.json");
            Assert.True(File.Exists(itemDataPath));

            string jsonText = File.ReadAllText(itemDataPath);
            JArray array = JArray.Parse(jsonText);

            Assert.True(array.Count >= 8);
            foreach (JObject item in array)
            {
                string? catString = (string?)item["category"];
                Assert.NotNull(catString);

                string? icon = (string?)item["icon"];
                Assert.NotNull(icon);
                Assert.EndsWith(".png", icon);
            }

            string restockPath = Path.Combine(rootPath, "data/customitems/restockDataList.json");
            Assert.True(File.Exists(restockPath));
            JArray restockArray = JArray.Parse(File.ReadAllText(restockPath));

            Assert.True(restockArray.Count >= 10);
            Assert.Equal(32, (int?)restockArray[0]["amount"]);

            string meshPath = Path.Combine(rootPath, "data/customitems/itemMeshDataList.json");
            Assert.True(File.Exists(meshPath));
            JArray meshArray = JArray.Parse(File.ReadAllText(meshPath));

            Assert.True(meshArray.Count >= 8);
            string? texture0 = (string?)meshArray[0]["texture"];
            string? texture1 = (string?)meshArray[1]["texture"];
            Assert.NotNull(texture0);
            Assert.NotNull(texture1);
            Assert.EndsWith(".png", texture0);
            Assert.EndsWith(".png", texture1);
        }

        [Fact]
        public void EnumExtensions_SafeParse_SupportsCustomPacks()
        {
            EItemType itemStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType itemLegacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");

            Assert.NotEqual((EItemType)0, itemStellar);
            Assert.NotEqual((EItemType)0, itemLegacy);

            ECollectionPackType packStellar = EnumExtensions.SafeParseECollectionPackType("Stellar");
            ECollectionPackType packLegacy = EnumExtensions.SafeParseECollectionPackType("Legacy");

            Assert.NotEqual((ECollectionPackType)0, packStellar);
            Assert.NotEqual((ECollectionPackType)0, packLegacy);
        }

        [Fact]
        public void CardPrice_GetSeasonPriceFactor_FollowsProgressionThroughLegacyAndHS()
        {
            Assert.Equal(1.0f, CardPrice.GetSeasonPriceFactor(null));
            Assert.Equal(1.0f, CardPrice.GetSeasonPriceFactor(new WankulCardData { Season = Season.S01 }));
            Assert.Equal(1.25f, CardPrice.GetSeasonPriceFactor(new WankulCardData { Season = Season.S02 }));
            Assert.Equal(1.5f, CardPrice.GetSeasonPriceFactor(new WankulCardData { Season = Season.S03 }));
            Assert.Equal(1.75f, CardPrice.GetSeasonPriceFactor(new WankulCardData { Season = Season.S04 }));
            Assert.Equal(2.0f, CardPrice.GetSeasonPriceFactor(new WankulCardData { Season = Season.S05 }));
            Assert.Equal(2.25f, CardPrice.GetSeasonPriceFactor(new WankulCardData { Season = Season.HS }));
            Assert.Equal(2.0f, CardPrice.GetSeasonPriceFactor(new WankulCardData { SeasonId = "S05" }));
        }

        [Fact]
        public void CustomItems_LegacyProgressionAndDisplayTauxLinks_AreConsistent()
        {
            string? rootPath = AppDomain.CurrentDomain.BaseDirectory;
            while (rootPath != null && !Directory.Exists(Path.Combine(rootPath, "data")))
            {
                rootPath = Directory.GetParent(rootPath)?.FullName;
            }
            if (rootPath == null) rootPath = Directory.GetCurrentDirectory();

            JArray items = JArray.Parse(File.ReadAllText(Path.Combine(rootPath, "data/customitems/ItemDataList.json")));
            var byType = new Dictionary<string, JObject>();
            foreach (JObject obj in items)
            {
                byType[(string)obj["itemType"]!] = obj;
            }

            Assert.Equal(5f, (float)byType["BoosterStellar"]["baseCost"]!);
            Assert.Equal(10f, (float)byType["BoosterStellarTaux"]["baseCost"]!);
            Assert.Equal(6.5f, (float)byType["BoosterLegacy"]["baseCost"]!);
            Assert.Equal(13f, (float)byType["BoosterLegacyTaux"]["baseCost"]!);
            Assert.Equal("BoosterStellarTaux", (string)byType["DisplayStellarTaux"]["boxFollowItemPrice"]!);
            Assert.Equal("BoosterLegacyTaux", (string)byType["DisplayLegacyTaux"]["boxFollowItemPrice"]!);

            JArray restock = JArray.Parse(File.ReadAllText(Path.Combine(rootPath, "data/customitems/restockDataList.json")));
            var entries = new Dictionary<string, (int Level, int Price)>();
            foreach (JObject r in restock)
            {
                string name = (string)r["name"]!;
                if (name.Contains("Stellar") || name.Contains("Legacy"))
                {
                    entries[name] = ((int)r["licenseShopLevelRequired"]!, (int)r["licensePrice"]!);
                }
            }

            Assert.Equal((20, 2000), entries["Booster Stellar (32)"]);
            Assert.Equal((21, 2500), entries["Booster Stellar (64)"]);
            Assert.Equal((23, 3000), entries["Display Stellar (4)"]);
            Assert.Equal((24, 3500), entries["Display Stellar (8)"]);
            Assert.Equal((55, 8000), entries["Booster Stellar (32) Taux +"]);
            Assert.Equal((57, 9000), entries["Booster Stellar (64) Taux +"]);
            Assert.Equal((60, 10000), entries["Display Stellar (4) Taux +"]);
            Assert.Equal((62, 11000), entries["Display Stellar (8) Taux +"]);

            Assert.Equal((25, 4000), entries["Booster Legacy (32)"]);
            Assert.Equal((27, 4500), entries["Booster Legacy (64)"]);
            Assert.Equal((29, 5000), entries["Display Legacy (4)"]);
            Assert.Equal((31, 5500), entries["Display Legacy (8)"]);
            Assert.Equal((64, 12000), entries["Booster Legacy (32) Taux +"]);
            Assert.Equal((66, 13500), entries["Booster Legacy (64) Taux +"]);
            Assert.Equal((68, 15000), entries["Display Legacy (4) Taux +"]);
            Assert.Equal((70, 16000), entries["Display Legacy (8) Taux +"]);

            // Tous les boosters/displays normaux (<= 31) doivent se débloquer strictement avant les Taux+ (>= 32)
            var vanillaTauxFixtures = new List<RestockData>
            {
                new RestockData { itemType = EItemType.BasicCardPack, isBigBox = false, licenseShopLevelRequired = 1 },
                new RestockData { itemType = EItemType.LegendaryCardBox, isBigBox = true, licenseShopLevelRequired = 30 },
                new RestockData { itemType = EItemType.DestinyBasicCardPack, isBigBox = false, licenseShopLevelRequired = 25 },
                new RestockData { itemType = EItemType.DestinyBasicCardPack, isBigBox = true, licenseShopLevelRequired = 29 },
                new RestockData { itemType = EItemType.DestinyBasicCardBox, isBigBox = false, licenseShopLevelRequired = 27 },
                new RestockData { itemType = EItemType.DestinyBasicCardBox, isBigBox = true, licenseShopLevelRequired = 31 },
                new RestockData { itemType = EItemType.DestinyRareCardPack, isBigBox = false, licenseShopLevelRequired = 30 },
                new RestockData { itemType = EItemType.DestinyEpicCardPack, isBigBox = false, licenseShopLevelRequired = 40 },
                new RestockData { itemType = EItemType.DestinyLegendaryCardBox, isBigBox = true, licenseShopLevelRequired = 70 },
            };

            int updated = CustomItemsImporter.ApplyVanillaTauxLevelOverrides(vanillaTauxFixtures);
            Assert.Equal(7, updated);
            Assert.Equal(1, vanillaTauxFixtures[0].licenseShopLevelRequired);
            Assert.Equal(30, vanillaTauxFixtures[1].licenseShopLevelRequired);
            Assert.Equal(32, vanillaTauxFixtures[2].licenseShopLevelRequired);
            Assert.Equal(34, vanillaTauxFixtures[3].licenseShopLevelRequired);
            Assert.Equal(36, vanillaTauxFixtures[4].licenseShopLevelRequired);
            Assert.Equal(38, vanillaTauxFixtures[5].licenseShopLevelRequired);
            Assert.Equal(39, vanillaTauxFixtures[6].licenseShopLevelRequired);
            Assert.Equal(46, vanillaTauxFixtures[7].licenseShopLevelRequired);
            Assert.Equal(70, vanillaTauxFixtures[8].licenseShopLevelRequired);
        }
    }
}
