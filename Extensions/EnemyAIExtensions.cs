using GameNetcodeStuff;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering.HighDefinition;

namespace SnowyLib
{
    public static class EnemyAIExtensions
    {
        public static StatusEffectController StatusEffectController(this EnemyAI enemy)
        {
            return enemy.gameObject.TryGetComponent(out StatusEffectController controller) ? controller : enemy.gameObject.AddComponent<StatusEffectController>();
        }

        /// <summary>
        /// Finds and targets the closest enemy that meets specified criteria, optionally retaining the previous target
        /// if within a buffer distance.
        /// </summary>
        /// <param name="thisEnemy">The enemy AI instance performing the targeting operation.</param>
        /// <param name="targetEnemy">When this method returns, contains the closest targetable enemy if found; otherwise, null.</param>
        /// <param name="previousTargetEnemy">The previously targeted enemy to consider for retention based on buffer distance.</param>
        /// <param name="enemyIsTargetable">A function that determines whether an enemy can be targeted.</param>
        /// <param name="bufferDistance">The maximum distance difference to retain the previous target instead of switching to a new one.</param>
        /// <param name="requireLineOfSight">true to require line of sight to the target; otherwise, false.</param>
        /// <param name="viewWidth">The field of view angle, in degrees, used for line of sight checks.</param>
        /// <param name="doGroundCast">true to perform a ground cast to check for obstacles beneath the enemy; otherwise, false.</param>
        /// <param name="requirePath">true to require a valid path to the target; otherwise, false.</param>
        /// <param name="checkForMineshaftStartTile">true to check for the mineshaft start tile during targeting; otherwise, false.</param>
        /// <returns>true if a targetable enemy is found; otherwise, false.</returns>
        public static bool TargetClosestEnemy(this EnemyAI thisEnemy, out EnemyAI? targetEnemy, EnemyAI? previousTargetEnemy, Func<EnemyAI, bool> enemyIsTargetable, float bufferDistance = 1.5f, bool requireLineOfSight = false, float viewWidth = 70f, bool doGroundCast = false, bool requirePath = false, bool checkForMineshaftStartTile = true)
        {
            targetEnemy = previousTargetEnemy;
            thisEnemy.mostOptimalDistance = 2000f;
            EnemyAI? enemy = targetEnemy;
            targetEnemy = null;
            foreach (var e in RoundManager.Instance.SpawnedEnemies)
            {
                if (!enemyIsTargetable.Invoke(e))
                {
                    continue;
                }
                if (doGroundCast)
                {
                    if (!Physics.Raycast(e.transform.position, Vector3.down, out thisEnemy.raycastHit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) || (requirePath && thisEnemy.PathIsIntersectedByLineOfSight(thisEnemy.raycastHit.point, calculatePathDistance: false, avoidLineOfSight: false)))
                    {
                        continue;
                    }
                }
                else if (requirePath && thisEnemy.PathIsIntersectedByLineOfSight(e.transform.position, calculatePathDistance: false, avoidLineOfSight: false))
                {
                    continue;
                }
                if (!requireLineOfSight || thisEnemy.CheckLineOfSightForPosition(e.eye.transform.position, viewWidth, 40))
                {
                    thisEnemy.tempDist = Vector3.Distance(thisEnemy.transform.position, e.transform.position);
                    if (thisEnemy.tempDist < thisEnemy.mostOptimalDistance)
                    {
                        thisEnemy.mostOptimalDistance = thisEnemy.tempDist;
                        targetEnemy = e;
                    }
                }
            }
            if (targetEnemy != null && bufferDistance > 0f && enemy != null && Mathf.Abs(thisEnemy.mostOptimalDistance - Vector3.Distance(thisEnemy.transform.position, enemy.transform.position)) < bufferDistance)
            {
                targetEnemy = enemy;
            }
            return targetEnemy != null;
        }

        /// <summary>
        /// Finds the closest enemy in line of sight within the specified field of view and range.
        /// </summary>
        /// <param name="thisEnemy">The enemy performing the line of sight check.</param>
        /// <param name="targetEnemy">When this method returns, contains the currently targeted enemy, if any.</param>
        /// <param name="previousTargetEnemy">The previously targeted enemy.</param>
        /// <param name="width">The width of the field of view in degrees.</param>
        /// <param name="range">The maximum distance to check for enemies.</param>
        /// <param name="proximityAwareness">The distance within which the enemy is aware of others, regardless of field of view.</param>
        /// <param name="bufferDistance">The minimum distance change required to switch targets.</param>
        /// <returns>The closest enemy in line of sight, or null if none is found.</returns>
        public static EnemyAI? CheckLineOfSightForClosestEnemy(this EnemyAI thisEnemy, out EnemyAI? targetEnemy, EnemyAI? previousTargetEnemy, float width = 45f, int range = 60, int proximityAwareness = -1, float bufferDistance = 0f)
        {
            targetEnemy = previousTargetEnemy;
            if (thisEnemy.isOutside && !thisEnemy.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }
            float num = 1000f;
            float num2 = 1000f;
            EnemyAI? enemy3 = null;
            foreach (var e in RoundManager.Instance.SpawnedEnemies)
            {
                num = 1000f;
                Vector3 position = e.eye.transform.position;
                Vector3 to = position - thisEnemy.eye.position;
                bool flag = false;
                if (Vector3.Angle(thisEnemy.eye.forward, to) < width)
                {
                    flag = true;
                }
                else
                {
                    num = Vector3.Distance(thisEnemy.eye.position, position);
                    if (proximityAwareness != -1 && num < (float)proximityAwareness)
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    continue;
                }
                if (!Physics.Linecast(thisEnemy.eye.position, position, out thisEnemy.raycastHit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && !Physics.Linecast(position, thisEnemy.eye.position, out thisEnemy.raycastHit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    if (num == 1000f)
                    {
                        num = Vector3.Distance(thisEnemy.eye.position, position);
                    }
                    if (num < num2)
                    {
                        num2 = num;
                        enemy3 = e;
                    }
                }
            }
            if (targetEnemy != null && enemy3 != null && targetEnemy != enemy3 && bufferDistance > 0f && Mathf.Abs(num2 - Vector3.Distance(thisEnemy.transform.position, targetEnemy.transform.position)) < bufferDistance)
            {
                return null;
            }
            if (enemy3 == null)
            {
                return null;
            }
            thisEnemy.mostOptimalDistance = num2;
            return enemy3;
        }

        public static int GetMaxHealth(this EnemyAI enemy)
        {
            return enemy.enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP;
        }
    }
}
