using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataKeeper.Signals;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public enum IndexState
    {
        Empty = 0,
        Building = 1,
        Ready = 2,
    }

    [InitializeOnLoad]
    public static class ProjectIndex
    {
        public static readonly Signal OnIndexChanged = new Signal();

        // The maps themselves live in IndexQuery so a test can build one by hand; this class
        // owns building and refreshing it.
        private static readonly IndexQuery Data = new IndexQuery();

        private static readonly HashSet<string> PendingImports = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> PendingDeletes = new HashSet<string>(StringComparer.Ordinal);

        private static readonly string ProjectRoot;
        private static readonly string CacheFilePath;
        private static readonly string UnityVersion;

        // One per build rather than one shared counter: a cancelled build's workers keep
        // running until they next check the token, and they would otherwise go on bumping
        // the counter its replacement is already reading.
        private static ProgressCounter _progress = new ProgressCounter();

        private static CancellationTokenSource _cancellation;
        private static Task<BuildOutput> _buildTask;
        private static ScanScratch _drainScratch;
        private static List<AssetRecord> _degradedQueue;

        private static int _plannedTotal;
        private static bool _rebuildPending;
        private static bool _forcePending;
        private static bool _derivedDirty;
        private static bool _cacheDirty;

        static ProjectIndex()
        {
            // Resolved once, here, because every one of these is main-thread only and the
            // build task needs all three.
            ProjectRoot = IndexBuilder.ResolveProjectRoot();
            CacheFilePath = ProjectRoot + "/Library/DataKeeper/asset-commander-index.bin";
            UnityVersion = Application.unityVersion;

            EditorApplication.update += Pump;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.quitting += Shutdown;
        }

        public static IndexState State { get; private set; } = IndexState.Empty;

        public static bool TextScanningEnabled { get; private set; } = true;

        public static bool IsReady => State == IndexState.Ready;

        // What the analysis modes query. Handed out rather than forwarded per call so a mode
        // holds one reference for the length of an evaluation.
        public static IndexQuery Query => Data;

        public static int AssetCount => Data.Count;

        public static long BuildMilliseconds { get; private set; }

        public static int ReusedFromCache { get; private set; }

        public static bool LoadedFromCache => AssetCount > 0 && ReusedFromCache == AssetCount;

        public static int BrokenReferenceCount => Data.BrokenReferenceCount;

        public static int DuplicateGroupCount => Data.DuplicateGroupCount;

        public static float Progress01 =>
            State == IndexState.Building && _plannedTotal > 0
                ? Mathf.Clamp01((float)_progress.Read() / _plannedTotal)
                : 0f;

        public static string StatusText
        {
            get
            {
                if (State == IndexState.Building)
                    return _plannedTotal > 0
                        ? $"Indexing… {_progress.Read():N0} / {_plannedTotal:N0}"
                        : "Indexing…";

                if (_degradedQueue != null)
                    return $"Resolving dependencies… {_degradedQueue.Count:N0} left";

                if (State != IndexState.Ready) return "Index not built";

                return LoadedFromCache
                    ? $"Indexed {AssetCount:N0} assets (loaded from cache) in {BuildMilliseconds}ms"
                    : $"Indexed {AssetCount:N0} assets in {BuildMilliseconds}ms";
            }
        }

        public static void EnsureBuilt()
        {
            if (State != IndexState.Empty || _buildTask != null) return;
            StartBuild(force: false);
        }

        public static void RequestRebuild(bool force = true) => StartBuild(force);

        // ── Queries. Kept as forwarders so callers that only need one lookup do not have to
        // reach through Query. ──────────────────────────────────────────────────────────────

        public static bool TryGetByGuid(string guid, out AssetRecord record) =>
            Data.TryGetByGuid(guid, out record);

        public static bool TryGetByPath(string path, out AssetRecord record) =>
            Data.TryGetByPath(path, out record);

        public static IReadOnlyList<string> GetReferencedBy(string guid) => Data.GetReferencedBy(guid);

        public static IReadOnlyList<string> GetReferrersOfMissing(string missingGuid) =>
            Data.GetReferrersOfMissing(missingGuid);

        public static IReadOnlyList<string> GetDuplicates(ulong contentHash) => Data.GetDuplicates(contentHash);

        public static bool HasBrokenReferences(string guid) => Data.HasBrokenReferences(guid);

        public static bool HasMissingScript(string guid) => Data.HasMissingScript(guid);

        public static bool IsBuiltinGuid(string guid) => IndexQuery.IsBuiltinGuid(guid);

        public static IEnumerable<AssetRecord> AllRecords => Data.Records;

        public static IEnumerable<KeyValuePair<ulong, List<string>>> DuplicateGroups => Data.DuplicateGroups;

        public static IEnumerable<KeyValuePair<string, List<string>>> UnresolvedReferences =>
            Data.UnresolvedReferences;

        // ── Incremental invalidation, fed by IndexPostprocessor. ─────────────────────────

        public static void QueueChanges(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (State == IndexState.Empty && _buildTask == null) return;

            Queue(deleted, PendingDeletes, PendingImports);
            Queue(movedFrom, PendingDeletes, PendingImports);
            Queue(imported, PendingImports, PendingDeletes);
            Queue(moved, PendingImports, PendingDeletes);
        }

        private static void Queue(string[] paths, HashSet<string> into, HashSet<string> outOf)
        {
            if (paths == null) return;

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                outOf.Remove(path);
                into.Add(path);
            }
        }

        // ── Build ───────────────────────────────────────────────────────────────────────

        private static void StartBuild(bool force)
        {
            if (_buildTask != null)
            {
                if (!force) return;
                Cancel();
            }

            // A rebuild started mid-compile races the domain reload that follows it, and one
            // started mid-import indexes a project state that is about to change.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                _rebuildPending = true;
                _forcePending |= force;
                return;
            }

            _rebuildPending = false;
            _forcePending = false;
            _degradedQueue = null;

            TextScanningEnabled = EditorSettings.serializationMode == SerializationMode.ForceText;

            var entries = IndexBuilder.SnapshotProject(ProjectRoot);
            _plannedTotal = entries.Length;
            _progress = new ProgressCounter();
            PendingImports.Clear();
            PendingDeletes.Clear();

            State = IndexState.Building;

            // Deliberately not disposed: the running Parallel.ForEach still polls this token,
            // and disposing it out from under those workers is a race.
            _cancellation = new CancellationTokenSource();

            var token = _cancellation.Token;
            var cachePath = force ? null : CacheFilePath;
            var version = UnityVersion;
            var progress = _progress;
            bool scanText = TextScanningEnabled;

            _buildTask = Task.Run(() =>
            {
                var cache = cachePath == null ? null : IndexCache.TryLoad(cachePath, version);
                return IndexBuilder.Parse(entries, cache, scanText, progress, token);
            }, token);
        }

        private static void Cancel()
        {
            _cancellation?.Cancel();
            _cancellation = null;
            _buildTask = null;
        }

        private static void Shutdown()
        {
            if (_cacheDirty && State == IndexState.Ready) SaveCache();
            Cancel();
        }

        private static void Pump()
        {
            if (_rebuildPending && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                StartBuild(_forcePending);
                return;
            }

            if (_buildTask != null)
            {
                if (_buildTask.IsCompleted) CompleteBuild();
                return;
            }

            if (State != IndexState.Ready) return;

            DrainPendingChanges();
            FillDegradedDependencies();
        }

        private static void CompleteBuild()
        {
            var task = _buildTask;
            _buildTask = null;
            _cancellation = null;

            if (task.IsCanceled)
            {
                State = Data.Count > 0 ? IndexState.Ready : IndexState.Empty;
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError($"[AssetCommander] Index build failed: {task.Exception?.GetBaseException()}");
                State = Data.Count > 0 ? IndexState.Ready : IndexState.Empty;
                return;
            }

            var output = task.Result;

            Data.Replace(output.Records);

            ReusedFromCache = output.ReusedFromCache;
            BuildMilliseconds = output.ElapsedMilliseconds;
            State = IndexState.Ready;
            _derivedDirty = false;
            _cacheDirty = true;

            SaveCache();

            // In degraded mode the workers produced hashes and .meta references but no
            // in-file dependencies; those come from the main thread, a batch per tick.
            if (!TextScanningEnabled) _degradedQueue = new List<AssetRecord>(Data.Records);

            OnIndexChanged.Invoke();
        }

        private static void SaveCache()
        {
            if (State != IndexState.Ready) return;
            if (IndexCache.Save(CacheFilePath, UnityVersion, Data.Records)) _cacheDirty = false;
        }

        // ── Incremental drain ───────────────────────────────────────────────────────────

        // Batched onto the main thread rather than handed to a worker as the plan sketched:
        // a change set is normally one or two files, and the round trip through a task plus a
        // second apply tick costs more than the parse. A 5000-file import trickles through at
        // this rate instead of forcing the full rebuild it would otherwise trigger.
        private const int DrainBatch = 32;

        private static void DrainPendingChanges()
        {
            if (PendingDeletes.Count == 0 && PendingImports.Count == 0)
            {
                if (!_derivedDirty) return;

                _derivedDirty = false;
                _cacheDirty = true;
                Data.RebuildDerived();
                OnIndexChanged.Invoke();
                return;
            }

            int budget = DrainBatch;

            while (budget > 0 && PendingDeletes.Count > 0)
            {
                budget--;
                if (Data.RemoveByPath(Take(PendingDeletes))) _derivedDirty = true;
            }

            if (PendingImports.Count == 0) return;

            var prefixes = IndexBuilder.LocalPackagePrefixes();
            _drainScratch = _drainScratch ?? new ScanScratch();

            while (budget > 0 && PendingImports.Count > 0)
            {
                budget--;
                var path = Take(PendingImports);
                if (!IndexBuilder.TrySnapshotPath(path, ProjectRoot, prefixes, out var entry)) continue;

                var record = IndexBuilder.ParseEntry(entry, _drainScratch, null, TextScanningEnabled, out _);
                if (!TextScanningEnabled) record.DependencyGuids = DependenciesFromAssetDatabase(record);

                Data.Put(record);
                _derivedDirty = true;
            }
        }

        private static string Take(HashSet<string> set)
        {
            string value = null;
            foreach (var candidate in set)
            {
                value = candidate;
                break;
            }

            set.Remove(value);
            return value;
        }

        // ── Degraded mode (project not in ForceText) ────────────────────────────────────

        private const int DegradedBatch = 64;

        private static void FillDegradedDependencies()
        {
            if (_degradedQueue == null) return;

            int stop = Math.Max(0, _degradedQueue.Count - DegradedBatch);
            for (int i = _degradedQueue.Count - 1; i >= stop; i--)
            {
                var record = _degradedQueue[i];
                _degradedQueue.RemoveAt(i);
                if (record.Kind != AssetKind.Folder)
                    record.DependencyGuids = DependenciesFromAssetDatabase(record);
            }

            if (_degradedQueue.Count > 0) return;

            _degradedQueue = null;
            _cacheDirty = true;
            Data.RebuildDerived();
            OnIndexChanged.Invoke();
        }

        private static string[] DependenciesFromAssetDatabase(AssetRecord record)
        {
            var paths = AssetDatabase.GetDependencies(record.Path, false);
            if (paths.Length == 0) return AssetRecord.NoGuids;

            var result = new List<string>(paths.Length);
            foreach (var path in paths)
            {
                if (path == record.Path) continue;

                var guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid)) result.Add(guid);
            }

            return result.Count == 0 ? AssetRecord.NoGuids : result.ToArray();
        }
    }
}
