using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // One asset as the workers see it: plain strings only, with everything AssetDatabase had
    // to answer already resolved on the main thread.
    public readonly struct AssetEntry
    {
        public readonly string Guid;
        public readonly string Path;
        public readonly string AbsolutePath;
        public readonly bool IsFolder;

        public AssetEntry(string guid, string path, string absolutePath, bool isFolder)
        {
            Guid = guid;
            Path = path;
            AbsolutePath = absolutePath;
            IsFolder = isFolder;
        }
    }

    // The only thing workers write outside their own partition. Interlocked rather than the
    // plan's "volatile int" because ++ on a volatile field is still a read-modify-write.
    public sealed class ProgressCounter
    {
        private int _value;

        public void Increment() => Interlocked.Increment(ref _value);

        public int Read() => Volatile.Read(ref _value);
    }

    public sealed class BuildOutput
    {
        public AssetRecord[] Records;
        public int ReusedFromCache;
        public long ElapsedMilliseconds;
    }

    public static class IndexBuilder
    {
        // MAIN THREAD ONLY. Everything below Parse is System.IO over plain strings.
        public static AssetEntry[] SnapshotProject(string projectRoot)
        {
            var localPackagePrefixes = LocalPackagePrefixes();
            var all = AssetDatabase.GetAllAssetPaths();
            var entries = new List<AssetEntry>(all.Length);

            foreach (var path in all)
            {
                if (TrySnapshotPath(path, projectRoot, localPackagePrefixes, out var entry))
                    entries.Add(entry);
            }

            return entries.ToArray();
        }

        // MAIN THREAD ONLY.
        public static bool TrySnapshotPath(string path, string projectRoot,
            List<string> localPackagePrefixes, out AssetEntry entry)
        {
            entry = default;
            if (!IsIndexable(path, localPackagePrefixes)) return false;

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return false;

            entry = new AssetEntry(guid, path, projectRoot + "/" + path, AssetDatabase.IsValidFolder(path));
            return true;
        }

        // MAIN THREAD ONLY.
        public static List<string> LocalPackagePrefixes()
        {
            var prefixes = new List<string>();
            foreach (var package in PackageInfo.GetAllRegisteredPackages())
            {
                // Registry, git and tarball packages live in an immutable PackageCache; only
                // embedded and local ones can be edited, so only they are worth indexing.
                if (package.source != PackageSource.Embedded && package.source != PackageSource.Local) continue;
                if (!string.IsNullOrEmpty(package.assetPath)) prefixes.Add(package.assetPath + "/");
            }

            return prefixes;
        }

        public static BuildOutput Parse(AssetEntry[] entries, Dictionary<string, AssetRecord> cache,
            bool scanText, ProgressCounter progress, CancellationToken token)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var records = new AssetRecord[entries.Length];
            int reused = 0;

            // One core is left to the editor so a rebuild does not make the UI unresponsive —
            // this phase's playtest requires the window stay draggable while it runs.
            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
            };

            Parallel.ForEach(Partitioner.Create(0, entries.Length), options,
                () => new ScanScratch(),
                (range, _, scratch) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        records[i] = ParseEntry(entries[i], scratch, cache, scanText, out bool fromCache);
                        if (fromCache) Interlocked.Increment(ref reused);
                        progress.Increment();
                    }

                    return scratch;
                },
                _ => { });

            watch.Stop();

            return new BuildOutput
            {
                Records = records,
                ReusedFromCache = reused,
                ElapsedMilliseconds = watch.ElapsedMilliseconds,
            };
        }

        // WORKER THREAD. No UnityEditor/UnityEngine call may appear inside this method.
        public static AssetRecord ParseEntry(in AssetEntry entry, ScanScratch scratch,
            Dictionary<string, AssetRecord> cache, bool scanText, out bool reusedFromCache)
        {
            reusedFromCache = false;

            var record = new AssetRecord
            {
                Guid = entry.Guid,
                Path = entry.Path,
                Kind = entry.IsFolder ? AssetKind.Folder : AssetKinds.FromPath(entry.Path),
            };

            if (entry.IsFolder) return record;

            var file = new FileInfo(entry.AbsolutePath);
            var meta = new FileInfo(entry.AbsolutePath + ".meta");

            if (file.Exists)
            {
                record.Size = file.Length;
                record.LastWriteTicks = file.LastWriteTimeUtc.Ticks;
            }

            if (meta.Exists) record.MetaWriteTicks = meta.LastWriteTimeUtc.Ticks;

            if (cache != null
                && cache.TryGetValue(entry.Guid, out var cached)
                && cached.Size == record.Size
                && cached.LastWriteTicks == record.LastWriteTicks
                && cached.MetaWriteTicks == record.MetaWriteTicks)
            {
                // A move is the one change that touches neither timestamp.
                cached.Path = entry.Path;
                cached.Kind = record.Kind;
                reusedFromCache = true;
                return cached;
            }

            scratch.Reset();

            if (file.Exists)
                GuidScanner.ScanFile(entry.AbsolutePath, scratch,
                    forceGuidScan: scanText && AssetKinds.IsYamlExtension(entry.Path),
                    hashContent: true);

            record.ContentHash = scratch.Hash.Length > 0 ? scratch.Hash.Digest() : 0UL;

            // The .meta carries importer-side references — model materials, atlas members,
            // avatars. Omitting them produces false "unused" and false "broken" results. It is
            // always text, so it is scanned even when the project is not in ForceText mode.
            if (meta.Exists)
                GuidScanner.ScanFile(entry.AbsolutePath + ".meta", scratch,
                    forceGuidScan: true, hashContent: false);

            record.DependencyGuids = scratch.DependencyStrings(entry.Guid);
            record.ScriptGuids = scratch.ScriptStrings();
            return record;
        }

        private static bool IsIndexable(string path, List<string> localPackagePrefixes)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.StartsWith("Assets/", StringComparison.Ordinal) || path == "Assets") return true;
            if (!path.StartsWith("Packages/", StringComparison.Ordinal)) return false;

            foreach (var prefix in localPackagePrefixes)
                if (path.StartsWith(prefix, StringComparison.Ordinal)) return true;

            return false;
        }

        public static string ResolveProjectRoot() =>
            Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/') ?? "";
    }
}
