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
        [Fact]
        public void SeasonTestCards_JsonLoading_RegistersSeasonAndRarities()
        {
            SeasonsManager.LoadFromPluginPath(Directory.GetCurrentDirectory());
            RaritiesManager.ResetToDefaults();

            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "data/cards/SeasonTest/season_test.json");
            Assert.True(File.Exists(jsonPath), $"season_test.json file should exist at {jsonPath}");

            string jsonContent = File.ReadAllText(jsonPath);
            JToken token = JToken.Parse(jsonContent);
            List<WankulCardData> cards = JsonImporter.DeserializeToken(token);

            Assert.Equal(16, cards.Count);

            int terrainCount = 0;
            int effigyCount = 0;

            foreach (var card in cards)
            {
                Assert.Equal("SeasonTest", card.SeasonId);
                Assert.NotNull(SeasonsManager.GetSeason(card.SeasonId));

                if (card is TerrainCardData terrainCard)
                {
                    terrainCount++;
                    Assert.Equal("TEST-016", terrainCard.Number);
                    Assert.Equal("Test Terrain 001", terrainCard.Title);
                }
                else if (card is EffigyCardData effigyCard)
                {
                    effigyCount++;
                    Assert.NotNull(RaritiesManager.GetRarity(effigyCard.RarityId));
                }
            }

            Assert.Equal(1, terrainCount);
            Assert.Equal(15, effigyCount);
            Assert.Equal("Season Test", SeasonsManager.GetSeasonName("SeasonTest"));
        }

        [Fact]
        public void CustomItems_JsonFiles_ParseValidly()
        {
            string itemDataPath = Path.Combine(Directory.GetCurrentDirectory(), "data/customitems/ItemDataList.json");
            Assert.True(File.Exists(itemDataPath));

            string jsonText = File.ReadAllText(itemDataPath);
            JArray array = JArray.Parse(jsonText);

            Assert.Equal(2, array.Count);
            foreach (JObject item in array)
            {
                string catString = item["category"].Value<string>()!;
                EItemCategory category = (EItemCategory)Enum.Parse(typeof(EItemCategory), catString);
                Assert.Equal(EItemCategory.TCG, category);
                Assert.EndsWith(".png", item["icon"].Value<string>()!);
            }

            string restockPath = Path.Combine(Directory.GetCurrentDirectory(), "data/customitems/restockDataList.json");
            Assert.True(File.Exists(restockPath));
            JArray restockArray = JArray.Parse(File.ReadAllText(restockPath));

            Assert.Equal(2, restockArray.Count);
            Assert.Equal(32, restockArray[0]["amount"].Value<int>());
            Assert.Equal(64, restockArray[1]["amount"].Value<int>());
            Assert.Equal(1, restockArray[0]["licenseShopLevelRequired"].Value<int>());
            Assert.Equal(1, restockArray[1]["licenseShopLevelRequired"].Value<int>());

            string meshPath = Path.Combine(Directory.GetCurrentDirectory(), "data/customitems/itemMeshDataList.json");
            Assert.True(File.Exists(meshPath));
            JArray meshArray = JArray.Parse(File.ReadAllText(meshPath));

            Assert.Equal(2, meshArray.Count);
            Assert.EndsWith(".png", meshArray[0]["texture"].Value<string>()!);
            Assert.EndsWith(".png", meshArray[1]["texture"].Value<string>()!);
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
