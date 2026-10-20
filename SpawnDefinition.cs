using System;
using Unity.Netcode;
using UnityEngine;

namespace SnowyLib;

public class SpawnDefinition(Vector3 position, Quaternion rotation, Transform? parentTo = null, bool destroyWithScene = false, Action<NetworkObject>? postSpawnAction = null)
{
    public Vector3 position = position;
    public Quaternion rotation = rotation;
    public Transform? parentTo = parentTo;
    public bool destroyWithScene = destroyWithScene;
    public Action<NetworkObject>? postSpawnAction = postSpawnAction;
}