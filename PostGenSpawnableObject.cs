using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib;

public class PostGenSpawnableObject : IEquatable<PostGenSpawnableObject>
{
    public static List<PostGenSpawnableObject> RegisteredObjects { get; private set; } = new List<PostGenSpawnableObject>();

    public string Name => prefab.name;
    public GameObject prefab;
    public Func<int> spawnAmount;
    public Func<SpawnDefinition> spawnDefinition;

    public PostGenSpawnableObject(GameObject prefab, Func<int> spawnAmount, Func<SpawnDefinition> spawnDefinition)
    {
        this.prefab = prefab;
        this.spawnAmount = spawnAmount;
        this.spawnDefinition = spawnDefinition;
    }

    public static void Register(PostGenSpawnableObject postGenSpawnableObject)
    {
        if (RegisteredObjects.Contains(postGenSpawnableObject)) { logger.LogError($"Failed to register PostGenSpawnableObject ({postGenSpawnableObject.Name}). A PostGenSpawnableObject with the same prefab already exists."); return; }
        RegisteredObjects.Add(postGenSpawnableObject);
    }

    [OnFinishGeneratingLevel]
    internal static void Generate() // TODO: Add logs
    {
        if (!IsServerOrHost) { return; }

        foreach (var obj in RegisteredObjects)
        {
            int spawnAmount = obj.spawnAmount.Invoke();
            logger.LogDebug($"Spawning {spawnAmount} PostGenSpawnableObjects");

            for (int i = 0; i < spawnAmount; i++)
            {
                SpawnDefinition spawnDefinition = obj.spawnDefinition.Invoke();

                GameObject spawnedObj = GameObject.Instantiate(obj.prefab, spawnDefinition.position, spawnDefinition.rotation, spawnDefinition.parentTo);
                var netObj = spawnedObj.GetComponent<NetworkObject>();
                netObj.Spawn(destroyWithScene: spawnDefinition.destroyWithScene);
                if (spawnDefinition.postSpawnAction != null) netObj.DoActionAfterSpawn(spawnDefinition.postSpawnAction);
            }
        }
    }

    public bool Equals(PostGenSpawnableObject other)
    {
        return prefab == other.prefab;
    }
}