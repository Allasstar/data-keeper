using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    /// <summary>
    /// A set of <see cref="GameTag"/>s with Unreal-style hierarchical queries — the port of
    /// <c>FGameplayTagContainer</c>. A container holds the tags added to it explicitly; hierarchical
    /// queries (<see cref="HasTag"/>, <see cref="HasAny"/>, …) additionally match against the parent
    /// branches of those tags.
    /// </summary>
    /// <remarks>
    /// All query methods are zero-GC (index loops, no LINQ / no enumerator allocation). Empty-input
    /// semantics follow Unreal: <see cref="HasAny"/>/<see cref="HasAnyExact"/> on an empty or null
    /// argument return <c>false</c>, while <see cref="HasAll"/>/<see cref="HasAllExact"/> return
    /// <c>true</c> (no required tag is missing).
    /// <code>
    /// var c = new GameTagContainer();
    /// c.AddTag(GameTag.Find("Damage/Elemental/Fire"));
    /// c.HasTag(GameTag.Find("Damage"));        // true  — parent branch of a contained tag
    /// c.HasTagExact(GameTag.Find("Damage"));   // false — "Damage" was never added explicitly
    /// </code>
    /// </remarks>
    [Serializable]
    public class GameTagContainer
    {
        [SerializeField] private List<GameTag> _tags = new();

        /// <summary>The explicitly-added tags, in insertion order.</summary>
        public IReadOnlyList<GameTag> Tags => _tags;

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
        /// instead when you want only the most-specific tag of a branch.
        /// </remarks>
        public void AddTag(GameTag tag)
        {
            if (tag.IsValid && !HasTagExact(tag)) _tags.Add(tag);
        }

        /// <summary>Adds <paramref name="tag"/> without validity or uniqueness checks (Unreal <c>AddTagFast</c>).</summary>
        /// <remarks>
        /// For hot paths where the tag is already known valid and unique: skips the O(n) <see cref="HasTagExact"/>
        /// scan that <see cref="AddTag"/> performs. Can introduce duplicates or invalid entries if misused.
        /// </remarks>
        public void AddTagFast(GameTag tag) => _tags.Add(tag);

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
            if (!tag.IsValid) return false;

            // Already covered by an equal or more-specific tag -> nothing to do.
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesTag(tag)) return false;

            // Drop ancestors of the new leaf; they are now redundant.
            for (int i = _tags.Count - 1; i >= 0; i--)
                if (tag.MatchesTag(_tags[i])) _tags.RemoveAt(i);

            _tags.Add(tag);
            return true;
        }

        /// <summary>Removes <paramref name="tag"/> (exact). Returns <c>true</c> if it was present. Unreal <c>RemoveTag</c>.</summary>
        /// <remarks>
        /// Removal is by exact identity: it deletes only the literal member, never an ancestor or descendant.
        /// Removing <c>"Enemy"</c> from a container holding <c>"Enemy/Boss"</c> removes nothing and returns <c>false</c>.
        /// </remarks>
        public bool RemoveTag(GameTag tag) => _tags.Remove(tag);

        /// <summary>Removes every tag in <paramref name="other"/> from this container (Unreal <c>RemoveTags</c>).</summary>
        /// <remarks>Each removal is exact (see <see cref="RemoveTag"/>); hierarchy is not expanded.</remarks>
        public void RemoveTags(GameTagContainer other)
        {
            if (other == null) return;
            for (int i = 0; i < other._tags.Count; i++) _tags.Remove(other._tags[i]);
        }

        /// <summary>Removes all tags (Unreal <c>Reset</c>).</summary>
        public void Reset() => _tags.Clear();

        /// <summary>Removes all tags. C# alias for <see cref="Reset"/>.</summary>
        public void Clear() => _tags.Clear();

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
        /// membership use <see cref="HasTagExact"/>.
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
