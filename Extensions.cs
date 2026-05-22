using Dawn.Utils;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using static SnowyLib.Plugin;
using static SnowyLib.Utils;

namespace SnowyLib
{
    public static class Extensions
    {
        public static void FreezePlayer(this PlayerControllerB player, bool value)
        {
            localPlayerFrozen = value;
            player.disableInteract = value;
            player.disableLookInput = value;
            player.disableMoveInput = value;
        }

        public static void MakePlayerInvisible(this PlayerControllerB player, bool value)
        {
            GameObject scavengerModel = player.gameObject.transform.Find("ScavengerModel").gameObject;
            if (scavengerModel == null) { logger.LogError("ScavengerModel not found"); return; }
            scavengerModel.transform.Find("LOD1").gameObject.SetActive(!value);
            scavengerModel.transform.Find("LOD2").gameObject.SetActive(!value);
            scavengerModel.transform.Find("LOD3").gameObject.SetActive(!value);
            scavengerModel.transform.Find("metarig/spine/spine.001/spine.002/spine.003/LevelSticker").gameObject.SetActive(!value);
            scavengerModel.transform.Find("metarig/spine/spine.001/spine.002/spine.003/BetaBadge").gameObject.SetActive(!value);
            player.playerBadgeMesh.gameObject.SetActive(!value);

        }

        public static T? GetClosestGameObjectOfType<T>(Vector3 position) where T : Component
        {
            T[] objects = GameObject.FindObjectsOfType<T>();
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

        public static T? GetClosestToPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, IEnumerable<T>? excluded = null, bool fastDistance = false) where T : class
        {
            T? closest = null;
            float closestDistance = Mathf.Infinity;
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                float distance = Vector3.Distance(position, positionSelector(item));
                if (distance >= closestDistance) continue;

                closest = item;
                closestDistance = distance;
            }

            return closest;
        }

        /*public static T? SmartGetClosestToPosition<T>(this IEnumerable<T> list, PositionInfo positionInfo, Func<T, Vector3> positionSelector, IEnumerable<T>? excluded = null, bool fastDistance = false) where T : class
        {
            T? closest = null;
            float closestDistance = Mathf.Infinity;
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                float distance = Vector3.Distance(position, positionSelector(item));
                if (distance >= closestDistance) continue;

                closest = item;
                closestDistance = distance;
            }

            return closest;
        }*/

        public static T? GetFarthestFromPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, IEnumerable<T>? excluded = null) where T : class
        {
            T? farthest = null;
            float farthestDistance = 0f;
            excluded ??= [];

            foreach (var item in list)
            {
                if (item == null || excluded.Contains(item)) continue;

                float distance = Vector3.Distance(position, positionSelector(item));
                if (distance <= farthestDistance) continue;

                farthest = item;
                farthestDistance = distance;
            }

            return farthest;
        }

        public static List<T> GetInRange<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector, float range, IEnumerable<T>? excluded = null) where T : class
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

        public static void RebuildRig(this PlayerControllerB player)
        {
            if (player != null && player.playerBodyAnimator != null)
            {
                player.playerBodyAnimator.WriteDefaultValues();
                player.playerBodyAnimator.GetComponent<RigBuilder>()?.Build();
            }
        }

        public static void MufflePlayer(this PlayerControllerB player, bool muffle)
        {
            if (player.currentVoiceChatAudioSource == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (player.currentVoiceChatAudioSource != null)
            {
                player.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = muffle ? 5f : 1f;
                OccludeAudio component = player.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
                component.overridingLowPass = muffle;
                component.lowPassOverride = muffle ? 500f : 20000f;
                player.voiceMuffledByEnemy = muffle;
            }
        }

        public static Vector3 GetFloorPosition(this Vector3 position, float verticalOffset = 0)
        {
            if (Physics.Raycast(position, -Vector3.up, out var hitInfo, 80f, 268437761, QueryTriggerInteraction.Ignore))
            {
                return hitInfo.point + Vector3.up * 0.04f + verticalOffset * Vector3.up;
            }
            return position;
        }
    }
}
