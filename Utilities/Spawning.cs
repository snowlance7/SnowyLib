using Dawn;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Utilities;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static partial class Utils
    {

        /// <summary>
        /// Spawns an enemy of the specified type at the given position and rotation.
        /// </summary>
        /// <remarks>
        /// Execution: Server
        /// </remarks>
        /// <param name="key">The key identifying the enemy type to spawn.</param>
        /// <param name="position">The world position where the enemy is spawned.</param>
        /// <param name="rotation">The rotation to apply to the spawned enemy.</param>
        /// <param name="parentTo">The optional parent transform for the spawned enemy.</param>
        /// <param name="destroyWithScene">true to destroy the enemy when the scene is unloaded; otherwise, false.</param>
        /// <returns>The spawned EnemyAI instance, or null if not executed on the server.</returns>
        public static EnemyAI? SpawnEnemy(NamespacedKey<DawnEnemyInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = true)
        {
            if (!IsServerOrHost) { return null; }
            return SpawnEnemy(LethalContent.Enemies[key].EnemyType, position, rotation, parentTo, destroyWithScene);
        }

        public static EnemyAI? SpawnEnemy(EnemyType enemyType, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = true)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = UnityEngine.Object.Instantiate(enemyType.enemyPrefab, position, rotation, parentTo);
            EnemyAI enemy = obj.GetComponent<EnemyAI>();
            enemy.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            RoundManager.Instance.SpawnedEnemies.Add(enemy);
            return enemy;
        }

        public static void SpawnEnemy(NamespacedKey<DawnEnemyInfo> key, EnemyVent? vent = null, float spawnDelay = 0f)
        {
            if (!IsServerOrHost) { return; }
            SpawnEnemy(LethalContent.Enemies[key].EnemyType, vent, spawnDelay);
        }

        public static void SpawnEnemy(EnemyType enemyType, EnemyVent? vent = null, float spawnDelay = 0f)
        {
            if (!IsServerOrHost) { return; }
            if (vent == null)
                vent = RoundManager.Instance.allEnemyVents.GetRandom();

            if (vent == null) { return; }

            int enemyIndex = Array.IndexOf(RoundManager.Instance.currentLevel.Enemies.Select(x => x.enemyType).ToArray(), enemyType);
            vent.enemyType = enemyType;
            vent.enemyTypeIndex = enemyIndex;
            vent.occupied = true;
            vent.spawnTime = TimeOfDay.Instance.currentDayTime + spawnDelay;

            if (spawnDelay <= 0)
            {
                RoundManager.Instance.SpawnEnemyFromVent(vent);
            }
            else
            {
                vent.SyncVentSpawnTimeClientRpc((int)vent.spawnTime, enemyIndex);
            }
        }

        public static GrabbableObject? SpawnItem(NamespacedKey<DawnItemInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = false, Action<GrabbableObject>? actionAfterSpawn = null)
        {
            return SpawnItem(LethalContent.Items[key].Item, position, rotation, parentTo, destroyWithScene, actionAfterSpawn);
        }

        public static GrabbableObject? SpawnItem(NamespacedKey<DawnItemInfo> key, Transform parentTo, bool worldPositionStays = false, bool destroyWithScene = false, Action<GrabbableObject>? actionAfterSpawn = null)
        {
            return SpawnItem(LethalContent.Items[key].Item, parentTo, worldPositionStays, destroyWithScene, actionAfterSpawn);
        }

        public static GrabbableObject? SpawnItem(Item item, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = false, Action<GrabbableObject>? actionAfterSpawn = null)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, position, rotation, parentTo);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            if (actionAfterSpawn != null) { grabObj.DoActionAfterSpawn(actionAfterSpawn); }
            return grabObj;
        }

        public static GrabbableObject? SpawnItem(Item item, Transform parentTo, bool worldPositionStays = false, bool destroyWithScene = false, Action<GrabbableObject>? actionAfterSpawn = null)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, parentTo, worldPositionStays);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            if (actionAfterSpawn != null) { grabObj.DoActionAfterSpawn(actionAfterSpawn); }
            return grabObj;
        }

        public static GameObject? SpawnMapObject(NamespacedKey<DawnMapObjectInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = true)
        {
            if (!IsServerOrHost) { return null; }
            var prefab = LethalContent.MapObjects[key].GetMapObjectPrefab();
            if (prefab == null) { logger.LogError($"Couldnt find prefab for {key}"); return null; }
            GameObject obj = UnityEngine.Object.Instantiate(prefab, position, rotation, parentTo);
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: destroyWithScene);
            return obj;
        }

        public static GameObject? SpawnMapObject(GameObject prefab, InsideMapObjectSettings insideMapObjectSettings, Transform? parentTo = null, bool destroyWithScene = true)
        {
            Vector3 position = Utils.insideAINodes.GetRandom().transform.position.GetFloorPosition();
            position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, default, default, randomLocal, -8193);

            GameObject obj = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, parentTo);
            if (insideMapObjectSettings.spawnFacingAwayFromWall)
            {
                obj.transform.eulerAngles = new Vector3(0f, RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(position + Vector3.up * 0.2f), 0f);
            }
            else if (insideMapObjectSettings.spawnFacingWall)
            {
                obj.transform.eulerAngles = new Vector3(0f, RoundManager.Instance.YRotationThatFacesTheNearestFromPosition(position + Vector3.up * 0.2f), 0f);
            }
            else
            {
                obj.transform.eulerAngles = new Vector3(obj.transform.eulerAngles.x, UnityEngine.Random.Range(0, 360), obj.transform.eulerAngles.z);
            }
            if (insideMapObjectSettings.spawnWithBackToWall && Physics.Raycast(obj.transform.position, -obj.transform.forward, out var hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                obj.transform.position = hitInfo.point;
                if (insideMapObjectSettings.spawnWithBackFlushAgainstWall)
                {
                    obj.transform.forward = hitInfo.normal;
                    obj.transform.eulerAngles = new Vector3(0f, obj.transform.eulerAngles.y, 0f);
                }
            }
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            return obj;
        }
    }
}