using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WankulCrazyPlugin.patch;

namespace WankulCrazyPlugin.cards
{
    public class WankulCardData
    {
        public int Index;

        public string Number;

        public string Title;

        public string Artist;

        public CardType CardType;

        [JsonConverter(typeof(SeasonJsonConverter))]
        public Season Season;

        private string seasonId;

        public string SeasonId
        {
            get => !string.IsNullOrEmpty(seasonId) ? seasonId : Season.ToString();
            set
            {
                seasonId = value;
                if (!string.IsNullOrEmpty(value))
                {
                    SeasonsManager.RegisterSeason(new SeasonData(value, value));
                    if (Enum.TryParse<Season>(value, true, out var parsedSeason))
                    {
                        Season = parsedSeason;
                    }
                }
            }
        }

        public string TexturePath;

        private object texture;
        private object textureMask;
        private object sprite;
        private object spriteMask;

        [JsonIgnore]
        public object Texture { get => texture; set => texture = value; }

        [JsonIgnore]
        public object TextureMask { get => textureMask; set => textureMask = value; }

        [JsonIgnore]
        public object Sprite { get => sprite; set => sprite = value; }

        [JsonIgnore]
        public object SpriteMask { get => spriteMask; set => spriteMask = value; }

        public bool IsNumberInt(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        public int NumberInt
        {
            get
            {
                if (!IsNumberInt(Number))
                {
                    return -1;
                }
                return int.Parse(Number);
            }
        }

        public float Drop;

        public List<float> PastPercent = new List<float>();
        public float Percentage = 100;

        public float generatedMarketPrice;

        [JsonIgnore]
        public float MarketPrice
        {
            get
            {
                if (generatedMarketPrice == 0)
                {
                    try
                    {
                        generatedMarketPrice = CardPrice.generateMarketPrice(this);
                    }
                    catch
                    {
                        generatedMarketPrice = 1.0f;
                    }
                }
                return generatedMarketPrice * (Percentage / 100);
            }
            set
            {
                generatedMarketPrice = value;
            }
        }
    }
}
