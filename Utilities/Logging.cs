using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using static SnowyLib.Plugin;

namespace SnowyLib;

public static partial class Utils
{
    public static void LogChat(string msg, string nameOfUserWhoTyped = "Server")
    {
        HUDManager.Instance.AddChatMessage(msg, nameOfUserWhoTyped);
    }

    public static void LogShaderProperties(Shader shader)
    {
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            Debug.Log(
                $"{i}: {shader.GetPropertyName(i)} - {shader.GetPropertyType(i)}"
            );
        }
    }

    internal static void LogAnimatorParameters(Animator animator)
    {
        foreach (var param in animator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    logger.LogInfo($"{param.name} (Bool) = {animator.GetBool(param.name)}");
                    break;

                case AnimatorControllerParameterType.Float:
                    logger.LogInfo($"{param.name} (Float) = {animator.GetFloat(param.name)}");
                    break;

                case AnimatorControllerParameterType.Int:
                    logger.LogInfo($"{param.name} (Int) = {animator.GetInteger(param.name)}");
                    break;

                case AnimatorControllerParameterType.Trigger:
                    logger.LogInfo($"{param.name} (Trigger)");
                    break;
            }
        }
    }

    internal static void LogRarities(ContentType contentType)
    {
        foreach (var level in StartOfRound.Instance.levels)
        {
            logger.LogInfo($"- {level.name}:");

            switch (contentType)
            {
                case ContentType.Item:
                    foreach (var item in level.spawnableScrap)
                    {
                        logger.LogInfo($"-- {item.spawnableItem.itemName}: {item.rarity}");
                    }
                    break;
                case ContentType.Enemy:
                    foreach (var enemy in level.Enemies)
                    {
                        logger.LogInfo($"-- {enemy.enemyType.name}: {enemy.rarity}");
                    }
                    break;
                case ContentType.MapObject:
                    foreach (var mapObject in level.spawnableMapObjects)
                    {
                        logger.LogInfo($"-- {mapObject.prefabToSpawn.name}:");
                        Debug.Log(string.Join("\n", mapObject.numberToSpawn.keys.Select(k => $"--- ({k.time}, {k.value})")));
                    }
                    break;
                default:
                    break;
            }
        }
    }

    internal static void LogAverageRarities(ContentType contentType)
    {
        switch (contentType)
        {
            case ContentType.Item:
                LogItemAverageRarities();
                break;
            case ContentType.Enemy:
                LogEnemyAverageRarities();
                break;
            default:
                break;
        }
    }

    internal static void LogItemAverageRarities()
    {
        var sortedAveragedRarities = StartOfRound.Instance.levels
            .SelectMany(level => level.spawnableScrap)
            .GroupBy(scrap => scrap.spawnableItem)
            .Select(group => new
            {
                Item = group.Key,
                AverageRarity = (int)group.Average(scrap => scrap.rarity)
            })
            .OrderByDescending(item => item.AverageRarity);

        foreach (var item in sortedAveragedRarities)
        {
            logger.LogInfo($"{item.Item.itemName}: {item.AverageRarity}");
        }
    }

    internal static void LogEnemyAverageRarities()
    {
        var sortedAveragedRarities = StartOfRound.Instance.levels
            .SelectMany(level => level.Enemies)
            .GroupBy(enemy => enemy.enemyType)
            .Select(group => new
            {
                Enemy = group.Key,
                AverageRarity = (int)group.Average(enemy => enemy.rarity)
            })
            .OrderByDescending(enemy => enemy.AverageRarity);

        foreach (var enemy in sortedAveragedRarities)
        {
            logger.LogInfo($"{enemy.Enemy.enemyName}: {enemy.AverageRarity}");
        }
    }

    public static void LogLayerNames(LayerMask layerMask)
    {
        string[] names = GetLayerNames(layerMask);

        logger.LogDebug($"Layer names:");
        foreach (var name in names)
        {
            logger.LogDebug(name);
        }
    }
}