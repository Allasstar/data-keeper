using System.Collections.Generic;
using System.Text;
using DataKeeper.Pity;
using UnityEngine;

namespace DataKeeper.Pity
{
    /// <summary>
    /// Standalone simulation tester for any <see cref="PitySystem{T}"/>.
    /// Call <see cref="Run{T}"/> from any MonoBehaviour or editor tool.
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// PitySystemTester.Run(myPitySystem, luck: 0f, players: 50, attempts: 100);
    /// </code>
    /// </summary>
    public class PitySystemTester
    {
        // ── public entry point ────────────────────────────────────────────────

        /// <summary>
        /// Simulates <paramref name="players"/> independent players each performing
        /// <paramref name="attempts"/> rolls against <paramref name="system"/>,
        /// then logs one detailed message per drop and one summary message.
        /// </summary>
        /// <typeparam name="T">Drop type of the pity system.</typeparam>
        /// <param name="system">The pity system to test. State is reset before and after simulation.</param>
        /// <param name="luck">Luck value passed to every Roll call.</param>
        /// <param name="players">Number of simulated players.</param>
        /// <param name="attempts">Number of roll attempts per player.</param>
        public void Run<T>(PitySystem<T> system, float luck = 0f, int players = 50, int attempts = 100)
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

            players  = Mathf.Max(1, players);
            attempts = Mathf.Max(1, attempts);
            luck     = Mathf.Max(0f, luck);

            var results = Simulate(system, luck, players, attempts);
            Log(system, results, luck, players, attempts);

            // leave system in clean state after test
            system.ResetAll();
        }

        // ── simulation ────────────────────────────────────────────────────────

        private struct DropResult
        {
            public int   totalDrops;
            public int   playersWith0Drops;
            public int   minAttemptsToFirst;
            public int   maxAttemptsToFirst;
            public long  sumAttemptsToFirst;
            public int   playersWithFirst;
            public int[] dropsPerPlayer;
        }

        private DropResult[] Simulate<T>(PitySystem<T> system, float luck, int players, int attempts)
        {
            int dropCount = system.Drops.Count;

            var results = new DropResult[dropCount];
            for (int d = 0; d < dropCount; d++)
            {
                results[d].minAttemptsToFirst = int.MaxValue;
                results[d].maxAttemptsToFirst = int.MinValue;
                results[d].dropsPerPlayer     = new int[attempts + 1];
            }

            var firstDropAttempt = new int[players, dropCount];
            var playerDropCount  = new int[players, dropCount];

            for (int p = 0; p < players; p++)
                for (int d = 0; d < dropCount; d++)
                    firstDropAttempt[p, d] = -1;

            for (int p = 0; p < players; p++)
            {
                system.ResetAll();

                for (int a = 0; a < attempts; a++)
                {
                    if (!system.Roll(luck, out T drop)) continue;

                    for (int d = 0; d < dropCount; d++)
                    {
                        if (!EqualityComparer<T>.Default.Equals(system.Drops[d].drop, drop))
                            continue;

                        results[d].totalDrops++;
                        playerDropCount[p, d]++;

                        if (firstDropAttempt[p, d] == -1)
                        {
                            firstDropAttempt[p, d]         = a + 1;
                            results[d].sumAttemptsToFirst += a + 1;
                            results[d].playersWithFirst++;

                            if (a + 1 < results[d].minAttemptsToFirst) results[d].minAttemptsToFirst = a + 1;
                            if (a + 1 > results[d].maxAttemptsToFirst) results[d].maxAttemptsToFirst = a + 1;
                        }
                        break;
                    }
                }
            }

            for (int p = 0; p < players; p++)
                for (int d = 0; d < dropCount; d++)
                {
                    int cnt = playerDropCount[p, d];
                    if (cnt == 0) results[d].playersWith0Drops++;
                    results[d].dropsPerPlayer[Mathf.Min(cnt, attempts)]++;
                }

            return results;
        }

        // ── logging ───────────────────────────────────────────────────────────

        private const string DIV  = "────────────────────────────────────────────────────────────";
        private const string THIN = "············································································";
        private const int    BAR  = 28;

