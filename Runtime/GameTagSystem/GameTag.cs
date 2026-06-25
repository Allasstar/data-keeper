using System;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    /// <summary>
    /// Lightweight, zero-GC handle to a node in the <see cref="GameTagRegistry"/> tree.
    /// Stores only a stable int Id, so renaming or moving the node never breaks references.
    /// Name / Path / hierarchy / matching are resolved through the registry.
    /// </summary>
    [Serializable]
    public struct GameTag : IEquatable<GameTag>
    {
        public const string SEPARATOR = GameTagRegistry.SEPARATOR;

        [SerializeField] private int _id;

        public GameTag(int id) => _id = id;

        private static GameTagRegistry Registry => GameTagRegistry.Default;

        public int Id => _id;

        // Blackboard and other int-keyed stores use this; data always holds final (resolved) ids.
        public int Hash => _id;

        public bool IsValid => _id != GameTagRegistry.NONE && Registry != null && Registry.IsValid(_id);

        public string Name => Registry != null ? Registry.GetName(_id) : null;
        public string Path => Registry != null ? Registry.GetPath(_id) : null;
        public GameTag Parent => new GameTag(Registry != null ? Registry.GetParentId(_id) : GameTagRegistry.NONE);

        // ── Construction / lookup ───────────────────────────────────────────────
        public static GameTag FromId(int id) => new GameTag(id);

        // Resolves an existing path to its tag. Returns an invalid tag if the path is unknown
        // (lookup only — authoring/creation goes through the registry or the generated GameTags class).
        public static GameTag Find(string path) => new GameTag(Registry != null ? Registry.FindByPath(path) : GameTagRegistry.NONE);

        // ── Matching (Unreal semantics) ─────────────────────────────────────────
        // True when this == other, or other is an ancestor of this ("Enemy/Boss".MatchesTag("Enemy")).
        public bool MatchesTag(GameTag other) => Registry != null && Registry.Matches(_id, other._id);

        // Exact identity (redirect-aware), ignoring hierarchy.
        public bool MatchesTagExact(GameTag other)
            => _id != GameTagRegistry.NONE && Registry != null && Registry.Resolve(_id) == Registry.Resolve(other._id);

        // Strictly under (descendant, not equal).
        public bool IsChildOf(GameTag other) => MatchesTag(other) && !MatchesTagExact(other);

        // ── Equality (by id; consistent Hash/Equals/GetHashCode) ─────────────────
        public bool Equals(GameTag other) => _id == other._id;
        public override bool Equals(object obj) => obj is GameTag t && Equals(t);
        public override int GetHashCode() => _id;

        public static bool operator ==(GameTag a, GameTag b) => a._id == b._id;
        public static bool operator !=(GameTag a, GameTag b) => a._id != b._id;

        public override string ToString() => Path ?? string.Empty;
    }
}
