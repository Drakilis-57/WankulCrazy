using System;
using Newtonsoft.Json;

namespace WankulCrazyPlugin.cards
{
    [System.Serializable]
    public class EffigyCardData : WankulCardData
    {
        public string Effigy;

        public int Force;

        public int Cost;

        public string Rules;

        public string Combo;

        public string Quote;

        [JsonConverter(typeof(RarityJsonConverter))]
        public Rarity Rarity;

        private string rarityId;

        public string RarityId
        {
            get => !string.IsNullOrEmpty(rarityId) ? rarityId : Rarity.ToString();
            set
            {
                rarityId = value;
                if (!string.IsNullOrEmpty(value))
                {
                    RaritiesManager.RegisterRarity(new RarityData(value, value, 1.0f, 1.0f));
                    if (Enum.TryParse<Rarity>(value, true, out var parsedRarity))
                    {
                        Rarity = parsedRarity;
                    }
                }
            }
        }

        public bool isScoreur;

        /// <summary>
        /// Règle officielle Wankul : un personnage Scoreur peut déclencher le scoring d'un terrain
        /// s'il est posé sur un terrain actif (non incliné).
        /// Détection via le champ explicite isScoreur ou texte des champs Rules / Combo.
        /// </summary>
        public bool IsScoreur =>
            isScoreur ||
            (Rules != null && (Rules.Contains("Scoreur") || Rules.Contains("Scorez un terrain") || Rules.Contains("Scorez un Terrain"))) ||
            (Combo != null && (Combo.Contains("Scorez un Terrain") || Combo.Contains("Scorez un terrain")));
    }
}
