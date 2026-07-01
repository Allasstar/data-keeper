using System;
using System.Collections.Generic;
using DataKeeper.Signals;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    /// <summary>What happened to a tag in a <see cref="GameTagContainer"/>.</summary>
    public enum GameTagChangeType
    {
        /// <summary>The tag was added to the container.</summary>
        Added = 0,
        /// <summary>The tag was removed from the container.</summary>
        Removed = 1,
    }

    /// <summary>
    /// A set of <see cref="GameTag"/>s with Unreal-style hierarchical queries — the port of
    /// <c>FGameplayTagContainer</c>. A container holds the tags added to it explicitly; hierarchical
    /// queries (<see cref="HasTag"/>, <see cref="HasAny"/>, …) additionally match against the parent
    /// branches of those tags.
    /// </summary>
    /// <remarks>
    /// Query methods are zero-GC in steady state (index loops, no LINQ / no enumerator allocation); a
    /// container holding 8+ tags additionally builds a cached expanded-ancestor set on its first
    /// hierarchical query (one-time allocation, maintained incrementally), after which
    /// <see cref="HasTag"/> is a single hash probe. Mutations resolve rename/merge redirects, so the
    /// container stores only canonical ids — a retired handle and its replacement behave as the same
    /// tag for add/remove and for listener registration. Empty-input semantics follow Unreal:
    /// <see cref="HasAny"/>/<see cref="HasAnyExact"/> on an empty or null argument return <c>false</c>,
    /// while <see cref="HasAll"/>/<see cref="HasAllExact"/> return <c>true</c> (no required tag is missing).
    /// <code>
    /// var c = new GameTagContainer();
    /// c.AddTag(GameTag.Find("Damage/Elemental/Fire"));
    /// c.HasTag(GameTag.Find("Damage"));        // true  — parent branch of a contained tag
    /// c.HasTagExact(GameTag.Find("Damage"));   // false — "Damage" was never added explicitly
    /// </code>
    /// <para>
    /// <b>Observable.</b> Every mutation raises change events at four granularities, all built on the package's
    /// zero-GC <c>Signal</c>. The event infrastructure is lazily created on first subscription and released again
    /// when the last listener is removed, so a container that is not observed keeps its plain-data allocation
    /// profile and pays only constant per-mutation work (a redirect resolve and null checks).
    /// <list type="bullet">
    /// <item><see cref="AddListener(Action{GameTag,GameTagChangeType})"/> — any add/remove, receives the changed tag.</item>
    /// <item><see cref="AddTagListener"/> — a specific tag, exact (descendants ignored).</item>
    /// <item><see cref="AddBranchListener"/> — a tag or any descendant; raw, one event per matching change.</item>
    /// <item><see cref="AddBranchPresenceListener"/> — a branch's <c>present</c>/<c>absent</c> transitions, deduplicated.</item>
    /// </list>
    /// <code>
    /// c.AddListener((tag, change) => Debug.Log($"{tag.Path} {change}"));
    /// c.AddBranchPresenceListener(GameTag.Find("Buff/Shield"), shield.SetActive);
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public class GameTagContainer
    {
        [SerializeField] private List<GameTag> _tags = new();

        /// <summary>The explicitly-added tags, in insertion order.</summary>
        public IReadOnlyList<GameTag> Tags => _tags;

        // ── Observation (lazy: a container that is never subscribed to allocates none of this) ──
        [NonSerialized] private Signal<GameTag, GameTagChangeType> _onAnyChanged;
        [NonSerialized] private Dictionary<GameTag, Signal<GameTagChangeType>> _exactObservers;
        [NonSerialized] private Dictionary<GameTag, Signal<GameTag, GameTagChangeType>> _branchObservers;
        [NonSerialized] private Dictionary<GameTag, PresenceObserver> _presenceObservers;

        private bool HasObservers =>
            _onAnyChanged != null || _exactObservers != null || _branchObservers != null || _presenceObservers != null;

        // Resolves rename/merge redirects so the container stores, removes, and keys observers by
        // canonical ids only: a retired handle and its replacement behave as the same tag everywhere.
        private static GameTag Canonical(GameTag tag)
        {
            var registry = GameTagRegistry.Default;
            return registry != null ? new GameTag(registry.Resolve(tag.Id)) : tag;
        }

        // ── Expanded ancestor set (O(1) HasTag once the container is large) ──────
        // Ref-count of every contained tag's resolved id plus all its ancestor ids. Built lazily the
        // first time a hierarchical query runs on a container big enough that a hash probe beats the
        // linear scan; maintained incrementally by mutations, invalidated (and rebuilt on demand)
        // when the registry re-bakes or is swapped. Small containers never allocate it.
        private const int ExpandedQueryThreshold = 8;
        [NonSerialized] private Dictionary<int, int> _expanded;
        [NonSerialized] private GameTagRegistry _expandedRegistry;
        [NonSerialized] private int _expandedVersion;

        private void EnsureExpanded(GameTagRegistry registry)
        {
            if (_expanded != null && _expandedRegistry == registry && _expandedVersion == registry.BakeVersion)
                return;
            _expanded ??= new Dictionary<int, int>();
            _expanded.Clear();
            _expandedRegistry = registry;
            _expandedVersion = registry.BakeVersion;
            for (int i = 0; i < _tags.Count; i++) ApplyExpanded(_tags[i], +1, registry);
        }

        // Called after every _tags mutation (delta +1 add, -1 remove). No-op until the set exists.
        private void TrackChange(GameTag tag, int delta)
        {
            if (_expanded == null) return;
            var registry = GameTagRegistry.Default;
            if (registry == null || registry != _expandedRegistry || registry.BakeVersion != _expandedVersion)
            {
                _expandedRegistry = null; // stale; EnsureExpanded rebuilds on the next large query
                return;
            }
            ApplyExpanded(tag, delta, registry);
        }

        private void ApplyExpanded(GameTag tag, int delta, GameTagRegistry registry)
        {
            var node = registry.GetNode(registry.Resolve(tag.Id));
            if (node == null) return; // invalid/stale entries match nothing, so they contribute nothing
            var chain = node.AncestorPath;
            for (int i = 0; i < chain.Length; i++)
            {
                int id = chain[i];
                if (id == GameTagRegistry.NONE) continue;
                _expanded.TryGetValue(id, out int count);
                count += delta;
                if (count > 0) _expanded[id] = count;
                else _expanded.Remove(id);
            }
        }

        // Tracks how many contained tags currently fall under a subscribed branch, so a raw per-tag change can be
        // collapsed into a single present/absent transition: the branch is present exactly when Count > 0.
        private sealed class PresenceObserver
        {
            public int Count;
            public readonly Signal<bool> Signal = new Signal<bool>();

            public void Apply(GameTagChangeType change)
            {
                if (change == GameTagChangeType.Added)
                {
                    if (++Count == 1) Signal.Invoke(true);   // absent -> present
                }
                else if (Count > 0 && --Count == 0)
                {
                    Signal.Invoke(false);                     // present -> absent
                }
            }
        }

        /// <summary>
        /// Fires every subscriber whose query covers <paramref name="tag"/>: the global listeners, the exact
        /// listener for this tag, and every branch listener registered on this tag or any of its ancestors.
        /// The ancestor walk is what makes a branch subscription hierarchical — adding <c>"Enemy/Boss"</c> also
        /// notifies a listener on <c>"Enemy"</c>. Zero-GC (no walk unless a branch listener exists).
        /// </summary>
        private void Notify(GameTag tag, GameTagChangeType change)
        {
            _onAnyChanged?.Invoke(tag, change);

            if (_exactObservers != null && _exactObservers.TryGetValue(tag, out var exact))
                exact.Invoke(change);

            if (_branchObservers != null || _presenceObservers != null)
                for (var t = tag; t.IsValid; t = t.Parent)
                {
                    if (_branchObservers != null && _branchObservers.TryGetValue(t, out var branch))
                        branch.Invoke(tag, change);

                    if (_presenceObservers != null && _presenceObservers.TryGetValue(t, out var presence))
                        presence.Apply(change);
                }
        }

        // ── Subscription: any change ─────────────────────────────────────────────

        /// <summary>Subscribes to every add/remove in this container. The callback receives the changed tag and the change kind.</summary>
        public void AddListener(Action<GameTag, GameTagChangeType> onChanged)
            => (_onAnyChanged ??= new Signal<GameTag, GameTagChangeType>()).AddListener(onChanged);

        /// <summary>Unsubscribes a listener added with <see cref="AddListener(Action{GameTag,GameTagChangeType})"/>.</summary>
        public void RemoveListener(Action<GameTag, GameTagChangeType> onChanged)
        {
            if (_onAnyChanged == null) return;
            _onAnyChanged.RemoveListener(onChanged);
            if (_onAnyChanged.ListenerCount == 0) _onAnyChanged = null;
        }

        // ── Subscription: a specific tag (exact) ─────────────────────────────────

        /// <summary>
        /// Subscribes to add/remove of <paramref name="tag"/> exactly (hierarchy ignored). The callback fires only
        /// when that literal tag is added or removed — not when a descendant changes. The key is redirect-resolved:
        /// subscribing with a retired handle observes its replacement.
        /// </summary>
        public void AddTagListener(GameTag tag, Action<GameTagChangeType> onChanged)
        {
            tag = Canonical(tag);
            _exactObservers ??= new Dictionary<GameTag, Signal<GameTagChangeType>>();
            if (!_exactObservers.TryGetValue(tag, out var s))
                _exactObservers[tag] = s = new Signal<GameTagChangeType>();
            s.AddListener(onChanged);
        }

        /// <summary>Unsubscribes a listener added with <see cref="AddTagListener"/>.</summary>
        public void RemoveTagListener(GameTag tag, Action<GameTagChangeType> onChanged)
        {
            tag = Canonical(tag);
            if (_exactObservers == null || !_exactObservers.TryGetValue(tag, out var s)) return;
            s.RemoveListener(onChanged);
            if (s.ListenerCount == 0) _exactObservers.Remove(tag);
            if (_exactObservers.Count == 0) _exactObservers = null;
        }

        // ── Subscription: a branch (a tag or any of its descendants) ─────────────

        /// <summary>
        /// Subscribes to add/remove of <paramref name="parent"/> <b>or any of its descendants</b>. Adding
        /// <c>"Enemy/Boss/Elite"</c> notifies a listener registered on <c>"Enemy"</c>. The callback receives the
        /// concrete tag that changed (e.g. <c>"Enemy/Boss/Elite"</c>), not the subscribed branch. The key is
        /// redirect-resolved, like <see cref="AddTagListener"/>.
        /// </summary>
        /// <remarks>
        /// Raw per-descendant semantics: with both <c>Enemy/Boss</c> and <c>Enemy/Minion</c> present, a listener on
        /// <c>Enemy</c> fires once per add and once per remove — it is not deduplicated to a single "branch present /
        /// absent" transition. Track that yourself with <see cref="HasTag"/> if you need it.
        /// </remarks>
        public void AddBranchListener(GameTag parent, Action<GameTag, GameTagChangeType> onChanged)
        {
            parent = Canonical(parent);
            _branchObservers ??= new Dictionary<GameTag, Signal<GameTag, GameTagChangeType>>();
            if (!_branchObservers.TryGetValue(parent, out var s))
                _branchObservers[parent] = s = new Signal<GameTag, GameTagChangeType>();
            s.AddListener(onChanged);
        }

        /// <summary>Unsubscribes a listener added with <see cref="AddBranchListener"/>.</summary>
        public void RemoveBranchListener(GameTag parent, Action<GameTag, GameTagChangeType> onChanged)
        {
            parent = Canonical(parent);
            if (_branchObservers == null || !_branchObservers.TryGetValue(parent, out var s)) return;
            s.RemoveListener(onChanged);
            if (s.ListenerCount == 0) _branchObservers.Remove(parent);
            if (_branchObservers.Count == 0) _branchObservers = null;
        }

        // ── Subscription: branch presence (deduped present/absent transitions) ───

        /// <summary>
        /// Subscribes to the <b>presence</b> of <paramref name="parent"/>'s branch: the callback fires with
        /// <c>true</c> the moment the branch becomes present (its first covered tag — itself or a descendant — is
        /// added) and with <c>false</c> when it becomes absent (its last covered tag is removed). Adds/removes that
        /// don't flip <see cref="HasTag"/> for the branch are collapsed away — the opposite of
        /// <see cref="AddBranchListener"/>'s raw per-descendant events.
        /// </summary>
        /// <remarks>
        /// Maps directly onto toggle-style code, e.g. <c>c.AddBranchPresenceListener(stunned, shield.SetActive)</c>.
        /// Does not fire on subscribe; the current presence is captured so the next transition is correct. If you
        /// need the initial state, read <c>HasTag(parent)</c> once yourself. Backed by a per-branch ref-count, so
        /// it stays correct even with duplicate entries from <see cref="AddTagFast"/>. The key is redirect-resolved,
        /// like <see cref="AddTagListener"/>.
        /// </remarks>
        public void AddBranchPresenceListener(GameTag parent, Action<bool> onPresenceChanged)
        {
            parent = Canonical(parent);
            _presenceObservers ??= new Dictionary<GameTag, PresenceObserver>();
            if (!_presenceObservers.TryGetValue(parent, out var po))
                _presenceObservers[parent] = po = new PresenceObserver { Count = CountCovered(parent) };
            po.Signal.AddListener(onPresenceChanged);
        }

        /// <summary>Unsubscribes a listener added with <see cref="AddBranchPresenceListener"/>.</summary>
        public void RemoveBranchPresenceListener(GameTag parent, Action<bool> onPresenceChanged)
        {
            parent = Canonical(parent);
            if (_presenceObservers == null || !_presenceObservers.TryGetValue(parent, out var po)) return;
            po.Signal.RemoveListener(onPresenceChanged);
            if (po.Signal.ListenerCount == 0) _presenceObservers.Remove(parent); // Count is re-seeded on resubscribe
            if (_presenceObservers.Count == 0) _presenceObservers = null;
        }

        // Number of contained tags currently covered by (equal to, or a descendant of) branch. Seeds the ref-count
        // so the first transition after subscribing is detected correctly. Counts occurrences, matching Notify.
        private int CountCovered(GameTag branch)
        {
            int n = 0;
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesTag(branch)) n++;
            return n;
        }

        /// <summary>
        /// Removes every listener (global, exact, branch, and branch-presence) from this container and
        /// releases the observation infrastructure, restoring the never-observed allocation profile.
        /// </summary>
        public void RemoveAllListeners()
        {
            _onAnyChanged = null;
            _exactObservers = null;
            _branchObservers = null;
            _presenceObservers = null;
        }

        // ── Count / state (Unreal: Num / IsEmpty / IsValid) ──────────────────────

        /// <summary>Number of explicitly-added tags (Unreal <c>Num</c>).</summary>
        public int Num() => _tags.Count;

        /// <summary>Number of explicitly-added tags. C# alias for <see cref="Num"/>.</summary>
        public int Count => _tags.Count;

        /// <summary><c>true</c> when the container holds no tags (Unreal <c>IsEmpty</c>).</summary>
        public bool IsEmpty() => _tags.Count == 0;

        /// <summary><c>true</c> when the container holds at least one valid tag (Unreal <c>IsValid</c>).</summary>
        /// <remarks>
        /// Distinct from <c>!IsEmpty()</c>: a non-empty container can still be invalid if it holds only
        /// stale/retired ids (e.g. from old serialized data), since <see cref="AddTag"/> only admits valid tags
        /// but raw deserialization does not.
        /// </remarks>
        public bool IsValid()
        {
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].IsValid) return true;
            return false;
        }

        // ── Indexed access (Unreal: GetByIndex / First / Last) ───────────────────

        /// <summary>Tag at <paramref name="index"/>, or an invalid tag if out of range (Unreal <c>GetByIndex</c>).</summary>
        public GameTag GetByIndex(int index) => (uint)index < (uint)_tags.Count ? _tags[index] : default;

        /// <summary>First tag, or an invalid tag if empty (Unreal <c>First</c>).</summary>
        public GameTag First() => _tags.Count > 0 ? _tags[0] : default;

        /// <summary>Last tag, or an invalid tag if empty (Unreal <c>Last</c>).</summary>
        public GameTag Last() => _tags.Count > 0 ? _tags[_tags.Count - 1] : default;

        // ── Modification (Unreal: AddTag / AddTagFast / AddLeafTag / Remove… / Reset / AppendTags) ──

        /// <summary>Adds <paramref name="tag"/> if it is valid and not already present (exact). Unreal <c>AddTag</c>.</summary>
        /// <remarks>
        /// Uniqueness is by exact identity, not hierarchy: adding <c>"Enemy"</c> while <c>"Enemy/Boss"</c> is
        /// already present still adds it — they are different nodes and both are kept. Use <see cref="AddLeafTag"/>
        /// instead when you want only the most-specific tag of a branch. Redirects are resolved on entry, so the
        /// container stores canonical ids: adding a retired handle stores (and notifies with) its replacement.
        /// </remarks>
        public void AddTag(GameTag tag)
        {
            tag = Canonical(tag);
            if (tag.IsValid && !HasTagExact(tag))
            {
                _tags.Add(tag);
                TrackChange(tag, +1);
                Notify(tag, GameTagChangeType.Added);
            }
        }

        /// <summary>Adds <paramref name="tag"/> without validity or uniqueness checks (Unreal <c>AddTagFast</c>).</summary>
        /// <remarks>
        /// For hot paths where the tag is already known valid and unique: skips the O(n) <see cref="HasTagExact"/>
        /// scan that <see cref="AddTag"/> performs. Can introduce duplicates or invalid entries if misused.
        /// Redirects are still resolved (a single dictionary probe) so the container stays canonical.
        /// </remarks>
        public void AddTagFast(GameTag tag)
        {
            tag = Canonical(tag);
            _tags.Add(tag);
            TrackChange(tag, +1);
            Notify(tag, GameTagChangeType.Added);
        }

        /// <summary>
        /// Adds <paramref name="tag"/> as a leaf: removes any of its now-redundant parent branches already
        /// present, and refuses to add when an equal or more-specific (descendant) tag is already present.
        /// Returns <c>true</c> if the tag was added. Unreal <c>AddLeafTag</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// var c = new GameTagContainer();
        /// c.AddTag(GameTag.Find("Enemy"));
        /// c.AddLeafTag(GameTag.Find("Enemy/Boss")); // true  — drops the redundant "Enemy", now {Enemy/Boss}
        /// c.AddLeafTag(GameTag.Find("Enemy"));       // false — a more-specific tag already covers it, still {Enemy/Boss}
        /// c.AddLeafTag(GameTag.Find("Enemy/Boss")); // false — exact duplicate
        /// c.AddLeafTag(GameTag.Find("Nope"));        // false — invalid tag
        /// </code>
        /// </example>
        public bool AddLeafTag(GameTag tag)
        {
            tag = Canonical(tag);
            if (!tag.IsValid) return false;

            // Already covered by an equal or more-specific tag -> nothing to do.
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesTag(tag)) return false;

            // Drop ancestors of the new leaf; they are now redundant. A listener may mutate the
            // container from Notify, so the index is re-clamped before each access.
            for (int i = _tags.Count - 1; i >= 0; i--)
            {
                if (i >= _tags.Count) { i = _tags.Count; continue; }
                if (!tag.MatchesTag(_tags[i])) continue;
                var dropped = _tags[i];
                _tags.RemoveAt(i);
                TrackChange(dropped, -1);
                Notify(dropped, GameTagChangeType.Removed);
            }

            _tags.Add(tag);
            TrackChange(tag, +1);
            Notify(tag, GameTagChangeType.Added);
            return true;
        }

        /// <summary>Removes <paramref name="tag"/> (exact). Returns <c>true</c> if it was present. Unreal <c>RemoveTag</c>.</summary>
        /// <remarks>
        /// Removal is by exact identity: it deletes only the literal member, never an ancestor or descendant.
        /// Removing <c>"Enemy"</c> from a container holding <c>"Enemy/Boss"</c> removes nothing and returns <c>false</c>.
        /// Redirects are resolved on entry (mirroring <see cref="AddTag"/>): a retired handle removes the
        /// canonical member it was stored as.
        /// </remarks>
        public bool RemoveTag(GameTag tag)
        {
            tag = Canonical(tag);
            if (!_tags.Remove(tag)) return false;
            TrackChange(tag, -1);
            Notify(tag, GameTagChangeType.Removed);
            return true;
        }

        /// <summary>Removes every tag in <paramref name="other"/> from this container (Unreal <c>RemoveTags</c>).</summary>
        /// <remarks>Each removal is exact (see <see cref="RemoveTag"/>); hierarchy is not expanded.</remarks>
        public void RemoveTags(GameTagContainer other)
        {
            if (other == null) return;
            if (other == this) { Reset(); return; } // iterating other's list while removing from it would skip entries
            for (int i = 0; i < other._tags.Count; i++)
            {
                var tag = Canonical(other._tags[i]);
                if (!_tags.Remove(tag)) continue;
                TrackChange(tag, -1);
                Notify(tag, GameTagChangeType.Removed);
            }
        }

        /// <summary>Removes all tags, emitting a <see cref="GameTagChangeType.Removed"/> per tag to observers (Unreal <c>Reset</c>).</summary>
        /// <remarks>Safe against listeners that mutate the container from the callback; tags a listener re-adds during the sweep survive it.</remarks>
        public void Reset()
        {
            if (HasObservers)
            {
                // Notify from the end so a listener that inspects the container sees each tag already gone.
                // A listener may mutate the container from the callback: the index is re-clamped before each
                // access, and tags a listener re-adds during the sweep survive it (they land past the cursor).
                for (int i = _tags.Count - 1; i >= 0; i--)
                {
                    if (i >= _tags.Count) { i = _tags.Count; continue; }
                    var tag = _tags[i];
                    _tags.RemoveAt(i);
                    TrackChange(tag, -1);
                    Notify(tag, GameTagChangeType.Removed);
                }
            }
            else
            {
                _tags.Clear();
                _expanded?.Clear();
            }
        }

        /// <summary>Removes all tags. C# alias for <see cref="Reset"/>.</summary>
        public void Clear() => Reset();

        /// <summary>Adds all tags from <paramref name="other"/> (deduplicated). Unreal <c>AppendTags</c>.</summary>
        public void AppendTags(GameTagContainer other)
        {
            if (other == null) return;
            for (int i = 0; i < other._tags.Count; i++) AddTag(other._tags[i]);
        }

        // ── Queries (Unreal: HasTag / HasTagExact / HasAny… / HasAll…) ───────────

        /// <summary>
        /// Hierarchical: <c>true</c> when some contained tag equals <paramref name="tag"/> or is a descendant
        /// of it. Container holding <c>"Damage/Elemental/Fire"</c> <c>HasTag("Damage")</c> -> <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Answers "is <paramref name="tag"/> <i>covered</i> by what I hold, following the hierarchy?" The query
        /// tag need not be an explicit member — being an ancestor of a contained tag is enough. For literal
        /// membership use <see cref="HasTagExact"/>. Small containers scan linearly; containers holding 8+ tags
        /// answer from a cached expanded-ancestor set (a single hash probe, built lazily on the first such query).
        /// </remarks>
        /// <example>
        /// <code>
        /// var c = new GameTagContainer();
        /// c.AddTag(GameTag.Find("Enemy/Boss"));
        /// c.HasTag(GameTag.Find("Enemy/Boss"));   // true  — exact contained
        /// c.HasTag(GameTag.Find("Enemy"));        // true  — contained tag is a descendant of the query
        /// c.HasTag(GameTag.Find("Enemy/Minion")); // false — sibling of the contained tag
        /// c.HasTag(GameTag.Find("Player"));       // false — unrelated
        /// c.HasTagExact(GameTag.Find("Enemy"));   // false — "Enemy" was never added explicitly
        /// </code>
        /// </example>
        public bool HasTag(GameTag tag)
        {
            // Large containers answer from the expanded ancestor set: one hash probe instead of a scan.
            if (_tags.Count >= ExpandedQueryThreshold)
            {
                var registry = GameTagRegistry.Default;
                if (registry != null)
                {
                    EnsureExpanded(registry);
                    int id = registry.Resolve(tag.Id);
                    return id != GameTagRegistry.NONE && _expanded.ContainsKey(id);
                }
            }

            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesTag(tag)) return true;
            return false;
        }

        /// <summary>Exact: <c>true</c> when <paramref name="tag"/> is explicitly present (hierarchy ignored).</summary>
        /// <remarks>
        /// "Explicitly present" means <paramref name="tag"/> is literally one of the tags you added — not merely
        /// implied as an ancestor of one. A container holding only <c>"Enemy/Boss"</c> returns <c>false</c> for
        /// <c>HasTagExact("Enemy")</c> (the ancestor <c>Enemy</c> was never added), even though <see cref="HasTag"/>
        /// returns <c>true</c>. Use this when the precise tag matters (dedup, exact removal, authoring tools).
        /// </remarks>
        public bool HasTagExact(GameTag tag)
        {
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesTagExact(tag)) return true;
            return false;
        }

        /// <summary>Hierarchical: <c>true</c> when this container has ANY tag in <paramref name="other"/>. Null/empty -> <c>false</c>.</summary>
        /// <remarks>
        /// Per-tag rule is <see cref="HasTag"/>: a required tag is satisfied when this container holds it or any
        /// descendant of it. "Any" over no requirements is <c>false</c> (nothing was satisfied).
        /// </remarks>
        public bool HasAny(GameTagContainer other)
        {
            if (other == null) return false;
            for (int i = 0; i < other._tags.Count; i++)
                if (HasTag(other._tags[i])) return true;
            return false;
        }

        /// <summary>Exact: <c>true</c> when this container has ANY tag in <paramref name="other"/> exactly. Null/empty -> <c>false</c>.</summary>
        /// <remarks>Per-tag rule is <see cref="HasTagExact"/>: only literal membership counts; ancestors do not satisfy a requirement.</remarks>
        public bool HasAnyExact(GameTagContainer other)
        {
            if (other == null) return false;
            for (int i = 0; i < other._tags.Count; i++)
                if (HasTagExact(other._tags[i])) return true;
            return false;
        }

        /// <summary>Hierarchical: <c>true</c> when this container has ALL tags in <paramref name="other"/>. Null/empty -> <c>true</c>.</summary>
        /// <remarks>
        /// Every required tag must be covered hierarchically (<see cref="HasTag"/>). An empty/null requirement is
        /// vacuously satisfied — no required tag is missing — so it returns <c>true</c> (note the opposite default
        /// from <see cref="HasAny"/>).
        /// </remarks>
        public bool HasAll(GameTagContainer other)
        {
            if (other == null) return true;
            for (int i = 0; i < other._tags.Count; i++)
                if (!HasTag(other._tags[i])) return false;
            return true;
        }

        /// <summary>Exact: <c>true</c> when this container has ALL tags in <paramref name="other"/> exactly. Null/empty -> <c>true</c>.</summary>
        /// <remarks>Per-tag rule is <see cref="HasTagExact"/>: each required tag must be a literal member. Empty/null is vacuously <c>true</c>.</remarks>
        public bool HasAllExact(GameTagContainer other)
        {
            if (other == null) return true;
            for (int i = 0; i < other._tags.Count; i++)
                if (!HasTagExact(other._tags[i])) return false;
            return true;
        }

        // ── Advanced (Unreal: Filter / GetGameplayTagParents) ────────────────────

        /// <summary>
        /// Returns a new container with the tags of THIS container that hierarchically match any tag in
        /// <paramref name="other"/> (parents expanded). Unreal <c>Filter</c>.
        /// </summary>
        /// <remarks>
        /// A tag is kept when it is, or descends from, some tag in <paramref name="other"/> — the same per-tag
        /// rule as <see cref="HasTag"/>. Direction matters: a contained ancestor is dropped when only its
        /// descendants are queried. The originals are left untouched; a new container is returned.
        /// </remarks>
        /// <example>
        /// <code>
        /// var c = new GameTagContainer();
        /// c.AddTag(GameTag.Find("Enemy/Boss/Elite"));
        /// c.AddTag(GameTag.Find("Player"));
        /// c.Filter(Container("Enemy")).Count;        // 1 — keeps Enemy/Boss/Elite (a descendant), drops Player
        ///
        /// // Direction matters — a contained ancestor does NOT match a more-specific query:
        /// var a = new GameTagContainer();
        /// a.AddTag(GameTag.Find("Enemy"));
        /// a.Filter(Container("Enemy/Boss")).IsEmpty(); // true
        /// c.Filter(null).IsEmpty();                    // true — null -> empty
        /// </code>
        /// </example>
        public GameTagContainer Filter(GameTagContainer other)
        {
            var result = new GameTagContainer();
            if (other == null) return result;
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesAny(other)) result._tags.Add(_tags[i]);
            return result;
        }

        /// <summary>
        /// Returns a new container with this container's tags plus all of their parent branches.
        /// Shared ancestors appear once. Unreal <c>GetGameplayTagParents</c> (named for this project's
        /// <see cref="GameTag"/> type).
        /// </summary>
        /// <remarks>
        /// Turns the implicit ancestors that <see cref="HasTag"/> matches on into explicit members. After this,
        /// <see cref="HasTagExact"/> answers <c>true</c> for any ancestor of an original tag — the same trick
        /// Unreal uses internally to make hierarchical lookups flat. Useful when you query the same set many times.
        /// </remarks>
        /// <example>
        /// <code>
        /// var c = new GameTagContainer();
        /// c.AddTag(GameTag.Find("Enemy/Boss/Elite"));
        /// c.GetGameTagParents();      // { Enemy/Boss/Elite, Enemy/Boss, Enemy }  (Count 3)
        ///
        /// c.AddTag(GameTag.Find("Enemy/Minion"));
        /// c.GetGameTagParents();      // { Elite, Boss, Enemy, Minion } — Enemy deduped (Count 4)
        /// </code>
        /// </example>
        public GameTagContainer GetGameTagParents()
        {
            var result = new GameTagContainer();
            for (int i = 0; i < _tags.Count; i++)
                for (var t = _tags[i]; t.IsValid; t = t.Parent)
                    result.AddTag(t);
            return result;
        }
    }
}
