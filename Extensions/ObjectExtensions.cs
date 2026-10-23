using System;
using System.Collections;
using UnityEngine;

namespace SnowyLib;

public static class ObjectExtensions
{
    public static void InvokeWhenNotNull(this Func<object?> getObject, Action<object> action, float maxWaitTime = 10f)
    {
        IEnumerator invokeWhenNotNull()
        {
            float elapsedTime = 0f;
            object? obj;

            while ((obj = getObject()) == null)
            {
                elapsedTime += Time.deltaTime;

                if (elapsedTime > maxWaitTime)
                    yield break;

                yield return null;
            }

            try
            {
                action.Invoke(obj);
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e);
                yield break;
            }
        }

        NetworkHandler.Instance.StartCoroutine(invokeWhenNotNull());
    }
}