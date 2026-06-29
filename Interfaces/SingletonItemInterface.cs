using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace SnowyLib
{
    public interface ISingletonItem { } // TODO: Test this

    [HarmonyPatch]
    internal class ISingletonItemPatch
    {
        private static HashSet<Type> spawned = new HashSet<Type>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.InitializeAfterPositioning))]
        private static void GrabbableObject_InitializeAfterPositioning_Postfix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer || __instance is not ISingletonItem) { return; }

                Type type = __instance.GetType();

                if (spawned.Contains(type))
                {
                    var spawnableScrap = RoundManager.Instance.currentLevel.spawnableScrap.Select(x => x.rarity).ToList();

                    int randomWeightedIndexList = RoundManager.Instance.GetRandomWeightedIndexList(spawnableScrap);
                    Item replacementItem = RoundManager.Instance.currentLevel.spawnableScrap[randomWeightedIndexList].spawnableItem;
                    int replacementItemValue = (int)(UnityEngine.Random.Range(replacementItem.minValue, replacementItem.maxValue) * RoundManager.Instance.scrapValueMultiplier);

                    var vector = __instance.transform.position + Vector3.up * 0.25f;
                    var spawnedReplacementObj = GameObject.Instantiate(replacementItem.spawnPrefab, vector, quaternion.identity);
                    GrabbableObject spawnedReplacementItem = spawnedReplacementObj.GetComponent<GrabbableObject>();
                    spawnedReplacementItem.startFallingPosition = vector;
                    spawnedReplacementItem.targetFloorPosition = spawnedReplacementItem.GetItemFloorPosition(__instance.transform.position);
                    spawnedReplacementItem.NetworkObject.Spawn();
                    NetworkHandler.SetScrapValueServerRpc(spawnedReplacementItem.NetworkObject, replacementItemValue);

                    __instance.NetworkObject.Despawn(destroy: true);
                }
                else
                {
                    spawned.Add(type);
                }
            }
            catch
            {
                return;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.OnNetworkDespawn))]
        private static void GrabbableObject_OnNetworkDespawn_Prefix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer || __instance is not ISingletonItem) { return; }

                Type type = __instance.GetType();
                spawned.Remove(type);
            }
            catch
            {
                return;
            }
        }
    }
}
