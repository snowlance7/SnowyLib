using Dawn.Utils;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SnowyLib.StatusEffectController;

namespace SnowyLib
{
    /// <summary>
    /// Manages and applies status effects to a player or enemy.
    /// </summary>
    /// <remarks>Handles the application, removal, and updating of status effects, ensuring proper conflict
    /// resolution and cleanup on destruction.</remarks>
    public class StatusEffectController : MonoBehaviour
    {
        public List<StatusEffect> effects { get; private set; } = new List<StatusEffect>();
        public PlayerControllerB? player => gameObject.GetComponent<PlayerControllerB>();
        public EnemyAI? enemy => gameObject.GetComponent<EnemyAI>();

        public enum ConflictResult
        {
            Allow,
            Replace,
            Deny
        }

        private void OnDestroy()
        {
            foreach (var effect in effects)
            {
                effect.OnRemove();
            }
        }

        private void Update()
        {
            foreach (var effect in effects.ToList())
            {
                effect.Tick();

                if (effect.timeExpired
                    || (player != null && player.isPlayerDead && effect.removeOnDeath)
                    || (enemy != null && enemy.isEnemyDead && effect.removeOnDeath))
                {
                    effect.OnRemove();
                    effects.Remove(effect);
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

            newEffect.controller = this;
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

    /// <summary>
    /// Represents an abstract status effect applied to an entity, supporting duration, conflict resolution, and removal
    /// behaviors.
    /// </summary>
    /// <param name="source">The origin of the status effect.</param>
    /// <param name="id">The unique identifier for the status effect.</param>
    /// <param name="duration">The duration of the status effect in seconds.</param>
    /// <param name="onConflict">A delegate that determines the outcome when conflicting status effects are applied.</param>
    /// <param name="onRemove">A delegate invoked when the status effect is removed.</param>
    /// <param name="removeOnDeath">Indicates whether the effect is removed when the source entity dies.</param>
    /// <param name="pauseInOrbit">Indicates whether the effect pauses while the entity is in orbit.</param>
    /// <param name="curable">Indicates whether the status effect can be cured.</param>
    public abstract class StatusEffect(string source, string id, float duration, Func<StatusEffect, StatusEffect, ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true)
    {
        public StatusEffectController? controller;

        public string source = source;
        public string id = id;
        public float duration = duration;
        public Func<StatusEffect, StatusEffect, ConflictResult> onConflict = onConflict ?? ((existing, incoming) => ConflictResult.Deny);
        public Action<StatusEffect>? onRemove = onRemove;
        public bool removeOnDeath = removeOnDeath;
        public bool pauseInOrbit = pauseInOrbit;
        public bool curable = curable;


        protected float elapsedTime;

        public bool timeExpired => duration > 0 && elapsedTime >= duration;
        public float timeLeft => duration > 0 ? duration - elapsedTime : Mathf.Infinity;

        public void Tick()
        {
            OnTick();

            if (duration > 0 && !(pauseInOrbit && (StartOfRound.Instance.inShipPhase && !Utils.inTestRoom)))
                elapsedTime += Time.deltaTime;
        }

        public virtual void OnApply() { }
        public virtual void OnTick() { }
        public void Remove()
        {
            controller?.RemoveEffect(this);
        }
        public virtual void OnRemove()
        {
            onRemove?.Invoke(this);
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

    /// <summary>
    /// Applies a status effect that executes a specified action at random intervals within a defined range.
    /// </summary>
    /// <param name="randomInterval">The range specifying the minimum and maximum interval between action executions.</param>
    /// <param name="action">The action to invoke at each random interval.</param>
    /// <param name="source">The identifier for the source of the effect.</param>
    /// <param name="id">The optional unique identifier for the effect instance.</param>
    /// <param name="duration">The duration in seconds for which the effect remains active.</param>
    /// <param name="onConflict">A function to resolve conflicts with other status effects.</param>
    /// <param name="onRemove">An action to perform when the effect is removed.</param>
    /// <param name="removeOnDeath">true to remove the effect when the entity dies; otherwise, false.</param>
    /// <param name="pauseInOrbit">true to pause the effect when the entity is in orbit; otherwise, false.</param>
    /// <param name="curable">true if the effect can be cured; otherwise, false.</param>
    public class RandomIntervalActionEffect(BoundedRange randomInterval, Action action, string source, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
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

    /// <summary>
    /// Applies a status effect that executes a specified action at regular intervals for a set duration.
    /// </summary>
    /// <param name="interval">The interval in seconds between each action execution.</param>
    /// <param name="action">The action to execute at each interval.</param>
    /// <param name="source">The source identifier for the status effect.</param>
    /// <param name="id">An optional unique identifier for the status effect.</param>
    /// <param name="duration">The total duration in seconds for which the status effect remains active.</param>
    /// <param name="onConflict">A function to handle conflicts with other status effects.</param>
    /// <param name="onRemove">An action to execute when the status effect is removed.</param>
    /// <param name="removeOnDeath">true to remove the effect when the target dies; otherwise, false.</param>
    /// <param name="pauseInOrbit">true to pause the effect when the target is in orbit; otherwise, false.</param>
    /// <param name="curable">true if the effect can be cured; otherwise, false.</param>
    public class IntervalActionEffect(float interval, Action action, string source, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
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

    /// <summary>
    /// Represents a status effect that invokes a specified action when removed.
    /// </summary>
    /// <param name="action">The action to invoke upon removal of the effect.</param>
    /// <param name="source">The source identifier for the status effect.</param>
    /// <param name="id">An optional unique identifier for the status effect.</param>
    /// <param name="duration">The duration, in seconds, for which the effect remains active.</param>
    /// <param name="onConflict">A function to resolve conflicts with other status effects.</param>
    /// <param name="removeOnDeath">true to remove the effect when the target dies; otherwise, false.</param>
    /// <param name="pauseInOrbit">true to pause the effect when the target is in orbit; otherwise, false.</param>
    /// <param name="curable">true if the effect can be cured; otherwise, false.</param>
    public class OnRemoveActionEffect(Action action, string source, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, null, removeOnDeath, pauseInOrbit, curable)
    {
        Action action = action;

        public override void OnRemove()
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// Represents a status effect that executes a specified action on each tick.
    /// </summary>
    /// <param name="action">The action to execute on each tick.</param>
    /// <param name="source">The origin of the status effect.</param>
    /// <param name="id">The optional identifier for the status effect.</param>
    /// <param name="duration">The duration of the status effect in seconds.</param>
    /// <param name="onConflict">A function to resolve conflicts with other status effects.</param>
    /// <param name="onRemove">An action to execute when the status effect is removed.</param>
    /// <param name="removeOnDeath">Indicates whether the effect is removed when the target dies.</param>
    /// <param name="pauseInOrbit">Indicates whether the effect pauses when the target is in orbit.</param>
    /// <param name="curable">Indicates whether the effect can be cured.</param>
    public class TickActionEffect(Action action, string source, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
    {
        Action action = action;

        public override void OnTick()
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// Applies a status effect that invokes a specified action at a given probability per second while active.
    /// </summary>
    /// <param name="chancePerSecond">The probability per second that the action is invoked.</param>
    /// <param name="action">The action to invoke when the chance condition is met.</param>
    /// <param name="source">The source identifier for the status effect.</param>
    /// <param name="id">An optional unique identifier for the status effect instance.</param>
    /// <param name="duration">The duration in seconds for which the effect remains active.</param>
    /// <param name="onConflict">A function to resolve conflicts with other status effects.</param>
    /// <param name="onRemove">An action to execute when the effect is removed.</param>
    /// <param name="removeOnDeath">true to remove the effect when the entity dies; otherwise, false.</param>
    /// <param name="pauseInOrbit">true to pause the effect when the entity is in orbit; otherwise, false.</param>
    /// <param name="curable">true if the effect can be cured; otherwise, false.</param>
    public class ChanceTickActionEffect(float chancePerSecond, Action action, string source, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
    {
        float chance = chancePerSecond;
        Action action = action;

        public override void OnTick()
        {
            if (Utils.randomLocal.NextFloat(0f, 1f) < Mathf.Clamp01(chance) * Time.deltaTime)
                action.Invoke();
        }
    }

    /// <summary>
    /// Represents a status effect that executes a specified action when a condition is met, with support for cooldowns,
    /// trigger limits, and effect removal options.
    /// </summary>
    /// <param name="condition">A delegate that evaluates whether the action should be executed.</param>
    /// <param name="action">The action to execute when the condition is satisfied.</param>
    /// <param name="removeOnTrigger">true to remove the effect after the action is triggered; otherwise, false.</param>
    /// <param name="source">The identifier for the source of the effect.</param>
    /// <param name="cooldown">The minimum time in seconds between action executions.</param>
    /// <param name="maxTriggerCount">The maximum number of times the action can be triggered before the effect is removed. Set to 0 for unlimited
    /// triggers.</param>
    /// <param name="id">An optional identifier for the effect instance.</param>
    /// <param name="duration">The duration of the effect in seconds. Set to 0 for indefinite duration.</param>
    /// <param name="onConflict">A delegate to handle conflicts with other status effects.</param>
    /// <param name="onRemove">A delegate invoked when the effect is removed.</param>
    /// <param name="removeOnDeath">true to remove the effect upon death; otherwise, false.</param>
    /// <param name="pauseInOrbit">true to pause the effect while in orbit; otherwise, false.</param>
    /// <param name="curable">true if the effect can be cured; otherwise, false.</param>
    public class ConditionalActionEffect(Func<bool> condition, Action action, bool removeOnTrigger, string source, float cooldown = 0f, int maxTriggerCount = 0, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
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

    /// <summary>
    /// Applies a linear interpolation to a float value over a specified duration, invoking a setter with the
    /// interpolated value each frame.
    /// </summary>
    /// <param name="setter">The action to invoke with the interpolated value.</param>
    /// <param name="startValue">The initial value at the start of the interpolation.</param>
    /// <param name="endValue">The final value at the end of the interpolation.</param>
    /// <param name="duration">The duration of the interpolation in seconds.</param>
    /// <param name="source">The source identifier for the effect.</param>
    /// <param name="id">An optional identifier for the effect.</param>
    /// <param name="onConflict">An optional function to handle conflicts with other effects.</param>
    /// <param name="onRemove">An optional action to invoke when the effect is removed.</param>
    /// <param name="removeOnDeath">Indicates whether the effect should be removed when the target dies.</param>
    /// <param name="pauseInOrbit">Indicates whether the effect should pause while in orbit.</param>
    /// <param name="curable">Indicates whether the effect can be cured.</param>
    public class LerpValueEffect(Action<float> setter, float startValue, float endValue, float duration, string source, string id = "", Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
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
            base.OnRemove();
        }
    }

    /// <summary>
    /// Applies a status effect that triggers actions at random intervals, with each phase lasting a random duration and
    /// supporting custom start, tick, and end actions.
    /// </summary>
    /// <param name="randomInterval">The range for random intervals between action phases.</param>
    /// <param name="randomPhaseDuration">The range for the duration of each action phase.</param>
    /// <param name="onStartAction">The action invoked at the start of each phase, receiving the phase duration.</param>
    /// <param name="tickAction">The action invoked on each tick during an active phase, receiving the remaining phase time.</param>
    /// <param name="onEndAction">The action invoked when a phase ends.</param>
    /// <param name="source">The identifier for the source of the status effect.</param>
    /// <param name="id">The optional identifier for the status effect.</param>
    /// <param name="duration">The total duration of the status effect.</param>
    /// <param name="onConflict">The optional function to resolve conflicts with other status effects.</param>
    /// <param name="onRemove">The optional action invoked when the status effect is removed.</param>
    /// <param name="removeOnDeath">Indicates whether the effect is removed upon death.</param>
    /// <param name="pauseInOrbit">Indicates whether the effect is paused while in orbit.</param>
    /// <param name="curable">Indicates whether the effect can be cured.</param>
    public class RandomIntervalPhaseActionEffect(BoundedRange randomInterval, BoundedRange randomPhaseDuration, Action<float> onStartAction, Action<float> tickAction, Action onEndAction, string source, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
    {
        BoundedRange randomInterval = randomInterval;
        BoundedRange randomPhaseDuration = randomPhaseDuration;
        Action<float> onStartAction = onStartAction;
        Action<float> tickAction = tickAction;
        Action onEndAction = onEndAction;

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
                onStartAction.Invoke(phaseTimer);
            }

            if (phaseTimer > 0)
            {
                phaseTimer -= Time.deltaTime;

                if (phaseTimer <= 0)
                {
                    onEndAction.Invoke();
                    return;
                }

                tickAction.Invoke(phaseTimer);
            }
        }
    }

    /// <summary>
    /// Applies a value effect over time using an animation curve, invoking a setter with the evaluated value at each
    /// update.
    /// </summary>
    /// <param name="setter">Invoked with the evaluated curve value at each update.</param>
    /// <param name="curve">Defines the progression of values over the effect's duration.</param>
    /// <param name="duration">Total duration of the effect in seconds.</param>
    /// <param name="source">Identifier for the effect's source.</param>
    /// <param name="id">Optional unique identifier for the effect.</param>
    /// <param name="onConflict">Optional function to resolve conflicts with other status effects.</param>
    /// <param name="onRemove">Optional action invoked when the effect is removed.</param>
    /// <param name="removeOnDeath">Indicates whether the effect is removed upon death.</param>
    /// <param name="pauseInOrbit">Indicates whether the effect pauses while in orbit.</param>
    /// <param name="curable">Indicates whether the effect can be cured.</param>
    public class CurveValueEffect(Action<float> setter, AnimationCurve curve, float duration, string source, string id = "", Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
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
            base.OnRemove();
        }
    }

    /// <summary>
    /// Applies a status effect that invokes a specified action at evenly distributed, randomized intervals over a set
    /// duration.
    /// </summary>
    /// <param name="action">The action to invoke at each trigger time.</param>
    /// <param name="totalActions">The number of times the action is triggered during the effect's duration.</param>
    /// <param name="source">The identifier for the source of the status effect.</param>
    /// <param name="id">An optional identifier for the status effect instance.</param>
    /// <param name="duration">The total duration over which the actions are distributed.</param>
    /// <param name="onConflict">A function to resolve conflicts with other status effects.</param>
    /// <param name="onRemove">An action to execute when the status effect is removed.</param>
    /// <param name="removeOnDeath">Indicates whether the effect is removed when the entity dies.</param>
    /// <param name="pauseInOrbit">Indicates whether the effect pauses while the entity is in orbit.</param>
    /// <param name="curable">Indicates whether the effect can be cured.</param>
    public class DistributedActionEffect(Action action, int totalActions, string source, string id = "", float duration = 0, Func<StatusEffect, StatusEffect, StatusEffectController.ConflictResult>? onConflict = null, Action<StatusEffect>? onRemove = null, bool removeOnDeath = true, bool pauseInOrbit = true, bool curable = true) : StatusEffect(source, id, duration, onConflict, onRemove, removeOnDeath, pauseInOrbit, curable)
    {
        int totalActions = totalActions;
        Action action = action;

        List<float> triggerTimes = new();
        int currentIndex;

        public override void OnApply()
        {
            triggerTimes.Clear();

            float segmentLength = duration / totalActions;

            for (int i = 0; i < totalActions; i++)
            {
                float segmentStart = i * segmentLength;
                float segmentEnd = segmentStart + segmentLength;

                float time = Utils.randomLocal.NextFloat(segmentStart, segmentEnd);
                triggerTimes.Add(time);
            }

            triggerTimes.Sort();

            currentIndex = 0;
        }

        public override void OnTick()
        {
            while (currentIndex < triggerTimes.Count &&
                   elapsedTime >= triggerTimes[currentIndex])
            {
                action.Invoke();
                currentIndex++;
            }
        }
    }
}
