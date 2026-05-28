using UnityEngine;

namespace SnowyLib
{
    public static class GameObjectExtensions
    {
        public static StatusEffectController StatusEffectController(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out StatusEffectController controller) ? controller : gameObject.AddComponent<StatusEffectController>();
        }

        public static bool TryGetComponentInChildren<T>(this GameObject go, out T? component) where T : Component
        {
            component = go.GetComponentInChildren<T>();
            return component != null;
        }
    }
}
