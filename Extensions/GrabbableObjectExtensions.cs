using System;
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
    }
}
