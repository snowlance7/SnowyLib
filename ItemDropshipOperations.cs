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

public static class ItemDropshipOperations
{
    public static ItemDropship? itemDropship { get; private set; }
    internal static float shipTimer;

    internal static List<ItemDropshipOperation> operations { get; set; } = [];
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

    internal static void OnInteractLocalClient()
    {
        NetworkHandler.Instance.ItemDropshipOperationOnInteractRpc(localPlayer.actualClientId);
    }

    internal static void OnInteract(PlayerControllerB player)
    {
        if (currentOperation == null) { logger.LogError("OnInteract: currentOperation is null"); return; }
        currentOperation.OnInteract(player);
    }

    internal static bool IsItemDropshipNeeded()
    {
        return (currentOperation != null && !currentOperation.inProgress) || orderedItems.Count > 0;
    }

    public static void UpdateItemDropshipTrigger(string message)
    {
        NetworkHandler.Instance.UpdateItemDropshipTriggerRpc(message);
    }

    public static void ShipLeave()
    {
        if (itemDropship == null || !itemDropship.shipLanded || !IsServerOrHost) { return; }
        itemDropship.timesPlayedWithoutTurningOff = 0;
        itemDropship.shipLanded = false;
        itemDropship.ShipLeaveClientRpc();
    }

    public static void Request(OperationCondition condition, Action<bool> postRetrieval, float processingTime, string baseConditionMessage = "Requires: ", string conditionSeparator = ", ")
    {
        operations.Add(new ItemDropshipConditionalOperation([condition], postRetrieval, processingTime, baseConditionMessage, conditionSeparator));
    }

    public static void Request(List<OperationCondition> conditions, Action<bool> postRetrieval, float processingTime, string baseConditionMessage = "Requires: ", string conditionSeparator = ", ")
    {
        operations.Add(new ItemDropshipConditionalOperation(conditions, postRetrieval, processingTime, baseConditionMessage, conditionSeparator));
    }

    public static void Deliver(NamespacedKey<DawnItemInfo>[] keys)
    {
        Deliver(keys.Select(x => new DeliverableItem(x)).ToArray());
    }

    public static void Deliver(NamespacedKey<DawnItemInfo> key, Action<GrabbableObject>? action = null, int count = 1)
    {
        Deliver(new DeliverableItem(key, action), count);
    }

    public static void Deliver(DeliverableItem item, int count = 1)
    {
        if (!IsServerOrHost) { logger.LogError("DeliverItem: Only the server can order Deliverables"); return; }
        for (int i = 0; i < count; i++)
        {
            orderedItems.Add(item);
        }
    }

    public static void Deliver(DeliverableItem[] items)
    {
        if (!IsServerOrHost) { logger.LogError("DeliverItems: Only the server can order Deliverables"); return; }
        orderedItems.AddRange(items);
    }
}

public abstract class ItemDropshipOperation(float processingTime)
{
    public float processingTime = processingTime;
    internal bool inProgress;
    internal bool awaitingResult;

    public virtual void OnShipLanded() { }
    public virtual void OnShipDoorsOpened() { }
    public virtual void OnShipLeave() { }
    public abstract bool ConfirmProcessing();
    public abstract void OnProcessingComplete();
    public abstract void OnInteract(PlayerControllerB player);
}

public class ItemDropshipConditionalOperation(List<OperationCondition> conditions, Action<bool> postRetrieval, float processingTime, string baseConditionMessage = "Requires: ", string conditionSeparator = ", ") : ItemDropshipOperation(processingTime)
{
    public List<OperationCondition> conditions = conditions;
    public Action<bool> postRetrieval = postRetrieval;
    public string baseConditionMessage = baseConditionMessage;
    public string conditionSeparator = conditionSeparator;

    public override bool ConfirmProcessing()
    {
        if (conditions.Count > 0)
        {
            postRetrieval.Invoke(false);
            return false;
        }
        return true;
    }

    public override void OnInteract(PlayerControllerB player)
    {
        var satisfiedCondition = conditions.FirstOrDefault(x => x.condition.Invoke(player));
        if (satisfiedCondition == null) { logger.LogDebug("No conditionals met"); return; }
        conditions.Remove(satisfiedCondition);

        if (conditions.Count > 0)
        {
            string message = $"{baseConditionMessage}{(string.Join(conditionSeparator, conditions.Select(x => x.message)))}";
            NetworkHandler.Instance.UpdateItemDropshipTriggerRpc(message);
        }
        else
        {
            ItemDropshipOperations.ShipLeave();
        }
    }

    public override void OnProcessingComplete()
    {
        postRetrieval.Invoke(true);
    }
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
                ItemDropshipOperations.currentOperation.OnShipDoorsOpened();
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
                var operation = ItemDropshipOperations.currentOperation;

                NetworkHandler.Instance.ResetItemDropshipTriggerRpc();
                operation.awaitingResult = true;
                operation.OnShipLeave();

                InvokeAfterDelay(() =>
                {
                    if (operation.ConfirmProcessing())
                    {
                        InvokeAfterDelay(() =>
                        {
                            operation.OnProcessingComplete();
                            ItemDropshipOperations.operations.Remove(operation);
                        }, operation.processingTime);
                    }
                }, 50f);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemDropship), nameof(ItemDropship.ShipLandedAnimationEvent))]
    public static void ItemDropship_ShipLandedAnimationEvent_Postfix(ItemDropship __instance)
    {
        try
        {
            if (!__instance.IsServer) { return; }

            ItemDropshipOperations.currentOperation?.OnShipLanded();
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemDropship), nameof(ItemDropship.ShipLandedInAnimation))]
    public static void ItemDropship_ShipLandedInAnimation_Postfix(ItemDropship __instance)
    {
        try
        {
            if (!__instance.IsServer) { return; }

            ItemDropshipOperations.currentOperation?.OnShipLanded();
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return;
        }
    }
}