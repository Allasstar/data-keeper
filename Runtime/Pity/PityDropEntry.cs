using System;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Defines one possible drop in a <see cref="PitySystem{T}"/>.
    ///
    /// <para><b>How pity works in this model:</b></para>
    /// <list type="bullet">
    ///   <item>
    ///     Every roll always produces exactly one drop — the entry with the highest
    ///     effective weight wins the weighted random selection.
    ///   </item>
    ///   <item>
    ///     When an entry is NOT selected, its miss counter increments.
    ///     Once misses exceed <see cref="pityActivationThreshold"/>, each additional miss adds
    ///     <see cref="pityWeightIncrement"/> to its effective weight, making it
    ///     progressively more likely to be chosen on future rolls.
    ///   </item>
    ///   <item>
    ///     When an entry IS selected (drops), its miss counter and weight bonus reset
    ///     automatically. Other entries keep their accumulated pity.
    ///   </item>
    ///   <item>
    ///     <see cref="guaranteedDropThreshold"/> is a hard cap: if an entry's miss count reaches
    ///     this value it is forced to drop on the next roll regardless of weight.
    ///     If multiple entries hit their cap simultaneously, the one with the highest
    ///     effective weight wins.
    ///   </item>
    /// </list>
    /// </summary>
    [Serializable]
    public class PityDropEntry<T>
    {
        [Tooltip("The item/value to drop.")]
        public T item;

        [Tooltip("Base weight for weighted selection. Higher = more likely to be chosen each roll.")]
        [Min(0f)]
        public float baseWeight = 100f;

        [Tooltip("How strongly luck influences this entry. The luck bonus added to weight is: luck × luckInfluence. " +
                 "Set to 0 to make this entry unaffected by luck.")]
        public float luckInfluence = 0f;

        [Tooltip("Pity starts accumulating after this many consecutive misses. 0 = pity starts immediately.")]
        [Min(0)]
        public int pityActivationThreshold = 0;

        [Tooltip("Weight added per miss once pity has started (after pityActivationThreshold misses).")]
        [Min(0f)]
        public float pityWeightIncrement = 0f;

        [Tooltip("Hard cap: after this many consecutive misses the entry is guaranteed to drop next roll. " +
                 "0 = no hard cap.")]
        [Min(0)]
        public int guaranteedDropThreshold = 0;

        // ── runtime state ─────────────────────────────────────────────────────

        [NonSerialized]
        private PityDropState _state = new PityDropState();

        /// <summary>Current runtime pity state (miss count, weight bonus).</summary>
        public PityDropState State => _state;

        // ── computed properties ───────────────────────────────────────────────

        /// <summary>
        /// Effective weight used for weighted selection this roll (without luck).
        /// Equals <see cref="baseWeight"/> plus any accumulated pity bonus.
        /// </summary>
        public float EffectiveWeight => baseWeight + _state.WeightBonus;

        /// <summary>
        /// Effective weight used for weighted selection this roll, including a luck bonus.
        /// The luck bonus is: <paramref name="luck"/> × <see cref="luckInfluence"/>.
        /// </summary>
        /// <param name="luck">
        /// Player luck as a percentage (e.g. 0.25 = 25 %, 5.0 = 500 %).
        /// No clamping is applied — negative values act as a luck penalty,
        /// reducing the effective weight of this entry.
        /// </param>
        public float GetEffectiveWeightWithLuck(float luck)
        {
            return baseWeight + _state.WeightBonus + luck * luckInfluence;
        }

        /// <summary>
        /// True when this entry's miss count has reached <see cref="guaranteedDropThreshold"/>
        /// and it must drop on the next roll.
        /// </summary>
        public bool IsGuaranteed => guaranteedDropThreshold > 0 && _state.Misses >= guaranteedDropThreshold;

        // ── internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Called when this entry was NOT selected on a roll.
        /// Increments the miss counter and, once past <see cref="pityActivationThreshold"/>,
        /// adds <see cref="pityWeightIncrement"/> to the weight bonus.
        /// </summary>
        internal void RecordMiss()
        {
            if (_state.Misses >= pityActivationThreshold)
                _state.IncrementMisses(pityWeightIncrement);
            else
                _state.IncrementMisses(0f);
        }

        /// <summary>
        /// Called when this entry was selected and dropped.
        /// Resets the miss counter and weight bonus for this entry only.
        /// </summary>
        internal void RecordDrop() => _state.Reset();

        /// <summary>Manually resets this entry's pity state.</summary>
        internal void Reset() => _state.Reset();

        /// <summary>
        /// Returns the effective weight at a given miss count without touching runtime state.
        /// Used for display and simulation.
        /// </summary>
        public float GetEffectiveWeightAt(int atMisses)
        {
            float bonus = 0f;
            if (atMisses > pityActivationThreshold)
                bonus = (atMisses - pityActivationThreshold) * pityWeightIncrement;
            return baseWeight + bonus;
        }

        // ── serialization helpers ─────────────────────────────────────────────

        internal PityDropSaveData ExportState() =>
            new PityDropSaveData { misses = _state.Misses, weightBonus = _state.WeightBonus };

        internal void ImportState(PityDropSaveData data) =>
            _state.SetMisses(data.misses, data.weightBonus);
    }
}
