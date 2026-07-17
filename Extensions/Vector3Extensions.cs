using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace SnowyLib
{
    public static class Vector3Extensions
    {
        public static Vector3 GetFloorPosition(this Vector3 position, float verticalOffset = 0)
        {
            if (Physics.Raycast(position, -Vector3.up, out var hitInfo, 80f, 268437761, QueryTriggerInteraction.Ignore))
            {
                return hitInfo.point + Vector3.up * 0.04f + verticalOffset * Vector3.up;
            }
            return position;
        }

        /// <summary>
        /// Finds the closest component of the specified type to the given position.
        /// </summary>
        /// <typeparam name="T">The type of Unity component to search for.</typeparam>
        /// <param name="position">The position from which to measure distance.</param>
        /// <returns>The closest component of type T, or null if none are found.</returns>
        public static T? GetClosestGameObjectOfType<T>(this Vector3 position) where T : Component
        {
            T[] objects = UnityEngine.Object.FindObjectsOfType<T>();
            T closest = null!;
            float closestDistance = Mathf.Infinity;

            foreach (T obj in objects)
            {
                float distance = Vector3.Distance(position, obj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = obj;
                }
            }

            return closest;
        }

        /// <summary>
        /// Retrieves all player controllers within a specified distance from a position, excluding any specified
        /// players.
        /// </summary>
        /// <param name="position">The origin position from which to search for nearby players.</param>
        /// <param name="distance">The maximum distance from the position to include players. Defaults to 10 units.</param>
        /// <param name="ignoredPlayers">A list of players to exclude from the search, or null to include all players.</param>
        /// <returns>An array of PlayerControllerB instances representing players within the specified distance.</returns>
        public static PlayerControllerB[] GetNearbyPlayers(this Vector3 position, float distance = 10f, List<PlayerControllerB>? ignoredPlayers = null)
        {
            List<PlayerControllerB> players = [];

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !player.isPlayerControlled) { continue; }
                if (ignoredPlayers != null && ignoredPlayers.Contains(player)) { continue; }
                if (Vector3.Distance(position, player.transform.position) > distance) { continue; }
                players.Add(player);
            }

            return players.ToArray();
        }

        /// <summary>
        /// Determines whether the vector is outside the defined vertical bounds.
        /// </summary>
        /// <param name="position">The vector to evaluate.</param>
        /// <returns>true if the y-component of the vector is greater than -80; otherwise, false.</returns>
        public static bool IsOutside(this Vector3 position)
        {
            return position.y > -80f;
        }
    }
}
