using Dawn;
using Dawn.Utils;
using Dusk;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static Dawn.MapObjectInfoBuilder;
using static SnowyLib.Plugin;

namespace SnowyLib;

[CreateAssetMenu(fileName = "FaunaDef", menuName = "SnowyLib/Spawnable Fauna Definition", order = 0)]
public class SpawnableFaunaDefinition// : DuskMapObjectDefinition // TODO
{
    [SerializeField]
    public bool CanSpawnOnGround { get; private set; } = true;

    [SerializeField]
    public bool CanSpawnOnWalls { get; private set; } = false;

    [SerializeField]
    public bool CanSpawnOnTrees { get; private set; } = false;

    [field: Header("Group Spawning")]
    [SerializeField]
    public bool SpawnInGroups { get; private set; } = false;

    [SerializeField]
    public BoundedRange GroupSpawnAmount { get; private set; } = new BoundedRange(0, 0);

    [SerializeField]
    public float GroupMinimumSpawnDistance { get; private set; }

    [OnFinishGeneratingLevel]
    internal static void GenerateFauna()
    {
        // TODO // UPDATE
    }
}

public class SpawnableFauna : SpawnableMapObject
{
    public bool canSpawnOnGround = true;

    public bool canSpawnOnWalls = false;

    public bool canSpawnOnTrees = false;

    public bool spawnInGroups = false;

    public BoundedRange groupSpawnAmount = new BoundedRange(0, 0);

    public float groupMinimumSpawnDistance;
}

[HarmonyPatch]
internal static class SpawnableFaunaPatches
{
    /*[HarmonyPostfix]
    [HarmonyPatch(typeof(OutsideBuilder), "Build")]
    private static bool OutsideBuilder_Build_Postfix(OutsideBuilder __instance, ref DawnInsideMapObjectInfo __result)
    {
        try
        {
            //if ()
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return true;
        }
    }*/
}