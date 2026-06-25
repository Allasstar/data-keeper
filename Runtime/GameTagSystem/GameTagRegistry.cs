using System;
using System.Collections.Generic;
using DataKeeper.Attributes;
using DataKeeper.Base;
using DataKeeper.Utility;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    // A single tree node. Name is ONE path segment (no separator). ParentId == 0 means root.
    // Id is a stable, never-reused handle: references store the Id, so rename/move never breaks them.
    [Serializable]
    public struct GameTagEntry
    {
        public int Id;
        public string Name;
        public int ParentId;
    }

    // Maps a retired Id to its replacement (0 = deprecated, no replacement). Optional, delete/merge only.
    [Serializable]
    public struct GameTagRedirect
    {
        public int FromId;
        public int ToId;
    }

    /// <summary>
    /// Source of truth for the tag tree. Authoring data is a flat list of <see cref="GameTagEntry"/>
    /// (Id + Name + ParentId); paths/hierarchy are derived and baked into runtime caches.
    /// </summary>
    [CreateAssetMenu(menuName = "DataKeeper/GameTagRegistry", fileName = "GameTagRegistry", order = 1000)]
    public class GameTagRegistry : SO
    {
        public const string SEPARATOR = "/";
        public const int NONE = 0;
        private const int MAX_DEPTH = 256; // cycle/stack guard; no real tag tree is this deep

        private const string DEFAULT_REGISTRY_NAME = "GameTagRegistry";

        private int GetNewId()
        {
            var id = UID.Int32Id();
            while (id == NONE || _byId.ContainsKey(id))
            {
                id = UID.Int32Id();
            }
            return id;
        }

        [SerializeField] private List<GameTagEntry> _entries = new();
        [SerializeField] private List<GameTagRedirect> _redirects = new();

        // ── Baked runtime caches (rebuilt by Bake; not serialized) ─────────────
        public sealed class Node
        {
            public int Id;
            public string Name;
            public int ParentId;
            public int Depth;      // root == 0
            public string Path;     // cached full path "A/B/C"
            public readonly List<int> Children = new();
        }

        private readonly Dictionary<int, Node> _byId = new();
        private readonly Dictionary<string, int> _byPath = new(StringComparer.Ordinal);
        private readonly Dictionary<int, int> _redirectMap = new();
        private readonly List<int> _roots = new();
        private bool _baked;

        public IReadOnlyList<GameTagEntry> Entries => _entries;
        public IReadOnlyList<int> RootIds { get { EnsureBaked(); return _roots; } }

        // ── Default instance ───────────────────────────────────────────────────
        private static GameTagRegistry _default;

        public static GameTagRegistry Default
        {
            get
            {
                if (_default == null)
                {
                    _default = Resources.Load<GameTagRegistry>(DEFAULT_REGISTRY_NAME);
                    if (_default != null) _default.EnsureBaked();
                }
                return _default;
            }
        }

        // Tooling / test hook: make a registry the active one (and bake it).
        public static void SetDefault(GameTagRegistry registry)
        {
            _default = registry;
            if (registry != null) registry.Bake();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _default = null;

        public override void Initialize() => Bake();

        // ── Baking ──────────────────────────────────────────────────────────────
        public void EnsureBaked() { if (!_baked) Bake(); }

        [Button]
        public void Bake()
        {
            _byId.Clear();
            _byPath.Clear();
            _redirectMap.Clear();
            _roots.Clear();

            foreach (var e in _entries)
            {
                if (e.Id == NONE || _byId.ContainsKey(e.Id)) continue;
                _byId[e.Id] = new Node { Id = e.Id, Name = e.Name, ParentId = e.ParentId };
            }

            foreach (var node in _byId.Values)
            {
                if (node.ParentId != NONE && _byId.TryGetValue(node.ParentId, out var parent))
                    parent.Children.Add(node.Id);
                else
                    _roots.Add(node.Id);
            }

            foreach (var node in _byId.Values)
                ResolvePathAndDepth(node, 0);

            foreach (var node in _byId.Values)
                _byPath[node.Path] = node.Id;

            foreach (var r in _redirects)
                if (r.FromId != NONE) _redirectMap[r.FromId] = r.ToId;

            _baked = true;
        }
        
#if UNITY_EDITOR
        // Inspector edits and Undo change _entries without going through the mutator methods.
        // Hand-added list elements default to Id 0 (reserved None) — mint a real id for them — and
        // invalidate the baked cache so the next query (picker, drawer, codegen) re-bakes.
        private void OnValidate()
        {
            bool changed = false;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != NONE) continue;
                var e = _entries[i];
                e.Id = GetNewId();
                _entries[i] = e;
                changed = true;
            }
            _baked = false;
            if (changed) UnityEditor.EditorUtility.SetDirty(this);
        }

        // Bridges the inspector button to the editor-only code generator (GameTagsCodeGen
        // registers this on load), so the runtime assembly needn't reference editor code.
        public static System.Action<GameTagRegistry> RegenerateCodeHook;
        
        [Button("Regenerate GameTags C# Class")]
        private void RegenerateGameTags()
        {
            if (RegenerateCodeHook != null) RegenerateCodeHook(this);
            else Debug.LogWarning("[GameTag] GameTags code generator is not loaded.");
        }
