using Dawn;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SnowyLib.Plugin;

namespace SnowyLib;

public static class AttachableObjectManager
{
    public static List<AttachmentPoint> AvailableAttachmentPoints { get; } = new();

    public static AutoDictionary<PlayerControllerB, List<AttachmentPoint>> PlayerAttachmentPoints = new(player => AvailableAttachmentPoints.ToList());

    [StaticInit]
    public static void Init()
    {
        AddPoint(0, new Vector3(0, 0.365f, 0.002f), new Vector3(0, 0, 0));
        AddPoint(0, new Vector3(-0.1028f, 0.3071f, 0.0856f), new Vector3(-0.245f, 0.815f, 33.496f));
        AddPoint(0, new Vector3(0.138f, 0.3f, 0f), new Vector3(0, 0, -60.948f));
        AddPoint(0, new Vector3(0.1094f, 0f, 0f), new Vector3(0, 0, -60.948f));
        AddPoint(0, new Vector3(0.096f, 0.0902f, -0.0783f), new Vector3(-11.136f, 44.702f, -73.178f));
        AddPoint(0, new Vector3(0.0431f, 0.2706f, -0.1359f), new Vector3(-11.246f, 38.263f, -73.052f));
        AddPoint(0, new Vector3(-0.079f, 0.216f, -0.135f), new Vector3(-9.79f, 101.624f, -73.593f));
        AddPoint(0, new Vector3(-0.143f, 0.273f, -0.04f), new Vector3(-8.845f, 147.146f, -72.937f));
        AddPoint(0, new Vector3(-0.095f, 0.022f, -0.034f), new Vector3(-9.038f, 135.199f, -73.18f));
        AddPoint(1, new Vector3(-0.03f, -0.059f, -0.083f), new Vector3(-71.716f, 44.6f, -33.023f));
        AddPoint(1, new Vector3(0.0334f, -0.0434f, 0.0661f), new Vector3(-57.89f, -5.879f, -119.005f));
        AddPoint(1, new Vector3(-0.075f, 0.136f, 0.017f), new Vector3(-49.812f, 192.527f, -88.63f));
        AddPoint(1, new Vector3(0.048f, 0.14f, 0.004f), new Vector3(-27.101f, 193.738f, -270.77f));
        AddPoint(1, new Vector3(-0.026f, 0.131f, -0.051f), new Vector3(-16.438f, 306.002f, -303.578f));
        AddPoint(2, new Vector3(-0.008f, 0.042f, -0.069f), new Vector3(-18.538f, -70.192f, 104.316f));
        AddPoint(2, new Vector3(-0.038f, 0.167f, 0.065f), new Vector3(30.865f, 23.854f, 57.64f));
        AddPoint(2, new Vector3(-0.035f, 0.019f, 0.089f), new Vector3(26.452f, 226.07f, -112.116f));
        AddPoint(2, new Vector3(0.051f, 0.157f, -0.035f), new Vector3(-19.561f, 214.95f, 70.289f));
        AddPoint(3, new Vector3(0.089f, 0.009f, 0.02f), new Vector3(-35.165f, -24.071f, -92.253f));
        AddPoint(3, new Vector3(0.0282f, 0.2983f, -0.0863f), new Vector3(-50.138f, -293.786f, -117.426f));
        AddPoint(3, new Vector3(-0.0697f, 0.012f, 0.0797f), new Vector3(37.579f, -123.803f, -88.869f));
        AddPoint(3, new Vector3(-0.0672f, 0.1735f, -0.0732f), new Vector3(-10.263f, -223.565f, -125.123f));
        AddPoint(3, new Vector3(0.083f, 0.181f, -0.038f), new Vector3(-172.115f, -169.928f, -279.59f));
        AddPoint(3, new Vector3(0.0478f, 0.2481f, 0.0887f), new Vector3(-176.204f, -257.097f, -235.701f));
        AddPoint(3, new Vector3(-0.07f, 0.236f, 0.062f), new Vector3(-141.741f, -381.508f, -218.368f));
        AddPoint(4, new Vector3(-0.054f, -0.012f, -0.056f), new Vector3(-37.21f, -84.346f, 100.846f));
        AddPoint(4, new Vector3(-0.073f, 0.09f, -0.023f), new Vector3(-62.196f, -0.96f, 63.314f));
        AddPoint(4, new Vector3(0.053f, 0.155f, -0.081f), new Vector3(-101.773f, -126.592f, 86.861f));
        AddPoint(4, new Vector3(-0.0237f, 0.2073f, 0.1117f), new Vector3(-97.917f, -287.48f, 112.384f));
        AddPoint(5, new Vector3(-0.156f, 0.002f, 0.115f), new Vector3(63.455f, -365.208f, 26.286f));
        AddPoint(5, new Vector3(0.002f, 0.0025f, 0.1483f), new Vector3(-7.833f, -423.314f, -131.667f));
        AddPoint(5, new Vector3(0.216f, 0.31f, 0.108f), new Vector3(22.333f, -406.705f, -68.389f));
        AddPoint(5, new Vector3(-0.26f, 0.4082f, 0.0569f), new Vector3(30.392f, -462.337f, -40.781f));
        AddPoint(5, new Vector3(-0.2428f, -0.0008f, 0.0117f), new Vector3(64.106f, -374.719f, 93.005f));
        AddPoint(5, new Vector3(0.118f, 0.002f, 0.115f), new Vector3(56.17f, -212.746f, 73.226f));
        AddPoint(5, new Vector3(0.0688f, 0.16f, 0.161f), new Vector3(14.626f, -271.287f, 84.833f));
        AddPoint(5, new Vector3(0.2411f, 0.121f, -0.25f), new Vector3(-2.129f, -153.411f, 116.528f));
        AddPoint(5, new Vector3(-0.242f, 0.121f, -0.25f), new Vector3(65.686f, -12.508f, 102.029f));
        AddPoint(5, new Vector3(-0.061f, 0.12f, -0.339f), new Vector3(1.305f, -116.493f, 81.985f));
        AddPoint(5, new Vector3(-0.153f, 0.295f, -0.342f), new Vector3(23.736f, -33.542f, 116.857f));
        AddPoint(5, new Vector3(0.071f, 0.295f, -0.342f), new Vector3(-40.673f, -89.174f, 61.959f));
        AddPoint(5, new Vector3(0.071f, 0.4919f, -0.3132f), new Vector3(-22.328f, 68.897f, -9.012f));
        AddPoint(5, new Vector3(-0.189f, 0.492f, -0.313f), new Vector3(-59.425f, -114.368f, 110.685f));
        AddPoint(7, new Vector3(0.086f, 0.009f, 0.022f), new Vector3(-20.337f, -362.786f, 261.855f));
        AddPoint(7, new Vector3(0.049f, -0.004f, -0.102f), new Vector3(6.561f, -250.508f, 308.16f));
        AddPoint(7, new Vector3(-0.232f, -0.032f, -0.174f), new Vector3(23.031f, -333.859f, 241.566f));
        AddPoint(7, new Vector3(-0.03f, 0.017f, 0.114f), new Vector3(-35.427f, -304.178f, 98.455f));
        AddPoint(8, new Vector3(0.1021f, 0.1404f, 0.1034f), new Vector3(-38.722f, -226.187f, 64.424f));
        AddPoint(8, new Vector3(-0.0661f, -0.0196f, 0.0773f), new Vector3(-35.231f, -303.833f, 54.792f));
        AddPoint(8, new Vector3(-0.032f, 0.156f, -0.101f), new Vector3(-11.888f, -396.463f, 34.612f));
        AddPoint(8, new Vector3(0.092931f, 0.1944469f, 0.01660633f), new Vector3(92.15399f, -306.724f, 271.902f));
        AddPoint(8, new Vector3(-0.0823f, 0.0683f, -0.0716f), new Vector3(-0.41f, -363.386f, 120.789f));
    }

