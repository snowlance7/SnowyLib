using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static BepInEx.BepInDependency;

// UPDATE: A specialized map object type that can appear on company moons, like venders (vending machines, washing machine, etc) or even big ones like the casino or pet center or something. and could even be like a market of sorts, where different shops could appear going down each side of the walking area with little paths

namespace SnowyLib
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(Dawn.DawnLib.PLUGIN_GUID)]
    internal class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS8618
        public static Plugin Instance { get; private set; }
        public static ManualLogSource logger { get; private set; }
#pragma warning restore CS8618

        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }
        public static PlayerControllerB? PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts.Where(x => x.actualClientId == id).FirstOrDefault(); }
        public static bool IsServerOrHost { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }
        
        public static AssetBundle ModAssets = null!;

        public static ConfigEntry<bool> cfgTesting = null!;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            cfgTesting = Config.Bind("Debugging", "Testing", false, "For debugging purposes");

            logger = Instance.Logger;

            harmony.PatchAll();

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "snowylib_assets"));

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }
    }
}