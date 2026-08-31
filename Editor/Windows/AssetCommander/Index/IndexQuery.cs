using System;
using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The index's maps and every lookup over them, split out of ProjectIndex so an analysis
    // mode can be pointed at a hand-built set of records in a test. ProjectIndex keeps the
    // build orchestration and owns one of these; nothing else about the data moved, so the
    // modes and the tests exercise the same derivation.
    public sealed class IndexQuery
    {
        // Guids Unity ships inside the editor itself. They resolve to nothing on disk, so
        // without this set every default material and built-in font reads as a broken
        // reference.
        private static readonly HashSet<string> Builtins = new HashSet<string>(StringComparer.Ordinal)
        {
            "00000000000000000000000000000000",
            "0000000000000000d000000000000000", // unity editor resources
            "0000000000000000e000000000000000", // unity builtin extra
            "0000000000000000f000000000000000", // unity default resources
        };

        private readonly Dictionary<string, AssetRecord> _byGuid =
            new Dictionary<string, AssetRecord>(StringComparer.Ordinal);

        private readonly Dictionary<string, AssetRecord> _byPath =
            new Dictionary<string, AssetRecord>(StringComparer.Ordinal);

        private readonly HashSet<string> _brokenOwners = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _missingScriptOwners = new HashSet<string>(StringComparer.Ordinal);

        private Dictionary<string, List<string>> _referencedBy =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private Dictionary<string, List<string>> _unresolved =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private Dictionary<ulong, List<string>> _duplicates = new Dictionary<ulong, List<string>>();

        public IndexQuery()
        {
        }

        public IndexQuery(IEnumerable<AssetRecord> records)
        {
            Replace(records);
        }

        public int Count => _byGuid.Count;

        public int BrokenReferenceCount => _unresolved.Count;

        public int DuplicateGroupCount => _duplicates.Count;

        public ICollection<AssetRecord> Records => _byGuid.Values;

        public IEnumerable<KeyValuePair<ulong, List<string>>> DuplicateGroups => _duplicates;

        public IEnumerable<KeyValuePair<string, List<string>>> UnresolvedReferences => _unresolved;

        // ── Lookups. Every one is a map hit: no disk I/O, so a mode can call these per row
        // while the user types. ─────────────────────────────────────────────────────────────

        public bool Contains(string guid) => guid != null && _byGuid.ContainsKey(guid);

        public bool TryGetByGuid(string guid, out AssetRecord record)
        {
            if (!string.IsNullOrEmpty(guid)) return _byGuid.TryGetValue(guid, out record);

            record = null;
            return false;
        }

        public bool TryGetByPath(string path, out AssetRecord record)
        {
            if (!string.IsNullOrEmpty(path)) return _byPath.TryGetValue(path, out record);

            record = null;
            return false;
        }

        public IReadOnlyList<string> GetReferencedBy(string guid) =>
            guid != null && _referencedBy.TryGetValue(guid, out var list) ? list : Array.Empty<string>();

        public IReadOnlyList<string> GetReferrersOfMissing(string missingGuid) =>
            missingGuid != null && _unresolved.TryGetValue(missingGuid, out var list)
                ? list
                : Array.Empty<string>();

        public IReadOnlyList<string> GetDuplicates(ulong contentHash) =>
            _duplicates.TryGetValue(contentHash, out var list) ? list : Array.Empty<string>();

        public bool HasBrokenReferences(string guid) => guid != null && _brokenOwners.Contains(guid);

        public bool HasMissingScript(string guid) => guid != null && _missingScriptOwners.Contains(guid);

        public static bool IsBuiltinGuid(string guid) => guid != null && Builtins.Contains(guid);

        // A dependency that is neither in the project nor shipped with the editor. The owner's
        // record cannot answer this on its own — resolution is a question about the whole set.
        public bool IsMissing(string guid) =>
            !string.IsNullOrEmpty(guid) && !_byGuid.ContainsKey(guid) && !Builtins.Contains(guid);

        // ── Mutation. ProjectIndex is the only caller; the derived maps are rebuilt by hand
        // afterwards so a batch of changes pays for one pass, not one per file. ─────────────

        public void Replace(IEnumerable<AssetRecord> records)
        {
            _byGuid.Clear();
            _byPath.Clear();

            if (records != null)
            {
                foreach (var record in records)
                {
                    if (record == null) continue;
                    _byGuid[record.Guid] = record;
                    _byPath[record.Path] = record;
                }
            }

            RebuildDerived();
        }

        public void Put(AssetRecord record)
        {
            if (record == null) return;

            // A move reaches the index as delete-plus-import, but a re-import under a new path
            // does not, so the stale path entry has to go explicitly.
            if (_byGuid.TryGetValue(record.Guid, out var previous) && previous.Path != record.Path)
                _byPath.Remove(previous.Path);

            _byGuid[record.Guid] = record;
            _byPath[record.Path] = record;
        }

        public bool RemoveByPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !_byPath.TryGetValue(path, out var record)) return false;

            _byPath.Remove(path);
            _byGuid.Remove(record.Guid);
            return true;
        }

        // Rebuilt wholesale rather than patched. For 50k records this is a few hundred thousand
        // dictionary operations — tens of milliseconds, once per drained batch run — and
        // patching four interlocking maps by hand is where this kind of index goes wrong.
        public void RebuildDerived()
        {
            _referencedBy = new Dictionary<string, List<string>>(_byGuid.Count, StringComparer.Ordinal);
            _unresolved = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            _brokenOwners.Clear();
            _missingScriptOwners.Clear();

            var hashes = new Dictionary<ulong, List<string>>();

            foreach (var record in _byGuid.Values)
            {
                foreach (var dependency in record.DependencyGuids)
                {
                    if (_byGuid.ContainsKey(dependency))
                    {
                        Append(_referencedBy, dependency, record.Guid);
                    }
                    else if (!Builtins.Contains(dependency))
                    {
                        Append(_unresolved, dependency, record.Guid);
                        _brokenOwners.Add(record.Guid);
                    }
                }

                foreach (var script in record.ScriptGuids)
                {
                    if (_byGuid.ContainsKey(script) || Builtins.Contains(script)) continue;

                    _missingScriptOwners.Add(record.Guid);
                    break;
                }

                if (record.ContentHash != 0UL) Append(hashes, record.ContentHash, record.Guid);
            }

            _duplicates = new Dictionary<ulong, List<string>>();
            foreach (var group in hashes)
            {
                if (group.Value.Count > 1) _duplicates.Add(group.Key, group.Value);
            }
        }

        private static void Append<TKey>(Dictionary<TKey, List<string>> map, TKey key, string value)
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<string>(1);
                map.Add(key, list);
            }

            list.Add(value);
        }
    }
}
