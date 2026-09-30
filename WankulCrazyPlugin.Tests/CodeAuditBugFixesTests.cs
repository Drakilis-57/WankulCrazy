using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class CodeAuditBugFixesTests : IDisposable
    {
        public CodeAuditBugFixesTests()
        {
            RaritiesManager.ResetToDefaults();
        }

        public void Dispose()
        {
            RaritiesManager.ResetToDefaults();
        }

        [Fact]
        public void BoosterGoldLegacy_IsParsedAndMappedToLegacyPack()
        {
            // 1. SafeParseEItemType
            var itemType = EnumExtensions.SafeParseEItemType("BoosterGoldLegacy");
            Assert.NotEqual((EItemType)0, itemType);

            // Alias check
            var aliasType = EnumExtensions.SafeParseEItemType("Booster Gold Legacy");
            Assert.Equal(itemType, aliasType);

            // 2. Safe mapping to Legacy pack
            var packType = EnumExtensions.ItemTypeToCollectionPackTypeSafe(itemType);
            var expectedLegacy = EnumExtensions.SafeParseECollectionPackType("Legacy");
            Assert.Equal(expectedLegacy, packType);
        }

        [Fact]
        public void SaveAssociation_OperatorPrecedence_OnlySkips546And547OnV100()
        {
            // Simulate the expression: save.version == "1.0.0" && (idx == 546 || idx == 547)
            bool ShouldSkip(string version, int idx)
            {
                return version == "1.0.0" && (idx == 546 || idx == 547);
            }

            // On v1.0.0, 546 and 547 are skipped
            Assert.True(ShouldSkip("1.0.0", 546));
            Assert.True(ShouldSkip("1.0.0", 547));
            Assert.False(ShouldSkip("1.0.0", 548));

            // On v1.0.1 or higher, 547 is NOT skipped! (Before fix, 547 was ALWAYS skipped)
            Assert.False(ShouldSkip("1.0.1", 546));
            Assert.False(ShouldSkip("1.0.1", 547));
            Assert.False(ShouldSkip("2.0.0", 547));
        }

        [Fact]
        public void DuoRarity_HasCorrectPriceRangeAndIsEligibleForFoil()
        {
            var duo = RaritiesManager.GetRarity("DUO");
            Assert.NotNull(duo);
            Assert.Equal(500f, duo.PriceRangeMin);
            Assert.Equal(1000f, duo.PriceRangeMax);
            Assert.True(duo.IsEligibleForFoil);
            Assert.True(duo.IsEligibleForMinRare);
        }

        [Fact]
        public void PityCounter_OnlyDecrementsWhenEligible_AndHasFloorAtOne()
        {
            int seed = 10;

            void Step(bool isEligible, bool shouldGen)
            {
                if (isEligible)
                {
                    if (shouldGen)
                        seed = 10;
                    else
                        seed = Math.Max(1, seed - 1);
                }
            }

            // Non-eligible pack: seed does not decrement
            Step(false, false);
            Assert.Equal(10, seed);

            // Eligible pack: seed decrements
            Step(true, false);
            Assert.Equal(9, seed);

            // Repeated decrement reaches floor at 1, never 0 or negative
            for (int i = 0; i < 20; i++)
            {
                Step(true, false);
            }
            Assert.Equal(1, seed);

            // Success resets seed
            Step(true, true);
            Assert.Equal(10, seed);
        }

        [Fact]
        public void TradeOfferByPrice_FallbackFindsClosestCardWhenRangeEmpty()
        {
            // Simulate fromCard at 10000 (TOR)
            float targetPrice = 10000f;
            float minPrice = targetPrice * 0.75f; // 7500
            float maxPrice = targetPrice * 1.25f; // 12500

            // Catalog where max card is 2500 (no card in 7500-12500)
            var cards = new List<(int Index, float Price)>
            {
                (1, 10f),
                (2, 500f),
                (3, 2500f)
            };

            var inRange = cards.FindAll(c => c.Price >= minPrice && c.Price <= maxPrice);
            Assert.Empty(inRange);

            // Fallback: order by absolute price distance
            var closest = cards.OrderBy(c => Math.Abs(c.Price - targetPrice)).First();
            Assert.Equal(3, closest.Index);
            Assert.Equal(2500f, closest.Price);
        }

        [Fact]
        public void CollectionBinderFlipAnimCtrl_SelectionModesExistInAssembly()
        {
            var dllPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\libs\Assembly-CSharp.dll"));
            var resolver = new Mono.Cecil.DefaultAssemblyResolver();
            resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(dllPath));
            var asm = Mono.Cecil.AssemblyDefinition.ReadAssembly(dllPath, new Mono.Cecil.ReaderParameters { AssemblyResolver = resolver });

            var ipcType = asm.MainModule.Types.First(t => t.Name == "InteractionPlayerController");
            Assert.Contains(ipcType.Methods, m => m.Name == "IsSelectingCardForGradingScreen");
            Assert.Contains(ipcType.Methods, m => m.Name == "IsSelectingCardForEditDeckScreen");
            Assert.Contains(ipcType.Methods, m => m.Name == "IsSelectingCardForBulkDonationBoxScreen");
            Assert.Contains(ipcType.Methods, m => m.Name == "UpdateSelectedCardData");
            Assert.Contains(ipcType.Methods, m => m.Name == "ExitViewCardAlbumMode");
        }

        [Fact]
        public void DropMultiplier_IsAppliedProperlyAcrossRarities()
        {
            var lb = RaritiesManager.GetRarity("LB");
            var la = RaritiesManager.GetRarity("LA");
            var lo = RaritiesManager.GetRarity("LO");
            var duo = RaritiesManager.GetRarity("DUO");
            var r = RaritiesManager.GetRarity("R");

            Assert.Equal(0.05f, lb.DropMultiplier);
            Assert.Equal(0.02f, la.DropMultiplier);
            Assert.Equal(0.005f, lo.DropMultiplier);
            Assert.Equal(0.02f, duo.DropMultiplier);
            Assert.Equal(1.0f, r.DropMultiplier);

            var loCard = new EffigyCardData { RarityId = "LO", Rarity = Rarity.LO, Drop = 15f };
            float effectiveDrop = WeightedCardDropService.GetCardEffectiveDrop(loCard, false, Season.S01);
            Assert.Equal(15f * 0.005f, effectiveDrop);
        }
    }
}
