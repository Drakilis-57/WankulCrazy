namespace WankulCrazyPlugin.cards
{
    public class RarityData
    {
        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                if (string.Equals(_id, "DUO", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (PriceRangeMin == 0.01f && PriceRangeMax == 0.5f)
                    {
                        PriceRangeMin = 500f;
                        PriceRangeMax = 1000f;
                        IsEligibleForFoil = true;
                        IsEligibleForMinRare = true;
                    }
                }
            }
        }
        public string Name { get; set; } = string.Empty;
        public float ExperienceMultiplier { get; set; } = 1.0f;
        public float PriceMultiplier { get; set; } = 1.0f;

        // Nouvelles propriétés pour le système dynamique
        public float PriceRangeMin { get; set; } = 0.01f;
        public float PriceRangeMax { get; set; } = 0.5f;
        public bool IsEligibleForFoil { get; set; } = false;
        public bool IsEligibleForMinRare { get; set; } = false;
        public float DropMultiplier { get; set; } = 1.0f;

        public RarityData() { }

        public RarityData(string id, string name, float experienceMultiplier = 1.0f, float priceMultiplier = 1.0f)
        {
            Id = id;
            Name = name;
            ExperienceMultiplier = experienceMultiplier;
            PriceMultiplier = priceMultiplier;

            // Assignation des valeurs par défaut pour les raretés
            if (id == "LB")
            {
                DropMultiplier = 0.05f;
            }
            else if (id == "LA")
            {
                DropMultiplier = 0.02f;
            }
            else if (id == "LO")
            {
                DropMultiplier = 0.005f;
            }
            else if (id == "DUO")
            {
                PriceRangeMin = 500f;
                PriceRangeMax = 1000f;
                IsEligibleForFoil = true;
                IsEligibleForMinRare = true;
                DropMultiplier = 0.02f;
            }
        }
    }
}
