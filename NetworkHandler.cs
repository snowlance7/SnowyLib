using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; } = null!;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (Instance != null && Instance != this)
                {
                    Instance.GetComponent<NetworkObject>().Despawn(destroy: true);
                }
            }

            hideFlags = HideFlags.HideAndDontSave;
            Instance = this;

            logger.LogDebug("NetworkHandler spawned");
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (Instance == this)
                Instance = null;
        }

        [Rpc(SendTo.Everyone)]
        public void ShakeCameraRpc(ulong clientId, ScreenShakeType screenShakeType)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            HUDManager.Instance.ShakeCamera(screenShakeType);
        }

        [Rpc(SendTo.Everyone)]
        public void ShakeCameraRpc(ulong[] clientId, ScreenShakeType screenShakeType)
        {
            if (!clientId.Contains(localPlayer.actualClientId)) { return; }
            HUDManager.Instance.ShakeCamera(screenShakeType);
        }

        [Rpc(SendTo.Everyone)]
        public void ChangePlayerSizeRpc(ulong clientId, float size)
        {
            PlayerControllerB? playerHeldBy = PlayerFromId(clientId);
            if (playerHeldBy == null) { return; }
            playerHeldBy.thisPlayerBody.localScale = new Vector3(size, size, size);
        }

        [Rpc(SendTo.Everyone)]
        public void MufflePlayerRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.MufflePlayer(value);
        }

        [Rpc(SendTo.Everyone)]
        public void KillPlayerRpc(ulong clientId)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            localPlayer.KillPlayer(Vector3.zero);
        }

        internal IEnumerator SetScrapSpawnOnNetworkSpawn(GrabbableObject grabbableObject, int value)
        {
            yield return null;
            yield return new WaitUntil(() => grabbableObject.NetworkObject != null && grabbableObject.NetworkObject.IsSpawned);
            NetworkHandler.Instance.SetScrapValueRpc(grabbableObject.NetworkObject, value);
        }

        [Rpc(SendTo.Everyone)]
        public void SetScrapValueRpc(NetworkObjectReference netRef, int value)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject grabObj)) { return; }
            logger.LogDebug($"Setting scrap value of {grabObj.name} to {value}");
            grabObj.SetScrapValue(value);
        }

        [Rpc(SendTo.Server)]
        public void SpawnEnemyRpc(NamespacedKey<DawnEnemyInfo> key, Vector3 position, Quaternion rotation = default, bool destroyWithScene = true)
        {
            if (!IsServer) { return; }
            GameObject obj = GameObject.Instantiate(LethalContent.Enemies[key].EnemyType.enemyPrefab, position, rotation);
            EnemyAI enemy = obj.GetComponent<EnemyAI>();
            enemy.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            RoundManager.Instance.SpawnedEnemies.Add(enemy);
            return;
        }

        [Rpc(SendTo.Server)]
        public void SpawnItemRpc(NamespacedKey<DawnItemInfo> key, Vector3 position, Quaternion rotation = default, float fallTime = 0f, bool destroyWithScene = false)
        {
            if (!IsServer) { return; }
            GameObject obj = GameObject.Instantiate(LethalContent.Items[key].Item.spawnPrefab, position, rotation);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.fallTime = fallTime;
            grabObj.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            return;
        }

        [Rpc(SendTo.Server)]
        public void SpawnMapObjectRpc(NamespacedKey<DawnMapObjectInfo> key, Vector3 position, Quaternion rotation = default, bool destroyWithScene = true)
        {
            if (!IsServer) { return; }
            GameObject obj = GameObject.Instantiate(LethalContent.MapObjects[key].GetMapObjectPrefab(), position, rotation);
            var mapObj = obj.GetComponent<SpawnableMapObject>();
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: destroyWithScene);
            return;
        }

        [Rpc(SendTo.Everyone)]
        public void SpawnExplosionRpc(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, bool goThroughCar = false)
        {
            Landmine.SpawnExplosion(explosionPosition: explosionPosition, spawnExplosionEffect: spawnExplosionEffect, killRange: killRange, damageRange: damageRange, nonLethalDamage: nonLethalDamage, physicsForce: physicsForce, goThroughCar: goThroughCar);
        }

        [Rpc(SendTo.Everyone)]
        public void SetEarsRingingRpc(ulong clientId, float time)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            SoundManager.Instance.earsRingingTimer = time;
        }

        [Rpc(SendTo.Everyone)]
        public void SetEarsRingingRpc(ulong[] clientId, float time)
        {
            if (!clientId.Contains(localPlayer.actualClientId)) { return; }
            SoundManager.Instance.earsRingingTimer = time;
        }

        [Rpc(SendTo.Everyone)]
        public void DisplayStatusEffectRpc(ulong clientId, string statusEffectString)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            Utils.DisplayStatusEffect(statusEffectString);
        }

        [Rpc(SendTo.Everyone)]
        public void DisplayStatusEffectRpc(ulong[] clientId, string statusEffectString)
        {
            if (!clientId.Contains(localPlayer.actualClientId)) { return; }
            Utils.DisplayStatusEffect(statusEffectString);
        }

        [Rpc(SendTo.Everyone)]
        public void SetShipLeaveEarlyServerRpc(float timeToLeaveEarly, string message, string speakerText = "SAFETY COMPUTER", float waitTime = 4f)
        {
            DialogueSegment dialogueSegment = new DialogueSegment();
            dialogueSegment.speakerText = speakerText;
            dialogueSegment.bodyText = message;
            dialogueSegment.waitTime = waitTime;
            Utils.SetShipLeaveEarly(timeToLeaveEarly, [dialogueSegment]);
        }

        [Rpc(SendTo.Server)]
        public void DisplayAdRpc()
        {
            HUDManager.Instance.ChooseAdItem();
        }
    }

    [HarmonyPatch]
    public static class NetworkHandlerPatches
    {
        public static GameObject? networkPrefab;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void GameNetworkManager_Start_Postfix()
        {
            if (networkPrefab != null)
                return;

            if (ModAssets == null) { logger.LogError("Couldnt get ModAssets to create network handler"); return; }
            networkPrefab = (GameObject)ModAssets.LoadAsset("Assets/ModAssets/NetworkHandler.prefab");

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void StartOfRound_Awake_Postfix()
        {
            if (!IsServerOrHost) { return; }

            GameObject networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab!, Vector3.zero, Quaternion.identity);
            networkHandlerHost!.GetComponent<NetworkObject>().Spawn();
        }
    }
}