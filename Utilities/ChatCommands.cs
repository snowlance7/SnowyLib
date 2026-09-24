using Dawn;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using static SnowyLib.Plugin;

namespace SnowyLib
{
    public static partial class Utils
    {
        internal static void ChatCommand(string[] args)
        {
            if (!testing) { return; }

            switch (args[0])
            {
                case "/spawning":
                    DEBUG_disableSpawning = !DEBUG_disableSpawning;
                    HUDManager.Instance.DisplayTip("SnowyLib", $"Spawning {(DEBUG_disableSpawning ? "disabled" : "enabled")}");
                    break;
                case "/time":
                    DEBUG_disableTime = !DEBUG_disableTime;
                    StartOfRound.Instance.currentLevel.planetHasTime = !DEBUG_disableTime;
                    HUDManager.Instance.DisplayTip("Snowylib", $"Time {(DEBUG_disableTime ? "disabled" : "enabled")}");
                    break;
                case "/log":
                    if (args.Length == 1)
                    {
                        logger.LogInfo("- rarities");
                        logger.LogInfo("- archetypes");
                        logger.LogInfo("- dungeons");
                        logger.LogInfo("- enemies");
                        logger.LogInfo("- items");
                        logger.LogInfo("- mapobjects");
                        logger.LogInfo("- moons");
                        logger.LogInfo("- storylogs");
                        logger.LogInfo("- surfaces");
                        logger.LogInfo("- terminalcommands");
                        logger.LogInfo("- tilesets");
                        logger.LogInfo("- unlockables");
                        logger.LogInfo("- weathers");
                        logger.LogInfo("- animations");
                        logger.LogInfo("- canvas");
                        return;
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
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "dungeons":
                            foreach (var item in LethalContent.Dungeons.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "enemies":
                            foreach (var item in LethalContent.Enemies.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "items":
                            foreach (var item in LethalContent.Items.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "mapobjects":
                            foreach (var item in LethalContent.MapObjects.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "moons":
                            foreach (var item in LethalContent.Moons.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "storylogs":
                            foreach (var item in LethalContent.StoryLogs.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "surfaces":
                            foreach (var item in LethalContent.Surfaces.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "terminalcommands":
                            foreach (var item in LethalContent.TerminalCommands.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "tilesets":
                            foreach (var item in LethalContent.TileSets.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "unlockables":
                            foreach (var item in LethalContent.Unlockables.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "weathers":
                            foreach (var item in LethalContent.Weathers.Values)
                                logger.LogInfo(item.TypedKey.ToString());
                            break;
                        case "animations":
                            LogAnimatorParameters(localPlayer.playerBodyAnimator);
                            break;
                        case "canvas":
                            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
                            {
                                logger.LogInfo($"{(canvas.transform.parent != null ? canvas.transform.parent.name + "/" : "")}{canvas.name} | Sorting Order: {canvas.sortingOrder} | Render Order: {canvas.renderOrder}");
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case "/spawnenemy":
                    if (args.Length == 1) { return; }
                    var enemy = LethalContent.Enemies.Where(x => x.Value.Key.Key.ToLower() == args[1].ToLower()).FirstOrDefault();
                    EnemyVent? vent = RoundManager.Instance.allEnemyVents.GetClosestToPosition(localPlayer.transform.position, (x) => x.transform.position);

                    if (args.Length > 2 && float.TryParse(args[2], out float ventDelay))
                        SpawnEnemy(enemy.Key, vent, ventDelay);
                    else
                        SpawnEnemy(enemy.Key, vent);

                    break;
                case "/refresh":
                    RoundManager.Instance.RefreshEnemiesList();
                    HoarderBugAI.RefreshGrabbableObjectsInMapList();
                    break;
                case "/dungeon":
                    var dInfo = RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.GetDawnInfo();
                    logger.LogInfo($"{dInfo.TypedKey.ToString()} | {RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name}");
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
                case "/debug":
                    if (args.Length == 1) { return; }
                    string text = string.Join(" ", args);
                    text = text.Substring(8);
                    HUDManager.Instance.SetDebugText(text);
                    break;
                case "/unlock":
                    HUDManager.Instance.DisplayTip("SnowyLib", "Unlocking doors");
                    foreach (var tao in UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>())
                    {
                        if (!tao.isBigDoor) { continue; }
                        tao.SetDoorLocalClient(open: true);
                    }
                    foreach (var doorLock in UnityEngine.Object.FindObjectsOfType<DoorLock>())
                    {
                        if (!doorLock.isLocked) { continue; }
                        doorLock.UnlockDoorSyncWithServer();
                    }
                    break;
                case "/nv":
                    if (localPlayerNightVision == null)
                        SetNightVisionEnabled(true);
                    else
                        SetNightVisionEnabled(!localPlayerNightVision.enabled);
                    HUDManager.Instance.DisplayTip("SnowyLib", $"Night vision {(localPlayerNightVision!.enabled ? "enabled" : "disabled")}");
                    break;
                default:
                    break;
            }
        }
    }
}
