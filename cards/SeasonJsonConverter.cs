namespace WankulCrazyPlugin.cards
{
    public class SeasonJsonConverter : DynamicEnumJsonConverter<Season>
    {
        protected override Season FallbackValue => Season.HS;

        protected override void OnUnknownValue(string value)
        {
            SeasonsManager.RegisterSeason(new SeasonData(value, value));
        }
    }
}
