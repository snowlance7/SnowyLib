using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyLib.Plugin;
using static SnowyLib.Utils;

namespace SnowyLib;

public static class ItemDropshipOperations // TODO: Test this
{
    public static ItemDropship? itemDropship { get; private set; }
    internal static float shipTimer;

    internal static List<ItemDropshipOperation> operations = [];
    internal static ItemDropshipOperation? currentOperation => operations.FirstOrDefault();

    internal static List<DeliverableItem> orderedItems = [];

    [OnFinishGeneratingLevel]
    internal static void GetItemDropshipReference()
    {
        itemDropship = GameObject.FindObjectOfType<ItemDropship>();
    }

    [StaticUpdate]
    internal static void ItemDropshipOperationsUpdate()
    {
        if (!IsServerOrHost || terminal == null || itemDropship == null) { return; }

        if (itemDropship.deliveringOrder || !IsItemDropshipNeeded())
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

    internal static string GetCurrentOperationMessage()
    {
        return $"{currentOperation!.baseConditionMessage}{(string.Join(currentOperation.conditionSeparator, currentOperation.conditions.Select(x => x.message)))}";
    }

    internal static void TrySatisfyConditionLocalClient()
    {
        NetworkHandler.Instance.TrySatisfyItemDropshipOperationConditionRpc(localPlayer.actualClientId);
    }

    internal static void TrySatisfyCondition(PlayerControllerB player)
    {
        if (currentOperation == null) { logger.LogError("TrySatisfyCondition: currentOperation is null"); return; }
        var satisfiedCondition = currentOperation.conditions.FirstOrDefault(x => x.condition.Invoke(player));
        if (satisfiedCondition == null) { logger.LogDebug("No conditionals met"); return; }
        currentOperation.conditions.Remove(satisfiedCondition);

        if (currentOperation.conditions.Count > 0)
        {
            NetworkHandler.Instance.UpdateItemDropshipTriggerRpc(GetCurrentOperationMessage());
        }
        else if (itemDropship != null)
        {
            itemDropship.timesPlayedWithoutTurningOff = 0;
            itemDropship.shipLanded = false;
            itemDropship.ShipLeaveClientRpc();
        }
    }

    internal static bool IsItemDropshipNeeded()
    {
        return (currentOperation != null && !currentOperation.inProgress) || orderedItems.Count > 0;
    }

    public static void RequestOperation(OperationCondition condition, Action<bool> postRetrieval, float processingTime, string baseConditionMessage = "Requires: ", string conditionSeparator = ", ")
    {
        operations.Add(new ItemDropshipOperation([condition], postRetrieval, processingTime, baseConditionMessage, conditionSeparator));
    }

    public static void RequestOperation(List<OperationCondition> conditions, Action<bool> postRetrieval, float processingTime, string baseConditionMessage = "Requires: ", string conditionSeparator = ", ")
    {
        operations.Add(new ItemDropshipOperation(conditions, postRetrieval, processingTime, baseConditionMessage, conditionSeparator));
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
}

public class ItemDropshipOperation(List<OperationCondition> conditions, Action<bool> postRetrieval, float processingTime, string baseConditionMessage = "Requires: ", string conditionSeparator = ", ")
{
    public List<OperationCondition> conditions = conditions;
    public Action<bool> postRetrieval = postRetrieval;
    public float processingTime = processingTime;
    public string baseConditionMessage = baseConditionMessage;
    public string conditionSeparator = conditionSeparator;
    internal bool inProgress;
    internal bool awaitingResult;
}

public class OperationCondition(Func<PlayerControllerB, bool> condition, string message)
{
    public Func<PlayerControllerB, bool> condition = condition;
    public string message = message;
}

public class DeliverableItem(NamespacedKey<DawnItemInfo> key, Action<GrabbableObject>? action = null)
{
    public NamespacedKey<DawnItemInfo> key = key;
    public Action<GrabbableObject>? action = action;
}

[HarmonyPatch]
internal static class Patches
{
    [HarmonyPostfix, HarmonyPatch(typeof(ItemDropship), nameof(ItemDropship.OpenShipDoorsOnServer))]
    public static void ItemDropship_OpenShipDoorsOnServer_Postfix(ItemDropship __instance)
    {
        try
        {
            if (!__instance.IsServer) { return; }

            if (ItemDropshipOperations.orderedItems.Count > 0)
            {
                logger.LogDebug($"Spawning {ItemDropshipOperations.orderedItems.Count} items from dropship");

                foreach (DeliverableItem item in ItemDropshipOperations.orderedItems)
                {
                    GameObject obj = GameObject.Instantiate(LethalContent.Items[item.key].Item.spawnPrefab, __instance.itemSpawnPositions.GetRandom().position, Quaternion.identity, __instance.playersManager.propsContainer);
                    obj.GetComponent<GrabbableObject>().fallTime = 0f;
                    obj.GetComponent<NetworkObject>().Spawn(actionAfterSpawn: (netObj) => item.action?.Invoke(netObj.GetComponent<GrabbableObject>()));
                    obj.GetComponent<NetworkObject>().TrySetParent(obj.transform.parent);
                }

                ItemDropshipOperations.orderedItems.Clear();
            }

            if (ItemDropshipOperations.currentOperation != null)
            {
                ItemDropshipOperations.currentOperation.inProgress = true;
                NetworkHandler.Instance.UpdateItemDropshipTriggerRpc(ItemDropshipOperations.GetCurrentOperationMessage());
                logger.LogDebug("ItemDropship operation started");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemDropship), nameof(ItemDropship.ShipLeave))]
    public static void ItemDropship_ShipLeave_Postfix(ItemDropship __instance)
    {
        try
        {
            if (!__instance.IsServer) { return; }

            if (ItemDropshipOperations.currentOperation != null && ItemDropshipOperations.currentOperation.inProgress && !ItemDropshipOperations.currentOperation.awaitingResult)
            {
                NetworkHandler.Instance.ResetItemDropshipTriggerRpc();
                ItemDropshipOperations.currentOperation.awaitingResult = true;
                InvokeAfterDelay(() =>
                {
                    if (ItemDropshipOperations.currentOperation.conditions.Count > 0)
                    {
                        ItemDropshipOperations.currentOperation.postRetrieval.Invoke(false);
                        return;
                    }

                    InvokeAfterDelay(() => ItemDropshipOperations.currentOperation.postRetrieval.Invoke(true), ItemDropshipOperations.currentOperation.processingTime);
                }, 50f);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return;
        }
    }
}