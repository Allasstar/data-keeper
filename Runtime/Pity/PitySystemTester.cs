using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Monte Carlo simulation tester for any <see cref="PitySystem{T}"/>.
    ///
    /// <para>
    ///   Runs <c>players</c> independent simulated players, each performing <c>rolls</c> rolls.
    ///   For each drop entry the report shows:
    ///   <list type="bullet">
    ///     <item>First drop — on which roll the player first received this item (avg / min / max).</item>
    ///     <item>Most unlucky streak — the longest run of rolls without this item across all players.</item>
    ///     <item>Drops per player — how many times a player got this item (avg / min / max).</item>
    ///     <item>Players who never got it — how many players finished all rolls without a single drop.</item>
    ///   </list>
    ///   Each drop entry is logged as a separate Unity console message so the console
    ///   does not overflow when there are many entries.
    /// </para>
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// new PitySystemTester().Run(myPitySystem, players: 10_000, rolls: 200);
    /// </code>
    /// </summary>
    public class PitySystemTester
    {
        // ── public entry point ────────────────────────────────────────────────

        /// <summary>
        /// Runs a Monte Carlo simulation against <paramref name="system"/> and logs a
        /// human-readable report to the Unity console.
        /// Each drop entry is printed as a separate console message.
        /// </summary>
        /// <param name="system">The pity system to test. State is reset before and after.</param>
        /// <param name="luck">Luck value forwarded to every <see cref="PitySystem{T}.Roll"/> call.</param>
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
            rolls   = Mathf.Max(1, rolls);

            var results = Simulate(system, luck, players, rolls);
            Log(system, results, players, rolls, luck);

            system.ResetAll();
        }

        // ── per-drop result ───────────────────────────────────────────────────

        private struct DropResult
        {
            // first-drop stats (only players who got at least one drop)
            public int   playersWithDrop;   // players that received this drop at least once
            public long  sumFirstRoll;      // sum of first-drop roll numbers
            public int   minFirstRoll;      // earliest first drop across all players
            public int   maxFirstRoll;      // latest   first drop across all players

            // drops-per-player stats
            public long  totalDrops;        // total drops across all players
            public int   minDropsPerPlayer; // fewest  drops any single player got
            public int   maxDropsPerPlayer; // most    drops any single player got

            // unlucky streak: longest consecutive rolls without this drop
            public int   worstStreak;       // across all players
        }

        // ── simulation ────────────────────────────────────────────────────────

        private DropResult[] Simulate<T>(PitySystem<T> system, float luck, int players, int rolls)
        {
            int dropCount = system.Drops.Count;

            var results = new DropResult[dropCount];
            for (int d = 0; d < dropCount; d++)
            {
                results[d].minFirstRoll      = int.MaxValue;
                results[d].maxFirstRoll      = 0;
                results[d].minDropsPerPlayer = int.MaxValue;
                results[d].maxDropsPerPlayer = 0;
                results[d].worstStreak       = 0;
            }

            // per-player working arrays (reused each player)
            var playerDropCount = new int[dropCount]; // drops this player got
            var playerFirstRoll = new int[dropCount]; // roll number of first drop (0 = none yet)
            var playerLastDrop  = new int[dropCount]; // roll number of last drop  (0 = none yet)

            for (int p = 0; p < players; p++)
            {
                system.ResetAll();

                for (int d = 0; d < dropCount; d++)
                {
                    playerDropCount[d] = 0;
                    playerFirstRoll[d] = 0;
                    playerLastDrop[d]  = 0;
                }

                for (int r = 1; r <= rolls; r++)
                {
                    T drop = system.Roll(luck);

                    for (int d = 0; d < dropCount; d++)
                    {
                        if (!EqualityComparer<T>.Default.Equals(system.Drops[d].item, drop))
                            continue;

                        playerDropCount[d]++;

                        // first drop for this player?
                        if (playerFirstRoll[d] == 0)
                            playerFirstRoll[d] = r;

                        // track the gap since last drop (= unlucky streak between drops)
                        if (playerLastDrop[d] > 0)
                        {
                            int gap = r - playerLastDrop[d] - 1; // rolls with no drop between them
                            if (gap > results[d].worstStreak)
                                results[d].worstStreak = gap;
                        }

                        playerLastDrop[d] = r;
                        break;
                    }
                }

                // after all rolls: accumulate per-player stats
                for (int d = 0; d < dropCount; d++)
                {
                    int count = playerDropCount[d];
                    int first = playerFirstRoll[d];

                    results[d].totalDrops += count;

                    if (count < results[d].minDropsPerPlayer) results[d].minDropsPerPlayer = count;
                    if (count > results[d].maxDropsPerPlayer) results[d].maxDropsPerPlayer = count;

                    if (first > 0)
                    {
                        results[d].playersWithDrop++;
                        results[d].sumFirstRoll += first;
                        if (first < results[d].minFirstRoll) results[d].minFirstRoll = first;
                        if (first > results[d].maxFirstRoll) results[d].maxFirstRoll = first;
                    }
                    else
                    {
                        // player never got this drop — the whole session is a streak
                        if (rolls > results[d].worstStreak)
                            results[d].worstStreak = rolls;
                    }
                }
            }

            // fix sentinel values for drops nobody ever received
            for (int d = 0; d < dropCount; d++)
            {
                if (results[d].minFirstRoll      == int.MaxValue) results[d].minFirstRoll      = 0;
                if (results[d].minDropsPerPlayer == int.MaxValue) results[d].minDropsPerPlayer = 0;
            }

            return results;
        }

        // ── logging ───────────────────────────────────────────────────────────

        private const string LINE = "------------------------------------------------------------";

        private void Log<T>(PitySystem<T> system, DropResult[] results, int players, int rolls, float luck)
        {
            int dropCount = system.Drops.Count;

            // compute total base weight once
            float totalBaseW = 0f;
            foreach (var e in system.Drops) totalBaseW += Mathf.Max(0f, e.baseWeight);

            // ── Header (one log entry) ────────────────────────────────────────
            var header = new StringBuilder();
            header.AppendLine("=== PITY SYSTEM - MONTE CARLO SIMULATION ===");
            header.AppendLine($"  Players : {players:N0}");
            header.AppendLine($"  Rolls   : {rolls} per player");
            if (luck != 0f)
                header.AppendLine($"  Luck    : {luck:P}");
            header.AppendLine($"  Drops   : {dropCount}  (each drop is logged separately below)");
            Debug.Log(header.ToString());

            // ── One log entry per drop ────────────────────────────────────────
            for (int d = 0; d < dropCount; d++)
            {
                var entry = system.Drops[d];
                var r     = results[d];

                int   neverGot = players - r.playersWithDrop;
                float avgDrops = r.totalDrops / (float)players;
                float avgFirst = r.playersWithDrop > 0
                                   ? r.sumFirstRoll / (float)r.playersWithDrop
                                   : 0f;
                float hitRate  = r.playersWithDrop / (float)players * 100f;
                float basePct  = totalBaseW > 0f
                                   ? Mathf.Max(0f, entry.baseWeight) / totalBaseW * 100f
                                   : 0f;

                var sb = new StringBuilder();
                sb.AppendLine($"[{d}] \"{entry.item}\"   base chance: {basePct:F1}%");
                sb.AppendLine(LINE);

                // ── First Drop ──────────────────────────────────────────────
                sb.AppendLine("FIRST DROP  (roll on which a player first received this item)");
                if (r.playersWithDrop > 0)
                {
                    sb.AppendLine($"  Average  : roll {avgFirst:F1}");
                    sb.AppendLine($"  Earliest : roll {r.minFirstRoll}   <- luckiest player");
                    sb.AppendLine($"  Latest   : roll {r.maxFirstRoll}   <- unluckiest player who still got it");
                }
                else
                {
                    sb.AppendLine("  No player received this drop.");
                }
                sb.AppendLine();

                // ── Unlucky Streak ──────────────────────────────────────────
                sb.AppendLine("UNLUCKY STREAK  (longest run of rolls without this item)");
                sb.AppendLine($"  Worst streak : {r.worstStreak} rolls in a row with no drop");
                if (entry.guaranteedDropThreshold > 0)
                    sb.AppendLine($"  Hard cap     : {entry.guaranteedDropThreshold} misses  (pity guarantee)");
                sb.AppendLine();

                // ── Drops Per Player ────────────────────────────────────────
                sb.AppendLine("DROPS PER PLAYER  (across all rolls)");
                sb.AppendLine($"  Average : {avgDrops:F2} drops");
                sb.AppendLine($"  Minimum : {r.minDropsPerPlayer} drops   <- least lucky player");
                sb.AppendLine($"  Maximum : {r.maxDropsPerPlayer} drops   <- most lucky player");
                sb.AppendLine();

                // ── Coverage ────────────────────────────────────────────────
                sb.AppendLine("COVERAGE");
                sb.AppendLine($"  Got at least 1 : {r.playersWithDrop:N0} / {players:N0} players  ({hitRate:F1}%)");
                if (neverGot > 0)
                    sb.AppendLine($"  Never got it   : {neverGot:N0} / {players:N0} players  ({neverGot / (float)players * 100f:F1}%)");
                else
                    sb.AppendLine("  Never got it   : 0 players  - everyone received it at least once!");

                sb.AppendLine(LINE);

                Debug.Log(sb.ToString());
            }
        }
    }
}
