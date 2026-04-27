using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static SnowyLib.Plugin;
using static SnowyLib.StatusEffectController;

namespace SnowyLib
{
    public class StatusEffectController : NetworkBehaviour
    {
        public VignetteOverlay vignetteOverlay = null!;

        public static StatusEffectController Instance = null!;

        public PlayerControllerB playerAttachedTo = null!;

        readonly List<StatusEffect> effects = new();

        public enum ConflictResult
        {
            Allow,
            Replace,
            Deny
        }

        public static void Init(PlayerControllerB player)
        {
            StatusEffectController _instance = GameObject.Instantiate(SnowyLibContentHandler.Instance.StatusEffectController!.StatusEffectControllerPrefab, player.transform).GetComponent<StatusEffectController>();

            if (IsServerOrHost)
                _instance.NetworkObject.Spawn();

            _instance.playerAttachedTo = player;

            if (player == localPlayer)
                StatusEffectController.Instance = _instance;
        }

        void Update()
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                effect.Tick();

                if (effect.isFinished || (playerAttachedTo.isPlayerDead && effect.removeOnDeath))
                {
                    effect.OnRemove();
                    effects.RemoveAt(i);
                }
            }
        }

        public void ApplyEffect(StatusEffect newEffect)
        {
            var existing = !string.IsNullOrEmpty(newEffect.id)
                ? effects.FirstOrDefault(e => e.id == newEffect.id)
                : null;

            if (existing != null)
            {
                switch (newEffect.onConflict(existing, newEffect))
                {
                    case ConflictResult.Allow:
                        break;
                    case ConflictResult.Replace:
                        existing.OnRemove();
                        effects.Remove(existing);
                        break;
                    case ConflictResult.Deny:
                        return;
                    default:
                        return;
                }
            }

            newEffect.OnApply();
            effects.Add(newEffect);
        }

        public void RemoveEffect<T>() where T : StatusEffect
        {
            effects.RemoveAll(e =>
            {
                if (e is T effect)
                {
                    effect.OnRemove();
                    return true;
                }
                return false;
            });
        }

        public void RemoveEffect(StatusEffect effect)
        {
            effect.OnRemove();
            effects.Remove(effect);
        }

        public void RemoveEffect(Func<StatusEffect, bool> predicate)
        {
            effects.RemoveAll(e =>
            {
                if (predicate(e))
                {
                    e.OnRemove();
                    return true;
                }
                return false;
            });
        }

        public bool HasEffect<T>() where T : StatusEffect
        {
            return effects.Any(e => e is T);
        }

        public void ClearAll()
        {
            foreach (var effect in effects)
                effect.OnRemove();

            effects.Clear();
        }
    }

    public abstract class StatusEffect(string source, string id, float duration, bool removeOnDeath, bool pauseInOrbit, Func<StatusEffect, StatusEffect, ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true)
    {
        protected StatusEffectController controller => StatusEffectController.Instance;

        public string source = source;
        public string id = id;
        public float duration = duration;
        public bool removeOnDeath = removeOnDeath;
        public bool pauseInOrbit = pauseInOrbit;
        public Action? onRemove = onRemove;
        public bool curableBySCP500 = curableBySCP500;

        // Takes (existingEffect, newEffect) → returns bool
        // Default: no conflict (always false)
        public Func<StatusEffect, StatusEffect, ConflictResult> onConflict = onConflict ?? ((existing, incoming) => ConflictResult.Deny);

        protected float elapsedTime;

        public bool isFinished => duration > 0 && elapsedTime >= duration;
        public float timeLeft => duration > 0 ? duration - elapsedTime : Mathf.Infinity;

        public void Tick()
        {
            OnTick();

            if (duration > 0 && !(pauseInOrbit && StartOfRound.Instance.inShipPhase))
                elapsedTime += Time.deltaTime;
        }

        public virtual void OnApply() { }
        public virtual void OnTick() { }
        public void Remove()
        {
            onRemove?.Invoke();
            controller?.RemoveEffect(this);
        }
        public virtual void OnRemove() { }
    }

    public class VignetteOverlay : MonoBehaviour
    {
#pragma warning disable CS8618
        public Image visual;
        Material material;
#pragma warning restore CS8618

        static readonly int InsetId = Shader.PropertyToID("_Inset");

        public float intensityDecreasePerSecond = 0.05f;

        float currentIntensity;

        void Awake()
        {
            material = visual.material;
        }

        void Update()
        {
            if (currentIntensity <= 0f) return;

            currentIntensity = Mathf.Max(0f,
                currentIntensity - intensityDecreasePerSecond * Time.deltaTime);

            material.SetFloat(InsetId, currentIntensity);
        }

        public void SetIntensity(float intensity)
        {
            currentIntensity = Mathf.Clamp01(intensity);
            material.SetFloat(InsetId, currentIntensity);
        }
    }

    [HarmonyPatch]
    public class StatusEffectControllerPatches
    {

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        public static void ConnectClientToPlayerObjectPostfix(PlayerControllerB __instance)
        {
            try
            {
                StatusEffectController.Init(__instance);
            }
            catch
            {
                return;
            }
        }
    }

    /* bodyparts
     * 0 head
     * 1 right arm
     * 2 left arm
     * 3 right leg
     * 4 left leg
     * 5 chest
     * 6 feet
     * 7 right hip
     * 8 crotch
     * 9 left shoulder
     * 10 right shoulder */

    //localPlayer.sprintMeter 0-1
    //localPlayer.sprintTime 11, idk what this does
    //localPlayer.sprintMultiplier 1-2.5, controls sprint speed

    /*ShortFallLanding (Trigger) - coughing small motion
    SpawnPlayer (Trigger) - puking
    startCrouching (Trigger) - force crouch, specialanimation time for duration
    Damage (Trigger) - hands in air
    Overheat (Trigger) - hands in air lower
    SA_Typing (Trigger) - puking motion, head forward?
    SA_stopAnimation (Trigger)
    SA_ChargeItem (Trigger) - hand out
    SA_PushLeverBack (Trigger) - forces screen to middle and does quick animation*/

    public class RandomIntervalActionEffect(BoundedRange randomInterval, Action action, string source, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        BoundedRange randomInterval = randomInterval;
        Action action = action;

        float timeSinceLastInterval;
        float nextInterval;

        public override void OnApply()
        {
            nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override void OnTick()
        {
            timeSinceLastInterval += Time.deltaTime;

            if (timeSinceLastInterval > nextInterval)
            {
                timeSinceLastInterval = 0f;
                nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);

                action.Invoke();
            }
        }
    }

    public class IntervalActionEffect(float interval, Action action, string source, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        float interval = interval;
        Action action = action;

        float timeSinceLastInterval;

        public override void OnTick()
        {
            timeSinceLastInterval += Time.deltaTime;

            if (timeSinceLastInterval > interval)
            {
                timeSinceLastInterval = 0f;
                action.Invoke();
            }
        }
    }

    public class OnRemoveActionEffect(Action action, string source, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, null, curableBySCP500)
    {
        Action action = action;

        public override void OnRemove()
        {
            action.Invoke();
        }
    }

    public class TickActionEffect(Action action, string source, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        Action action = action;

        public override void OnTick()
        {
            action.Invoke();
        }
    }

    public class ChanceTickActionEffect(float chancePerSecond, Action action, string source, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        float chance = chancePerSecond;
        Action action = action;

        public override void OnTick()
        {
            if (Utils.randomLocal.NextFloat(0f, 1f) < Mathf.Clamp01(chance) * Time.deltaTime)
                action.Invoke();
        }
    }

    public class ConditionalActionEffect(Func<bool> condition, Action action, bool removeOnTrigger, string source, float cooldown = 0f, int maxTriggerCount = 0, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        Func<bool> condition = condition;
        Action action = action;
        bool removeOnTrigger = removeOnTrigger;
        float cooldown = cooldown;
        int maxTriggerCount = maxTriggerCount;

        float timeSinceLastTrigger;
        int triggerCount;

        public override void OnTick()
        {
            timeSinceLastTrigger += Time.deltaTime;

            if (condition() && timeSinceLastTrigger > cooldown)
            {
                timeSinceLastTrigger = 0f;
                triggerCount++;
                action.Invoke();

                if (removeOnTrigger || (maxTriggerCount > 0 && triggerCount >= maxTriggerCount))
                    Remove();
            }
        }
    }

    public class LerpValueEffect(Action<float> setter, float startValue, float endValue, float duration, string source, string id = "", bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        Action<float> setter = setter;

        float startValue = startValue;
        float endValue = endValue;

        public override void OnApply()
        {
            setter.Invoke(startValue);
        }

        public override void OnTick()
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            float value = Mathf.Lerp(startValue, endValue, t);

            setter.Invoke(value);
        }

        public override void OnRemove()
        {
            setter.Invoke(endValue);
        }
    }

    public class RandomIntervalPhaseActionEffect(BoundedRange randomInterval, BoundedRange randomPhaseDuration, Action action, string source, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        BoundedRange randomInterval = randomInterval;
        BoundedRange randomPhaseDuration = randomPhaseDuration;
        Action action = action;

        float timeSinceLastInterval;
        float nextInterval;

        float phaseTimer;

        public override void OnApply()
        {
            nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override void OnTick()
        {
            if (phaseTimer <= 0)
                timeSinceLastInterval += Time.deltaTime;

            if (timeSinceLastInterval > nextInterval)
            {
                timeSinceLastInterval = 0f;
                nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
                phaseTimer = randomPhaseDuration.GetRandomInRange(Utils.randomLocal);
            }

            if (phaseTimer > 0)
            {
                phaseTimer -= Time.deltaTime;
                action.Invoke();
            }
        }
    }

    public class CurveValueEffect(Action<float> setter, AnimationCurve curve, float duration, string source, string id = "", bool removeOnDeath = true, bool pauseInOrbit = true, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action? onRemove = null, bool curableBySCP500 = true) : StatusEffect(source, id, duration, removeOnDeath, pauseInOrbit, onConflict, onRemove, curableBySCP500)
    {
        Action<float> setter = setter;
        AnimationCurve curve = curve;

        public override void OnApply()
        {
            // Start at the beginning of the curve
            setter.Invoke(curve.Evaluate(0f));
        }

        public override void OnTick()
        {
            if (duration <= 0) return;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            float value = curve.Evaluate(t);
            setter.Invoke(value);
        }

        public override void OnRemove()
        {
            setter.Invoke(curve.Evaluate(1f));
        }
    }
}
