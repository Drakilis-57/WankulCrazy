using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class SeasonTestAndCustomPacksTests
    {
        [Fact(Skip="Missing legacy.json asset in CI")]
        public void LegacyCards_JsonLoading_RegistersSeasonAndRarities()
        {
            SeasonsManager.LoadFromPluginPath(Directory.GetCurrentDirectory());
            RaritiesManager.ResetToDefaults();

            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "data/cards/Legacy/legacy.json");
            Assert.True(File.Exists(jsonPath), $"legacy.json file should exist at {jsonPath}");

            string jsonContent = File.ReadAllText(jsonPath);
            JToken token = JToken.Parse(jsonContent);
            List<WankulCardData> cards = JsonImporter.DeserializeToken(token);

            Assert.Equal(185, cards.Count);

            int terrainCount = 0;
            int effigyCount = 0;

            foreach (var card in cards)
            {
                Assert.Equal("S05", card.SeasonId);
                Assert.NotNull(SeasonsManager.GetSeason(card.SeasonId));

                if (card is TerrainCardData)
                {
                    terrainCount++;
                }
                else if (card is EffigyCardData effigyCard)
                {
                    effigyCount++;
                    Assert.NotNull(RaritiesManager.GetRarity(effigyCard.RarityId));
                }
            }

            Assert.Equal(30, terrainCount);
            Assert.Equal(155, effigyCount);
            Assert.Equal("Legacy", SeasonsManager.GetSeasonName("S05"));
        }

        [Fact(Skip="Missing custom items asset in CI")]
        public void CustomItems_JsonFiles_ParseValidly()
        {
            string itemDataPath = Path.Combine(Directory.GetCurrentDirectory(), "data/customitems/ItemDataList.json");
            Assert.True(File.Exists(itemDataPath));

            string jsonText = File.ReadAllText(itemDataPath);
            JArray array = JArray.Parse(jsonText);

            Assert.Equal(2, array.Count);
            foreach (JObject item in array)
            {
                string? catString = (string?)item["category"];
                Assert.NotNull(catString);
                EItemCategory category = (EItemCategory)Enum.Parse(typeof(EItemCategory), catString);
                Assert.Equal(EItemCategory.TCG, category);
                string? icon = (string?)item["icon"];
                Assert.NotNull(icon);
                Assert.EndsWith(".png", icon);
            }

            string restockPath = Path.Combine(Directory.GetCurrentDirectory(), "data/customitems/restockDataList.json");
            Assert.True(File.Exists(restockPath));
            JArray restockArray = JArray.Parse(File.ReadAllText(restockPath));

            Assert.Equal(2, restockArray.Count);
            Assert.Equal(32, (int?)restockArray[0]["amount"]);
            Assert.Equal(64, (int?)restockArray[1]["amount"]);
            Assert.Equal(1, (int?)restockArray[0]["licenseShopLevelRequired"]);
            Assert.Equal(1, (int?)restockArray[1]["licenseShopLevelRequired"]);

            string meshPath = Path.Combine(Directory.GetCurrentDirectory(), "data/customitems/itemMeshDataList.json");
            Assert.True(File.Exists(meshPath));
            JArray meshArray = JArray.Parse(File.ReadAllText(meshPath));

            Assert.Equal(2, meshArray.Count);
            string? texture0 = (string?)meshArray[0]["texture"];
            string? texture1 = (string?)meshArray[1]["texture"];
            Assert.NotNull(texture0);
            Assert.NotNull(texture1);
            Assert.EndsWith(".png", texture0);
            Assert.EndsWith(".png", texture1);
        }

        [Fact]
        public void EnumExtensions_SafeParse_SupportsSeasonTestPacks()
        {
            EItemType item32 = EnumExtensions.SafeParseEItemType("TestCardPack32");
            EItemType item64 = EnumExtensions.SafeParseEItemType("TestCardPack64");

            Assert.NotEqual((EItemType)0, item32);
            Assert.NotEqual((EItemType)0, item64);

            ECollectionPackType pack32 = EnumExtensions.SafeParseECollectionPackType("SeasonTestPack32");
            ECollectionPackType pack64 = EnumExtensions.SafeParseECollectionPackType("SeasonTestPack64");

            Assert.NotEqual((ECollectionPackType)0, pack32);
            Assert.NotEqual((ECollectionPackType)0, pack64);
        }
    }
}
