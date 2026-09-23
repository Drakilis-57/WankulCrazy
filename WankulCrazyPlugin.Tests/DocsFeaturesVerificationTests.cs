using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class DocsFeaturesVerificationTests
    {
        [Fact]
        public void MultiFileCardsLoading_DeserializeToken_SupportsArraysAndContainers()
        {
            string pack1Json = @"[
                { 'Title': 'Pack1 Card1', 'Number': '101', 'SeasonId': 'S01', 'RarityId': 'C' },
                { 'Title': 'Pack1 Card2', 'Number': '102', 'SeasonId': 'S01', 'RarityId': 'UC' }
            ]";

            string pack2Json = @"{
                'wankuls': [
                    { 'Title': 'Pack2 Wankul', 'Number': '201', 'SeasonId': 'S02', 'RarityId': 'R', 'Effigy': 'Laink' }
                ],
                'terrains': [
                    { 'Title': 'Pack2 Terrain', 'Number': '202', 'SeasonId': 'S02' }
                ],
                'specials': [
                    { 'Title': 'Pack2 Special', 'Number': '203', 'SeasonId': 'S02' }
                ]
            }";

            List<WankulCardData> pack1Cards = JsonImporter.DeserializeToken(JToken.Parse(pack1Json));
            List<WankulCardData> pack2Cards = JsonImporter.DeserializeToken(JToken.Parse(pack2Json));

            Assert.Equal(2, pack1Cards.Count);
            Assert.Equal(3, pack2Cards.Count);

            List<WankulCardData> combined = new List<WankulCardData>();
            combined.AddRange(pack1Cards);
            combined.AddRange(pack2Cards);

            Assert.Equal(5, combined.Count);
            Assert.IsType<EffigyCardData>(pack2Cards[0]);
            Assert.IsType<TerrainCardData>(pack2Cards[1]);
            Assert.IsType<SpecialCardData>(pack2Cards[2]);
        }

        [Fact]
        public void DynamicSeasonsManager_FullLifecycle()
        {
            SeasonsManager.ResetToDefaults();

            Assert.Equal("Origins", SeasonsManager.GetSeasonName("S01"));
            Assert.Equal("Campus", SeasonsManager.GetSeasonName("S02"));
            Assert.Equal("Battle", SeasonsManager.GetSeasonName("S03"));
            Assert.Equal("Stellar", SeasonsManager.GetSeasonName("S04"));
            Assert.Equal("Hors Serie", SeasonsManager.GetSeasonName("HS"));

            SeasonsManager.RegisterSeason(new SeasonData("S05", "Apocalypse"));
            Assert.Equal("Apocalypse", SeasonsManager.GetSeasonName("S05"));

            var seasonObj = SeasonsManager.GetSeason("S05");
            Assert.NotNull(seasonObj);
            Assert.Equal("Apocalypse", seasonObj?.Name);
        }

        [Fact]
        public void DynamicRaritiesManager_FullLifecycle()
        {
            RaritiesManager.ResetToDefaults();

            Assert.Equal(1.0f, RaritiesManager.GetExperienceMultiplier("C"));
            Assert.Equal(2.0f, RaritiesManager.GetExperienceMultiplier("UC"));
            Assert.Equal(4.0f, RaritiesManager.GetExperienceMultiplier("R"));
            Assert.Equal(7.0f, RaritiesManager.GetExperienceMultiplier("UR1"));
            Assert.Equal(10.0f, RaritiesManager.GetExperienceMultiplier("UR2"));

            RaritiesManager.RegisterRarity(new RarityData("MYTHIC", "Mythic Card", 150.0f, 5.0f));

            Assert.Equal(150.0f, RaritiesManager.GetExperienceMultiplier("MYTHIC"));
            Assert.Equal(5.0f, RaritiesManager.GetPriceMultiplier("MYTHIC"));

            var card = new EffigyCardData { RarityId = "MYTHIC" };
            int xp = RaritiesManager.CalculateExperience(card, shopLevel: 10);
            Assert.True(xp > 150, "XP calculation should scale with shop level and rarity multiplier");
        }

        [Fact]
        public void CardData_PropertySetters_AutoRegisterSeasonAndRarity()
        {
            SeasonsManager.ResetToDefaults();
            RaritiesManager.ResetToDefaults();

            var card = new EffigyCardData();
            card.SeasonId = "S10";
            card.RarityId = "GOD_TIER";

            Assert.Equal("S10", card.SeasonId);
            Assert.Equal("GOD_TIER", card.RarityId);

            Assert.NotNull(SeasonsManager.GetSeason("S10"));
            Assert.NotNull(RaritiesManager.GetRarity("GOD_TIER"));
        }

        [Fact]
        public void SeasonAndRarityJsonConverters_RegisterCustomValues()
        {
            SeasonsManager.ResetToDefaults();
            RaritiesManager.ResetToDefaults();

            string json = @"{
                'Title': 'Test Custom Card',
                'SeasonId': 'S88',
                'RarityId': 'ULTRA_SECRET'
            }";

            EffigyCardData? card = JsonConvert.DeserializeObject<EffigyCardData>(json);

            Assert.NotNull(card);
            Assert.Equal("S88", card?.SeasonId);
            Assert.Equal("ULTRA_SECRET", card?.RarityId);

            Assert.Equal("S88", SeasonsManager.GetSeasonName("S88"));
            Assert.NotNull(RaritiesManager.GetRarity("ULTRA_SECRET"));
        }
    }
}