        private void Log<T>(PitySystem<T> system, DropResult[] results, float luck, int players, int attempts)
        {
            int dropCount = system.Drops.Count;

            // one message per drop
            for (int d = 0; d < dropCount; d++)
            {
                var   entry    = system.Drops[d];
                var   r        = results[d];
                float baseEff  = entry.GetEffectiveChancePublic(luck, 0);
                float avgDrops = r.totalDrops / (float)players;
                float hitRate  = r.playersWithFirst / (float)players * 100f;
                float avgFirst = r.playersWithFirst > 0
                                   ? r.sumAttemptsToFirst / (float)r.playersWithFirst : -1f;

                var sb = new StringBuilder();
                sb.AppendLine($"  ▶  DROP [{d}]  \"{entry.drop}\"   |   players:{players}  attempts:{attempts}  luck:{luck:F2}");
                sb.AppendLine(THIN);
                sb.AppendLine($"  Config    base={entry.baseChance:P0}  weight={entry.weight:F1}  " +
                              $"pityStart={entry.pityStartAt}  +{entry.increasePerAttempt:P0}/roll  " +
                              $"guaranteed={(entry.guaranteedAt > 0 ? entry.guaranteedAt.ToString() : "off")}  " +
                              $"autoReset={(entry.autoReset ? "yes" : "no")}");
                sb.AppendLine($"  Eff.chance at luck {luck:F2}  →  {baseEff:P1}");
                sb.AppendLine();
                sb.AppendLine($"  Total drops          {r.totalDrops}");
                sb.AppendLine($"  Avg drops / player   {avgDrops:F2}");
                sb.AppendLine($"  Players who got ≥1   {r.playersWithFirst} / {players}  ({hitRate:F1}%)");
                sb.AppendLine($"  Players with 0 drops {r.playersWith0Drops} / {players}  ({r.playersWith0Drops / (float)players * 100f:F1}%)");

                if (r.playersWithFirst > 0)
                    sb.AppendLine($"  First drop: avg att  {avgFirst:F1}   min {r.minAttemptsToFirst}   max {r.maxAttemptsToFirst}");

                sb.AppendLine();
                sb.AppendLine("  Distribution (players by drop count):");

                int maxBucket = 0;
                for (int i = r.dropsPerPlayer.Length - 1; i >= 0; i--)
                    if (r.dropsPerPlayer[i] > 0) { maxBucket = i; break; }

                for (int i = 0; i <= Mathf.Min(maxBucket, 20); i++)
                {
                    int    count  = r.dropsPerPlayer[i];
                    float  pct    = count / (float)players;
                    int    filled = Mathf.RoundToInt(pct * BAR);
                    string bar    = new string('█', filled) + new string('░', BAR - filled);
                    string label  = i == r.dropsPerPlayer.Length - 1 ? $"{i}+" : $"{i}x";
                    sb.AppendLine($"    {label,3}  {bar}  {count,4} players  ({pct * 100f:F1}%)");
                }

                sb.AppendLine(DIV);
                Debug.Log(sb.ToString());
            }

            // summary message
            var sum = new StringBuilder();
            sum.AppendLine("PITY SYSTEM  —  SUMMARY");
            sum.AppendLine("══════════════════════════════════════════════════════════════");
            sum.AppendLine("                 PITY SYSTEM  —  SUMMARY                      ");
            sum.AppendLine("══════════════════════════════════════════════════════════════");
            sum.AppendLine($"  Players : {players}   |   Attempts per player : {attempts}   |   Luck : {luck:F2}");
            sum.AppendLine(DIV);
            sum.AppendLine();
            sum.AppendLine($"  {"Drop",-24}  {"Total",7}  {"Avg/plr",8}  {"Got ≥1",14}  {"Avg 1st",8}");
            sum.AppendLine($"  {new string('-', 24)}  {new string('-', 7)}  {new string('-', 8)}  {new string('-', 14)}  {new string('-', 8)}");

            for (int d = 0; d < dropCount; d++)
            {
                var   entry   = system.Drops[d];
                var   r       = results[d];
                float avg     = r.totalDrops / (float)players;
                float hit     = r.playersWithFirst / (float)players * 100f;
                float first   = r.playersWithFirst > 0
                                  ? r.sumAttemptsToFirst / (float)r.playersWithFirst : -1f;

                string name     = $"\"{entry.drop}\"";
                string gotLine  = $"{r.playersWithFirst}/{players} ({hit:F0}%)";
                string firstStr = first >= 0 ? first.ToString("F1") : "—";

                sum.AppendLine($"  {name,-24}  {r.totalDrops,7}  {avg,8:F2}  {gotLine,14}  {firstStr,8}");
            }

            sum.AppendLine();
            sum.AppendLine(DIV);
            Debug.Log(sum.ToString());
        }
    }
}