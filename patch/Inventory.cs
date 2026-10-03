using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.inventory;

namespace WankulCrazyPlugin.patch
{
    public class Inventory
    {
        public static void AddCard(CardData cardData, int addAmount)
        {
            if (cardData == null || cardData.cardGrade > 0)
            {
                return;
            }

            WankulCardData wankulCardData = WankulCardsData.Instance.GetFromMonster(cardData, true);

            if (wankulCardData == null)
            {
                wankulCardData = WankulCardsData.GetAJETER();
                Plugin.Logger.LogWarning("wankulCardData is null, using AJETER");
            }

            WankulInventory.AddCard(wankulCardData, cardData, addAmount);
        }

        public static void RemoveCard(CardData cardData, int reduceAmount)
        {
            if (cardData == null || cardData.cardGrade > 0)
            {
                return;
            }

            WankulCardData wankulCardData = WankulCardsData.Instance.GetFromMonster(cardData, true);

            if (wankulCardData == null)
            {
                Plugin.Logger.LogError("wankulCardData is null");
                return;
            }

            WankulInventory.RemoveCard(wankulCardData, reduceAmount);
        }
    }
}
