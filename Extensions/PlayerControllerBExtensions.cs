using Dissonance;
using GameNetcodeStuff;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static SnowyLib.Plugin;
using static UnityEngine.InputSystem.InputRemoting;

namespace SnowyLib
{
    public static class PlayerControllerBExtensions
    {
        public static StatusEffectController StatusEffectController(this PlayerControllerB player)
        {
            return player.gameObject.TryGetComponent(out StatusEffectController controller) ? controller : player.gameObject.AddComponent<StatusEffectController>();
        }

        /// <summary>
        /// Attempts to grab the specified grabbable object if the player meets all requirements.
        /// </summary>
        /// <param name="player">The player attempting to grab the object.</param>
        /// <param name="grabbableObject">The object to be grabbed.</param>
        /// <returns>true if the object was successfully grabbed; otherwise, false.</returns>
        public static bool GrabGrabbableObject(this PlayerControllerB player, GrabbableObject grabbableObject)
        {
            if (player.twoHanded || player.sinkingValue > 0.73f) { return false; }

            player.currentlyGrabbingObject = grabbableObject;

            if (!GameNetworkManager.Instance.gameHasStarted && !player.currentlyGrabbingObject.itemProperties.canBeGrabbedBeforeGameStart && StartOfRound.Instance.testRoom == null) { return false; }

            player.grabInvalidated = false;

            if (player.currentlyGrabbingObject == null || player.inSpecialInteractAnimation || player.currentlyGrabbingObject.isHeld || player.currentlyGrabbingObject.isPocketed) { return false; }

            NetworkObject networkObject = player.currentlyGrabbingObject.NetworkObject;
            if (networkObject == null || !networkObject.IsSpawned) { return false; }

            player.currentlyGrabbingObject.InteractItem();

            if (!player.currentlyGrabbingObject.grabbable || player.FirstEmptyItemSlot(player.currentlyGrabbingObject) == -1) { return false; }

            player.playerBodyAnimator.SetBool("GrabInvalidated", value: false);
            player.playerBodyAnimator.SetBool("GrabValidated", value: false);
            player.playerBodyAnimator.SetBool("cancelHolding", value: false);
            player.playerBodyAnimator.ResetTrigger("Throw");
            player.SetSpecialGrabAnimationBool(setTrue: true);
            player.isGrabbingObjectAnimation = true;
            player.cursorIcon.enabled = false;
            player.cursorTip.text = "";
            player.twoHanded = player.currentlyGrabbingObject.itemProperties.twoHanded;
            player.carryWeight = Mathf.Clamp(player.carryWeight + (player.currentlyGrabbingObject.itemProperties.weight - 1f), 1f, 10f);
            StartOfRound.Instance.SendChangedWeightEvent();
            if (player.currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f)
            {
                player.grabObjectAnimationTime = player.currentlyGrabbingObject.itemProperties.grabAnimationTime;
            }
            else
            {
                player.grabObjectAnimationTime = 0.4f;
            }
            if (!player.isTestingPlayer)
            {
                player.GrabObjectServerRpc(networkObject);
            }
            if (player.grabObjectCoroutine != null)
            {
                player.StopCoroutine(player.grabObjectCoroutine);
            }
            player.grabObjectCoroutine = player.StartCoroutine(player.GrabObject());

            return true;
        }

        /// <summary>
        /// Enables or disables player interaction, look input, and movement.
        /// </summary>
        /// <param name="player">The player controller to modify.</param>
        /// <param name="value">true to freeze the player; false to unfreeze.</param>
        public static void FreezePlayer(this PlayerControllerB player, bool value)
        {
            Utils.localPlayerFrozen = value;
            player.disableInteract = value;
            player.disableLookInput = value;
            player.disableMoveInput = value;
        }

