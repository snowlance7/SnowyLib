using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowyLib
{
    public static class NetworkObjectExtensions
    {
        public static void DoActionAfterSpawn(this NetworkObject networkObject, Action<NetworkObject> actionAfterSpawn)
        {
            IEnumerator doActionAfterSpawn(NetworkObject networkObject, Action<NetworkObject> actionAfterSpawn)
            {
                yield return null;
                yield return new WaitUntil(() => networkObject.IsSpawned);
                actionAfterSpawn.Invoke(networkObject);
            }

            NetworkHandler.Instance.StartCoroutine(doActionAfterSpawn(networkObject, actionAfterSpawn));
        }
    }
}
