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

        /// <summary>
        /// Règle officielle Wankul : un personnage Scoreur peut déclencher le scoring d'un terrain
        /// s'il est posé sur un terrain actif (non incliné).
        /// Détection via le texte des champs Rules ou Combo.
        /// </summary>
        public bool IsScoreur =>
            (Rules != null && Rules.Contains("Scoreur")) ||
            (Combo != null && Combo.Contains("Scorez un Terrain"));
    }
}
