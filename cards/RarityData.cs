namespace WankulCrazyPlugin.cards
{
    public class RarityData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float ExperienceMultiplier { get; set; } = 1.0f;
        public float PriceMultiplier { get; set; } = 1.0f;

        public RarityData() { }

        public RarityData(string id, string name, float experienceMultiplier = 1.0f, float priceMultiplier = 1.0f)
        {
            Id = id;
            Name = name;
            ExperienceMultiplier = experienceMultiplier;
            PriceMultiplier = priceMultiplier;
        }
    }
}