#endif

        private void ResolvePathAndDepth(Node node, int guard)
        {
            if (node.Path != null) return;
            if (guard >= MAX_DEPTH || node.ParentId == NONE || !_byId.TryGetValue(node.ParentId, out var parent))
            {
                node.Depth = 0;
                node.Path = node.Name;
                return;
            }
            ResolvePathAndDepth(parent, guard + 1);
            node.Depth = parent.Depth + 1;
            node.Path = parent.Path + SEPARATOR + node.Name;
        }

        // ── Runtime queries (zero-GC) ────────────────────────────────────────────
        public bool IsValid(int id) { EnsureBaked(); return id != NONE && _byId.ContainsKey(id); }

        public int Resolve(int id) // follow redirects to the final id
        {
            EnsureBaked();
            int guard = 0;
            while (_redirectMap.TryGetValue(id, out var to) && guard++ < 64) id = to;
            return id;
        }

        public string GetName(int id)   { EnsureBaked(); return _byId.TryGetValue(id, out var n) ? n.Name : null; }
        public string GetPath(int id)   { EnsureBaked(); return _byId.TryGetValue(id, out var n) ? n.Path : null; }
        public int GetParentId(int id)  { EnsureBaked(); return _byId.TryGetValue(id, out var n) ? n.ParentId : NONE; }
        public int GetDepth(int id)     { EnsureBaked(); return _byId.TryGetValue(id, out var n) ? n.Depth : -1; }
        public Node GetNode(int id)     { EnsureBaked(); return _byId.TryGetValue(id, out var n) ? n : null; }

        public int FindByPath(string path)
        {
            EnsureBaked();
            if (string.IsNullOrEmpty(path)) return NONE;
            return _byPath.TryGetValue(path, out var id) ? id : NONE;
        }

        // True when childId == ancestorId, or ancestorId is a strict ancestor of childId (Unreal MatchesTag).
        public bool Matches(int childId, int ancestorId)
        {
            EnsureBaked();
            childId = Resolve(childId);
            ancestorId = Resolve(ancestorId);
            if (childId == NONE || ancestorId == NONE) return false;
            if (childId == ancestorId) return true;

            if (!_byId.TryGetValue(childId, out var child)) return false;
            if (!_byId.TryGetValue(ancestorId, out var anc)) return false;
            if (anc.Depth >= child.Depth) return false; // can't be an ancestor

            int cur = child.ParentId;
            int guard = 0;
            while (cur != NONE && guard++ < MAX_DEPTH)
            {
                if (cur == ancestorId) return true;
                cur = _byId.TryGetValue(cur, out var n) ? n.ParentId : NONE;
            }
            return false;
        }

        // ── Authoring (editor) ───────────────────────────────────────────────────
        // Resolve a path to an id, creating any missing nodes along the way.
        public int GetOrCreate(string path)
        {
            if (string.IsNullOrEmpty(path)) return NONE;
            EnsureBaked();

            var segments = path.Split(SEPARATOR[0]);
            int parentId = NONE;
            string acc = null;
            foreach (var raw in segments)
            {
                var seg = SanitizeSegment(raw);
                if (string.IsNullOrEmpty(seg)) continue;
                acc = acc == null ? seg : acc + SEPARATOR + seg;
                if (_byPath.TryGetValue(acc, out var existing)) { parentId = existing; continue; }
                parentId = AddChild(parentId, seg);
            }
            return parentId;
        }

        public int AddChild(int parentId, string name)
        {
            name = SanitizeSegment(name);
            if (string.IsNullOrEmpty(name)) return NONE;
            EnsureBaked();

            if (parentId != NONE && _byId.TryGetValue(parentId, out var p))
            {
                foreach (var cid in p.Children)
                    if (string.Equals(_byId[cid].Name, name, StringComparison.Ordinal)) return cid;
            }
            else if (parentId == NONE && _byPath.TryGetValue(name, out var rootId))
            {
                return rootId;
            }

            int id = GetNewId();
            _entries.Add(new GameTagEntry { Id = id, Name = name, ParentId = parentId });
            MarkDirtyAndBake();
            return id;
        }

        // Re-add a previously-retired id at the given path, so existing references to that id resolve
        // again. Any missing parent segments are created (with fresh ids); the LAST segment takes the
        // supplied id. No-op if the id is already live. Drops any redirect that pointed the id elsewhere.
        // Returns true if the entry was added.
        public bool ReAddId(int id, string path)
        {
            if (id == NONE || string.IsNullOrEmpty(path)) return false;
            EnsureBaked();
            if (_byId.ContainsKey(id)) return false; // already present

            var clean = new List<string>();
            foreach (var raw in path.Split(SEPARATOR[0]))
            {
                var seg = SanitizeSegment(raw);
                if (!string.IsNullOrEmpty(seg)) clean.Add(seg);
            }
            if (clean.Count == 0) return false;

            // Create/resolve every segment except the last as the parent chain (AddChild re-bakes).
            int parentId = NONE;
            for (int i = 0; i < clean.Count - 1; i++)
                parentId = AddChild(parentId, clean[i]);

            if (_byId.ContainsKey(id)) return false; // re-check after the AddChild bakes
            _redirects.RemoveAll(r => r.FromId == id);
            _entries.Add(new GameTagEntry { Id = id, Name = clean[clean.Count - 1], ParentId = parentId });
            MarkDirtyAndBake();
            return true;
        }

        public void Rename(int id, string newName)
        {
            newName = SanitizeSegment(newName);
            if (id == NONE || string.IsNullOrEmpty(newName)) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != id) continue;
                var e = _entries[i]; e.Name = newName; _entries[i] = e;
                MarkDirtyAndBake();
                return;
            }
        }

        // Move a node under a new parent (NONE = make it a root). Refuses cycles.
        public void Reparent(int id, int newParentId)
        {
            if (id == NONE || id == newParentId) return;
            if (newParentId != NONE && Matches(newParentId, id)) return; // newParent is in id's subtree
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id != id) continue;
                var e = _entries[i]; e.ParentId = newParentId; _entries[i] = e;
                MarkDirtyAndBake();
                return;
            }
        }

        // Delete a node and its subtree. Optionally redirect the node's old id to a replacement.
        public void Delete(int id, int redirectToId = NONE)
        {
            if (id == NONE) return;
            EnsureBaked();

            var subtree = new HashSet<int>();
            CollectSubtree(id, subtree);
            _entries.RemoveAll(e => subtree.Contains(e.Id)); // ids are retired, never reused
            if (redirectToId != NONE) _redirects.Add(new GameTagRedirect { FromId = id, ToId = redirectToId });
            MarkDirtyAndBake();
        }

        private void CollectSubtree(int id, HashSet<int> acc)
        {
            if (!acc.Add(id)) return;
            if (_byId.TryGetValue(id, out var n))
                foreach (var c in n.Children) CollectSubtree(c, acc);
        }

        private static string SanitizeSegment(string name)
            => name?.Replace(SEPARATOR, string.Empty).Trim();

        private void MarkDirtyAndBake()
        {
            Bake();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
