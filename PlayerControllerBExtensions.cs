using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SnowyLib
{
    public static class PlayerControllerBExtensions
    {
        public static StatusEffectController StatusEffectController(this PlayerControllerB player)
        {
            return player.gameObject.TryGetComponent(out StatusEffectController controller) ? controller : player.gameObject.AddComponent<StatusEffectController>();
        }

        public static bool GrabObject(this PlayerControllerB player, GrabbableObject grabbableObject)
        {
            player.currentlyGrabbingObject = grabbableObject;
            player.grabInvalidated = false;

            if (player.FirstEmptyItemSlot(grabbableObject) == -1) { return false; }

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
            return true;
        }
    }
}
