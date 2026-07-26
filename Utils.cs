using Dawn;
using Dawn.Utils;
using Dissonance;
using GameNetcodeStuff;
using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SocialPlatforms;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static class Utils
    {
        public static bool testing => cfgTesting.Value;

        public static bool inTestRoom => StartOfRound.Instance?.testRoom != null;
        public static bool DEBUG_disableSpawning = false;
        public static bool DEBUG_disableTime = false;
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

        public static List<GrabbableObject> spawnedItems = new List<GrabbableObject>();

        public static List<EntranceTeleport> entrances = [];
        public static MineshaftElevatorController? elevator;
        public static Terminal? terminal;

        public static BoundedRange randomPercentage = new BoundedRange(0f, 1f);
        public static System.Random randomLocal { get; private set; } = new();
        public static System.Random randomGlobal { get; private set; } = new();

        public static UnityEvent OnFinishGeneratingLevel = new();
        public static UnityEvent OnShipLanded = new();

        public const ulong RodrigoSteamID = 76561198164429786;
        public const ulong LizzieSteamID = 76561199094139351;
        public const ulong GlitchSteamID = 76561198984467725;
        public const ulong RatSteamID = 76561199182474292;
        public const ulong XuSteamID = 76561198399127090;
        public const ulong SlayerSteamID = 76561198077184650;
        public const ulong SnowySteamID = 76561198253760639;
        public const ulong FunoSteamID = 76561198993437314;

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

        internal static void ChatCommand(string[] args)
        {
            if (!testing) { return; }

            switch (args[0])
            {
                case "/spawning":
                    DEBUG_disableSpawning = !DEBUG_disableSpawning;
                    HUDManager.Instance.DisplayTip("Disable Spawning", DEBUG_disableSpawning.ToString());
                    break;
                case "/log":
                    if (args.Length == 1)
                    {
                        logger.LogDebug("- rarities");
                        logger.LogDebug("- archetypes");
                        logger.LogDebug("- dungeons");
                        logger.LogDebug("- enemies");
                        logger.LogDebug("- items");
                        logger.LogDebug("- mapobjects");
                        logger.LogDebug("- moons");
                        logger.LogDebug("- storylogs");
                        logger.LogDebug("- surfaces");
                        logger.LogDebug("- terminalcommands");
                        logger.LogDebug("- tilesets");
                        logger.LogDebug("- unlockables");
                        logger.LogDebug("- weathers");
                        logger.LogDebug("- animations");
                        logger.LogDebug("- footstepsurfaces");
                    }
                    switch (args[1])
                    {
                        case "rarities":
                            if (args.Length == 2) { return; }
                            switch (args[2])
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
                        case "archetypes":
                            foreach (var item in LethalContent.Archetypes.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "dungeons":
                            foreach (var item in LethalContent.Dungeons.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "enemies":
                            foreach (var item in LethalContent.Enemies.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "items":
                            foreach (var item in LethalContent.Items.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "mapobjects":
                            foreach (var item in LethalContent.MapObjects.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "moons":
                            foreach (var item in LethalContent.Moons.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "storylogs":
                            foreach (var item in LethalContent.StoryLogs.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "surfaces":
                            foreach (var item in LethalContent.Surfaces.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "terminalcommands":
                            foreach (var item in LethalContent.TerminalCommands.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "tilesets":
                            foreach (var item in LethalContent.TileSets.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "unlockables":
                            foreach (var item in LethalContent.Unlockables.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "weathers":
                            foreach (var item in LethalContent.Weathers.Values)
                                logger.LogDebug(item.TypedKey.ToString());
                            break;
                        case "animations":
                            LogAnimatorParameters(localPlayer.playerBodyAnimator);
                            break;
                        case "footstepsurfaces":
                            foreach (var surface in StartOfRound.Instance.footstepSurfaces)
                            {
                                logger.LogDebug(surface.surfaceTag);
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case "/refresh":
                    RoundManager.Instance.RefreshEnemiesList();
                    HoarderBugAI.RefreshGrabbableObjectsInMapList();
                    break;
                case "/dungeon":
                    var dInfo = RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.GetDawnInfo();
                    logger.LogDebug($"{dInfo.TypedKey.ToString()} | {RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name}");
                    break;
                case "/vignette":
                    if (args.Length > 2)
                    {
                        if (!float.TryParse(args[1], out float intensity) || !float.TryParse(args[2], out float decrease)) { return; }
                        VignetteOverlay.SetIntensity(intensity, decrease);
                        HUDManager.Instance.DisplayTip("SnowyLib", $"Vignette intensity set to {intensity} and insanity decrease per second is set to {decrease}");
                    }
                    else
                    {
                        if (!float.TryParse(args[1], out float intensity)) { return; }
                        VignetteOverlay.SetIntensity(intensity);
                        HUDManager.Instance.DisplayTip("SnowyLib", $"Vignette intensity set to {intensity}");
                    }
                    break;
                case "/spawnanim":
                    localPlayer.SpawnPlayerAnimation();
                    break;
                case "/time":
                    DEBUG_disableTime = !DEBUG_disableTime;
                    StartOfRound.Instance.currentLevel.planetHasTime = !DEBUG_disableTime;
                    HUDManager.Instance.DisplayTip("Snowylib", "disableTime: " + DEBUG_disableTime);
                    break;
                case "/playanim":
                    if (args.Length > 3 && float.TryParse(args[3], out float time))
                    {
                        PlayPlayerAnimation(args[1], args[2], time);
                    }
                    else if (args.Length > 2)
                    {
                        PlayPlayerAnimation(args[1], args[2], 3f);
                    }
                    else if (args.Length > 1)
                    {
                        PlayPlayerAnimation(args[1]);
                    }
                    break;
                case "/drunkness":
                    if (args.Length == 1 || !float.TryParse(args[1], out float drunkness)) { return; }
                    localPlayer.drunkness = drunkness;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Determines whether a navigable path exists between two positions, considering entrances and elevator
        /// transitions.
        /// </summary>
        /// <param name="startPos">The starting position in world coordinates.</param>
        /// <param name="endPos">The destination position in world coordinates.</param>
        /// <param name="isOutside">true if the starting position is outside the building; otherwise, false.</param>
        /// <returns>true if a valid path exists; otherwise, false.</returns>
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
                    Vector3 to = entrance.exitScript!.entrancePoint.position;

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

        /// <summary>
        /// Determines whether a valid navigable path exists between two positions on the NavMesh.
        /// </summary>
        /// <param name="startPos">The starting position in world coordinates.</param>
        /// <param name="endPos">The target position in world coordinates.</param>
        /// <returns>true if a valid path exists; otherwise, false.</returns>
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

        internal static void LogRarities(ContentType contentType)
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

        /// <summary>
        /// Plays an animation on the local player's animator using the specified parameter and value for a given
        /// duration.
        /// </summary>
        /// <param name="animName">The name of the animation parameter to modify.</param>
        /// <param name="animValue">The value to assign to the animation parameter, interpreted according to its type.</param>
        /// <param name="time">The duration in seconds for which the animation should play.</param>
        public static void PlayPlayerAnimation(string animName, string animValue = "", float time = 1f)
        {
            var param = localPlayer.playerBodyAnimator.parameters.Where(x => x.name == animName).FirstOrDefault();

            localPlayer.PlayQuickSpecialAnimation(time);

            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    if (!float.TryParse(animValue, out float value1)) { return; }
                    localPlayer.playerBodyAnimator.SetFloat(animName, value1);
                    break;
                case AnimatorControllerParameterType.Int:
                    if (!int.TryParse(animValue, out int value2)) { return; }
                    localPlayer.playerBodyAnimator.SetInteger(animName, value2);
                    break;
                case AnimatorControllerParameterType.Bool:
                    if (!bool.TryParse(animValue, out bool value3)) { return; }
                    localPlayer.playerBodyAnimator.SetBool(animName, value3);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    localPlayer.playerBodyAnimator.SetTrigger(animName);
                    break;
                default:
                    break;
            }
        }

        internal static void LogAnimatorParameters(Animator animator)
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

        /// <summary>
        /// Calculates the optimal direction to throw an object by performing multiple raycasts around the forward
        /// vector and selecting the direction with the farthest unobstructed path.
        /// </summary>
        /// <param name="origin">The starting position for the raycasts.</param>
        /// <param name="forward">The initial forward direction to base the search around.</param>
        /// <param name="rayCount">The number of directions to sample around the forward vector.</param>
        /// <param name="maxDistance">The maximum distance each raycast can reach.</param>
        /// <param name="layerMask">The layer mask used to filter raycast collisions.</param>
        /// <returns>The direction vector that allows for the farthest throw from the origin.</returns>
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

        /// <summary>
        /// Generates a random position on the NavMesh within an annular region defined by minimum and maximum radii
        /// from a center point.
        /// </summary>
        /// <param name="center">The center point around which to sample the NavMesh.</param>
        /// <param name="minRadius">The minimum distance from the center point for the annulus.</param>
        /// <param name="maxRadius">The maximum distance from the center point for the annulus.</param>
        /// <param name="sampleCount">The number of attempts to find a valid NavMesh position.</param>
        /// <returns>A valid position on the NavMesh within the specified annulus, or the original center position if none is
        /// found.</returns>
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

        /// <summary>
        /// Generates a list of evenly spaced positions on the NavMesh around a specified center point.
        /// </summary>
        /// <param name="center">The center point around which to generate positions.</param>
        /// <param name="count">The number of positions to generate.</param>
        /// <param name="minRadius">The minimum distance from the center for generated positions.</param>
        /// <param name="maxRadius">The maximum distance from the center for generated positions.</param>
        /// <returns>A list of valid positions on the NavMesh.</returns>
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
            GameObject obj = GameObject.Instantiate(LethalContent.Enemies[key].EnemyType.enemyPrefab, position, rotation, parentTo);
            EnemyAI enemy = obj.GetComponent<EnemyAI>();
            enemy.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            RoundManager.Instance.SpawnedEnemies.Add(enemy);
            return enemy;
        }

        /// <summary>
        /// Instantiates a grabbable item at the specified position and rotation.
        /// </summary>
        /// <remarks>
        /// Execution: Server
        /// </remarks>
        /// <param name="key">The key identifying the item to instantiate.</param>
        /// <param name="position">The world position where the item will be spawned.</param>
        /// <param name="rotation">The rotation to apply to the spawned item.</param>
        /// <param name="parentTo">The optional parent transform for the spawned item.</param>
        /// <param name="fallTime">The duration for the item to fall after spawning.</param>
        /// <param name="destroyWithScene">true to destroy the item when the scene unloads; otherwise, false.</param>
        /// <returns>The spawned grabbable object, or null if not executed on the server.</returns>
        public static GrabbableObject? SpawnItem(NamespacedKey<DawnItemInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, float fallTime = 0f, bool destroyWithScene = false)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = GameObject.Instantiate(LethalContent.Items[key].Item.spawnPrefab, position, rotation, parentTo);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.fallTime = fallTime;
            grabObj.NetworkObject.Spawn(destroyWithScene: destroyWithScene);
            return grabObj;
        }

        /// <summary>
        /// Instantiates a map object prefab at the specified position and rotation.
        /// </summary>
        /// <remarks>
        /// Execution: Server
        /// </remarks>
        /// <param name="key">The key identifying the map object to instantiate.</param>
        /// <param name="position">The world position for the instantiated map object.</param>
        /// <param name="rotation">The rotation to apply to the instantiated map object.</param>
        /// <param name="parentTo">The optional parent transform for the instantiated map object.</param>
        /// <param name="destroyWithScene">true to destroy the map object when the scene is unloaded; otherwise, false.</param>
        /// <returns>The spawned map object, or null if not executed on the server or host.</returns>
        public static SpawnableMapObject? SpawnMapObject(NamespacedKey<DawnMapObjectInfo> key, Vector3 position, Quaternion rotation = default, Transform? parentTo = null, bool destroyWithScene = true)
        {
            if (!IsServerOrHost) { return null; }
            var prefab = LethalContent.MapObjects[key].GetMapObjectPrefab();
            if (prefab == null) { logger.LogError($"Couldnt find prefab for {key}"); return null; }
            GameObject obj = GameObject.Instantiate(prefab, position, rotation, parentTo);
            var mapObj = obj.GetComponent<SpawnableMapObject>();
            obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: destroyWithScene);
            return mapObj;
        }

        /// <summary>
        /// Plays an audio clip at a specified world position with optional pitch randomization, 3D spatialization,
        /// distance attenuation, and low-pass filtering.
        /// </summary>
        /// <remarks>
        /// Execution: Local
        /// Destroys the temporary sound object after playback and transmits audio to
        /// walkie-talkies. Registers the sound as audible noise if spatial3D is enabled and audibleNoiseID is
        /// non-negative.</remarks>
        /// <param name="pos">The transform representing the world position where the sound is played.</param>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="volume">The playback volume, from 0.0 (silent) to 1.0 (full volume).</param>
        /// <param name="randomizePitch">true to randomize the pitch of the sound; otherwise, false.</param>
        /// <param name="spatial3D">true to spatialize the sound in 3D space; otherwise, false.</param>
        /// <param name="min3DDistance">The minimum distance at which the sound is audible.</param>
        /// <param name="max3DDistance">The maximum distance at which the sound can be heard.</param>
        /// <param name="cutoffFrequency">The cutoff frequency for the low-pass filter applied to the sound.</param>
        /// <param name="audibleNoiseID">The identifier for registering the sound as audible noise, or a negative value to disable.</param>
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

        /// <summary>
        /// Plays an audio clip at a specified world position with configurable volume, pitch, spatialization, distance
        /// attenuation, and low-pass filtering. Optionally registers the sound as an audible noise event.
        /// </summary>
        /// <remarks>
        /// Execution: Local
        /// </remarks>
        /// <param name="pos">The world position where the audio clip is played.</param>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="volume">The playback volume, from 0 (silent) to 1 (full volume).</param>
        /// <param name="randomizePitch">true to randomize the pitch slightly; otherwise, false.</param>
        /// <param name="spatial3D">true to spatialize the sound in 3D space; otherwise, false.</param>
        /// <param name="min3DDistance">The minimum distance at which the sound is heard at full volume.</param>
        /// <param name="max3DDistance">The maximum distance at which the sound can be heard.</param>
        /// <param name="cutoffFrequency">The cutoff frequency for the low-pass filter applied to the audio.</param>
        /// <param name="audibleNoiseID">The identifier for registering the sound as an audible noise event, or a negative value to skip
        /// registration.</param>
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

        /// <summary>
        /// Plays a random audio clip from the specified array at the given position with optional pitch randomization,
        /// 3D spatialization, distance settings, and cutoff frequency.
        /// </summary>
        /// <remarks>
        /// Execution: Local
        /// </remarks>
        /// <param name="pos">The transform representing the position in world space where the sound is played.</param>
        /// <param name="clips">The array of audio clips to select from for playback.</param>
        /// <param name="volume">The playback volume, where 1.0 is full volume.</param>
        /// <param name="randomizePitch">true to randomize the pitch of the audio clip; otherwise, false.</param>
        /// <param name="spatial3D">true to enable 3D spatialization of the sound; otherwise, false.</param>
        /// <param name="min3DDistance">The minimum distance at which the sound is audible.</param>
        /// <param name="max3DDistance">The maximum distance at which the sound is audible.</param>
        /// <param name="cutoffFrequency">The cutoff frequency applied to the audio playback.</param>
        /// <param name="audibleNoiseID">The identifier for the audible noise event.</param>
        public static void PlaySoundAtPosition(Transform pos, AudioClip[] clips, float volume = 1f, bool randomizePitch = true, bool spatial3D = true, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            int index = UnityEngine.Random.Range(0, clips.Length);
            PlaySoundAtPosition(pos, clips[index], volume, randomizePitch, spatial3D, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        /// <summary>
        /// Plays a random audio clip from the specified array at the given 3D position with optional pitch
        /// randomization, spatialization, distance attenuation, and frequency cutoff.
        /// </summary>
        /// <remarks>
        /// Execution: Local
        /// </remarks>
        /// <param name="pos">The position in world space where the sound is played.</param>
        /// <param name="clips">An array of audio clips to select from for playback.</param>
        /// <param name="volume">The playback volume, ranging from 0.0 (silent) to 1.0 (full volume).</param>
        /// <param name="randomizePitch">true to randomize the pitch of the audio clip; otherwise, false.</param>
        /// <param name="spatial3D">true to enable 3D spatialization of the sound; otherwise, false.</param>
        /// <param name="min3DDistance">The minimum distance from the source at which the sound is audible.</param>
        /// <param name="max3DDistance">The maximum distance from the source at which the sound is audible.</param>
        /// <param name="cutoffFrequency">The cutoff frequency applied to the audio playback in hertz.</param>
        /// <param name="audibleNoiseID">The identifier for the audible noise event.</param>
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

        public static void LogChat(string msg, string nameOfUserWhoTyped = "Server")
        {
            HUDManager.Instance.AddChatMessage(msg, nameOfUserWhoTyped);
        }

        /// <summary>
        /// Calculates the shortest distance between two positions, accounting for building entrances and exits when
        /// transitioning between inside and outside areas.
        /// </summary>
        /// <param name="position1">The starting position.</param>
        /// <param name="position2">The target position.</param>
        /// <param name="fastDistanceCheck">If true, uses a faster, less accurate calculation based on squared distances.</param>
        /// <returns>The shortest distance between the two positions, or -1 if no valid entrance is found during a fast distance
        /// check.</returns>
        public static float SmartDistance(Vector3 position1, Vector3 position2, bool fastDistanceCheck = false)
        {
            if (position1.IsOutside() == position2.IsOutside())
                return Vector3.Distance(position1, position2);

            float closestDistance = Mathf.Infinity;
            EntranceTeleport? bestEntrance = null;

            foreach (var entrance in Utils.entrances)
            {
                if (entrance == null)
                    continue;

                if (entrance.isEntranceToBuilding != position1.IsOutside())
                    continue;

                if (entrance.exitScript == null &&
                    (entrance.exitPointDoesntExist || !entrance.FindExitPoint()))
                    continue;

                if (entrance.exitScript == null)
                    continue;

                if (fastDistanceCheck)
                {
                    float distance =
                        (position1 - entrance.transform.position).sqrMagnitude +
                        (entrance.exitScript.transform.position - position2).sqrMagnitude;

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestEntrance = entrance;
                    }
                }
                else
                {
                    float distance =
                        Vector3.Distance(position1, entrance.transform.position) +
                        Vector3.Distance(entrance.exitScript.transform.position, position2);

                    closestDistance = Mathf.Min(closestDistance, distance);
                }
            }

            if (!fastDistanceCheck)
                return closestDistance;

            if (bestEntrance == null)
                return -1;

            return
                Vector3.Distance(position1, bestEntrance.transform.position) +
                Vector3.Distance(bestEntrance.exitScript.transform.position, position2);
        }

        /// <summary>
        /// Creates a LayerMask that includes the specified layers by name.
        /// </summary>
        /// <param name="layerNames">An array of layer names to include in the mask.</param>
        /// <returns>A LayerMask representing the combined layers.</returns>
        public static LayerMask CreateMask(params string[] layerNames)
        {
            LayerMask mask = 0;

            foreach (var name in layerNames)
            {
                int layer = LayerMask.NameToLayer(name);
                if (layer >= 0)
                    mask |= (1 << layer);
            }

            return mask;
        }

        public static Collider? GetLargestCollider(Collider[] colliders)
        {
            if (colliders.Length == 0)
                return null;

            Collider largest = colliders[0];
            float largestVolume = largest.bounds.size.x *
                                  largest.bounds.size.y *
                                  largest.bounds.size.z;

            foreach (Collider col in colliders)
            {
                Bounds bounds = col.bounds;

                float volume =
                    bounds.size.x *
                    bounds.size.y *
                    bounds.size.z;

                logger.LogDebug($"{col.name} | Enabled: {col.enabled} | Size: {bounds.size} | Volume: {volume}");

                if (volume > largestVolume)
                {
                    largestVolume = volume;
                    largest = col;
                }
            }

            return largest;
        }

        /// <summary>
        /// Creates and initializes a ping GameObject at the specified world position with customizable display text,
        /// node type, range, and lifetime.
        /// </summary>
        /// <remarks>
        /// Execution: Local
        /// </remarks>
        /// <param name="position">The world position where the ping is instantiated.</param>
        /// <param name="headerText">The main text displayed on the ping.</param>
        /// <param name="subText">The secondary text displayed on the ping.</param>
        /// <param name="nodeType">The type of node represented by the ping.</param>
        /// <param name="requiresLineOfSight">true if line of sight is required for the ping; otherwise, false.</param>
        /// <param name="minRange">The minimum effective range of the ping.</param>
        /// <param name="maxRange">The maximum effective range of the ping.</param>
        /// <param name="destroyTime">The time in seconds before the ping is destroyed. Set to 0 or less to persist indefinitely.</param>
        /// <returns>The instantiated ping GameObject.</returns>
        public static GameObject Ping(Vector3 position, string headerText = "Ping", string subText = "", int nodeType = 0, bool requiresLineOfSight = false, int minRange = 1, int maxRange = 2000, float destroyTime = 10)
        {
            GameObject ping = GameObject.Instantiate(new GameObject("Ping"), position, Quaternion.identity);

            ping.tag = "DoNotSet";
            ping.layer = LayerMask.NameToLayer("ScanNode");

            ping.AddComponent<BoxCollider>();

            ScanNodeProperties scanNode = ping.AddComponent<ScanNodeProperties>();
            scanNode.maxRange = maxRange;
            scanNode.minRange = minRange;
            scanNode.headerText = headerText;
            scanNode.subText = subText;
            scanNode.nodeType = nodeType;
            scanNode.requiresLineOfSight = requiresLineOfSight;

            if (destroyTime > 0)
                GameObject.Destroy(ping, destroyTime);

            HUDManager h = HUDManager.Instance;

            if (!h.nodesOnScreen.Contains(scanNode))
            {
                h.nodesOnScreen.Add(scanNode);
            }

            if (h.scanNodes.ContainsValue(scanNode)) { return ping; }

            for (int i = 0; i < h.scanElements.Length; i++)
            {
                if (h.scanNodes.TryAdd(h.scanElements[i], scanNode))
                {
                    break;
                }
            }

            return ping;
        }

        /// <summary>
        /// Sets the ship to leave early at the specified time, displays the provided dialogue, and triggers the ship
        /// leaving alert.
        /// </summary>
        /// <remarks>
        /// Execution: Local
        /// </remarks>
        /// <param name="timeToLeaveEarly">The time, in hours, when the ship should leave early.</param>
        /// <param name="shipLeavingEarlyDialogue">The dialogue segments to display when the ship is leaving early.</param>
        public static void SetShipLeaveEarly(float timeToLeaveEarly, DialogueSegment[] shipLeavingEarlyDialogue)
        {
            TimeOfDay.Instance.shipLeaveAutomaticallyTime = timeToLeaveEarly;
            TimeOfDay.Instance.shipLeavingAlertCalled = true;
            HUDManager.Instance.ReadDialogue(shipLeavingEarlyDialogue);
            HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
        }

        /// <remarks>
        /// Execution: Local
        /// </remarks>
        public static void DisplayStatusEffect(string message)
        {
            HUDManager.Instance.DisplayStatusEffect(message);
        }

        /// <remarks>
        /// Execution: Local
        /// </remarks>
        public static void DisplayDialogue(DialogueSegment[] dialogues)
        {
            HUDManager.Instance.ReadDialogue(dialogues);
        }


        /// <remarks>
        /// Requires spawned signal translator to work
        /// </remarks>
        public static void DisplaySignalTranslatorMessage(string message)
        {
            HUDManager.Instance.UseSignalTranslatorServerRpc(message);
        }

        /// <summary>
        /// Displays a random ad
        /// </summary>
        public static void DisplayAd()
        {
            HUDManager.Instance.ChooseAdItem();
        }

        /// <summary>
        /// Displays an advertisement for the specified item with optional top and bottom text.
        /// </summary>
        /// <param name="item">The item to advertise.</param>
        /// <param name="top">The text displayed at the top of the advertisement.</param>
        /// <param name="bottom">The text displayed at the bottom of the advertisement.</param>
        public static void DisplayAd(Item item, string top, string bottom)
        {
            if (item == null)
            {
                logger.LogWarning("Display Item Ad: Item is Null");
                return;
            }
            if (item.spawnPrefab == null)
            {
                logger.LogWarning("Display Item Ad: Item spawn prefab is null");
                return;
            }
            HUDManager.Instance.CreateToolAdModel(-100, item);
            DoAdStuff(top, bottom);
        }

        /// <summary>
        /// Displays an advertisement for the specified unlockable with optional top and bottom text.
        /// </summary>
        /// <param name="unlockable">The unlockable to advertise.</param>
        /// <param name="top">The text displayed at the top of the advertisement.</param>
        /// <param name="bottom">The text displayed at the bottom of the advertisement.</param>
        public static void DisplayAd(UnlockableItem unlockable, string top, string bottom)
        {
            if (unlockable == null)
            {
                logger.LogWarning("Display Unlockable Ad: Unlockable is Null");
                return;
            }
            if (unlockable.prefabObject == null)
            {
                logger.LogWarning("Display Unlockable Ad: Unlockable prefabObject is Null");
                return;
            }
            HUDManager.Instance.CreateFurnitureAdModel(unlockable);
            DoAdStuff(top, bottom);
        }

        private static void DoAdStuff(string top, string bottom)
        {
            logger.LogDebug($"Do Ad Stuff: {top} {bottom}");
            var hm = HUDManager.Instance;
            hm.advertTopText.text = top;
            hm.advertBottomText.text = bottom;
            if (hm.displayAdCoroutine != null)
            {
                hm.StopCoroutine(hm.displayAdCoroutine);
            }
            hm.displayAdCoroutine = hm.StartCoroutine(hm.displayAd());
        }

        public static Vector3 GetTopOfObjectRender(GameObject obj, bool includeDisabled = false)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            Bounds combinedBounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled && !includeDisabled) { continue; }
                combinedBounds.Encapsulate(renderer.bounds);
            }

            return new Vector3(
                combinedBounds.center.x,
                combinedBounds.max.y,
                combinedBounds.center.z
            );
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

                Utils.entrances = GameObject.FindObjectsOfType<EntranceTeleport>().ToList();
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

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        public static void GrabbableObject_Start_Postfix(GrabbableObject __instance)
        {
            try
            {
                Utils.spawnedItems.Add(__instance);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.OnDestroy))]
        public static void GrabbableObject_OnDestroy_Postfix(GrabbableObject __instance)
        {
            try
            {
                Utils.spawnedItems.Remove(__instance);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}