        /// <summary>
        /// Sets the visibility of the player's scavenger model and related components.
        /// </summary>
        /// <param name="player">The player controller whose scavenger model visibility is being changed.</param>
        /// <param name="value">true to make the scavenger model invisible; false to make it visible.</param>
        public static void MakePlayerInvisible(this PlayerControllerB player, bool value)
        {
            GameObject scavengerModel = player.gameObject.transform.Find("ScavengerModel").gameObject;
            if (scavengerModel == null) { logger.LogError("ScavengerModel not found"); return; }
            scavengerModel.transform.Find("LOD1").gameObject.SetActive(!value);
            scavengerModel.transform.Find("LOD2").gameObject.SetActive(!value);
            scavengerModel.transform.Find("LOD3").gameObject.SetActive(!value);
            scavengerModel.transform.Find("metarig/spine/spine.001/spine.002/spine.003/LevelSticker").gameObject.SetActive(!value);
            scavengerModel.transform.Find("metarig/spine/spine.001/spine.002/spine.003/BetaBadge").gameObject.SetActive(!value);
            player.playerBadgeMesh.gameObject.SetActive(!value);

        }

        /// <summary>
        /// Rebuilds the animation rig for the specified player controller.
        /// </summary>
        /// <param name="player">The player controller whose animation rig is rebuilt.</param>
        public static void RebuildRig(this PlayerControllerB player)
        {
            if (player != null && player.playerBodyAnimator != null)
            {
                player.playerBodyAnimator.WriteDefaultValues();
                player.playerBodyAnimator.GetComponent<RigBuilder>()?.Build();
            }
        }

        /// <summary>
        /// Applies or removes a muffling effect to the player's voice chat audio.
        /// </summary>
        /// <param name="player">The player controller to modify.</param>
        /// <param name="muffle">true to enable the muffling effect; false to disable it.</param>
        public static void MufflePlayer(this PlayerControllerB player, bool muffle)
        {
            if (player.currentVoiceChatAudioSource == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (player.currentVoiceChatAudioSource != null)
            {
                player.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = muffle ? 5f : 1f;
                OccludeAudio component = player.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
                component.overridingLowPass = muffle;
                component.lowPassOverride = muffle ? 500f : 20000f;
                player.voiceMuffledByEnemy = muffle;
            }
        }

        public static bool IsPlayerSpeaking(this PlayerControllerB player, float amplitudeThreshold = 0.3f, bool useRelativeAmplitude = true)
        {
            return GetPlayerVoiceAmplitude(player, useRelativeAmplitude) > amplitudeThreshold;
        }

        public static bool IsPlayerMuted(this PlayerControllerB player)
        {
            StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            return player.voicePlayerState == null || !player.voicePlayerState.IsConnected;
        }

        public static float GetPlayerVoiceAmplitude(this PlayerControllerB player, bool getRelativeAmplitude = false)
        {
            if (player.voicePlayerState == null || !player.voicePlayerState.IsConnected) { return 0f; }
            return getRelativeAmplitude ? player.voicePlayerState.Amplitude / Mathf.Clamp(StartOfRound.Instance.averageVoiceAmplitude, 0.008f, 0.5f) : player.voicePlayerState.Amplitude;
        }

        public static void DiscardItemInSlot(this PlayerControllerB player, int slot, bool placeObject = false, NetworkObject? parentObjectTo = null, Vector3 placePosition = default(Vector3), bool matchRotationOfParent = true, bool setInShip = false, bool setInElevator = false, Vector3 syncedPlayerPosition = default(Vector3), Vector3 syncedHeldObjectPosition = default(Vector3), Vector3 syncedHeldObjectRotation = default(Vector3), Vector3 syncedPlayerCamPosition = default(Vector3), Vector3 syncedPlayerCamRotation = default(Vector3))
        {
            if (player.currentItemSlot == slot)
            {
                player.DiscardHeldObject(placeObject: placeObject, parentObjectTo: parentObjectTo, placePosition: placePosition, matchRotationOfParent: matchRotationOfParent);
                return;
            }
            GrabbableObject? item = player.ItemSlots[slot];
            if (item == null) { return; }
            player.DropHeldItem(item, itemsFall: true, disconnecting: false, syncedPlayerPosition, syncedHeldObjectPosition, syncedHeldObjectRotation, syncedPlayerCamPosition, syncedPlayerCamRotation, setInShip, setInElevator);
            if (player.IsOwner)
                HUDManager.Instance.itemSlotIcons[slot].enabled = false;
        }
    }
}
