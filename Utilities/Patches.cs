using HarmonyLib;
using System;
using System.Linq;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    [HarmonyPatch]
    public static class UtilsPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnInsideEnemiesFromVentsIfReady))]
        public static bool RoundManager_SpawnInsideEnemiesFromVentsIfReady_Prefix()
        {
            try
            {
                if (Utils.testing && Utils.DEBUG_disableSpawning) { return false; }
                return true;
            }
            catch
            {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnDaytimeEnemiesOutside))]
        public static bool RoundManager_SpawnDaytimeEnemiesOutside_Prefix()
        {
            try
            {
                if (Utils.testing && Utils.DEBUG_disableSpawning) { return false; }
                return true;
            }
            catch
            {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnEnemiesOutside))]
        public static bool RoundManager_SpawnEnemiesOutside_Prefix()
        {
            try
            {
                if (Utils.testing && Utils.DEBUG_disableSpawning) { return false; }
                return true;
            }
            catch
            {
                return true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingLevel))]
        public static void RoundManager_FinishGeneratingLevel_Postfix()
        {
            try
            {
                Utils.elevator = null;
                Utils.entrances.Clear();

                Utils.entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>().ToList();
                Utils.elevator = UnityEngine.Object.FindObjectOfType<MineshaftElevatorController>();

                Utils.SetRandoms();

                Utils.OnFinishGeneratingLevel.Invoke();
            }
            catch
            {
                return;
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        public static void StartOfRound_OnShipLandedMiscEvents_Postfix()
        {
            try
            {
                Utils.OnShipLanded.Invoke();
            }
            catch
            {
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Terminal), nameof(Terminal.Start))]
        public static void Terminal_Start_Prefix(Terminal __instance)
        {
            try
            {
                Utils.terminal = __instance;
            }
            catch
            {
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void HUDManager_SubmitChat_performed_Prefix(HUDManager __instance)
        {
            try
            {
                if (!Utils.testing) { return; }
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");

                Utils.ChatCommand(args);
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        public static void GrabbableObject_Start_Postfix(GrabbableObject __instance)
        {
            try
            {
                Utils.spawnedItems.Add(__instance);
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.OnDestroy))]
        public static void GrabbableObject_OnDestroy_Postfix(GrabbableObject __instance)
        {
            try
            {
                Utils.spawnedItems.Remove(__instance);
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}
