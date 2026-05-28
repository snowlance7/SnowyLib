using UnityEngine;

namespace SnowyLib.Extensions
{
    internal static class ComponentExtensions
    {
        public static bool TryGetComponentInChildren<T>(this Component comp, out T? component) where T : Component
        {
            component = comp.GetComponentInChildren<T>();
            return component != null;
        }
    }
}
