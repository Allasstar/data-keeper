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
    ///   <item>Build a weighted pool from all entries (weight 0 = excluded).</item>
    ///   <item>Select one candidate entry via weighted random.</item>
    ///   <item>Roll against that entry's effective chance (base + pity + luck).</item>
    ///   <item>On success: output the drop, record success, auto-reset if configured.</item>
    ///   <item>On failure: increment that entry's attempt counter; pity increases next roll.</item>
    /// </list>
    ///
    /// <para><b>Weight vs chance — what each field controls:</b></para>
    /// <list type="bullet">
    ///   <item>
    ///     <b>weight</b> — relative probability of being selected as the candidate this roll.
    ///     Higher weight = chosen more often = pity counter advances faster.
    ///     Weight 0 disables normal selection; the entry can still drop via guaranteedAt.
    ///   </item>
    ///   <item>
    ///     <b>baseChance</b> — probability of actually dropping once selected [0..1].
    ///     Independent of other entries. Pity and luck scale this value up over time.
    ///   </item>
    /// </list>
    ///
    /// <para>
    ///   Overall drop rate per roll = (weight / totalWeight) * effectiveChance.
    ///   Weight controls how often pity accumulates; chance controls how hard
    ///   the drop is once it is attempted.
    /// </para>
    ///
    /// <para><b>Example loot table — total weight 19:</b></para>
    /// <code>
    ///  Drop           weight  select%   baseChance   avg/100 rolls   pityStart  +perRoll  guarantee
    ///  ──────────────────────────────────────────────────────────────────────────────────────────────
    ///  Common           10    52.6 %      70 %          36.8           0        0 %        off
    ///  Uncommon          5    26.3 %      40 %          10.5           5        2 %        off
    ///  Rare              3    15.8 %      15 %           2.4          10        5 %         40
    ///  Epic              1     5.3 %       5 %           0.3          15        4 %         60
    ///  Legendary         0     0.0 %       1 %           0.0 (*)      20        3 %         80
    ///
    ///  (*) weight 0 = never selected normally; only guaranteedAt can trigger this drop.
    ///      Pity still accumulates each time any roll occurs for this entry.
    ///
    ///  avg/100 rolls = (weight / totalWeight) * baseChance * 100  (no pity, no luck)
    /// </code>
    ///
    /// <para><b>Key design patterns:</b></para>
    /// <list type="bullet">
    ///   <item>High weight + low chance  → attempted often, pity builds fast, guarantee reached sooner.</item>
    ///   <item>Low weight + high chance  → rarely attempted, nearly always succeeds when chosen.</item>
    ///   <item>Weight 0 + guaranteedAt   → impossible to get normally; only drops at the hard cap.</item>
    ///   <item>Weight 0 + no guarantee   → entry is fully disabled until re-enabled at runtime.</item>
    /// </list>
    ///
    /// <para>Every entry tracks its own independent pity counter.</para>
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
        /// Attempts a single roll against the weighted drop pool.
        /// </summary>
        /// <param name="luck">
        /// Luck value ≥ 0. Applied via "closes the gap" with diminishing returns —
        /// rare drops benefit most, common drops barely change.
        /// 0 = no bonus. Values above 1 are valid and keep improving odds naturally.
        /// </param>
        /// <param name="result">
        /// The dropped value when the method returns true; default(T) otherwise.
        /// </param>
        /// <returns>True if a drop was awarded, false otherwise.</returns>
        public bool Roll(float luck, out T result)
        {
            result = default;

            if (_drops == null || _drops.Count == 0)
            {
                Debug.LogWarning("[PitySystem] Roll called with no drop entries configured.");
                return false;
            }

            luck = Mathf.Max(0f, luck);

            // Step 1: weighted selection of candidate entry
            int candidateIndex = SelectWeightedCandidate();
            if (candidateIndex < 0)
            {
                Debug.LogWarning("[PitySystem] Weighted selection returned no candidate (all weights are zero?).");
                return false;
            }

            PityDropEntry<T> entry = _drops[candidateIndex];

            // Step 2: roll against effective chance
            float effectiveChance = entry.GetEffectiveChance(luck);
            bool  success         = UnityEngine.Random.value <= effectiveChance;

            // Step 3: record the attempt (handles auto-reset internally)
            entry.RecordAttempt(success);

            if (success)
                result = entry.drop;

            return success;
        }

        /// <summary>Resets attempt counters for all entries.</summary>
        public void ResetAll()
        {
            foreach (var entry in _drops)
                entry.Reset();
        }

        /// <summary>Resets the attempt counter for the entry at the given index.</summary>
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
        /// Serializes all attempt counters to a JSON string.
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
        /// Restores attempt counters from a JSON string previously produced by <see cref="SaveState"/>.
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
        /// Weighted random selection. Each entry's <c>weight</c> determines
        /// its relative probability of being chosen as the candidate for rolling.
        /// </summary>
        private int SelectWeightedCandidate()
        {
            float totalWeight = 0f;
            foreach (var entry in _drops)
                totalWeight += Mathf.Max(0f, entry.weight);

            if (totalWeight <= 0f)
                return -1;

            float roll      = UnityEngine.Random.value * totalWeight;
            float cumulated = 0f;

            for (int i = 0; i < _drops.Count; i++)
            {
                cumulated += Mathf.Max(0f, _drops[i].weight);
                if (roll <= cumulated)
                    return i;
            }

            // Floating point safety: return last entry
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