using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    internal class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; } = null!;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            Instance = this;
            logger.LogDebug("NetworkHandler spawned");
            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ShakePlayerCamerasServerRpc(ScreenShakeType type, Vector3 position, float range)
        {
            if (!IsServer) { return; }
            ShakePlayerCamerasClientRpc(type, position, range);
        }

        [ClientRpc]
        private void ShakePlayerCamerasClientRpc(ScreenShakeType type, Vector3 position, float range)
        {
            float num = Vector3.Distance(localPlayer.transform.position, position);
            if (num < range)
            {
                HUDManager.Instance.ShakeCamera(type);
            }
            else if (num < range * 2f)
            {
                if ((int)type - 1 >= 0) { HUDManager.Instance.ShakeCamera((ScreenShakeType)((int)type - 1)); }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangePlayerSizeServerRpc(ulong clientId, float size)
        {
            if (!IsServer) { return; }
            ChangePlayerSizeClientRpc(clientId, size);
        }

        [ClientRpc]
        private void ChangePlayerSizeClientRpc(ulong clientId, float size)
        {
            PlayerControllerB? playerHeldBy = PlayerFromId(clientId);
            if (playerHeldBy == null) { return; }
            playerHeldBy.thisPlayerBody.localScale = new Vector3(size, size, size);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MufflePlayerServerRpc(ulong clientId, bool value)
        {
            if (!IsServer) { return; }
            MufflePlayerClientRpc(clientId, value);
        }

        [ClientRpc]
        private void MufflePlayerClientRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.MufflePlayer(value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void KillPlayerServerRpc(ulong clientId)
        {
            if (!IsServer) { return; }
            KillPlayerClientRpc(clientId);
        }

        [ClientRpc]
        private void KillPlayerClientRpc(ulong clientId)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            localPlayer.KillPlayer(Vector3.zero);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetScrapValueServerRpc(NetworkObjectReference netRef, int value)
        {
            if (!IsServer) { return; }
            SetScrapValueClientRpc(netRef, value);
        }

        [ClientRpc]
        private void SetScrapValueClientRpc(NetworkObjectReference netRef, int value)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject grabObj)) { return; }
            logger.LogDebug($"Setting scrap value of {grabObj.name} to {value}");
            grabObj.SetScrapValue(value);
        }
    }

    [HarmonyPatch]
    public class NetworkHandlerPatches
    {
        static GameObject? networkHandlerPrefab;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        static void GameNetworkManager_Start_PostFix()
        {
            // https://lethal.wiki/dev/advanced/networking/messaging
            networkHandlerPrefab = new GameObject("SnowyLibNetworkHandler");
            networkHandlerPrefab.hideFlags = HideFlags.HideAndDontSave;
            NetworkObject netObj = networkHandlerPrefab.AddComponent<NetworkObject>();

            netObj.AlwaysReplicateAsRoot = false;
            netObj.SynchronizeTransform = false;
            netObj.ActiveSceneSynchronization = false;
            netObj.SceneMigrationSynchronization = true;
            netObj.SpawnWithObservers = true;
            netObj.DontDestroyWithOwner = false;
            netObj.AutoObjectParentSync = false;

            var objectIdHash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes($"{MyPluginInfo.PLUGIN_GUID}.SnowyLibNetworkHandler"));

            netObj.GlobalObjectIdHash = BitConverter.ToUInt32(objectIdHash);

            //DawnLib.RegisterNetworkPrefab // TODO: Figure this out
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void StartOfRound_Awake_Postfix(StartOfRound __instance)
        {
            if (!__instance.IsServer) { return; }
            var networkHandlerHost = UnityEngine.Object.Instantiate(networkHandlerPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost?.GetComponent<NetworkObject>().Spawn();
        }
    }
}