    private static void AddPoint(int bodyPartIndex, Vector3 position, Vector3 rotation)
    {
        AvailableAttachmentPoints.Add(new AttachmentPoint(bodyPartIndex, position, rotation));
    }

    public static AttachmentPoint? FindAvailablePoint(PlayerControllerB player)
    {
        return PlayerAttachmentPoints[player].Where(x => !x.IsOccupied).GetRandom();
    }

    public static bool TrySpawnItemOnPlayer(NamespacedKey<DawnItemInfo> key, PlayerControllerB player)
    {
        var attachmentPoint = FindAvailablePoint(player);
        if (attachmentPoint == null) { return false; }
        Utils.SpawnItem(key, Vector3.zero, actionAfterSpawn: (item) =>
        {
            AttachableObject? attachable = item as AttachableObject;
            if (attachable == null) { logger.LogError($"Failed to attach {item.name} to {player.playerUsername}, {item.name} is not an AttachableObject"); return; }
            attachable.SetPlayerAttachedTo(player, attachmentPoint);
        });

        return true;
    }
}

public class AttachmentPoint(int bodyPartIndex, Vector3 positionOffset, Vector3 rotationOffset)
{
    public int bodyPartIndex = bodyPartIndex;

    public Vector3 positionOffset = positionOffset;
    public Vector3 rotationOffset = rotationOffset;

