using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SnowyLib
{
    public static class IEnumerableExtensions
    {
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
