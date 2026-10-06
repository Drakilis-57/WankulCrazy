using Xunit;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.duel;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.Tests
{
    public class DuelSeason5EffectsTests
    {
        [Fact]
        public void ResolveEffectIds_Season5_AssignsMill5()
        {
            var terrain = new TerrainCardData { Title = "STUDIO DE CAST", LosingEffect = "meule cinq cartes." };
            var duelCard = CardAdapter.ToDuelCard(terrain);
            Assert.Contains("mill_5", duelCard.EffectIds);
        }

        [Fact]
        public void EffectRegistry_Season5_ContainsNewEffects()
        {
            var registry = new EffectRegistry();
            Assert.True(registry.TryGetEffect("mill_5", out _));
            Assert.True(registry.TryGetEffect("banish_deck_6", out _));
        }
    }
}
