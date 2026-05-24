using UnityEngine;

namespace SnowyLib.Extensions
{
    public static class GameObjectExtensions
    {
        public static StatusEffectController StatusEffectController(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out StatusEffectController controller) ? controller : gameObject.AddComponent<StatusEffectController>();
        }
    }
}
