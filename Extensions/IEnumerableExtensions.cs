using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SnowyLib
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Selects and returns a random element from the collection.
        /// </summary>
        /// <typeparam name="T">The reference type of elements in the collection.</typeparam>
        /// <param name="source">The collection to select a random element from.</param>
        /// <param name="random">The random number generator to use for selection.</param>
        /// <returns>A randomly selected element from the collection, or null if the collection is empty.</returns>
        public static T? GetRandom<T>(this IEnumerable<T> source, System.Random random) where T : class
        {
            if (source is IList<T> list)
            {
                if (list.Count == 0) return null;
                return list[random.Next(list.Count)];
            }

            var array = source as T[] ?? source.ToArray();
            if (array.Length == 0) return null;

            return array[random.Next(array.Length)];
        }

        /// <summary>
        /// Returns a random element from the collection or null if the collection is empty.
        /// </summary>
        /// <typeparam name="T">The reference type of elements in the collection.</typeparam>
        /// <param name="source">The collection to select a random element from.</param>
        /// <returns>A random element from the collection, or null if the collection is empty.</returns>
        public static T? GetRandom<T>(this IEnumerable<T> source) where T : class
        {
            if (source is IList<T> list)
            {
                if (list.Count == 0) return null;
                return list[UnityEngine.Random.Range(0, list.Count)];
            }

            var array = source as T[] ?? source.ToArray();
            if (array.Length == 0) return null;

            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Finds the element in the collection whose position is closest to the specified target position.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The collection of elements to search.</param>
        /// <param name="position">The target position to compare against.</param>
        /// <param name="positionSelector">A function that returns the position of an element.</param>
        /// <param name="closestDistance">When this method returns, contains the distance to the closest element found.</param>
        /// <param name="fastDistanceCheck">true to use a faster, approximate distance calculation; otherwise, false.</param>
        /// <param name="excluded">A collection of elements to exclude from the search, or null to include all elements.</param>
        /// <returns>The element closest to the specified position, or null if no element is found.</returns>
        public static T? GetClosestToPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, out float closestDistance, bool fastDistanceCheck = false, IEnumerable<T>? excluded = null) where T : class
        {
            T? closest = null;
            closestDistance = Mathf.Infinity;
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                float distance = Utils.SmartDistance(position, positionSelector(item), fastDistanceCheck);
                if (distance >= closestDistance) continue;

                closest = item;
                closestDistance = distance;
            }

            return closest;
        }

        /// <summary>
        /// Finds the element in the collection whose position is closest to the specified position.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The collection of elements to search.</param>
        /// <param name="position">The target position to compare against.</param>
        /// <param name="positionSelector">A function that returns the position of an element.</param>
        /// <param name="fastDistanceCheck">true to use a faster, less precise distance calculation; otherwise, false.</param>
        /// <param name="excluded">A collection of elements to exclude from the search, or null to include all elements.</param>
        /// <returns>The element closest to the specified position, or null if no suitable element is found.</returns>
        public static T? GetClosestToPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, bool fastDistanceCheck = false, IEnumerable<T>? excluded = null) where T : class
        {
            T? closest = null;
            float closestDistance = Mathf.Infinity;
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                float distance = Utils.SmartDistance(position, positionSelector(item), fastDistanceCheck);
                if (distance >= closestDistance) continue;

                closest = item;
                closestDistance = distance;
            }

            return closest;
        }

        /// <summary>
        /// Finds the element in the collection that is farthest from the specified position.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The collection to search.</param>
        /// <param name="position">The reference position to measure distance from.</param>
        /// <param name="positionSelector">A function that returns the position of an element.</param>
        /// <param name="farthestDistance">When this method returns, contains the distance to the farthest element found.</param>
        /// <param name="fastDistanceCheck">true to use a faster, approximate distance calculation; otherwise, false.</param>
        /// <param name="excluded">A collection of elements to exclude from the search, or null to include all elements.</param>
        /// <returns>The element farthest from the specified position, or null if no valid element is found.</returns>
        public static T? GetFarthestFromPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, out float farthestDistance, bool fastDistanceCheck = false, IEnumerable<T>? excluded = null) where T : class
        {
            T? farthest = null;
            farthestDistance = 0f;
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                float distance = Utils.SmartDistance(position, positionSelector(item), fastDistanceCheck);
                if (distance <= farthestDistance) continue;

                farthest = item;
                farthestDistance = distance;
            }

            return farthest;
        }

        /// <summary>
        /// Finds the element in the collection that is farthest from the specified position.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The sequence of elements to search.</param>
        /// <param name="position">The reference position to measure distance from.</param>
        /// <param name="positionSelector">A function that returns the position of each element.</param>
        /// <param name="fastDistanceCheck">true to use a faster, less precise distance calculation; otherwise, false.</param>
        /// <param name="excluded">A sequence of elements to exclude from the search, or null to include all elements.</param>
        /// <returns>The element farthest from the specified position, or null if the collection is empty or all elements are
        /// excluded.</returns>
        public static T? GetFarthestFromPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, bool fastDistanceCheck = false, IEnumerable<T>? excluded = null) where T : class
        {
            T? farthest = null;
            float farthestDistance = 0f;
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                float distance = Utils.SmartDistance(position, positionSelector(item), fastDistanceCheck);
                if (distance <= farthestDistance) continue;

                farthest = item;
                farthestDistance = distance;
            }

            return farthest;
        }

        /// <summary>
        /// Returns the elements in the collection whose positions are within the specified range of a reference
        /// position, excluding any specified elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The collection of elements to filter.</param>
        /// <param name="position">The reference position to measure distances from.</param>
        /// <param name="positionSelector">A function that returns the position of each element.</param>
        /// <param name="range">The maximum distance from the reference position to include an element.</param>
        /// <param name="excluded">A collection of elements to exclude from the results, or null to exclude none.</param>
        /// <returns>A collection of elements within the specified range of the reference position.</returns>
        public static IEnumerable<T> GetInRange<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, float range, IEnumerable<T>? excluded = null) where T : class
        {
            List<T> inRange = new List<T>();
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                if (Vector3.Distance(position, positionSelector(item)) < range)
                    inRange.Add(item);
            }

            return inRange;
        }
    }
}
