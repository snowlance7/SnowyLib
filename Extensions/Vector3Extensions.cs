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
    }
}
