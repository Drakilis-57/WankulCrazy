using System;
using Newtonsoft.Json;

namespace WankulCrazyPlugin.cards
{
    public class SeasonJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Season) || objectType == typeof(Season?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null || reader.TokenType == JsonToken.Null)
            {
                return default(Season);
            }

            string value = reader.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return default(Season);
            }

            if (Enum.TryParse<Season>(value, true, out var seasonEnum))
            {
                return seasonEnum;
            }

            SeasonsManager.RegisterSeason(new SeasonData(value, value));
            return Season.HS;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }
    }
}
