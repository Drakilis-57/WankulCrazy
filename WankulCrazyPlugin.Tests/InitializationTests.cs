using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class InitializationTests
    {
        [Fact]
        public void SeasonsAndRarities_CanBeInitializedFromPluginPath()
        {
            SeasonsManager.ResetToDefaults();
            RaritiesManager.ResetToDefaults();

            string testPath = AppDomain.CurrentDomain.BaseDirectory;
            SeasonsManager.LoadFromPluginPath(testPath);
            RaritiesManager.LoadFromPluginPath(testPath);

            Assert.NotNull(SeasonsManager.GetSeasonName("S01"));
            Assert.NotNull(RaritiesManager.GetRarity("C"));
        }

        [Fact]
        public void JsonImporter_DeserializeToken_LoadsAllCardTypes()
        {
            string json = @"{
                'wankuls': [
                    { 'Title': 'Wankul Effigy 1', 'Number': '1', 'Season': 'S01', 'Rarity': 'C' }
                ],
                'terrains': [
                    { 'Title': 'Wankul Terrain 1', 'Number': '2', 'Season': 'S01' }
                ],
                'specials': [
                    { 'Title': 'Wankul Special 1', 'Number': '3', 'Season': 'S01', 'Special': 'AJETER' }
                ]
            }";

            JToken token = JToken.Parse(json);
            List<WankulCardData> cards = JsonImporter.DeserializeToken(token);

            Assert.Equal(3, cards.Count);
            Assert.Contains(cards, c => c is EffigyCardData);
            Assert.Contains(cards, c => c is TerrainCardData);
            Assert.Contains(cards, c => c is SpecialCardData);
        }
    }
}
