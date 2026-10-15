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

        public static void Despawn(this NetworkObject networkObject, bool destroy = true, float despawnTime = 0f)
        {
            if (despawnTime <= 0f)
            {
                networkObject.Despawn(destroy: destroy);
                return;
            }

            IEnumerator despawn(NetworkObject networkObject, bool destroy, float despawnTime)
            {
                yield return new WaitForSeconds(despawnTime);
                networkObject.Despawn(destroy: destroy);
            }

            NetworkHandler.Instance.StartCoroutine(despawn(networkObject, destroy, despawnTime));
        }
    }
}
