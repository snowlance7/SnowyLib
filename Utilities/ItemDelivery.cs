using Dawn;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib;

public static partial class Utils
{
    private static ItemDropship? itemDropship;
    internal static List<DeliverableItem> orderedItems = [];
    private static float shipTimer;

    [StaticUpdate]
    internal static void ItemDeliveryUpdate()
    {
        if (!IsServerOrHost || terminal == null || itemDropship == null || itemDropship.deliveringOrder || orderedItems.Count <= 0)
        {
            shipTimer = 0f;
            return;
        }

        if (StartOfRound.Instance.shipHasLanded)
        {
            shipTimer += Time.deltaTime;
        }
        if (itemDropship.playersFirstOrder)
        {
            itemDropship.playersFirstOrder = false;
            shipTimer = 20f;
        }
        if (shipTimer > 40f)
        {
            itemDropship.LandShipOnServer();
        }
    }

    [OnFinishGeneratingLevel]
    internal static void GetItemDropshipReference()
    {
        itemDropship = GameObject.FindObjectOfType<ItemDropship>();
    }

    public static void DeliverItems(NamespacedKey<DawnItemInfo>[] keys)
    {
        DeliverItems(keys.Select(x => new DeliverableItem(x)).ToArray());
    }

    public static void DeliverItem(NamespacedKey<DawnItemInfo> key, Action<GrabbableObject>? action = null, int count = 1)
    {
        DeliverItem(new DeliverableItem(key, action), count);
    }

    public static void DeliverItem(DeliverableItem item, int count = 1)
    {
        if (!IsServerOrHost) { logger.LogError("DeliverItem: Only the server can order Deliverables"); return; }
        for (int i = 0; i < count; i++)
        {
            orderedItems.Add(item);
        }
    }

    public static void DeliverItems(DeliverableItem[] items)
    {
        if (!IsServerOrHost) { logger.LogError("DeliverItems: Only the server can order Deliverables"); return; }
        orderedItems.AddRange(items);
    }

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
                throw new NotImplementedException(); // TODO
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

public class DeliverableItem(NamespacedKey<DawnItemInfo> key, Action<GrabbableObject>? action = null)
{
    public NamespacedKey<DawnItemInfo> key = key;
    public Action<GrabbableObject>? action = action;
}

[HarmonyPatch]
internal static class ItemDeliveryPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(ItemDropship), nameof(ItemDropship.OpenShipDoorsOnServer))]
    private static void ItemDropship_OpenShipDoorsOnServer_Postfix(ItemDropship __instance)
    {
        try
        {
            if (Utils.orderedItems.Count <= 0 || !__instance.IsServer) { return; }
            if (__instance.shipLanded && !__instance.shipDoorsOpened)
            {
                foreach (DeliverableItem item in Utils.orderedItems)
                {
                    GameObject obj = GameObject.Instantiate(LethalContent.Items[item.key].Item.spawnPrefab, __instance.itemSpawnPositions.GetRandom().position, Quaternion.identity, __instance.playersManager.propsContainer);
                    obj.GetComponent<GrabbableObject>().fallTime = 0f;
                    obj.GetComponent<NetworkObject>().Spawn(actionAfterSpawn: (netObj) => item.action.Invoke(netObj.GetComponent<GrabbableObject>()));
                    obj.GetComponent<NetworkObject>().TrySetParent(obj.transform.parent);
                }

                Utils.orderedItems.Clear();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return;
        }
    }
}