    public AttachableObject? occupant = null;

    public bool IsOccupied => occupant != null;
}

public abstract class AttachableObject : PhysicsProp
{
    public PlayerControllerB? playerAttachedTo;
    public AttachmentPoint? attachmentPoint;

    public override void LateUpdate()
    {
        if (playerAttachedTo == null || attachmentPoint == null)
        {
            base.LateUpdate();
            return;
        }

        var attachedTo = playerAttachedTo.bodyParts[attachmentPoint.bodyPartIndex];

        base.transform.rotation = attachedTo.rotation;
        base.transform.Rotate(attachmentPoint.rotationOffset);
        base.transform.position = attachedTo.position;
        Vector3 _positionOffset = attachmentPoint.positionOffset;
        _positionOffset = attachedTo.rotation * _positionOffset;
        base.transform.position += _positionOffset;

        if (rotateObject)
        {
            base.transform.Rotate(new Vector3(0f, Time.deltaTime * 60f, 0f), Space.World);
        }
        if (radarIcon != null)
        {
            radarIcon.position = base.transform.position;
        }
    }

    public override void EquipItem()
    {
        Detach();
        base.EquipItem();
    }

    public override void OnDestroy()
    {
        Detach();
        base.OnDestroy();
    }

    public void SetPlayerAttachedTo(PlayerControllerB player, AttachmentPoint attachmentPoint)
    {
        SetPlayerAttachedToRpc(player.actualClientId, AttachableObjectManager.PlayerAttachmentPoints[player].IndexOf(attachmentPoint));
    }

    public void Detach()
    {
        if (attachmentPoint == null && playerAttachedTo == null) { return; }
        DetachRpc();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void DetachRpc()
    {
        if (attachmentPoint != null)
            attachmentPoint.occupant = null;
        attachmentPoint = null;

        if (playerAttachedTo == localPlayer)
        {
            EnablePhysics(true);
            EnableItemMeshes(true);
        }

        playerAttachedTo = null;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void SetPlayerAttachedToRpc(ulong clientId, int attachmentPointIndex)
    {
        playerAttachedTo = PlayerFromId(clientId);
        if (playerAttachedTo == null) { logger.LogError("SetPlayerAttachedToRpc: Failed to get player from clientId"); return; }

        if (playerAttachedTo == localPlayer)
        {
            EnablePhysics(false);
            EnableItemMeshes(false);
        }

        attachmentPoint = AttachableObjectManager.PlayerAttachmentPoints[playerAttachedTo][attachmentPointIndex];
        attachmentPoint.occupant = this;
    }
}
