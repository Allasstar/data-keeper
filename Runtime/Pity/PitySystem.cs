using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Generic pity system that works with any type: int, string, enum,
    /// custom class or struct.
    ///
    /// <para><b>How a roll works:</b></para>
    /// <list type="number">
    ///   <item>
    ///     Check for guaranteed entries — any entry whose accumulated miss count means
    ///     this is its guaranteed roll (i.e. <c>misses + 1 &gt;= guaranteedDropThreshold</c>,
    ///     so the entry is forced to drop by roll N when the threshold is set to N).
    ///     If multiple entries are guaranteed simultaneously, the one with the highest
    ///     effective weight wins.
    ///   </item>
    ///   <item>
    ///     If no entry is guaranteed, perform a weighted random selection using
    ///     each entry's effective weight (base weight + pity bonus).
    ///   </item>
    ///   <item>
    ///     The selected entry drops. Its miss counter and weight bonus reset.
    ///   </item>
    ///   <item>
    ///     Every entry that was NOT selected has its miss counter incremented.
    ///     Once an entry's miss count reaches <c>pityActivationThreshold</c>, that miss and
    ///     every subsequent miss also adds <c>pityWeightIncrement</c> to its weight bonus.
    ///   </item>
    /// </list>
    ///
    /// <para><b>Key properties:</b></para>
    /// <list type="bullet">
    ///   <item>Every roll always produces exactly one drop.</item>
    ///   <item>Pity is per-entry and independent — dropping one item does not reset others.</item>
    ///   <item>Weight 0 entries are never selected normally but can still be forced by <c>guaranteedAt</c>.</item>
    /// </list>
    ///
    /// <para><b>Luck system:</b> Pass a <c>luck</c> value to <see cref="Roll"/> (e.g. 0.25 = 25 %, 5.0 = 500 %).
    ///   Each entry has a <c>luckInfluence</c> that controls how much it is affected by luck.
    ///   The bonus added to an entry's weight is: <c>luck × luckInfluence</c>.
    ///   No clamping is applied — luck can exceed 1 (e.g. 500 %) or be negative (penalty).
    ///   Set <c>luckInfluence = 0</c> on common entries and higher values on rare entries
    ///   to make luck shift the distribution toward rarer drops.
    /// </para>
    ///
    /// <para><b>Example loot table — total base weight 19:</b></para>
    /// <code>
    ///  Drop        baseWeight  luckInfluence  pityActivationThreshold  pityWeightIncrement  guaranteedDropThreshold
    ///  ────────────────────────────────────────────────────────────────────────
    ///  Common        10       0.0          0          0.0           off
    ///  Uncommon       5       0.5          5          0.5           off
    ///  Rare           3       1.0         10          1.0            40
    ///  Epic           1       2.0         15          2.0            60
    ///  Legendary      0       3.0         20          3.0            80
    /// </code>
    /// </summary>
    /// <typeparam name="T">Type of item to drop.</typeparam>
    [Serializable]
    public class PitySystem<T>
    {
        // ── configuration ─────────────────────────────────────────────────────

        [SerializeField]
        private List<PityDropEntry<T>> _drops = new List<PityDropEntry<T>>();

        /// <summary>Read-only access to the configured drop entries.</summary>
        public IReadOnlyList<PityDropEntry<T>> Drops => _drops;

        // ── constructors ──────────────────────────────────────────────────────

        public PitySystem() { }

        /// <summary>Convenience constructor — supply the drop list directly.</summary>
        public PitySystem(List<PityDropEntry<T>> drops)
        {
            _drops = drops ?? throw new ArgumentNullException(nameof(drops));
        }

        // ── public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Performs a single roll. Always produces exactly one drop.
        ///
        /// <para>
        ///   If one or more entries have reached their <c>guaranteedDropThreshold</c> miss cap,
        ///   the guaranteed entry with the highest effective weight is forced to drop.
        ///   Otherwise a weighted random selection is made using effective weights.
        /// </para>
        ///
        /// <para><b>Luck:</b> Pass any value (e.g. 0.25 = 25 %, 5.0 = 500 %, -0.5 = −50 % penalty).
        ///   For each entry the luck modifier applied to its weight is:
        ///   <c>luck × luckInfluence</c>.  Entries with a higher <c>luckInfluence</c>
        ///   are affected more by luck, letting you tune which rarities are boosted or penalised.
        ///   No clamping is applied in either direction.
        /// </para>
        /// </summary>
        /// <param name="luck">
        /// Player luck as a percentage (e.g. 0.25 = 25 %, 5.0 = 500 %, -1.0 = −100 % penalty).
        /// Defaults to 0 (no luck modifier). No upper or lower limit.
        /// </param>
        public T Roll(float luck = 0f)
        {
            if (_drops == null || _drops.Count == 0)
            {
                Debug.LogWarning("[PitySystem] Roll called with no drop entries configured.");
                return default;
            }

            // Step 1: check for guaranteed entries
            int winnerIndex = FindGuaranteedWinner(luck);

            // Step 2: if no guarantee, weighted random selection
            if (winnerIndex < 0)
                winnerIndex = SelectWeightedCandidate(luck);

            if (winnerIndex < 0)
            {
                // All weights are zero — fall back to uniform random
                winnerIndex = UnityEngine.Random.Range(0, _drops.Count);
            }

            // Step 3: record drop for winner, record miss for all others
            for (int i = 0; i < _drops.Count; i++)
            {
                if (i == winnerIndex)
                    _drops[i].RecordDrop();
                else
                    _drops[i].RecordMiss();
            }

            return  _drops[winnerIndex].item;
        }

        /// <summary>Resets pity state for all entries.</summary>
        public void ResetAll()
        {
            foreach (var entry in _drops)
                entry.Reset();
        }

        /// <summary>Resets the pity state for the entry at the given index.</summary>
        /// <param name="index">Zero-based index into the Drops list.</param>
        public void ResetDrop(int index)
        {
            AssertIndex(index);
            _drops[index].Reset();
        }

        /// <summary>
        /// Returns a snapshot of the current pity state for the entry at <paramref name="index"/>.
        /// </summary>
        public PityDropState GetState(int index)
        {
            AssertIndex(index);
            return _drops[index].State;
        }

        /// <summary>Adds a new drop entry at runtime.</summary>
        public void AddDrop(PityDropEntry<T> entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _drops.Add(entry);
        }

        /// <summary>Removes the drop entry at the given index.</summary>
        public void RemoveDrop(int index)
        {
            AssertIndex(index);
            _drops.RemoveAt(index);
        }

        // ── persistence ───────────────────────────────────────────────────────

        /// <summary>
        /// Serializes all pity states to a JSON string.
        /// Store this in PlayerPrefs, a save file, or your own persistence layer.
        /// </summary>
        public string SaveState()
        {
            var data = new PitySystemSaveData
            {
                drops = new PityDropSaveData[_drops.Count]
            };

            for (int i = 0; i < _drops.Count; i++)
                data.drops[i] = _drops[i].ExportState();

            return JsonUtility.ToJson(data, prettyPrint: false);
        }

        /// <summary>
        /// Restores pity states from a JSON string previously produced by <see cref="SaveState"/>.
        /// Drop count must match; mismatches are logged and silently ignored.
        /// </summary>
        public void LoadState(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[PitySystem] LoadState called with null or empty JSON.");
                return;
            }

            PitySystemSaveData data;
            try
            {
                data = JsonUtility.FromJson<PitySystemSaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PitySystem] Failed to parse save data: {ex.Message}");
                return;
            }

            if (data.drops == null)
            {
                Debug.LogWarning("[PitySystem] Save data contained no drop entries.");
                return;
            }

            if (data.drops.Length != _drops.Count)
            {
                Debug.LogWarning($"[PitySystem] Save data has {data.drops.Length} entries but system has {_drops.Count}. " +
                                 "Attempting partial restore — unmatched entries are skipped.");
            }

            int count = Mathf.Min(data.drops.Length, _drops.Count);
            for (int i = 0; i < count; i++)
                _drops[i].ImportState(data.drops[i]);
        }

        // ── private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the index of the guaranteed entry with the highest effective weight
        /// (including luck bonus), or -1 if no entry has reached its guarantee threshold.
        /// When multiple entries are guaranteed, the one with the highest effective weight wins.
        /// </summary>
        private int FindGuaranteedWinner(float luck)
        {
            int   bestIndex  = -1;
            float bestWeight = float.NegativeInfinity;

            for (int i = 0; i < _drops.Count; i++)
            {
                if (!_drops[i].IsGuaranteed) continue;

                float w = _drops[i].GetEffectiveWeightWithLuck(luck);
                if (w > bestWeight)
                {
                    bestWeight = w;
                    bestIndex  = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Weighted random selection using each entry's effective weight (including luck bonus).
        /// Returns -1 if all effective weights are zero or negative.
        /// </summary>
        private int SelectWeightedCandidate(float luck)
        {
            float totalWeight = 0f;
            for (int i = 0; i < _drops.Count; i++)
                totalWeight += Mathf.Max(0f, _drops[i].GetEffectiveWeightWithLuck(luck));

            if (totalWeight <= 0f)
                return -1;

            float roll      = UnityEngine.Random.value * totalWeight;
            float cumulated = 0f;

            for (int i = 0; i < _drops.Count; i++)
            {
                cumulated += Mathf.Max(0f, _drops[i].GetEffectiveWeightWithLuck(luck));
                if (roll <= cumulated)
                    return i;
            }

            // Floating-point safety: return last entry with positive weight
            for (int i = _drops.Count - 1; i >= 0; i--)
                if (_drops[i].GetEffectiveWeightWithLuck(luck) > 0f) return i;

            return _drops.Count - 1;
        }

        private void AssertIndex(int index)
        {
            if (index < 0 || index >= _drops.Count)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Drop index {index} is out of range [0, {_drops.Count - 1}].");
        }
    }
}
