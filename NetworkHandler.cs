using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections;
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

        public void Start()
        {
            InitConfigManager.Initialize();
            StaticUpdateManager.Initialize();
        }

        public void Update()
        {
            StaticUpdateManager.Update();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        public void ShakeCameraRpc(ScreenShakeType screenShakeType, RpcParams rpcParams)
        {
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

        [Rpc(SendTo.SpecifiedInParams)]
        public void SetEarsRingingRpc(float time, RpcParams rpcParams)
        {
            SoundManager.Instance.earsRingingTimer = time;
        }

        [Rpc(SendTo.SpecifiedInParams)]
        public void DisplayStatusEffectRpc(string statusEffectString, RpcParams rpcParams)
        {
            Utils.DisplayStatusEffect(statusEffectString);
        }

        [Rpc(SendTo.Everyone)]
        public void SetShipLeaveEarlyServerRpc(float timeToLeaveEarly, string message, string speakerText = "SAFETY COMPUTER", float waitTime = 4f)
        {
            DialogueSegment dialogueSegment = new DialogueSegment();
            dialogueSegment.speakerText = speakerText;
            dialogueSegment.bodyText = message;
            dialogueSegment.waitTime = waitTime;

            TimeOfDay.Instance.shipLeaveAutomaticallyTime = timeToLeaveEarly;
            TimeOfDay.Instance.shipLeavingAlertCalled = true;
            HUDManager.Instance.ReadDialogue([dialogueSegment]);
            HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
        }

        [Rpc(SendTo.Server)]
        public void DisplayAdRpc()
        {
            HUDManager.Instance.ChooseAdItem();
        }

        [Rpc(SendTo.Everyone)]
        public void RevivePlayerRpc(ulong clientId, Vector3 position = default(Vector3))
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }

            if (position == default)
                position = StartOfRound.Instance.GetPlayerSpawnPosition(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));

            player.isInsideFactory = false;
            player.isInElevator = true;
            player.isInHangarShipRoom = true;
            player.ResetPlayerBloodObjects(player.isPlayerDead);
            player.health = 5;
            player.isClimbingLadder = false;
            player.clampLooking = false;
            player.inVehicleAnimation = false;
            player.disableMoveInput = false;
            player.disableLookInput = false;
            player.disableInteract = false;
            player.ResetZAndXRotation();
            player.thisController.enabled = true;
            if (player.isPlayerDead)
            {
                player.thisController.enabled = true;
                player.isPlayerDead = false;
                player.isPlayerControlled = true;
                player.health = 5;
                player.hasBeenCriticallyInjured = false;
                player.criticallyInjured = false;
                player.playerBodyAnimator.SetBool("Limp", false);
                player.TeleportPlayer(position, false, 0f, false, true);
                player.parentedToElevatorLastFrame = false;
                player.overrideGameOverSpectatePivot = null;
                StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
                player.setPositionOfDeadPlayer = false;
                player.DisablePlayerModel(player.gameObject, true, true);
                player.helmetLight.enabled = false;
                player.Crouch(false);
                Animator playerBodyAnimator = player.playerBodyAnimator;
                if (playerBodyAnimator != null)
                {
                    playerBodyAnimator.SetBool("Limp", false);
                }
                player.bleedingHeavily = false;
                if (player.deadBody != null)
                {
                    player.deadBody.enabled = false;
                    player.deadBody.gameObject.SetActive(false);
                }
                player.bleedingHeavily = true;
                player.deadBody = null;
                player.activatingItem = false;
                player.twoHanded = false;
                player.inShockingMinigame = false;
                player.inSpecialInteractAnimation = false;
                player.freeRotationInInteractAnimation = false;
                player.disableSyncInAnimation = false;
                player.inAnimationWithEnemy = null;
                player.holdingWalkieTalkie = false;
                player.speakingToWalkieTalkie = false;
                player.isSinking = false;
                player.isUnderwater = false;
                player.sinkingValue = 0f;
                player.statusEffectAudio.Stop();
                player.DisableJetpackControlsLocally();
                player.mapRadarDotAnimator.SetBool("dead", false);
                player.hasBegunSpectating = false;
                player.externalForceAutoFade = Vector3.zero;
                player.hinderedMultiplier = 1f;
                player.isMovementHindered = 0;
                player.sourcesCausingSinking = 0;
                player.reverbPreset = StartOfRound.Instance.shipReverb;
                SoundManager.Instance.earsRingingTimer = 0f;
                player.voiceMuffledByEnemy = false;
                SoundManager.Instance.playerVoicePitchTargets[Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player)] = 1f;
                SoundManager.Instance.SetPlayerPitch(1f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                if (player.currentVoiceChatIngameSettings == null)
                {
                    StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
                }
                if (player.currentVoiceChatIngameSettings != null)
                {
                    if (player.currentVoiceChatIngameSettings.voiceAudio == null)
                    {
                        player.currentVoiceChatIngameSettings.InitializeComponents();
                    }
                    if (player.currentVoiceChatIngameSettings.voiceAudio == null)
                    {
                        return;
                    }
                    (player.currentVoiceChatIngameSettings.voiceAudio).GetComponent<OccludeAudio>().overridingLowPass = false;
                }
                HUDManager.Instance.UpdateBoxesSpectateUI();
                HUDManager.Instance.UpdateSpectateBoxSpeakerIcons();
            }
            if (GameNetworkManager.Instance.localPlayerController == player)
            {
                player.bleedingHeavily = false;
                player.criticallyInjured = false;
                player.health = 5;
                HUDManager.Instance.UpdateHealthUI(5, true);
                Animator playerBodyAnimator2 = player.playerBodyAnimator;
                if (playerBodyAnimator2 != null)
                {
                    playerBodyAnimator2.SetBool("Limp", false);
                }
                player.spectatedPlayerScript = null;
                StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, player);
                StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
                (HUDManager.Instance.audioListenerLowPass).enabled = false;
                HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
                HUDManager.Instance.RemoveSpectateUI();
                HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
            }
            StartOfRound.Instance.allPlayersDead = false;
            StartOfRound instance = StartOfRound.Instance;
            instance.livingPlayers++;
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        [Rpc(SendTo.Everyone)]
        public void DropHeldItemRpc(ulong clientId, int dropItemSlot, bool itemsFall, bool disconnecting, Vector3 syncedPlayerPosition = default(Vector3), Vector3 syncedHeldObjectPosition = default(Vector3), Vector3 syncedHeldObjectRotation = default(Vector3), Vector3 syncedPlayerCamPosition = default(Vector3), Vector3 syncedPlayerCamRotation = default(Vector3), bool setInShip = false, bool setInElevator = false)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            GrabbableObject? item = player.ItemSlots[dropItemSlot];
            if (item == null) { return; }
            player.DropHeldItem(item, itemsFall, disconnecting, syncedPlayerPosition, syncedHeldObjectPosition, syncedHeldObjectRotation, syncedPlayerCamPosition, syncedPlayerCamRotation, setInShip, setInElevator);
        }

        [Rpc(SendTo.Everyone)]
        public void DiscardItemInSlotRpc(ulong clientId, int slot, NetworkObjectReference parentObjectTo, bool placeObject = false, Vector3 placePosition = default(Vector3), bool matchRotationOfParent = true, bool setInShip = false, bool setInElevator = false, Vector3 syncedPlayerPosition = default(Vector3), Vector3 syncedHeldObjectPosition = default(Vector3), Vector3 syncedHeldObjectRotation = default(Vector3), Vector3 syncedPlayerCamPosition = default(Vector3), Vector3 syncedPlayerCamRotation = default(Vector3))
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.DiscardItemInSlot(slot, placeObject, parentObjectTo, placePosition, matchRotationOfParent, setInShip, setInElevator, syncedPlayerPosition, syncedHeldObjectPosition, syncedHeldObjectRotation, syncedPlayerCamPosition, syncedPlayerCamRotation);
        }

        [Rpc(SendTo.Everyone)]
        public void DiscardItemInSlotRpc(ulong clientId, int slot, bool placeObject = false, Vector3 placePosition = default(Vector3), bool matchRotationOfParent = true, bool setInShip = false, bool setInElevator = false, Vector3 syncedPlayerPosition = default(Vector3), Vector3 syncedHeldObjectPosition = default(Vector3), Vector3 syncedHeldObjectRotation = default(Vector3), Vector3 syncedPlayerCamPosition = default(Vector3), Vector3 syncedPlayerCamRotation = default(Vector3))
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.DiscardItemInSlot(slot, placeObject, null, placePosition, matchRotationOfParent, setInShip, setInElevator, syncedPlayerPosition, syncedHeldObjectPosition, syncedHeldObjectRotation, syncedPlayerCamPosition, syncedPlayerCamRotation);
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