namespace WankulCrazyPlugin.cards
{
    public class RarityJsonConverter : DynamicEnumJsonConverter<Rarity>
    {
        protected override Rarity FallbackValue => Rarity.C;

        protected override void OnUnknownValue(string value)
        {
            RaritiesManager.RegisterRarity(new RarityData(value, value, 1.0f, 1.0f));
        }
    }
}
