using Dawn;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
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
            if (actionAfterSpawn != null) { DoActionAfterItemSpawn(grabObj, actionAfterSpawn); }
            return grabObj;
        }

        public static GrabbableObject? SpawnItem(Item item, Transform parentTo, bool worldPositionStays = false, bool destroyWithScene = false, Action<GrabbableObject>? actionAfterSpawn = null)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, parentTo, worldPositionStays);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            if (actionAfterSpawn != null) { DoActionAfterItemSpawn(grabObj, actionAfterSpawn); }
            return grabObj;
        }

        public static void DoActionAfterItemSpawn(GrabbableObject grabbableObject, Action<GrabbableObject> actionAfterSpawn)
        {
            IEnumerator doActionAfterItemSpawn(GrabbableObject grabbableObject, Action<GrabbableObject> actionAfterSpawn)
            {
                yield return null;
                yield return new WaitUntil(() => grabbableObject.NetworkObject != null && grabbableObject.NetworkObject.IsSpawned);
                actionAfterSpawn.Invoke(grabbableObject);
            }

            NetworkHandler.Instance.StartCoroutine(doActionAfterItemSpawn(grabbableObject, actionAfterSpawn));
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
    }
}
