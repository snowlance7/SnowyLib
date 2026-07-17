using UnityEngine;

namespace SnowyLib
{
    public static class GameObjectExtensions
    {
        public static StatusEffectController StatusEffectController(this GameObject gameObject)
        {
            return gameObject.TryGetComponent(out StatusEffectController controller) ? controller : gameObject.AddComponent<StatusEffectController>();
        }

        /// <summary>
        /// Attempts to retrieve a component of type T from the specified GameObject or its children.
        /// </summary>
        /// <typeparam name="T">The type of Component to retrieve.</typeparam>
        /// <param name="go">The GameObject to search for the component.</param>
        /// <param name="component">When this method returns, contains the found component if one exists; otherwise, null.</param>
        /// <returns>true if a component of type T is found; otherwise, false.</returns>
        public static bool TryGetComponentInChildren<T>(this GameObject go, out T? component) where T : Component
        {
            component = go.GetComponentInChildren<T>();
            return component != null;
        }
    }
}
