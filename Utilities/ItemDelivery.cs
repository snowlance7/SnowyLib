using Dawn;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static partial class Utils
    {
        public static void BuyItem(NamespacedKey<DawnItemInfo> key, int count = 1, bool ignoreMax = false, int newGroupCredits = -1)
        {
            if (terminal == null) { logger.LogError("BuyItem failed, unable to find terminal"); return; }

            var dawnShopItem = LethalContent.Items[key].ShopInfo;
            if (dawnShopItem == null) { logger.LogError($"Failed to buy item {key}, item is not buyable from the shop"); return; }

            dawnShopItem.AddToDropship(ignoreMax, count);
            terminal.groupCredits = newGroupCredits >= 0 ? newGroupCredits : terminal.groupCredits;
            terminal.SyncBoughtItemsWithServer(terminal.orderedItemsFromTerminal.ToArray(), terminal.numberOfItemsInDropship);
        }

        public static void BuyItems(NamespacedKey<DawnItemInfo>[] keys, bool ignoreMax = false, int newGroupCredits = -1)
        {
            if (terminal == null) { logger.LogError("BuyItem failed, unable to find terminal"); return; }

            foreach (var key in keys)
            {
                var dawnShopItem = LethalContent.Items[key].ShopInfo;
                if (dawnShopItem == null)
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.numberOfItemsInDropship = 0;
                    logger.LogError($"Failed to buy item {key}, item is not buyable from the shop");
                    return;
                }

                dawnShopItem.AddToDropship(ignoreMax);
            }
            terminal.groupCredits = newGroupCredits >= 0 ? newGroupCredits : terminal.groupCredits;
            terminal.SyncBoughtItemsWithServer(terminal.orderedItemsFromTerminal.ToArray(), terminal.numberOfItemsInDropship);
        }

        public static bool TryBuyItem(NamespacedKey<DawnItemInfo> key, int count = 1, bool ignoreMax = false)
        {
            if (terminal == null) { logger.LogError("TryBuyItem failed, unable to find terminal"); return false; }

            var dawnShopItem = LethalContent.Items[key].ShopInfo;
            if (dawnShopItem == null) { logger.LogError($"Failed to buy item {key}, item is not buyable from the shop"); return false; }

            int cost = (int)(dawnShopItem.DawnPurchaseInfo.Cost.Provide() * (dawnShopItem.GetSalePercentage() / 100f)) * count;
            int newGroupCredits = terminal.groupCredits - cost;

            if (newGroupCredits < 0) { return false; }

            dawnShopItem.AddToDropship(ignoreMax, count);
            terminal.groupCredits = newGroupCredits;
            terminal.SyncBoughtItemsWithServer(terminal.orderedItemsFromTerminal.ToArray(), terminal.numberOfItemsInDropship);
            return true;
        }

        public static bool TryBuyItems(NamespacedKey<DawnItemInfo>[] keys, bool ignoreMax = false)
        {
            if (terminal == null) { logger.LogError("TryBuyItems failed, unable to find terminal"); return false; }

            int newGroupCredits = terminal.groupCredits;

            foreach (var key in keys)
            {
                if (!ignoreMax && terminal.orderedItemsFromTerminal.Count > 12) { break; }

                var dawnShopItem = LethalContent.Items[key].ShopInfo;
                if (dawnShopItem == null)
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.numberOfItemsInDropship = 0;
                    logger.LogError($"Failed to buy item {key}, item is not buyable from the shop");
                    return false;
                }

                int cost = (int)(dawnShopItem.DawnPurchaseInfo.Cost.Provide() * (dawnShopItem.GetSalePercentage() / 100f));
                newGroupCredits -= cost;

                if (newGroupCredits < 0)
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.numberOfItemsInDropship = 0;
                    return false;
                }

                dawnShopItem.AddToDropship(ignoreMax);
            }

            terminal.groupCredits = newGroupCredits;
            terminal.SyncBoughtItemsWithServer(terminal.orderedItemsFromTerminal.ToArray(), terminal.numberOfItemsInDropship);
            return true;
        }
    }
}
