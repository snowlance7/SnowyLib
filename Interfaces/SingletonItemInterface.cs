using Dawn;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SnowyLib.Plugin;

// UPDATE: Do singleton or maxSpawned using transpiler on spawnscrapinlevel

namespace SnowyLib
{
    public interface ISingletonItem { }

    [HarmonyPatch]
    internal static class ISingletonItemPatch
    {
        private static HashSet<Type> spawned = new HashSet<Type>();
        private static GrabbableObject? despawningDuplicate;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void GrabbableObject_Start_Postfix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer || __instance is not ISingletonItem) { return; }

                Type type = __instance.GetType();

                if (spawned.Contains(type))
                {
                    var spawnableScrap = RoundManager.Instance.currentLevel.spawnableScrap.Where(x => x.spawnableItem != __instance.itemProperties).ToList();
                    var spawnableScrapRarities = spawnableScrap.Select(x => x.rarity).ToList();

                    int randomWeightedIndexList = RoundManager.Instance.GetRandomWeightedIndexList(spawnableScrapRarities);
                    Item replacementItem = spawnableScrap[randomWeightedIndexList].spawnableItem;
                    int replacementItemValue = (int)(UnityEngine.Random.Range(replacementItem.minValue, replacementItem.maxValue) * RoundManager.Instance.scrapValueMultiplier);
                    logger.LogDebug($"Only one {__instance.itemProperties.name} instance can be spawned, replacing duplicate with {replacementItem.name} with a scrap value of {replacementItemValue}");

                    var spawnPos = __instance.transform.position + Vector3.up * 0.25f;
                    GrabbableObject spawnedReplacementItem = Utils.SpawnItem(replacementItem.GetDawnInfo().TypedKey, spawnPos)!;

                    NetworkHandler.Instance.StartCoroutine(NetworkHandler.Instance.SetScrapSpawnOnNetworkSpawn(spawnedReplacementItem, replacementItemValue));

                    despawningDuplicate = __instance;
                    __instance.NetworkObject.Despawn(destroy: true);
                }
                else
                {
                    spawned.Add(type);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.OnDestroy))]
        private static void GrabbableObject_OnDestroy_Prefix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer || __instance is not ISingletonItem) { return; }
                if (despawningDuplicate != null && despawningDuplicate == __instance) { despawningDuplicate = null; return; }

                spawned.Remove(__instance.GetType());
            }
            catch
            {
                return;
            }
        }
    }
}
