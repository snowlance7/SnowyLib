using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SnowyLib
{
    public static class GrabbableObjectExtensions
    {
        public static void GetItemMeshes(this GrabbableObject grabbableObject, out MeshRenderer[] meshRenderers, out SkinnedMeshRenderer[] skinnedMeshRenderers, bool includeDoNotSet = false, bool includeInteractTriggers = false)
        {
            meshRenderers = grabbableObject.GetComponentsInChildren<MeshRenderer>()
                .Where(x => includeDoNotSet || !x.CompareTag("DoNotSet"))
                .Where(x => includeInteractTriggers || !x.CompareTag("InteractTrigger"))
                .ToArray();

            skinnedMeshRenderers = grabbableObject.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Where(x => includeDoNotSet || !x.CompareTag("DoNotSet"))
                .Where(x => includeInteractTriggers || !x.CompareTag("InteractTrigger"))
                .ToArray();
        }

        public static void DoActionAfterSpawn(this GrabbableObject grabbableObject, Action<GrabbableObject> actionAfterSpawn)
        {
            IEnumerator doActionAfterSpawn(GrabbableObject grabbableObject, Action<GrabbableObject> actionAfterSpawn)
            {
                yield return null;
                yield return new WaitUntil(() => grabbableObject.NetworkObject != null && grabbableObject.NetworkObject.IsSpawned);
                actionAfterSpawn.Invoke(grabbableObject);
            }

            NetworkHandler.Instance.StartCoroutine(doActionAfterSpawn(grabbableObject, actionAfterSpawn));
        }
    }
}
