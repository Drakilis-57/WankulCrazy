using System;
using Newtonsoft.Json;

namespace WankulCrazyPlugin.cards
{
    public class RarityJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Rarity) || objectType == typeof(Rarity?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null || reader.TokenType == JsonToken.Null)
            {
                return default(Rarity);
            }

            string value = reader.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return default(Rarity);
            }

            if (Enum.TryParse<Rarity>(value, true, out var rarityEnum))
            {
                return rarityEnum;
            }

            RaritiesManager.RegisterRarity(new RarityData(value, value, 1.0f, 1.0f));
            return Rarity.C;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }
    }
}
