using System;
using System.Collections;
using UnityEngine;

namespace SnowyLib;

public static class ObjectExtensions
{
    public static void DoActionWhenNotNull(this object obj, Action<object> action)
    {
        IEnumerator doActionWhenNotNull(object obj, Action<object> action)
        {
            yield return new WaitUntil(() => obj != null);
            action.Invoke(obj);
        }

        NetworkHandler.Instance.StartCoroutine(doActionWhenNotNull(obj, action));
    }
}