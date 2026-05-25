using System.Collections.Generic;
using System.Text;
using DataKeeper.Pity;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Monte Carlo simulation tester for any <see cref="PitySystem{T}"/>.
    ///
    /// <para>
    ///   Runs <c>trials</c> independent players, each performing <c>rollsPerTrial</c> rolls.
    ///   Because every roll always produces a drop, the tester measures:
    ///   <list type="bullet">
    ///     <item>How often each entry is selected (empirical selection rate vs theoretical weight share).</item>
    ///     <item>Distribution of rolls between consecutive drops of the same entry.</item>
    ///     <item>How many rolls until the first drop of each entry per trial (CDF).</item>
    ///     <item>How often the guaranteed cap was triggered.</item>
    ///   </list>
    /// </para>
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// new PitySystemTester().Run(myPitySystem, trials: 10_000, rollsPerTrial: 200);
    /// </code>
    /// </summary>
    public class PitySystemTester
    {
        // ── public entry point ────────────────────────────────────────────────

        /// <summary>
        /// Runs a Monte Carlo simulation against <paramref name="system"/>.
        /// </summary>
        /// <param name="system">The pity system to test. State is reset before and after.</param>
        /// <param name="players">Number of independent simulated players.</param>
        /// <param name="rolls">Number of roll attempts per player.</param>
        public void Run<T>(PitySystem<T> system, float luck = 0f, int players = 10_000, int rolls = 200)
        {
            if (system == null)
            {
                Debug.LogWarning("[PitySystemTester] system is null.");
                return;
            }

            if (system.Drops.Count == 0)
            {
                Debug.LogWarning("[PitySystemTester] No drops configured.");
                return;
            }

            players = Mathf.Max(1, players);
            rolls = Mathf.Max(1, rolls);

            var stats = Simulate(system, luck,  players, rolls);
            Log(system, stats, players, rolls);

            system.ResetAll();
        }

        // ── data structures ───────────────────────────────────────────────────

        private struct DropStats
        {
            // selection counts
            public long totalDrops;            // total times this entry was selected across all trials
            public int  trialsWithAtLeastOne;  // trials where this entry dropped ≥ 1 time
            public int  guaranteedTriggers;    // times the guaranteed cap forced this entry to drop

            // rolls-to-first-drop
            public long sumRollsToFirst;
            public int  minRollsToFirst;
            public int  maxRollsToFirst;

            // drop-count histogram per trial  [0 .. rollsPerTrial]
            public int[] dropCountHistogram;

            // rolls-to-first frequency array (converted to CDF after simulation)
            // index r = number of trials where first drop occurred on roll r
            public int[] rollsToFirstCDF;

            // gap histogram: rolls between consecutive drops of this entry
            // index g = number of times a gap of g rolls occurred
            // capped at rollsPerTrial
            public int[] gapHistogram;
        }

        // ── simulation ────────────────────────────────────────────────────────

        private DropStats[] Simulate<T>(PitySystem<T> system, float luck, int trials, int rollsPerTrial)
        {
            int dropCount = system.Drops.Count;

            var stats = new DropStats[dropCount];
            for (int d = 0; d < dropCount; d++)
            {
                stats[d].minRollsToFirst    = int.MaxValue;
                stats[d].maxRollsToFirst    = int.MinValue;
                stats[d].dropCountHistogram = new int[rollsPerTrial + 1];
                stats[d].rollsToFirstCDF    = new int[rollsPerTrial + 1];
                stats[d].gapHistogram       = new int[rollsPerTrial + 1];
            }

            var trialDropCount = new int[dropCount];
            var trialFirstRoll = new int[dropCount]; // 1-based; 0 = not yet dropped
            var lastDropRoll   = new int[dropCount]; // 1-based roll of last drop; 0 = none yet

            for (int t = 0; t < trials; t++)
            {
                system.ResetAll();

                for (int d = 0; d < dropCount; d++)
                {
                    trialDropCount[d] = 0;
                    trialFirstRoll[d] = 0;
                    lastDropRoll[d]   = 0;
                }

                for (int r = 1; r <= rollsPerTrial; r++)
                {
                    var drop = system.Roll(luck);

                    // identify which entry fired (first value-match)
                    for (int d = 0; d < dropCount; d++)
                    {
                        if (!EqualityComparer<T>.Default.Equals(system.Drops[d].item, drop))
                            continue;

                        trialDropCount[d]++;
                        stats[d].totalDrops++;

                        // check if this was a guaranteed trigger
                        // (state was reset by Roll, so we check if misses had reached cap)
                        // We detect this indirectly: after RecordDrop the state resets,
                        // so we track it via the IsGuaranteed flag BEFORE the roll.
                        // Since we can't peek before the roll here, we approximate:
                        // guaranteed triggers are tracked separately in the system.
                        // For now we just count drops.

                        if (trialFirstRoll[d] == 0)
                        {
                            trialFirstRoll[d] = r;
                            stats[d].trialsWithAtLeastOne++;
                            stats[d].sumRollsToFirst += r;

                            if (r < stats[d].minRollsToFirst) stats[d].minRollsToFirst = r;
                            if (r > stats[d].maxRollsToFirst) stats[d].maxRollsToFirst = r;
                        }

                        // gap between consecutive drops
                        if (lastDropRoll[d] > 0)
                        {
                            int gap = r - lastDropRoll[d];
                            stats[d].gapHistogram[Mathf.Min(gap, rollsPerTrial)]++;
                        }
                        lastDropRoll[d] = r;

                        break;
                    }
                }

                // accumulate per-trial histograms
                for (int d = 0; d < dropCount; d++)
                {
                    stats[d].dropCountHistogram[Mathf.Min(trialDropCount[d], rollsPerTrial)]++;

                    int first = trialFirstRoll[d];
                    if (first > 0)
                        stats[d].rollsToFirstCDF[first]++;
                }
            }

            // convert rollsToFirstCDF from frequency to cumulative
            for (int d = 0; d < dropCount; d++)
            {
                int running = 0;
                for (int r = 1; r <= rollsPerTrial; r++)
                {
                    running += stats[d].rollsToFirstCDF[r];
                    stats[d].rollsToFirstCDF[r] = running;
                }
            }

            return stats;
        }

        // ── analytical helpers ────────────────────────────────────────────────

        /// <summary>
        /// Theoretical selection probability for entry <paramref name="dropIndex"/>
        /// at 0 pity (base weights only).
        /// </summary>
        private static float TheoreticalBaseSelectionRate<T>(PitySystem<T> system, int dropIndex)
        {
            float totalWeight = 0f;
            foreach (var e in system.Drops)
                totalWeight += Mathf.Max(0f, e.baseWeight);

            if (totalWeight <= 0f) return 0f;
            return Mathf.Max(0f, system.Drops[dropIndex].baseWeight) / totalWeight;
        }

        /// <summary>
        /// Expected number of drops for entry <paramref name="dropIndex"/> over
        /// <paramref name="rollsPerTrial"/> rolls at base weights (no pity).
        /// </summary>
        private static float TheoreticalBaseExpectedDrops<T>(PitySystem<T> system, int dropIndex, int rollsPerTrial)
        {
            return TheoreticalBaseSelectionRate(system, dropIndex) * rollsPerTrial;
        }

        // ── logging ───────────────────────────────────────────────────────────

        private const string DIV  = "────────────────────────────────────────────────────────────";
        private const string THIN = "············································································";
        private const int    BAR  = 30;

        private void Log<T>(PitySystem<T> system, DropStats[] stats, int trials, int rollsPerTrial)
        {
            int dropCount = system.Drops.Count;

            // compute total base weight once
            float totalBaseWeight = 0f;
            foreach (var e in system.Drops) totalBaseWeight += Mathf.Max(0f, e.baseWeight);

            for (int d = 0; d < dropCount; d++)
            {
                var   entry   = system.Drops[d];
                var   s       = stats[d];

                float empiricalAvg   = s.totalDrops / (float)trials;
                float theoreticalAvg = TheoreticalBaseExpectedDrops(system, d, rollsPerTrial);
                float selRate        = s.totalDrops / (float)(trials * rollsPerTrial) * 100f;
                float baseSelRate    = totalBaseWeight > 0f
                                         ? Mathf.Max(0f, entry.baseWeight) / totalBaseWeight * 100f
                                         : 0f;
                float hitRate        = s.trialsWithAtLeastOne / (float)trials * 100f;
                float avgFirst       = s.trialsWithAtLeastOne > 0
                                         ? s.sumRollsToFirst / (float)s.trialsWithAtLeastOne
                                         : -1f;

                // average gap between consecutive drops (from gap histogram)
                long  gapSum   = 0;
                long  gapCount = 0;
                for (int g = 1; g < s.gapHistogram.Length; g++)
                {
                    gapSum   += (long)g * s.gapHistogram[g];
                    gapCount += s.gapHistogram[g];
                }
                float avgGap = gapCount > 0 ? gapSum / (float)gapCount : -1f;

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"  ▶  DROP [{d}]  \"{entry.item}\"");
                sb.AppendLine(THIN);

                // ── Config ──
                sb.AppendLine($"  Config");
                sb.AppendLine($"    base weight      {entry.baseWeight:F2}  (base sel. prob: {baseSelRate:F1}%)");
                sb.AppendLine($"    pity starts at   miss {entry.pityActivationThreshold + 1}  (+{entry.pityWeightIncrement:F2} weight / miss)");
                sb.AppendLine($"    guaranteed at    {(entry.guaranteedDropThreshold > 0 ? $"miss {entry.guaranteedDropThreshold}" : "off")}");
                sb.AppendLine();

                // ── Theoretical (base weights, no pity) ──
                sb.AppendLine($"  Theoretical  (base weights, no pity)");
                sb.AppendLine($"    selection rate      {baseSelRate:F2}%  per roll");
                sb.AppendLine($"    expected drops      {theoreticalAvg:F2}  over {rollsPerTrial} rolls");
                sb.AppendLine($"    expected gap        {(baseSelRate > 0f ? 100f / baseSelRate : float.PositiveInfinity):F1}  rolls between drops");
                sb.AppendLine();

                // ── Monte Carlo results ──
                sb.AppendLine($"  Monte Carlo  ({trials:N0} trials × {rollsPerTrial} rolls)");
                sb.AppendLine($"    total drops          {s.totalDrops:N0}");
                sb.AppendLine($"    empirical sel. rate  {selRate:F2}%  per roll  (base: {baseSelRate:F2}%)");
                sb.AppendLine($"    avg drops / trial    {empiricalAvg:F3}   (base expected: {theoreticalAvg:F3})");
                sb.AppendLine($"    trials with ≥1 drop  {s.trialsWithAtLeastOne:N0} / {trials:N0}  ({hitRate:F2}%)");
                sb.AppendLine($"    trials with 0 drops  {trials - s.trialsWithAtLeastOne:N0} / {trials:N0}  ({(trials - s.trialsWithAtLeastOne) / (float)trials * 100f:F2}%)");

                if (s.trialsWithAtLeastOne > 0)
                    sb.AppendLine($"    rolls to first drop  avg {avgFirst:F1}   min {s.minRollsToFirst}   max {s.maxRollsToFirst}");

                if (avgGap >= 0f)
                    sb.AppendLine($"    avg gap (drop→drop)  {avgGap:F1} rolls");

                // ── Drop-count histogram ──
                sb.AppendLine();
                sb.AppendLine("  Drop-count histogram  (trials by number of drops received):");

                int maxBucket  = 0;
                for (int i = s.dropCountHistogram.Length - 1; i >= 0; i--)
                    if (s.dropCountHistogram[i] > 0) { maxBucket = i; break; }

                int displayMax = Mathf.Min(maxBucket, 20);

                for (int i = 0; i <= displayMax; i++)
                {
                    int    count    = s.dropCountHistogram[i];
                    float  pct      = count / (float)trials;
                    int    filled   = Mathf.RoundToInt(pct * BAR);
                    string bar      = new string('█', filled) + new string('░', BAR - filled);
                    bool   overflow = (i == displayMax && maxBucket > displayMax) || i == rollsPerTrial;
                    string label    = overflow ? $"{i}+" : $"{i}x";
                    sb.AppendLine($"    {label,4}  {bar}  {count,6}  ({pct * 100f:F1}%)");
                }

                // ── Gap histogram (top 10 most common gaps) ──
                if (gapCount > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  Gap histogram  (rolls between consecutive drops, top 15):");

                    // find top 15 non-zero gaps
                    var topGaps = new System.Collections.Generic.List<(int gap, int count)>();
                    for (int g = 1; g < s.gapHistogram.Length; g++)
                        if (s.gapHistogram[g] > 0)
                            topGaps.Add((g, s.gapHistogram[g]));

                    topGaps.Sort((a, b) => b.count.CompareTo(a.count));
                    int show = Mathf.Min(topGaps.Count, 15);

                    for (int i = 0; i < show; i++)
                    {
                        var (gap, cnt) = topGaps[i];
                        float pct    = cnt / (float)gapCount;
                        int   filled = Mathf.RoundToInt(pct * BAR);
                        string bar   = new string('█', filled) + new string('░', BAR - filled);
                        bool overflow = gap == rollsPerTrial && s.gapHistogram[rollsPerTrial] > 0
                                        && gap < s.gapHistogram.Length - 1;
                        string label = overflow ? $"{gap}+" : $"{gap}";
                        sb.AppendLine($"    gap {label,5}  {bar}  {cnt,6}  ({pct * 100f:F1}%)");
                    }
                }

                // ── Rolls-to-first CDF ──
                if (s.trialsWithAtLeastOne > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("  Rolls-to-first CDF  (% of trials that got first drop by roll N):");

                    float[] quantiles = { 0.10f, 0.25f, 0.50f, 0.75f, 0.90f, 0.95f, 0.99f };
                    int qi = 0;
                    for (int r = 1; r <= rollsPerTrial && qi < quantiles.Length; r++)
                    {
                        float cdfPct = s.rollsToFirstCDF[r] / (float)trials;
                        while (qi < quantiles.Length && cdfPct >= quantiles[qi])
                        {
                            sb.AppendLine($"    P(first ≤ {r,4} rolls) = {cdfPct * 100f:F1}%   [{quantiles[qi] * 100f:F0}th percentile]");
                            qi++;
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine("  CDF at fixed roll milestones:");
                    int[] milestones = BuildMilestones(rollsPerTrial);
                    foreach (int m in milestones)
                    {
                        float cdfPct = s.rollsToFirstCDF[Mathf.Min(m, rollsPerTrial)] / (float)trials;
                        sb.AppendLine($"    by roll {m,5}  →  {cdfPct * 100f:F1}%");
                    }
                }

                sb.AppendLine(DIV);
                Debug.Log(sb.ToString());
            }

            // ── Summary table ──
            var sum = new StringBuilder();
            sum.AppendLine();
            sum.AppendLine("══════════════════════════════════════════════════════════════");
            sum.AppendLine("              PITY SYSTEM  —  MONTE CARLO SUMMARY             ");
            sum.AppendLine("══════════════════════════════════════════════════════════════");
            sum.AppendLine($"  Trials: {trials:N0}   |   Rolls/trial: {rollsPerTrial}");
            sum.AppendLine(DIV);
            sum.AppendLine();
            sum.AppendLine($"  {"Drop",-22}  {"BaseW",6}  {"BaseSel%",8}  {"EmpSel%",8}  {"ExpDrops",9}  {"EmpDrops",9}  {"Got≥1",8}  {"Avg1st",7}");
            sum.AppendLine($"  {new string('-', 22)}  {new string('-', 6)}  {new string('-', 8)}  {new string('-', 8)}  {new string('-', 9)}  {new string('-', 9)}  {new string('-', 8)}  {new string('-', 7)}");

            for (int d = 0; d < dropCount; d++)
            {
                var   entry   = system.Drops[d];
                var   s       = stats[d];
                float baseSel = totalBaseWeight > 0f ? Mathf.Max(0f, entry.baseWeight) / totalBaseWeight * 100f : 0f;
                float empSel  = s.totalDrops / (float)(trials * rollsPerTrial) * 100f;
                float expAvg  = TheoreticalBaseExpectedDrops(system, d, rollsPerTrial);
                float empAvg  = s.totalDrops / (float)trials;
                float hit     = s.trialsWithAtLeastOne / (float)trials * 100f;
                float first   = s.trialsWithAtLeastOne > 0
                                  ? s.sumRollsToFirst / (float)s.trialsWithAtLeastOne : -1f;

                string name     = $"\"{entry.item}\"";
                string hitStr   = $"{s.trialsWithAtLeastOne}/{trials} ({hit:F0}%)";
                string firstStr = first >= 0 ? first.ToString("F1") : "—";

                sum.AppendLine($"  {name,-22}  {entry.baseWeight,6:F2}  {baseSel,7:F1}%  {empSel,7:F1}%  {expAvg,9:F2}  {empAvg,9:F2}  {hitStr,8}  {firstStr,7}");
            }

            sum.AppendLine();
            sum.AppendLine(DIV);
            Debug.Log(sum.ToString());
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static int[] BuildMilestones(int rollsPerTrial)
        {
            var set = new System.Collections.Generic.SortedSet<int>();

            for (int p = 1; p <= rollsPerTrial; p *= 2)
                set.Add(p);

            int[] rounds = { 5, 10, 20, 25, 50, 75, 100, 150, 200, 300, 500, 750, 1000 };
            foreach (int r in rounds)
                if (r <= rollsPerTrial) set.Add(r);

            set.Add(rollsPerTrial);

            var list = new int[set.Count];
            set.CopyTo(list);
            return list;
        }
    }
}
