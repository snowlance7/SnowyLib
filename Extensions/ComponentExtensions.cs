using UnityEngine;

namespace SnowyLib
{
    public static class ComponentExtensions
    {
        /// <summary>
        /// Attempts to retrieve a component of type T from the children of the specified component.
        /// </summary>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <param name="comp">The component whose children to search.</param>
        /// <param name="component">When this method returns, contains the found component of type T, or null if no such component exists.</param>
        /// <returns>true if a component of type T is found; otherwise, false.</returns>
        public static bool TryGetComponentInChildren<T>(this Component comp, out T? component) where T : Component
        {
            component = comp.GetComponentInChildren<T>();
            return component != null;
        }
    }
}
