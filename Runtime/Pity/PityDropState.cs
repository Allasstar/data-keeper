using System;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Read-only snapshot of runtime pity state for one drop entry.
    /// Returned by PitySystem.GetState(index) so callers can inspect
    /// progress without mutating anything.
    /// </summary>
    [Serializable]
    public class PityDropState
    {
        [SerializeField] private int _attempts;

        /// <summary>Total roll attempts recorded for this entry since last reset.</summary>
        public int Attempts => _attempts;

        /// <summary>Effective chance (base + pity + luck) as of the last roll call.</summary>
        public float CurrentChance { get; internal set; }

        /// <summary>True when the next roll for this entry is forced to succeed.</summary>
        public bool IsGuaranteed { get; internal set; }

        internal void IncrementAttempts() => _attempts++;

        internal void Reset()
        {
            _attempts     = 0;
            IsGuaranteed  = false;
            CurrentChance = 0f;
        }

        internal void SetAttempts(int value)
        {
            _attempts = Mathf.Max(0, value);
        }
    }

    // ── persistence helpers ───────────────────────────────────────────────────

    /// <summary>Per-drop save data. Only the attempt counter needs to persist;
    /// everything else is recomputed from config on load.</summary>
    [Serializable]
    public struct PityDropSaveData
    {
        public int attempts;
    }

    /// <summary>Full save payload for a PitySystem — an ordered array, one entry per drop.</summary>
    [Serializable]
    public class PitySystemSaveData
    {
        public PityDropSaveData[] drops;
    }
}