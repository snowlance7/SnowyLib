using HarmonyLib;
using System;
using System.Collections.Generic;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public interface ISingletonEnemy
    {
        //public bool Replace { get; } // TODO
    }

    [HarmonyPatch]
    internal static class ISingletonEnemyPatch
    {
        private static HashSet<Type> spawned = new HashSet<Type>();
        private static EnemyAI? despawningDuplicate;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        private static void EnemyAI_Start_Postfix(EnemyAI __instance)
        {
            try
            {
                if (!__instance.IsServer || __instance is not ISingletonEnemy) { return; }

                Type type = __instance.GetType();

                if (spawned.Contains(type))
                {
                    despawningDuplicate = __instance;
                    Utils.DespawnNetworkObjectWhenSpawned(__instance.NetworkObject, destroy: true);
                    RoundManager.Instance.SpawnedEnemies.Remove(__instance);
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
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnDestroy))]
        private static void EnemyAI_OnDestroy_Prefix(EnemyAI __instance)
        {
            try
            {
                if (!__instance.IsServer || __instance is not ISingletonEnemy) { return; }
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
