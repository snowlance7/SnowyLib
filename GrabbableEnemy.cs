using GameNetcodeStuff;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static SnowyLib.Plugin;

namespace SnowyLib;


public abstract class GrabbableEnemy : GrabbableObject
{
    public GrabbableEnemyAI ai = null!;
    [HideInInspector] public PlayerControllerB previousPlayerHeldBy = null!;

    public override void EquipItem()
    {
        base.EquipItem();
        ai.PickUpGrabbableEnemyLocalClient();
        previousPlayerHeldBy = playerHeldBy;
    }

    [Rpc(SendTo.NotMe, RequireOwnership = false)]
    public void DropGrabbableEnemyRpc()
    {
        ai.DropGrabbableEnemyLocalClient();
    }

    public override void FallWithCurve()
    {
        if (ai.inSpecialAnimation)
        {
            base.FallWithCurve();
        }
    }

    public override void Start()
    {
        propColliders = base.gameObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < propColliders.Length; i++)
        {
            if (!propColliders[i].CompareTag("DoNotSet") && !propColliders[i].CompareTag("Enemy"))
            {
                propColliders[i].excludeLayers = -2621449;
            }
        }
        originalScale = base.transform.localScale;
        if (itemProperties.itemSpawnsOnGround)
        {
            startFallingPosition = base.transform.position;
            if (base.transform.parent != null)
            {
                startFallingPosition = base.transform.parent.InverseTransformPoint(startFallingPosition);
            }
            FallToGround();
        }
        else
        {
            fallTime = 1f;
            hasHitGround = true;
            reachedFloorTarget = true;
            targetFloorPosition = base.transform.localPosition;
        }
        if (itemProperties.isScrap)
        {
            fallTime = 1f;
            hasHitGround = true;
        }
        if (itemProperties.isScrap && RoundManager.Instance.mapPropsContainer != null)
        {
            radarIcon = GameObject.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
        }
        if (!itemProperties.isScrap)
        {
            HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
        }
        MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int j = 0; j < componentsInChildren.Length; j++)
        {
            componentsInChildren[j].renderingLayerMask = 1u;
        }
        SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int k = 0; k < componentsInChildren2.Length; k++)
        {
            componentsInChildren2[k].renderingLayerMask = 1u;
        }
    }

    public override void Update()
    {
        if (currentUseCooldown >= 0f)
        {
            currentUseCooldown -= Time.deltaTime;
        }
        if (base.IsOwner)
        {
            if (isBeingUsed && itemProperties.requiresBattery)
            {
                if (insertedBattery.charge > 0f)
                {
                    if (!itemProperties.itemIsTrigger)
                    {
                        insertedBattery.charge -= Time.deltaTime / itemProperties.batteryUsage;
                    }
                }
                else if (!insertedBattery.empty)
                {
                    insertedBattery.empty = true;
                    if (isBeingUsed)
                    {
                        Debug.Log("Use up batteries local");
                        isBeingUsed = false;
                        UseUpBatteries();
                        UseUpItemBatteriesServerRpc();
                    }
                }
            }
            if (!wasOwnerLastFrame)
            {
                wasOwnerLastFrame = true;
            }
        }
        else if (wasOwnerLastFrame)
        {
            wasOwnerLastFrame = false;
        }
        if (!isHeld && parentObject == null)
        {
            if (fallTime < 1f)
            {
                reachedFloorTarget = false;
                FallWithCurve();
                if (base.transform.localPosition.y - targetFloorPosition.y < 0.05f && !hasHitGround)
                {
                    PlayDropSFX();
                    OnHitGround();
                }
                return;
            }
            if (!reachedFloorTarget)
            {
                if (!hasHitGround)
                {
                    PlayDropSFX();
                    OnHitGround();
                }
                reachedFloorTarget = true;
                if (floorYRot == -1)
                {
                    base.transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z);
                }
                else
                {
                    base.transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z);
                }
            }
            if (ai.inSpecialAnimation)
            {
                base.transform.localPosition = targetFloorPosition;
            }
        }
        else if (isHeld || isHeldByEnemy)
        {
            reachedFloorTarget = false;
        }
    }

    public override void LateUpdate()
    {
        if (ai.inSpecialAnimation && parentObject != null)
        {
            base.transform.rotation = parentObject.rotation;
            base.transform.Rotate(itemProperties.rotationOffset);
            base.transform.position = parentObject.position;
            Vector3 positionOffset = itemProperties.positionOffset;
            positionOffset = parentObject.rotation * positionOffset;
            base.transform.position += positionOffset;
        }
        if (radarIcon != null)
        {
            radarIcon.position = base.transform.position;
        }
    }

    public override void EnableItemMeshes(bool enable)
    {
        MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger") && !componentsInChildren[i].gameObject.CompareTag("Enemy"))
            {
                componentsInChildren[i].enabled = enable;
            }
        }
        SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int j = 0; j < componentsInChildren2.Length; j++)
        {
            componentsInChildren2[j].enabled = enable;
            Debug.Log("DISABLING/ENABLING SKINNEDMESH: " + componentsInChildren2[j].gameObject.name);
        }
    }

    public override void DiscardItem()
    {
        ai.DropGrabbableEnemyLocalClient();
        DropGrabbableEnemyRpc();
        previousPlayerHeldBy = playerHeldBy;
        base.DiscardItem();
    }
}

