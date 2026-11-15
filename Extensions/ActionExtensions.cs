using System;
using System.Collections;
using UnityEngine;

namespace SnowyLib.Extensions;

public static class ActionExtensions
{
    public static void InvokeWhen(this Action action, Func<bool> condition, float maxWaitTime = Mathf.Infinity)
    {
        IEnumerator invokeWhen(Action action, Func<bool> condition, float maxWaitTime)
        {
            float elapsedTime = 0f;

            while (elapsedTime < maxWaitTime && !condition.Invoke())
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            action.Invoke();
        }

        NetworkHandler.Instance.StartCoroutine(invokeWhen(action, condition, maxWaitTime));
    }
}