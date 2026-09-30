using System;
using Newtonsoft.Json;
using Xunit;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class DuoRarityCharacterizationTests : IDisposable
    {
        public DuoRarityCharacterizationTests()
        {
            // Initialisation d'un état propre
            RaritiesManager.ResetToDefaults();
        }

        public void Dispose()
        {
            RaritiesManager.ResetToDefaults();
        }

        [Fact]
        public void DuoRarity_CurrentlyFallsBackToCommon_AndPriceMatchesCommon()
        {
            // Arrange
            string json = @"{
                'wankuls': [
                    { 'Title': 'Wankul DUO Test', 'Number': '1', 'Season': 'S05', 'Rarity': 'DUO', 'Drop': 0.001 }
                ]
            }";

            // Act
            var token = Newtonsoft.Json.Linq.JToken.Parse(json);
            var cards = importer.JsonImporter.DeserializeToken(token);

            // Assert
            Assert.Single(cards);
            var card = cards[0] as EffigyCardData;
            Assert.NotNull(card);

            // 1. Preuve du fallback: La rareté DUO est convertie en C (Commune)
            // car elle n'existe pas dans l'enum, même si elle a bien été parsée via JsonConverter.
            Assert.Equal(Rarity.C, card.Rarity);
            Assert.Equal("DUO", card.RarityId);

            // 2. Preuve Foil impossible: Puisqu'elle est vue comme C,
            // (card.Rarity >= Rarity.UR1) est faux.
            Assert.False(card.Rarity >= Rarity.UR1);

            // 3. Preuve Prix: Puisqu'elle est vue comme C dans les switchs,
            // Si on ne regarde que la rareté et non Drop, elle pourrait être évaluée différemment.
            // Dans CardPrice.generateMarketPrice actuel:
            // "if (wankulCardData is EffigyCardData effigyCard && effigyCard.Rarity >= Rarity.R) ... else { fallback price }"
            // Le fallback actuel se base sur `Drop` UNIQUEMENT.
            // Mais DUO est bien une EffigyCardData avec Rarity.C, donc le bloc "if (effigyCard.Rarity >= Rarity.R)"
            // ne sera PAS exécuté, et on tombera sur le bloc "else if (wankulCardData.Drop >= ...)" !

            // On s'attend à ce que le prix calculé actuel (avec un drop de 0.001) soit calculé via les IF sur Drop.
            // Vérifions d'abord la logique métier : Drop 0.001 (0.1%) ->
            // if (Drop >= 0.0008f) -> 1000f - 2500f.
            // Le prix ne vient donc PAS des mutliplicateurs de rarities.json actuellement.
            // C'est un test de caractérisation du comportement ACTUEL.

            // L'appel à generateMarketPrice lève une exception car UnityEngine.Random
            // n'est pas disponible hors de Unity, ce qui trigger le catch { return 1.0f }
            // C'est un test de caractérisation du comportement ACTUEL (incluant le crash Unity évité par le catch).
            Assert.Equal(1.0f, card.MarketPrice);
        }
    }
}
