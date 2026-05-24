using System;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Defines one possible drop in a PitySystem, including all pity parameters
    /// and the runtime state for that specific drop.
    /// </summary>
    [Serializable]
    public class PityDropEntry<T>
    {
        [Tooltip("The item/value to drop.")]
        public T drop;

        [Tooltip("Base probability [0..1] before any pity scaling.")]
        [Range(0f, 1f)]
        public float baseChance = 0.1f;

        [Tooltip("Relative weight for weighted selection. Higher = more likely to be chosen as the candidate drop.")]
        [Min(0f)]
        public float weight = 1f;

        [Tooltip("After this many failed attempts pity starts increasing the chance.")]
        [Min(0)]
        public int pityStartAt = 10;

        [Tooltip("Flat chance added per attempt once pity has started.")]
        [Range(0f, 1f)]
        public float increasePerAttempt = 0.05f;

        [Tooltip("After this many total attempts the drop is guaranteed (chance forced to 1). 0 = disabled.")]
        [Min(0)]
        public int guaranteedAt = 50;

        [Tooltip("If true, resets attempt counter automatically after a successful drop. " +
                 "If false, you must call ResetDrop(index) manually.")]
        public bool autoReset = true;

        // ── runtime state ─────────────────────────────────────────────────────

        [NonSerialized]
        private PityDropState _state = new PityDropState();

        /// <summary>Current runtime state for this entry (attempts, effective chance, etc.).</summary>
        public PityDropState State => _state;

        // ── internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Computes the effective drop chance for this entry given extra luck.
        ///
        /// <para><b>Luck model — "closes the gap":</b></para>
        /// <para>
        ///   Luck is first converted to a factor in [0, 1) via diminishing returns:
        ///   <c>luckFactor = 1 - 1 / (1 + luck)</c>
        ///   Then applied as:
        ///   <c>effectiveChance = chance + (1 - chance) * luckFactor</c>
        ///   This means rare drops (low base chance) benefit most from luck,
        ///   while near-guaranteed drops are barely affected.
        ///   Luck can exceed 1 freely — higher values keep improving odds
        ///   with natural diminishing returns, no hard cap needed.
        /// </para>
        /// <para>Examples at different luck values (base 1% → 5% → 50%):</para>
        /// <list type="bullet">
        ///   <item>luck=0   →  1% / 5% / 50%  (no change)</item>
        ///   <item>luck=0.5 →  4% / 8% / 60%</item>
        ///   <item>luck=1   →  9% /14% / 71%</item>
        ///   <item>luck=2   → 23% /28% / 83%</item>
        ///   <item>luck=5   → 51% /55% / 95%</item>
        /// </list>
        /// </summary>
        /// <param name="luck">
        /// Luck value ≥ 0. No upper cap — higher values give more benefit
        /// with natural diminishing returns. 0 = no luck bonus.
        /// </param>
        internal float GetEffectiveChance(float luck)
        {
            // Step 1: apply pity ramp to base chance
            float chance = baseChance;

            if (guaranteedAt > 0 && _state.Attempts >= guaranteedAt)
                return 1f;

            if (_state.Attempts > pityStartAt)
            {
                int pitySteps = _state.Attempts - pityStartAt;
                chance += pitySteps * increasePerAttempt;
                chance  = Mathf.Clamp01(chance);
            }

            // Step 2: apply luck via "closes the gap" model
            // luckFactor maps [0, +inf) → [0, 1) with diminishing returns
            if (luck > 0f)
            {
                float luckFactor = 1f - 1f / (1f + luck);
                chance = chance + (1f - chance) * luckFactor;
            }

            return Mathf.Clamp01(chance);
        }

        /// <summary>
        /// Records one roll attempt and optionally resets on success.
        /// </summary>
        /// <param name="success">Whether the roll succeeded.</param>
        internal void RecordAttempt(bool success)
        {
            if (success)
            {
                if (autoReset)
                    _state.Reset();
                else
                    _state.IncrementAttempts();
            }
            else
            {
                _state.IncrementAttempts();
            }

            _state.CurrentChance = GetEffectiveChance(0f);
            _state.IsGuaranteed  = guaranteedAt > 0 && _state.Attempts >= guaranteedAt;
        }

        internal void Reset() => _state.Reset();

        /// <summary>
        /// Returns the effective chance at a given attempt count and luck value
        /// without touching runtime state. Used for display and simulation.
        /// </summary>
        public float GetEffectiveChancePublic(float luck, int atAttempts)
        {
            float chance = baseChance;

            if (guaranteedAt > 0 && atAttempts >= guaranteedAt)
                return 1f;

            if (atAttempts > pityStartAt)
            {
                int pitySteps = atAttempts - pityStartAt;
                chance += pitySteps * increasePerAttempt;
                chance  = Mathf.Clamp01(chance);
            }

            if (luck > 0f)
            {
                float luckFactor = 1f - 1f / (1f + luck);
                chance = chance + (1f - chance) * luckFactor;
            }

            return Mathf.Clamp01(chance);
        }

        // ── serialization helpers ─────────────────────────────────────────────

        internal PityDropSaveData ExportState() =>
            new PityDropSaveData { attempts = _state.Attempts };

        internal void ImportState(PityDropSaveData data)
        {
            _state.SetAttempts(data.attempts);
            _state.CurrentChance = GetEffectiveChance(0f);
            _state.IsGuaranteed  = guaranteedAt > 0 && _state.Attempts >= guaranteedAt;
        }
    }
}