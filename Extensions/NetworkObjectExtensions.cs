using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowyLib
{
    public static class NetworkObjectExtensions
    {
        public static void InvokeAfterSpawn(this NetworkObject networkObject, Action<NetworkObject> actionAfterSpawn, float maxWaitTime = 10f)
        {
            IEnumerator invokeAfterSpawn()
            {
                float elapsedTime = 0f;

                while (networkObject != null && !networkObject.IsSpawned)
                {
                    if (elapsedTime >= maxWaitTime)
                        yield break;

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (networkObject != null && networkObject.IsSpawned)
                    actionAfterSpawn.Invoke(networkObject);
            }

            NetworkHandler.Instance.StartCoroutine(invokeAfterSpawn());
        }

        public static void Spawn(this NetworkObject networkObject, bool destroyWithScene = false, Action<NetworkObject>? actionAfterSpawn = null, float maxWaitTime = 10f)
        {
            networkObject.Spawn(destroyWithScene);
            if (actionAfterSpawn == null) { return; }
            networkObject.InvokeAfterSpawn(actionAfterSpawn, maxWaitTime);
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

                if (networkObject != null && networkObject.IsSpawned)
                    networkObject.Despawn(destroy: destroy);
            }

            NetworkHandler.Instance.StartCoroutine(despawn(networkObject, destroy, despawnTime));
        }
    }
}
