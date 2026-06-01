using BepInEx.Logging;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.Events;
using UnityEngine.InputSystem.Utilities;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static class Utils
    {
        public static bool testing => cfgTesting.Value;

        public static bool inTestRoom => StartOfRound.Instance?.testRoom != null;
        public static bool DEBUG_disableSpawning = false;
        //public static bool DEBUG_disableTargetting = false;
        //public static bool DEBUG_disableHostTargetting = false;
        //public static bool DEBUG_disableMoving = false;

        public static bool localPlayerFrozen = false;

        public static GameObject[] allAINodes => insideAINodes.Concat(outsideAINodes).ToArray();

        public static GameObject[] insideAINodes
        {
            get
            {
                if (RoundManager.Instance.insideAINodes != null && RoundManager.Instance.insideAINodes.Length > 0)
                {
                    return RoundManager.Instance.insideAINodes;
                }

                return GameObject.FindGameObjectsWithTag("AINode");
            }
        }
        public static GameObject[] outsideAINodes
        {
            get
            {
                if (RoundManager.Instance.outsideAINodes != null && RoundManager.Instance.outsideAINodes.Length > 0)
                {
                    return RoundManager.Instance.outsideAINodes;
                }

                return GameObject.FindGameObjectsWithTag("OutsideAINode");
            }
        }

        public static List<EntranceTeleport> entrances = [];
        public static MineshaftElevatorController? elevator;
        public static Terminal? terminal;

        public static BoundedRange randomPercentage = new BoundedRange(0f, 1f);
        public static System.Random randomLocal { get; private set; } = new();
        public static System.Random randomGlobal { get; private set; } = new();

        public static UnityEvent OnFinishGeneratingLevel = new();
        public static UnityEvent OnShipLanded = new();

        public enum ContentType
        {
            Item,
            Enemy,
            MapObject
        }

        internal static void SetRandoms()
        {
            Utils.randomLocal = new System.Random(StartOfRound.Instance.randomMapSeed);
            Utils.randomGlobal = new System.Random(StartOfRound.Instance.randomMapSeed);
        }

        public static void ChatCommand(string[] args)
        {
            if (!testing) { return; }

            switch (args[0])
            {
                case "/spawning":
                    DEBUG_disableSpawning = !DEBUG_disableSpawning;
                    HUDManager.Instance.DisplayTip("Disable Spawning", DEBUG_disableSpawning.ToString());
                    break;
                case "/hazards":
                    Dictionary<string, GameObject> hazards = Utils.GetAllHazards();

                    foreach (var hazard in hazards)
                    {
                        logger.LogDebug(hazard);
                    }
                    break;
                case "/surfaces":
                    foreach (var surface in StartOfRound.Instance.footstepSurfaces)
                    {
                        logger.LogDebug(surface.surfaceTag);
                    }
                    break;
                case "/enemies":
                    foreach (var enemy in Utils.GetEnemies())
                    {
                        logger.LogDebug(enemy.enemyType.name);
                    }
                    break;
                case "/refresh":
                    RoundManager.Instance.RefreshEnemiesList();
                    HoarderBugAI.RefreshGrabbableObjectsInMapList();
                    break;
                case "/levels":
                    foreach (var level in StartOfRound.Instance.levels)
                    {
                        logger.LogDebug(level.name);
                    }
                    break;
                case "/dungeon":
                    logger.LogDebug(RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name);
                    break;
                case "/dungeons":
                    foreach (var dungeon in RoundManager.Instance.dungeonFlowTypes)
                    {
                        logger.LogDebug(dungeon.dungeonFlow.name);
                    }
                    break;
                case "/animations":
                    LogAnimatorParameters(localPlayer.playerBodyAnimator);
                    break;
                case "/rarities":
                    switch (args[1])
                    {
                        case "item":
                            LogRarities(ContentType.Item);
                            break;
                        case "enemy":
                            LogRarities(ContentType.Enemy);
                            break;
                        case "mapobject":
                            LogRarities(ContentType.MapObject);
                            break;
                        default:
                            break;
                    }
                    break;
                case "/vignette":
                    VignetteOverlay.Instance.SetIntensity(float.Parse(args[1]));
                    HUDManager.Instance.DisplayTip("SnowyLib", $"Vignette intensity set to {args[1]}");
                    break;
                case "/spawnanim":
                    localPlayer.SpawnPlayerAnimation();
                    break;
                default:
                    break;
            }
        }

        public static bool SmartCanPathToPoint(Vector3 startPos, Vector3 endPos, bool isOutside)
        {
            bool inside = !isOutside;

            // 1. Direct path
            if (CanPathToPoint(startPos, endPos))
                return true;

            if (entrances.Count <= 0 && elevator == null)
                return false;

            // Cache elevator points if present
            Vector3 elevTop = Vector3.zero;
            Vector3 elevBottom = Vector3.zero;
            Vector3 elevInside = Vector3.zero;

            if (elevator != null)
            {
                elevTop = elevator.elevatorTopPoint.position;
                elevBottom = elevator.elevatorBottomPoint.position;
                elevInside = elevator.elevatorInsidePoint.position;
            }

            // 2. Entrances
            if (entrances != null)
            {
                foreach (var entrance in entrances)
                {
                    bool relevant = inside
                        ? !entrance.isEntranceToBuilding
                        : entrance.isEntranceToBuilding;

                    if (!relevant)
                        continue;

                    if (entrance.exitScript == null && !entrance.FindExitPoint())
                        continue;

                    Vector3 from = entrance.entrancePoint.position;
                    Vector3 to = entrance.exitScript!.entrancePoint.position; // TODO: Test this

                    // start -> entrance -> exit -> end
                    if (CanPathToPoint(startPos, from) && CanPathToPoint(to, endPos))
                        return true;

                    // Combine with elevator if present
                    if (elevator != null)
                    {
                        // start -> elevator -> entrance -> exit -> end
                        if (CanPathToPoint(startPos, elevBottom) &&
                            CanPathToPoint(elevTop, from) &&
                            CanPathToPoint(to, endPos))
                            return true;
                    }
                }
            }

            // 3. Elevator-only paths
            if (elevator != null)
            {
                bool usingElevator = inside &&
                    Vector3.Distance(startPos, elevInside) < 1f;

                if (usingElevator)
                {
                    if (CanPathToPoint(elevTop, endPos) || CanPathToPoint(elevBottom, endPos))
                        return true;
                }
                else
                {
                    if (CanPathToPoint(startPos, elevBottom) && CanPathToPoint(elevTop, endPos))
                        return true;

                    if (inside && CanPathToPoint(startPos, elevTop) && CanPathToPoint(elevBottom, endPos))
                        return true;
                }
            }

            return false;
        }

        public static bool CanPathToPoint(Vector3 startPos, Vector3 endPos)
        {
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(startPos, endPos, -1, path) || (int)path.status != 0)
            {
                return false;
            }
            float pathDistance = 0f;
            if (path.corners.Length > 1)
            {
                for (int i = 1; i < path.corners.Length; i++)
                {
                    pathDistance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }
            }
            return pathDistance > 0;
        }

        public static void LogRarities(ContentType contentType)
        {
            foreach (var level in StartOfRound.Instance.levels)
            {
                logger.LogDebug($"- {level.name}:");

                switch (contentType)
                {
                    case ContentType.Item:
                        foreach (var item in level.spawnableScrap)
                        {
                            logger.LogDebug($"-- {item.spawnableItem.itemName}: {item.rarity}");
                        }
                        break;
                    case ContentType.Enemy:
                        foreach (var enemy in level.Enemies)
                        {
                            logger.LogDebug($"-- {enemy.enemyType.name}: {enemy.rarity}");
                        }
                        break;
                    case ContentType.MapObject:
                        foreach (var mapObject in level.spawnableMapObjects)
                        {
                            logger.LogDebug($"-- {mapObject.prefabToSpawn.name}:");
                            Debug.Log(string.Join("\n", mapObject.numberToSpawn.keys.Select(k => $"--- ({k.time}, {k.value})")));
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public static void LogAnimatorParameters(Animator animator)
        {
            foreach (var param in animator.parameters)
            {
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        logger.LogDebug($"{param.name} (Bool) = {animator.GetBool(param.name)}");
                        break;

                    case AnimatorControllerParameterType.Float:
                        logger.LogDebug($"{param.name} (Float) = {animator.GetFloat(param.name)}");
                        break;

                    case AnimatorControllerParameterType.Int:
                        logger.LogDebug($"{param.name} (Int) = {animator.GetInteger(param.name)}");
                        break;

                    case AnimatorControllerParameterType.Trigger:
                        logger.LogDebug($"{param.name} (Trigger)");
                        break;
                }
            }
        }

        public static Vector3 GetBestThrowDirection(Vector3 origin, Vector3 forward, int rayCount, float maxDistance, LayerMask layerMask)
        {
            Vector3 bestDirection = forward;
            float farthestHit = 0f;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (360f / rayCount);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * forward.normalized;

                // Raycast from origin outward
                if (Physics.Raycast(origin + Vector3.up * 0.5f, dir, out RaycastHit hit, maxDistance, layerMask))
                {
                    if (hit.distance > farthestHit)
                    {
                        bestDirection = dir;
                        farthestHit = hit.distance;
                    }
                }
                else
                {
                    // If nothing is hit, assume max distance (ideal throw)
                    return dir;
                }
            }

            return bestDirection;
        }

        public static List<SpawnableEnemyWithRarity> GetEnemies()
        {
            List<SpawnableEnemyWithRarity> enemies = new List<SpawnableEnemyWithRarity>();
            enemies = GameObject.Find("Terminal")
                .GetComponentInChildren<Terminal>()
                .moonsCatalogueList
                .SelectMany(x => x.Enemies.Concat(x.DaytimeEnemies).Concat(x.OutsideEnemies))
                .Where(x => x != null && x.enemyType != null && x.enemyType.name != null)
                .GroupBy(x => x.enemyType.name, (k, v) => v.First())
                .ToList();

            return enemies;
        }

        public static Dictionary<string, GameObject> GetAllHazards()
        {
            Dictionary<string, GameObject> hazards = new Dictionary<string, GameObject>();
            List<SpawnableMapObject> spawnableMapObjects = (from x in StartOfRound.Instance.levels.SelectMany((SelectableLevel level) => level.spawnableMapObjects)
                                                            group x by ((UnityEngine.Object)x.prefabToSpawn).name into g
                                                            select g.First()).ToList();
            foreach (SpawnableMapObject item in spawnableMapObjects)
            {
                hazards.Add(item.prefabToSpawn.name, item.prefabToSpawn);
            }
            return hazards;
        }

        public static Vector3 GetRandomNavMeshPositionInAnnulus(Vector3 center, float minRadius, float maxRadius, int sampleCount = 10)
        {
            Vector3 randomDirection;
            float y = center.y;

            // Make sure minRadius is less than maxRadius
            if (minRadius >= maxRadius)
            {
                logger.LogWarning("minRadius should be less than maxRadius. Returning original position.");
                return center;
            }

            // Try a few times to get a valid point
            for (int i = 0; i < sampleCount; i++)
            {
                // Get a random direction
                randomDirection = UnityEngine.Random.insideUnitSphere;
                randomDirection.y = 0f;
                randomDirection.Normalize();

                // Random distance between min and max radius
                float distance = UnityEngine.Random.Range(minRadius, maxRadius);

                // Calculate the new position
                Vector3 pos = center + randomDirection * distance;
                pos.y = y;

                // Check if it's on the NavMesh
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            logger.LogWarning("Unable to find valid NavMesh position in annulus. Returning original position.");
            return center;
        }

        public static List<Vector3> GetEvenlySpacedNavMeshPositions(Vector3 center, int count, float minRadius, float maxRadius)
        {
            List<Vector3> positions = new List<Vector3>();

            // Validate
            if (count <= 0 || minRadius > maxRadius)
            {
                logger.LogWarning("Invalid parameters for turret spawn positions.");
                return positions;
            }

            float y = center.y;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;

                float radians = angle * Mathf.Deg2Rad;

                float radius = UnityEngine.Random.Range(minRadius, maxRadius);

                float x = Mathf.Cos(radians) * radius;
                float z = Mathf.Sin(radians) * radius;

                Vector3 pos = new Vector3(center.x + x, y, center.z + z);

                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    positions.Add(hit.position);
                }
                else
                {
                    logger.LogWarning($"Could not find valid NavMesh position for turret {i}. Skipping.");
                }
            }

            return positions;
        }

        public static EnemyAI? SpawnEnemy(NamespacedKey<DawnEnemyInfo> key, Vector3 position, Quaternion rotation = default)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = GameObject.Instantiate(LethalContent.Enemies[key].EnemyType.enemyPrefab, position, rotation);
            EnemyAI enemy = obj.GetComponent<EnemyAI>();
            enemy.NetworkObject.Spawn();
            RoundManager.Instance.SpawnedEnemies.Add(enemy);
            return enemy;
        }

        public static GrabbableObject? SpawnItem(NamespacedKey<DawnItemInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, float fallTime = 0f)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = GameObject.Instantiate(LethalContent.Items[key].Item.spawnPrefab, position, rotation, parentTo);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.fallTime = fallTime;
            grabObj.NetworkObject.Spawn();
            return grabObj;
        }

        public static SpawnableMapObject? SpawnMapObject(NamespacedKey<DawnMapObjectInfo> key, Vector3 position, Quaternion rotation = default)
        {
            throw new NotImplementedException();
            /*if (!IsServerOrHost) { return null; }
            GameObject obj = GameObject.Instantiate(LethalContent.MapObjects[key].OutsideInfo.p.spawnPrefab, position, rotation, StartOfRound);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.fallTime = fallTime;
            grabObj.NetworkObject.Spawn();
            return grabObj;*/
        } // TODO

        public static void PlaySoundAtPosition(Transform pos, AudioClip clip, float volume = 1f, bool randomizePitch = true, bool spatial3D = true, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            GameObject soundObj = GameObject.Instantiate(new GameObject("TempSoundEffectObj"), pos);
            AudioSource source = soundObj.AddComponent<AudioSource>();

            OccludeAudio occlude = soundObj.AddComponent<OccludeAudio>();
            occlude.lowPassOverride = 20000f;

            AudioLowPassFilter filter = soundObj.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = cutoffFrequency;

            source.rolloffMode = AudioRolloffMode.Linear;

            if (randomizePitch)
                source.pitch = UnityEngine.Random.Range(0.94f, 1.06f);

            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = spatial3D ? 1 : 0;
            source.minDistance = min3DDistance;
            source.maxDistance = max3DDistance;
            source.Play();
            GameObject.Destroy(soundObj, source.clip.length);

            WalkieTalkie.TransmitOneShotAudio(source, clip, 0.85f);
            if (spatial3D && audibleNoiseID >= 0)
                RoundManager.Instance.PlayAudibleNoise(source.transform.position, 4f * volume, volume / 2f, 0, noiseIsInsideClosedShip: true, audibleNoiseID);
        }

        public static void PlaySoundAtPosition(Vector3 pos, AudioClip clip, float volume = 1f, bool randomizePitch = true, bool spatial3D = true, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            GameObject soundObj = GameObject.Instantiate(new GameObject("TempSoundEffectObj"), pos, Quaternion.identity);
            AudioSource source = soundObj.AddComponent<AudioSource>();

            OccludeAudio occlude = soundObj.AddComponent<OccludeAudio>();
            occlude.lowPassOverride = 20000f;

            AudioLowPassFilter filter = soundObj.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = cutoffFrequency;

            source.rolloffMode = AudioRolloffMode.Linear;

            if (randomizePitch)
                source.pitch = UnityEngine.Random.Range(0.94f, 1.06f);

            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = spatial3D ? 1 : 0;
            source.minDistance = min3DDistance;
            source.maxDistance = max3DDistance;
            source.Play();
            GameObject.Destroy(soundObj, source.clip.length);

            WalkieTalkie.TransmitOneShotAudio(source, clip, 0.85f);
            if (spatial3D && audibleNoiseID >= 0)
                RoundManager.Instance.PlayAudibleNoise(source.transform.position, 4f * volume, volume / 2f, 0, noiseIsInsideClosedShip: true, audibleNoiseID);
        }

        public static void PlaySoundAtPosition(Transform pos, AudioClip[] clips, float volume = 1f, bool randomizePitch = true, bool spatial3D = true, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            int index = UnityEngine.Random.Range(0, clips.Length);
            PlaySoundAtPosition(pos, clips[index], volume, randomizePitch, spatial3D, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        public static void PlaySoundAtPosition(Vector3 pos, AudioClip[] clips, float volume = 1f, bool randomizePitch = true, bool spatial3D = true, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            int index = UnityEngine.Random.Range(0, clips.Length);
            PlaySoundAtPosition(pos, clips[index], volume, randomizePitch, spatial3D, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        public static PlayerControllerB GetRandomPlayer(System.Random random)
        {
            var players = StartOfRound.Instance.allPlayerScripts.Where(p => p != null && p.isPlayerControlled).ToArray();
            return players.Length == 0 ? StartOfRound.Instance.allPlayerScripts[random.Next(StartOfRound.Instance.allPlayerScripts.Length)] : players[random.Next(players.Length)];
        }

        public static PlayerControllerB GetRandomPlayer()
        {
            var players = StartOfRound.Instance.allPlayerScripts.Where(p => p != null && p.isPlayerControlled).ToArray();
            return players.Length == 0 ? StartOfRound.Instance.allPlayerScripts[UnityEngine.Random.Range(0, StartOfRound.Instance.allPlayerScripts.Length)] : players[UnityEngine.Random.Range(0, players.Length)];
        }

        public static void DespawnItemInSlotOnClient(int itemSlot)
        {
            HUDManager.Instance.itemSlotIcons[itemSlot].enabled = false;
            localPlayer.DestroyItemInSlotAndSync(itemSlot);
        }

        public static void LogChat(string msg, string nameOfUserWhoTyped = "Server")
        {
            HUDManager.Instance.AddChatMessage(msg, nameOfUserWhoTyped);
        }

        public static float SmartDistance(PositionInfo position1, PositionInfo position2)
        {
            if (position1.isOutside == position2.isOutside)
            {
                return Vector3.Distance(position1.position, position2.position);
            }
            else
            {
                float closestDistance = Mathf.Infinity;
                foreach (var entrance in Utils.entrances)
                {
                    if (entrance == null) { continue; }
                    if (entrance.isEntranceToBuilding != position1.isOutside) { continue; }
                    if (entrance.exitScript == null && (entrance.exitPointDoesntExist || !entrance.FindExitPoint())) { continue; }
                    if (entrance.exitScript == null) { continue; }

                    float position1ToEntrance = Vector3.Distance(position1.position, entrance.transform.position);
                    float exitToPosition2 = Vector3.Distance(entrance.exitScript.transform.position, position2.position);
                    float totalDistance = position1ToEntrance + exitToPosition2;
                    if (totalDistance > closestDistance) { continue; }
                    closestDistance = totalDistance;
                }

                return closestDistance;
            }
        }

        public static Texture2D? LoadEmbeddedTexture(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                logger.LogError($"Resource not found: {resourceName}");
                return null;
            }

            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(data);

            return texture;
        }
    }

    [HarmonyPatch]
    public class UtilsPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnInsideEnemiesFromVentsIfReady))]
        public static bool RoundManager_SpawnInsideEnemiesFromVentsIfReady_Prefix()
        {
            try
            {
                if (Utils.testing && Utils.DEBUG_disableSpawning) { return false; }
                return true;
            }
            catch
            {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnDaytimeEnemiesOutside))]
        public static bool RoundManager_SpawnDaytimeEnemiesOutside_Prefix()
        {
            try
            {
                if (Utils.testing && Utils.DEBUG_disableSpawning) { return false; }
                return true;
            }
            catch
            {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnEnemiesOutside))]
        public static bool RoundManager_SpawnEnemiesOutside_Prefix()
        {
            try
            {
                if (Utils.testing && Utils.DEBUG_disableSpawning) { return false; }
                return true;
            }
            catch
            {
                return true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingLevel))]
        public static void RoundManager_FinishGeneratingLevel_Postfix()
        {
            try
            {
                Utils.elevator = null;
                Utils.entrances.Clear();

                Utils.entrances = GameObject.FindObjectsOfType<EntranceTeleport>(includeInactive: false).ToList();
                Utils.elevator = GameObject.FindObjectOfType<MineshaftElevatorController>();

                Utils.SetRandoms();

                Utils.OnFinishGeneratingLevel.Invoke();
            }
            catch
            {
                return;
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        public static void StartOfRound_OnShipLandedMiscEvents_Postfix()
        {
            try
            {
                Utils.OnShipLanded.Invoke();
            }
            catch
            {
                return;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        public static void PlayerControllerB_ConnectClientToPlayerObject_Postfix(PlayerControllerB __instance)
        {
            try
            {
                Utils.terminal = GameObject.FindObjectOfType<Terminal>();
            }
            catch
            {
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void HUDManager_SubmitChat_performed_Prefix(HUDManager __instance)
        {
            try
            {
                if (!Utils.testing) { return; }
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");

                Utils.ChatCommand(args);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}