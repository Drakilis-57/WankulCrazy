using System;
using Newtonsoft.Json;

namespace WankulCrazyPlugin.cards
{
    /// <summary>
    /// Convertisseur JSON générique pour les enums dynamiques (Season, Rarity) :
    /// parse la valeur dans l'enum cible et délègue les identifiants inconnus
    /// au gestionnaire associé avant de retourner la valeur de repli.
    /// </summary>
    public abstract class DynamicEnumJsonConverter<TEnum> : JsonConverter where TEnum : struct, Enum
    {
        protected abstract TEnum FallbackValue { get; }

        protected abstract void OnUnknownValue(string value);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TEnum) || objectType == typeof(TEnum?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null || reader.TokenType == JsonToken.Null)
            {
                return default(TEnum);
            }

            string value = reader.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return default(TEnum);
            }

            if (Enum.TryParse<TEnum>(value, true, out var parsedEnum))
            {
                return parsedEnum;
            }

            OnUnknownValue(value);
            return FallbackValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }
    }
}