public abstract class GrabbableEnemyAI : EnemyAI
{
    public GrabbableEnemy propScript = null!;

    bool wasOutsideLastFrame;

    [HideInInspector] public bool holdingGrabbableEnemy;

    [HideInInspector] public PlayerControllerB? playerHolding;

    Coroutine? dropGrabbableEnemyCoroutine;

    public void PickUpGrabbableEnemyLocalClient()
    {
        currentOwnershipOnThisClient = (int)propScript.playerHeldBy.playerClientId;
        inSpecialAnimation = true;
        agent.enabled = false;
        holdingGrabbableEnemy = true;
        if (dropGrabbableEnemyCoroutine != null)
        {
            StopCoroutine(dropGrabbableEnemyCoroutine);
        }
        playerHolding = propScript.playerHeldBy;
    }

    public void DropGrabbableEnemyLocalClient()
    {
        holdingGrabbableEnemy = false;
        playerHolding = null;
        if (base.IsOwner)
        {
            ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
        }
        serverPosition = base.transform.position;
        bool flag = true;
        Vector3 itemFloorPosition = propScript.GetItemFloorPosition(propScript.previousPlayerHeldBy.transform.position + Vector3.up * 0.5f);
        Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(itemFloorPosition, default(NavMeshHit), 10f);
        if (!RoundManager.Instance.GotNavMeshPositionResult || DebugEnemy)
        {
            flag = false;
            itemFloorPosition = propScript.startFallingPosition;
            if (propScript.transform.parent != null)
            {
                itemFloorPosition = propScript.transform.parent.TransformPoint(propScript.startFallingPosition);
            }
            Transform transform = ChooseClosestNodeToPositionNoPathCheck(itemFloorPosition);
            navMeshPosition = RoundManager.Instance.GetNavMeshPosition(transform.transform.position);
        }
        if (propScript.transform.parent == null)
        {
            propScript.targetFloorPosition = navMeshPosition;
        }
        else
        {
            propScript.targetFloorPosition = propScript.transform.parent.InverseTransformPoint(navMeshPosition);
        }
        if (flag)
        {
            if (dropGrabbableEnemyCoroutine != null)
            {
                StopCoroutine(dropGrabbableEnemyCoroutine);
            }
            dropGrabbableEnemyCoroutine = StartCoroutine(DropGrabbableEnemyAnimation(navMeshPosition));
        }
        else
        {
            base.transform.position = navMeshPosition;
            inSpecialAnimation = false;
        }
    }

    public Transform ChooseClosestNodeToPositionNoPathCheck(Vector3 pos)
    {
        Array.Sort(allAINodes, (GameObject a, GameObject b) => (pos - a.transform.position).sqrMagnitude.CompareTo((pos - b.transform.position).sqrMagnitude));
        return allAINodes[0].transform;
    }

    private IEnumerator DropGrabbableEnemyAnimation(Vector3 dropOnPosition)
    {
        float time = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => propScript.reachedFloorTarget || Time.realtimeSinceStartup - time > 2f);
        base.transform.position = dropOnPosition;
        inSpecialAnimation = false;
        if (base.IsServer && propScript.previousPlayerHeldBy != null && propScript.previousPlayerHeldBy.isPlayerDead)
        {
            if (propScript.previousPlayerHeldBy == localPlayer)
            {
                propScript.previousPlayerHeldBy.DiscardHeldObject();
            }
        }
        dropGrabbableEnemyCoroutine = null;
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead || StartOfRound.Instance.livingPlayers == 0)
        {
            return;
        }
        if (!base.IsOwner && ((!holdingGrabbableEnemy && dropGrabbableEnemyCoroutine == null)))
        {
            base.transform.localEulerAngles = new Vector3(0f, base.transform.localEulerAngles.y, 0f);
        }
        if (wasOutsideLastFrame && base.transform.position.y < -80f)
        {
            wasOutsideLastFrame = false;
            SetEnemyOutside();
        }
        else if (!wasOutsideLastFrame && base.transform.position.y > -80f)
        {
            wasOutsideLastFrame = true;
            SetEnemyOutside(outside: true);
        }
        if (StartOfRound.Instance.shipIsLeaving)
        {
            propScript.grabbable = false;
            if (playerHolding != null)
            {
                playerHolding.DiscardHeldObject();
            }
        }
        else if (!inSpecialAnimation)
        {
            propScript.grabbable = true;
        }
    }
}