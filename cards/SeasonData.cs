namespace WankulCrazyPlugin.cards
{
    public class SeasonData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public SeasonData() { }

        public SeasonData(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
