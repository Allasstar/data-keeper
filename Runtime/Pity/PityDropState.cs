using System;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Runtime pity state for one drop entry.
    /// Tracks how many times this entry has been missed and the accumulated weight bonus.
    /// </summary>
    [Serializable]
    public class PityDropState
    {
        [SerializeField] private int   _misses;      // times this entry was NOT selected
        [SerializeField] private float _weightBonus; // extra weight accumulated via pity

        /// <summary>Number of consecutive misses since the last time this entry dropped.</summary>
        public int Misses => _misses;

        /// <summary>Extra weight added on top of the base weight due to pity accumulation.</summary>
        public float WeightBonus => _weightBonus;

        internal void IncrementMisses(float increasePerAttempt)
        {
            _misses++;
            _weightBonus += increasePerAttempt;
        }

        internal void Reset()
        {
            _misses      = 0;
            _weightBonus = 0f;
        }

        internal void SetMisses(int value, float weightBonus)
        {
            _misses      = Mathf.Max(0, value);
            _weightBonus = Mathf.Max(0f, weightBonus);
        }
    }

    // ── persistence helpers ───────────────────────────────────────────────────

    /// <summary>Per-drop save data. Persists miss count and weight bonus.</summary>
    [Serializable]
    public struct PityDropSaveData
    {
        public int   misses;
        public float weightBonus;
    }

    /// <summary>Full save payload for a PitySystem — an ordered array, one entry per drop.</summary>
    [Serializable]
    public class PitySystemSaveData
    {
        public PityDropSaveData[] drops;
    }
}
