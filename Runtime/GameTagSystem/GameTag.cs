using System;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    /// <summary>
    /// Lightweight, zero-GC handle to a node in the <see cref="GameTagRegistry"/> tree.
    /// Stores only a stable int Id, so renaming or moving the node never breaks references.
    /// Name / Path / hierarchy / matching are resolved through the registry.
    /// </summary>
    /// <remarks>
    /// Tags form a path-separated tree (Unreal GameplayTag style). A leaf carries its whole
    /// ancestry, so it belongs to <b>every</b> parent branch above it. Take this tree:
    /// <code>
    /// Damage
    ///  └ Elemental
    ///     ├ Fire   (leaf "Damage/Elemental/Fire")
    ///     └ Ice
    /// </code>
    /// The leaf <c>Fire</c> has two parent branches — <c>Damage/Elemental</c> and <c>Damage</c> —
    /// and matches against any of them.
    /// <para>
    /// Matching kinds:
    /// <list type="bullet">
    /// <item><see cref="MatchesTag"/> — hierarchical: this tag, or any of its ancestors, equals the query.</item>
    /// <item><see cref="MatchesTagExact"/> — only the same node (redirect-aware), hierarchy ignored.</item>
    /// <item><see cref="IsChildOf"/> — strictly under the query (descendant, not the same node).</item>
    /// </list>
    /// Use <see cref="Equals(GameTag)"/> / <c>==</c> for collection keys (raw-id structural identity);
    /// use <see cref="MatchesTagExact"/> for the redirect-aware "is this the same tag" question.
    /// </para>
    /// <para>
    /// Lookup goes through <see cref="Find"/> (or <see cref="TryFind"/> when a missing path is a caller error worth
    /// catching); <see cref="None"/> is the canonical invalid handle. The struct is ordered
    /// (<see cref="IComparable{T}"/> by raw id) for sorted collections, and <see cref="GetGameTagParents"/> /
    /// <see cref="GetSingleTagContainer"/> project a tag into a <see cref="GameTagContainer"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var fire = GameTag.Find("Damage/Elemental/Fire");
    ///
    /// // Hierarchical: a leaf matches every branch on its way to the root.
    /// fire.MatchesTag(GameTag.Find("Damage/Elemental"));     // true  — immediate parent branch
    /// fire.MatchesTag(GameTag.Find("Damage"));               // true  — grandparent branch (any ancestor)
    /// fire.MatchesTag(fire);                                 // true  — matches itself
    /// fire.MatchesTag(GameTag.Find("Damage/Elemental/Ice")); // false — sibling, not an ancestor
    /// GameTag.Find("Damage").MatchesTag(fire);               // false — a parent does NOT match its child
    ///
    /// // Exact: hierarchy is ignored, only the same node counts.
    /// fire.MatchesTagExact(GameTag.Find("Damage/Elemental")); // false
    /// fire.MatchesTagExact(fire);                             // true
    ///
    /// // Strictly under a branch.
    /// fire.IsChildOf(GameTag.Find("Damage")); // true
    /// fire.IsChildOf(fire);                   // false — same node, not a child
    /// </code>
    /// </example>
    [Serializable]
    public struct GameTag : IEquatable<GameTag>, IComparable<GameTag>
    {
        /// <summary>Path separator between segments, e.g. the <c>/</c> in <c>"Damage/Elemental/Fire"</c>.</summary>
        public const string SEPARATOR = GameTagRegistry.SEPARATOR;

        /// <summary>The canonical invalid tag (wraps <see cref="GameTagRegistry.NONE"/>); equal to <c>default</c> but self-documenting.</summary>
        public static readonly GameTag None = new GameTag(GameTagRegistry.NONE);

        [SerializeField] private int _id;

        /// <summary>Wraps a raw registry id. Prefer <see cref="Find"/> or the generated <c>GameTags</c> constants.</summary>
        /// <param name="id">A stable registry node id (<see cref="GameTagRegistry.NONE"/> for an invalid tag).</param>
        public GameTag(int id) => _id = id;

        private static GameTagRegistry Registry => GameTagRegistry.Default;

        /// <summary>The stable registry node id this handle wraps. Survives rename/move of the node.</summary>
        public int Id => _id;

        /// <summary>The id as an int key for blackboards / int-keyed stores; equals <see cref="Id"/>.</summary>
        public int Hash => _id;

        /// <summary><c>true</c> when this handle points at a live node in the active registry.</summary>
        public bool IsValid
        {
            get
            {
                if (_id == GameTagRegistry.NONE) return false;
                var registry = Registry;
                return registry != null && registry.IsValid(_id);
            }
        }

        /// <summary>The leaf segment only, e.g. <c>"Fire"</c> for <c>"Damage/Elemental/Fire"</c>. Empty string if unknown.</summary>
        public string Name => Registry != null ? Registry.GetName(_id) ?? string.Empty : string.Empty;

        /// <summary>The full separated path, e.g. <c>"Damage/Elemental/Fire"</c>. Empty string if unknown.</summary>
        public string Path => Registry != null ? Registry.GetPath(_id) ?? string.Empty : string.Empty;

        /// <summary>
        /// The immediate parent node — the first parent branch up. For <c>"Damage/Elemental/Fire"</c>
        /// this is <c>"Damage/Elemental"</c>. A root tag's parent is an invalid tag. Walk repeatedly to
        /// reach higher branches (<c>"Damage"</c>).
        /// </summary>
        public GameTag Parent => new GameTag(Registry != null ? Registry.GetParentId(_id) : GameTagRegistry.NONE);

        // ── Construction / lookup ───────────────────────────────────────────────

        /// <summary>Builds a handle from a raw registry id (e.g. a serialized/blackboard value).</summary>
        public static GameTag FromId(int id) => new GameTag(id);

        /// <summary>
        /// Resolves an existing path to its tag; returns an invalid tag if the path is unknown.
        /// Lookup only — authoring/creation goes through the registry or the generated <c>GameTags</c> class.
        /// </summary>
        /// <param name="path">Full separated path, e.g. <c>"Damage/Elemental/Fire"</c>.</param>
        public static GameTag Find(string path) => new GameTag(Registry != null ? Registry.FindByPath(path) : GameTagRegistry.NONE);

        /// <summary>
        /// Tries to resolve an existing path to its tag. Returns <c>true</c> and sets <paramref name="tag"/> to the
        /// resolved tag when the path is known; otherwise returns <c>false</c> and sets it to <see cref="None"/>.
        /// Prefer this over <see cref="Find"/> when a missing path is a caller error you want to catch rather than
        /// silently propagate as an invalid tag.
        /// </summary>
        /// <param name="path">Full separated path, e.g. <c>"Damage/Elemental/Fire"</c>.</param>
        /// <param name="tag">The resolved tag, or <see cref="None"/> when the path is unknown.</param>
        public static bool TryFind(string path, out GameTag tag)
        {
            tag = Find(path);
            return tag.IsValid;
        }

        // ── Matching (Unreal GameplayTag semantics) ─────────────────────────────
        // All matching is redirect-aware and delegates to the registry, so the rules live in
        // exactly one place. The registry walks the full parent chain, so a hierarchical match
        // succeeds against this tag itself OR any of its ancestors.

        /// <summary>
        /// Hierarchical match: <c>true</c> when <paramref name="other"/> is this tag itself or any of
        /// its parent branches. <c>"Damage/Elemental/Fire".MatchesTag("Damage")</c> is <c>true</c>;
        /// the reverse (<c>"Damage".MatchesTag("Damage/Elemental/Fire")</c>) is <c>false</c> — a parent
        /// never matches its child.
        /// </summary>
        /// <param name="other">The (usually broader) tag to test this tag against.</param>
        /// <remarks>
        /// Direction matters: the receiver is the specific tag, <paramref name="other"/> the (broader) query.
        /// "Matches" means coverage, not identity — the check climbs the receiver's parent chain looking for the
        /// query, so it succeeds for the tag itself or any of its ancestors. The reverse never holds: a broad tag
        /// does not match a more specific one.
        /// </remarks>
        /// <example>
        /// Tree used below: <c>Damage/Elemental/{Fire,Ice}</c>, <c>Damage/Physical</c>, <c>Status/Burning</c>.
        /// <code>
        /// var fire = GameTag.Find("Damage/Elemental/Fire");
        ///
        /// // Self + every ancestor branch -> true
        /// fire.MatchesTag(fire);                                  // true  — self
        /// fire.MatchesTag(GameTag.Find("Damage/Elemental"));      // true  — immediate parent
        /// fire.MatchesTag(GameTag.Find("Damage"));               // true  — deeper ancestor (any ancestor counts)
        ///
        /// // Everything off the ancestor chain -> false
        /// fire.MatchesTag(GameTag.Find("Damage/Elemental/Ice"));  // false — sibling
        /// fire.MatchesTag(GameTag.Find("Damage/Physical"));       // false — cousin branch
        /// fire.MatchesTag(GameTag.Find("Status/Burning"));        // false — unrelated root
        /// fire.MatchesTag(default);                               // false — invalid query
        /// GameTag.Find("Damage").MatchesTag(fire);                // false — a parent never matches its child
        ///
        /// // A retired tag that redirects to NONE (deprecated) matches nothing.
        ///
        /// // Typical use: react to a whole branch with one check.
        /// if (incomingDamage.MatchesTag(GameTag.Find("Damage/Elemental")))
        ///     ApplyElementalResist();
        /// </code>
        /// </example>
        public bool MatchesTag(GameTag other) => Registry != null && Registry.Matches(_id, other._id);

        /// <summary>
        /// Exact match: <c>true</c> only when this and <paramref name="other"/> are the SAME node
        /// (redirect-aware, hierarchy ignored). <c>"Damage/Elemental/Fire".MatchesTagExact("Damage/Elemental")</c>
        /// is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// Identity, not coverage — hierarchy is ignored entirely. It is redirect-aware (both sides are resolved
        /// through any rename/merge redirect first), which is exactly what separates it from <c>==</c> /
        /// <see cref="Equals(GameTag)"/>, which compare the raw id. A tag that resolves to NONE (deprecated)
        /// matches nothing, not even itself.
        /// </remarks>
        /// <example>
        /// <code>
        /// var fire = GameTag.Find("Damage/Elemental/Fire");
        ///
        /// fire.MatchesTagExact(fire);                                 // true  — same node
        /// fire.MatchesTagExact(GameTag.Find("Damage/Elemental"));     // false — ancestor, not the same node
        /// fire.MatchesTagExact(GameTag.Find("Damage/Elemental/Ice")); // false — sibling
        /// fire.MatchesTagExact(default);                              // false — invalid
        ///
        /// // Redirect-aware identity (this is what separates it from == / Equals):
        /// //   • a tag merged/renamed into another resolves to its replacement, so a stored handle
        /// //     still matches the canonical tag exactly:  retired.MatchesTagExact(replacement) -> true
        /// //   • a tag redirected to NONE (deprecated, no replacement) resolves to NONE and matches
        /// //     nothing — not even itself:                deprecated.MatchesTagExact(deprecated) -> false
        /// </code>
        /// </example>
        public bool MatchesTagExact(GameTag other) => Registry != null && Registry.MatchesExact(_id, other._id);

        /// <summary>
        /// Hierarchical match against a container: <c>true</c> when this tag matches (is, or is a child of)
        /// ANY tag in <paramref name="container"/>. Mirrors Unreal <c>FGameplayTag::MatchesAny</c>.
        /// <c>null</c>/empty container → <c>false</c>.
        /// </summary>
        /// <remarks>
        /// The container-side mirror of <see cref="MatchesTag"/>: succeeds when this tag is covered by at least
        /// one tag in the set. Handy for testing a tag against a whitelist of broad categories.
        /// </remarks>
        /// <example>
        /// <code>
        /// var fire = GameTag.Find("Damage/Elemental/Fire");
        /// fire.MatchesAny(Container("Damage", "Status"));        // true  — matches the "Damage" branch
        /// fire.MatchesAny(Container("Damage/Elemental/Ice"));    // false — only a sibling listed
        /// fire.MatchesAny(Container("Status", "Movement"));      // false — no branch matches
        /// fire.MatchesAny(new GameTagContainer());              // false — empty container
        /// fire.MatchesAny(null);                                // false — null container
        /// </code>
        /// </example>
        public bool MatchesAny(GameTagContainer container)
        {
            if (container == null) return false;
            var tags = container.Tags;
            for (int i = 0; i < tags.Count; i++)
                if (MatchesTag(tags[i])) return true;
            return false;
        }

        /// <summary>
        /// Exact match against a container: <c>true</c> when this tag is the SAME node as ANY tag in
        /// <paramref name="container"/> (hierarchy ignored). Mirrors Unreal <c>FGameplayTag::MatchesAnyExact</c>.
        /// <c>null</c>/empty container → <c>false</c>.
        /// </summary>
        /// <remarks>Exact mirror of <see cref="MatchesTagExact"/>: succeeds only when this tag is literally one of the set's members.</remarks>
        /// <example>
        /// <code>
        /// var fire = GameTag.Find("Damage/Elemental/Fire");
        /// fire.MatchesAnyExact(Container("Damage/Elemental/Fire", "Status")); // true  — exact entry present
        /// fire.MatchesAnyExact(Container("Damage/Elemental"));                // false — only an ancestor listed
        /// </code>
        /// </example>
        public bool MatchesAnyExact(GameTagContainer container)
        {
            if (container == null) return false;
            var tags = container.Tags;
            for (int i = 0; i < tags.Count; i++)
                if (MatchesTagExact(tags[i])) return true;
            return false;
        }

        /// <summary>
        /// How closely two tags match: the number of shared ancestor nodes (their common path prefix).
        /// Mirrors Unreal <c>FGameplayTag::MatchesTagDepth</c>. <c>0</c> means unrelated (different roots);
        /// a higher number means a closer/more specific match.
        /// </summary>
        /// <remarks>
        /// A graded version of <see cref="MatchesTag"/>: instead of yes/no it returns how many path segments the
        /// two tags share from the root. Symmetric (order-independent), and <c>0</c> exactly when the tags are
        /// unrelated. Use it to rank the best match among several candidate tags.
        /// </remarks>
        /// <example>
        /// <code>
        /// var fire = GameTag.Find("Damage/Elemental/Fire");
        /// fire.MatchesTagDepth(fire);                                 // 3  — share all of "Damage/Elemental/Fire"
        /// fire.MatchesTagDepth(GameTag.Find("Damage/Elemental"));     // 2  — share "Damage/Elemental"
        /// fire.MatchesTagDepth(GameTag.Find("Damage/Elemental/Ice")); // 2  — sibling, share "Damage/Elemental"
        /// fire.MatchesTagDepth(GameTag.Find("Damage"));               // 1  — share "Damage"
        /// fire.MatchesTagDepth(GameTag.Find("Damage/Physical"));      // 1  — cousin, share "Damage"
        /// fire.MatchesTagDepth(GameTag.Find("Status/Burning"));       // 0  — unrelated
        /// fire.MatchesTagDepth(default);                              // 0  — invalid
        /// // Symmetric: a.MatchesTagDepth(b) == b.MatchesTagDepth(a).
        /// </code>
        /// </example>
        public int MatchesTagDepth(GameTag other) => Registry != null ? Registry.MatchDepth(_id, other._id) : 0;

        // ── Tag → container (Unreal GameplayTag semantics) ──────────────────────

        /// <summary>
        /// A new container holding this tag plus all of its parent branches (self, then each ancestor up to the
        /// root). Mirrors Unreal <c>FGameplayTag::GetGameplayTagParents</c>. An invalid tag yields an empty container.
        /// </summary>
        /// <remarks>
        /// The tag-level counterpart of <see cref="GameTagContainer.GetGameTagParents"/>: it flattens the implicit
        /// ancestors that <see cref="MatchesTag"/> matches on into explicit members, so a later
        /// <see cref="GameTagContainer.HasTagExact"/> answers <c>true</c> for any ancestor.
        /// </remarks>
        /// <example>
        /// <code>
        /// GameTag.Find("Damage/Elemental/Fire").GetGameTagParents();
        /// //   { Damage/Elemental/Fire, Damage/Elemental, Damage }  (Count 3)
        /// GameTag.None.GetGameTagParents().IsEmpty();   // true
        /// </code>
        /// </example>
        public GameTagContainer GetGameTagParents()
        {
            var result = new GameTagContainer();
            for (var t = this; t.IsValid; t = t.Parent)
                result.AddTag(t);
            return result;
        }

        /// <summary>
        /// A new container holding only this tag. Mirrors Unreal <c>FGameplayTag::GetSingleTagContainer</c>.
        /// An invalid tag yields an empty container.
        /// </summary>
        /// <example>
        /// <code>
        /// GameTag.Find("Damage").GetSingleTagContainer().Count; // 1
        /// GameTag.None.GetSingleTagContainer().IsEmpty();        // true
        /// </code>
        /// </example>
        public GameTagContainer GetSingleTagContainer()
        {
            var result = new GameTagContainer();
            result.AddTag(this);
            return result;
        }

        // ── Extension beyond Unreal's API ───────────────────────────────────────

        /// <summary>
        /// <c>true</c> when this tag is strictly under <paramref name="other"/> — a descendant, but not the
        /// same node. <c>"Damage/Elemental/Fire".IsChildOf("Damage")</c> is <c>true</c>; a tag is never a
        /// child of itself. (Convenience helper; Unreal has no direct equivalent.)
        /// </summary>
        /// <remarks>
        /// The strict-descendant half of <see cref="MatchesTag"/>: the same hierarchical test, minus the
        /// self/equal case. Equivalent to <c>MatchesTag(other) &amp;&amp; !MatchesTagExact(other)</c>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var fire = GameTag.Find("Damage/Elemental/Fire");
        ///
        /// fire.IsChildOf(GameTag.Find("Damage/Elemental"));     // true  — direct parent
        /// fire.IsChildOf(GameTag.Find("Damage"));               // true  — deeper ancestor
        /// fire.IsChildOf(fire);                                 // false — same node, not a child
        /// fire.IsChildOf(GameTag.Find("Damage/Elemental/Ice")); // false — sibling
        ///
        /// // IsChildOf(x) == MatchesTag(x) AND NOT MatchesTagExact(x).
        /// </code>
        /// </example>
        public bool IsChildOf(GameTag other) => Registry != null && Registry.IsChildOf(_id, other._id);

        // ── Identity (structural, by raw id) ─────────────────────────────────────
        // Equals/==/GetHashCode compare the raw stored id — the handle's structural identity —
        // so GameTag is a correct, zero-GC dictionary/HashSet key with a stable hash. This is
        // deliberately NOT redirect-aware (that would change a stored key's hash when a redirect
        // is added): a retired id and the id it redirects to are distinct handles here, but the
        // same tag under MatchesTagExact. Use MatchesTagExact for "is this the same tag".

        /// <summary>Structural equality by raw id (NOT redirect-aware). Use <see cref="MatchesTagExact"/> for semantic identity.</summary>
        /// <example>
        /// <code>
        /// GameTag.Find("Damage").Equals(GameTag.Find("Damage")); // true  — same node
        /// GameTag.Find("Damage").Equals(GameTag.Find("Status")); // false — different nodes
        /// default(GameTag).Equals(default);                      // true  — both NONE
        ///
        /// // Where it diverges from MatchesTagExact: after a tag is merged/redirected, the retired
        /// // handle and its replacement are DIFFERENT here but the SAME under MatchesTagExact:
        /// //   retired == replacement                -> false   (raw ids differ)
        /// //   retired.MatchesTagExact(replacement)   -> true    (redirect resolved)
        /// </code>
        /// </example>
        public bool Equals(GameTag other) => _id == other._id;

        /// <inheritdoc cref="Equals(GameTag)"/>
        public override bool Equals(object obj) => obj is GameTag t && _id == t._id;

        /// <summary>Hash of the raw id; consistent with <see cref="Equals(GameTag)"/> so GameTag is a valid dictionary key.</summary>
        public override int GetHashCode() => _id;

        /// <summary>Structural equality by raw id — see <see cref="Equals(GameTag)"/>.</summary>
        public static bool operator ==(GameTag a, GameTag b) => a._id == b._id;

        /// <summary>Structural inequality by raw id — see <see cref="Equals(GameTag)"/>.</summary>
        public static bool operator !=(GameTag a, GameTag b) => a._id != b._id;

        /// <summary>
        /// Orders by raw id (structural, NOT redirect-aware — consistent with <see cref="Equals(GameTag)"/>).
        /// Gives a deterministic, allocation-free ordering for sorting and <see cref="System.Collections.Generic.SortedSet{T}"/> /
        /// <see cref="System.Collections.Generic.SortedDictionary{TKey,TValue}"/> keys. Note this is id order, not
        /// alphabetical <see cref="Path"/> order.
        /// </summary>
        public int CompareTo(GameTag other) => _id.CompareTo(other._id);

        /// <summary>The full <see cref="Path"/>, or an empty string when the tag is unknown/invalid.</summary>
        public override string ToString() => Path;
    }
}
