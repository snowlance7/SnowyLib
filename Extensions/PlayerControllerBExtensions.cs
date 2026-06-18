using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static class PlayerControllerBExtensions
    {
        public static StatusEffectController StatusEffectController(this PlayerControllerB player)
        {
            return player.gameObject.TryGetComponent(out StatusEffectController controller) ? controller : player.gameObject.AddComponent<StatusEffectController>();
        }

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

        public static void FreezePlayer(this PlayerControllerB player, bool value)
        {
            Utils.localPlayerFrozen = value;
            player.disableInteract = value;
            player.disableLookInput = value;
            player.disableMoveInput = value;
        }

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

        public static void RebuildRig(this PlayerControllerB player)
        {
            if (player != null && player.playerBodyAnimator != null)
            {
                player.playerBodyAnimator.WriteDefaultValues();
                player.playerBodyAnimator.GetComponent<RigBuilder>()?.Build();
            }
        }

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
    }
}
