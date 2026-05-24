using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static SnowyLib.Plugin;

namespace SnowyLib.Extensions
{
    public static class PlayerControllerBExtensions
    {
        public static StatusEffectController StatusEffectController(this PlayerControllerB player)
        {
            return player.gameObject.TryGetComponent(out StatusEffectController controller) ? controller : player.gameObject.AddComponent<StatusEffectController>();
        }

        public static void GrabGrabbableObject(this PlayerControllerB player, GrabbableObject grabbableObject)
        {
            player.currentlyGrabbingObject = grabbableObject;
            player.grabInvalidated = false;

            if (player.FirstEmptyItemSlot(grabbableObject) == -1) { return; }

            player.playerBodyAnimator.SetBool("GrabInvalidated", value: false);
            player.playerBodyAnimator.SetBool("GrabValidated", value: false);
            player.playerBodyAnimator.SetBool("cancelHolding", value: false);
            player.playerBodyAnimator.ResetTrigger("Throw");
            player.SetSpecialGrabAnimationBool(setTrue: true);
            player.isGrabbingObjectAnimation = true;
            player.cursorIcon.enabled = false;
            player.cursorTip.text = "";
            player.twoHanded = grabbableObject.itemProperties.twoHanded;
            player.carryWeight = Mathf.Clamp(player.carryWeight + (grabbableObject.itemProperties.weight - 1f), 1f, 10f);
            StartOfRound.Instance.SendChangedWeightEvent();
            if (grabbableObject.itemProperties.grabAnimationTime > 0f)
            {
                player.grabObjectAnimationTime = grabbableObject.itemProperties.grabAnimationTime;
            }
            else
            {
                player.grabObjectAnimationTime = 0.4f;
            }
            if (!player.isTestingPlayer)
            {
                player.GrabObjectServerRpc(grabbableObject.NetworkObject);
            }
            if (player.grabObjectCoroutine != null)
            {
                player.StopCoroutine(player.grabObjectCoroutine);
            }
            player.grabObjectCoroutine = player.StartCoroutine(player.GrabObject());
            return;
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
