using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class IndexAndExpansionTests
    {
        [Fact]
        public void DeserializeToken_AutoAssignsIndexesWhenIndexMissingOrZero()
        {
            string json = @"[
                { 'Title': 'Card A', 'Number': '1', 'SeasonId': 'S01' },
                { 'Title': 'Card B', 'Number': '2', 'SeasonId': 'S01' }
            ]";
            JToken token = JToken.Parse(json);
            List<WankulCardData> cards = JsonImporter.DeserializeToken(token);
            Assert.NotNull(cards);
            Assert.Equal(2, cards.Count);
        }
    }
}
