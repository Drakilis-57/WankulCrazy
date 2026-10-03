using System.Collections.Generic;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.duel;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelEngineStep5Tests
{
    [Fact]
    public void CardAdapter_ConvertsTerrainAndEffigyCorrectly()
    {
        var terrainData = new TerrainCardData
        {
            Index = 500001,
            Number = "001",
            Title = "ROAD TRIP",
            CardType = CardType.Terrain,
            SpecialEffect = "Effet terrain"
        };

        var effigyNormal = new EffigyCardData
        {
            Index = 500010,
            Number = "010",
            Title = "Laink Viking",
            Force = 250,
            isScoreur = false,
            Rules = "Aucun effet scoreur"
        };

        var effigyExplicitScoreur = new EffigyCardData
        {
            Index = 500011,
            Number = "011",
            Title = "Terracid Chef",
            Force = 150,
            isScoreur = true
        };

        var effigyTextualScoreur = new EffigyCardData
        {
            Index = 500012,
            Number = "012",
            Title = "Laink Ninja",
            Force = 100,
            Rules = "Scorez un terrain pour gagner."
        };

        var duelTerrain = CardAdapter.ToDuelCard(terrainData);
        var duelNormal = CardAdapter.ToDuelCard(effigyNormal);
        var duelExplicit = CardAdapter.ToDuelCard(effigyExplicitScoreur);
        var duelTextual = CardAdapter.ToDuelCard(effigyTextualScoreur);

        Assert.Equal("001", duelTerrain.Id);
        Assert.Equal("ROAD TRIP", duelTerrain.Name);
        Assert.Equal(CardKind.Terrain, duelTerrain.Kind);
        Assert.False(duelTerrain.IsScoreur);

        Assert.Equal(CardKind.Character, duelNormal.Kind);
        Assert.Equal(250, duelNormal.Force);
        Assert.False(duelNormal.IsScoreur);

        Assert.Equal(CardKind.Character, duelExplicit.Kind);
        Assert.Equal(150, duelExplicit.Force);
        Assert.True(duelExplicit.IsScoreur);

        Assert.Equal(CardKind.Character, duelTextual.Kind);
        Assert.Equal(100, duelTextual.Force);
        Assert.True(duelTextual.IsScoreur);
    }

    [Fact]
    public void CardAdapter_GenerateFallbackDeck_ProducesBalanced50CardDeck()
    {
        var deck = CardAdapter.GenerateFallbackDeck("Test");

        Assert.Equal(50, deck.Count);
        Assert.Equal(10, deck.Count(c => c.Kind == CardKind.Terrain));
        Assert.Equal(35, deck.Count(c => c.Kind == CardKind.Character && !c.IsScoreur));
        Assert.Equal(5, deck.Count(c => c.Kind == CardKind.Character && c.IsScoreur));
    }

    [Theory]
    [InlineData("FRANÃƒâ€¡AIS", "FRANÇAIS")]
    [InlineData("FRANÃ‡AIS", "FRANÇAIS")]
    [InlineData("LÃ©onard Lam", "Léonard Lam")]
    [InlineData("Ã€ la fin", "À la fin")]
    [InlineData("coÃ»t", "coût")]
    [InlineData("Normal Title", "Normal Title")]
    public void CardAdapter_FixMojibake_RestoresFrenchAccentedNames(string input, string expected)
    {
        string result = CardAdapter.FixMojibake(input);
        Assert.Equal(expected, result);
    }
}
