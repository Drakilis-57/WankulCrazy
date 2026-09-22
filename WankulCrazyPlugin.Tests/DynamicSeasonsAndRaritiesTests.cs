using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.Tests
{
    public class DynamicSeasonsAndRaritiesTests
    {
        [Fact]
        public void SeasonsManager_Defaults_AreLoadedCorrectly()
        {
            SeasonsManager.ResetToDefaults();

            Assert.Equal("Origins", SeasonsManager.GetSeasonName("S01"));
            Assert.Equal("Campus", SeasonsManager.GetSeasonName("S02"));
            Assert.Equal("Battle", SeasonsManager.GetSeasonName("S03"));
            Assert.Equal("Stellar", SeasonsManager.GetSeasonName("S04"));
            Assert.Equal("Hors Serie", SeasonsManager.GetSeasonName("HS"));
        }

        [Fact]
        public void SeasonsManager_CanRegisterCustomSeason()
        {
            SeasonsManager.ResetToDefaults();
            SeasonsManager.RegisterSeason(new SeasonData("S05", "Apocalypse"));

            Assert.Equal("Apocalypse", SeasonsManager.GetSeasonName("S05"));
        }

        [Fact]
        public void RaritiesManager_Defaults_AreLoadedCorrectly()
        {
            RaritiesManager.ResetToDefaults();

            Assert.Equal(1.0f, RaritiesManager.GetExperienceMultiplier("C"));
            Assert.Equal(7.0f, RaritiesManager.GetExperienceMultiplier("UR1"));
            Assert.Equal(50.0f, RaritiesManager.GetExperienceMultiplier("TOR"));
        }

        [Fact]
        public void RaritiesManager_CanRegisterCustomRarity()
        {
            RaritiesManager.ResetToDefaults();
            RaritiesManager.RegisterRarity(new RarityData("SUPER_LEGEND", "Super Legend", 100.0f, 2.0f));

            Assert.Equal(100.0f, RaritiesManager.GetExperienceMultiplier("SUPER_LEGEND"));
            Assert.Equal(2.0f, RaritiesManager.GetPriceMultiplier("SUPER_LEGEND"));
        }

        [Fact]
        public void DeserializeToken_ParsesSingleCardWithCustomSeasonAndRarity()
        {
            SeasonsManager.ResetToDefaults();
            RaritiesManager.ResetToDefaults();
            SeasonsManager.RegisterSeason(new SeasonData("S05", "Apocalypse"));

            string json = @"{
                'Title': 'Wankil S05 Special',
                'Number': '1',
                'SeasonId': 'S05',
                'RarityId': 'SUPER_RARE',
                'Effigy': 'Laink'
            }";

            JToken token = JToken.Parse(json);
            List<WankulCardData> cards = JsonImporter.DeserializeToken(token);

            Assert.Single(cards);
            Assert.IsType<EffigyCardData>(cards[0]);

            var effigy = (EffigyCardData)cards[0];
            Assert.Equal("S05", effigy.SeasonId);
            Assert.Equal("SUPER_RARE", effigy.RarityId);
            Assert.Equal("Apocalypse", SeasonsManager.GetSeasonName("S05"));
            Assert.NotNull(RaritiesManager.GetRarity("SUPER_RARE"));
        }

        [Fact]
        public void DeserializeToken_ParsesContainerWithWankulsTerrainsAndSpecials()
        {
            string json = @"{
                'wankuls': [
                    { 'Title': 'Carte 1', 'Number': '1', 'Season': 'S01', 'Rarity': 'C' }
                ],
                'terrains': [
                    { 'Title': 'Terrain 1', 'Number': '2', 'Season': 'S01' }
                ],
                'specials': [
                    { 'Title': 'Special 1', 'Number': '3', 'Season': 'S01' }
                ]
            }";

            JToken token = JToken.Parse(json);
            List<WankulCardData> cards = JsonImporter.DeserializeToken(token);

            Assert.Equal(3, cards.Count);
            Assert.IsType<EffigyCardData>(cards[0]);
            Assert.IsType<TerrainCardData>(cards[1]);
            Assert.IsType<SpecialCardData>(cards[2]);
        }

        [Fact]
        public void ExperienceCalculation_UsesDynamicRarityMultiplier()
        {
            RaritiesManager.ResetToDefaults();
            RaritiesManager.RegisterRarity(new RarityData("CUSTOM_XP", "Custom XP", 35.0f, 1.0f));

            var card = new EffigyCardData
            {
                RarityId = "CUSTOM_XP"
            };

            int xp = RaritiesManager.CalculateExperience(card, shopLevel: 1);
            Assert.True(xp >= 35, $"Expected XP to be >= 35, got {xp}");
        }
    }
}
