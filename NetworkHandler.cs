using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using StaticNetcodeLib;
using System;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    [StaticNetcode]
    public static class NetworkHandler
    {
        static bool IsServer => IsServerOrHost; // TODO: Test this

        [ServerRpc]
        public static void ShakePlayerCamerasServerRpc(ScreenShakeType type, Vector3 position, float range)
        {
            if (!IsServer) { return; }
            ShakePlayerCamerasClientRpc(type, position, range);
        }

        [ClientRpc]
        private static void ShakePlayerCamerasClientRpc(ScreenShakeType type, Vector3 position, float range)
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

        [ServerRpc]
        public static void ChangePlayerSizeServerRpc(ulong clientId, float size)
        {
            if (!IsServer) { return; }
            ChangePlayerSizeClientRpc(clientId, size);
        }

        [ClientRpc]
        private static void ChangePlayerSizeClientRpc(ulong clientId, float size)
        {
            PlayerControllerB? playerHeldBy = PlayerFromId(clientId);
            if (playerHeldBy == null) { return; }
            playerHeldBy.thisPlayerBody.localScale = new Vector3(size, size, size);
        }

        [ServerRpc]
        public static void MufflePlayerServerRpc(ulong clientId, bool value)
        {
            if (!IsServer) { return; }
            MufflePlayerClientRpc(clientId, value);
        }

        [ClientRpc]
        private static void MufflePlayerClientRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.MufflePlayer(value);
        }

        [ServerRpc]
        public static void KillPlayerServerRpc(ulong clientId)
        {
            if (!IsServer) { return; }
            KillPlayerClientRpc(clientId);
        }

        [ClientRpc]
        private static void KillPlayerClientRpc(ulong clientId)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            localPlayer.KillPlayer(Vector3.zero);
        }

        [ServerRpc]
        public static void SetScrapValueServerRpc(NetworkObjectReference netRef, int value)
        {
            if (!IsServer) { return; }
            SetScrapValueClientRpc(netRef, value);
        }

        [ClientRpc]
        private static void SetScrapValueClientRpc(NetworkObjectReference netRef, int value)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject grabObj)) { return; }
            logger.LogDebug($"Setting scrap value of {grabObj.name} to {value}");
            grabObj.SetScrapValue(value);
        }

        [ServerRpc]
        public static void SpawnEnemyServerRpc(NamespacedKey<DawnEnemyInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = true)
        {
            if (!IsServer) { return; }
            GameObject obj = GameObject.Instantiate(LethalContent.Enemies[key].EnemyType.enemyPrefab, position, rotation, parentTo);
            EnemyAI enemy = obj.GetComponent<EnemyAI>();
            enemy.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            RoundManager.Instance.SpawnedEnemies.Add(enemy);
            return;
        }

        [ServerRpc]
        public static void SpawnItemServerRpc(NamespacedKey<DawnItemInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, float fallTime = 0f, bool destroyWithScene = false)
        {
            if (!IsServer) { return; }
            GameObject obj = GameObject.Instantiate(LethalContent.Items[key].Item.spawnPrefab, position, rotation, parentTo);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.fallTime = fallTime;
            grabObj.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            return;
        }

        [ServerRpc]
        public static void SpawnMapObjectServerRpc(NamespacedKey<DawnMapObjectInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = true)
        {
            if (!IsServer) { return; }
            GameObject obj = GameObject.Instantiate(LethalContent.MapObjects[key].GetMapObjectPrefab(), position, rotation, parentTo);
            var mapObj = obj.GetComponent<SpawnableMapObject>();
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: destroyWithScene);
            return;
        }
    }
}