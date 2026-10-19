using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowyLib
{
    public static class NetworkBehaviourExtensions
    {
        public static void DoActionAfterSpawn(this NetworkBehaviour networkBehaviour, Action<NetworkBehaviour> actionAfterSpawn)
        {
            IEnumerator doActionAfterSpawn(NetworkBehaviour networkBehaviour, Action<NetworkBehaviour> actionAfterSpawn)
            {
                yield return null;
                yield return new WaitUntil(() => networkBehaviour.NetworkObject != null && networkBehaviour.NetworkObject.IsSpawned);
                actionAfterSpawn.Invoke(networkBehaviour);
            }

            NetworkHandler.Instance.StartCoroutine(doActionAfterSpawn(networkBehaviour, actionAfterSpawn));
        }
    }
}
