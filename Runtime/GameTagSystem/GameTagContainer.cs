using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    /// <summary>
    /// A set of <see cref="GameTag"/>s with Unreal-style hierarchical queries.
    /// All query methods are zero-GC (index loops, no LINQ / no enumerator allocation).
    /// </summary>
    [Serializable]
    public class GameTagContainer
    {
        [SerializeField] private List<GameTag> _tags = new();

        public IReadOnlyList<GameTag> Tags => _tags;
        public int Count => _tags.Count;

        public void AddTag(GameTag tag)
        {
            if (tag.IsValid && !HasTagExact(tag)) _tags.Add(tag);
        }

        public bool RemoveTag(GameTag tag) => _tags.Remove(tag);

        public void AppendTags(GameTagContainer other)
        {
            if (other == null) return;
            for (int i = 0; i < other._tags.Count; i++) AddTag(other._tags[i]);
        }

        public void Clear() => _tags.Clear();

        // Exact: some contained tag IS this tag (ignores hierarchy).
        public bool HasTagExact(GameTag tag)
        {
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesTagExact(tag)) return true;
            return false;
        }

        // Hierarchical: some contained tag == tag OR is a descendant of tag.
        // (Container "Enemy/Boss" HasTag "Enemy" -> true.)
        public bool HasTag(GameTag tag)
        {
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].MatchesTag(tag)) return true;
            return false;
        }

        public bool HasAny(GameTagContainer other)
        {
            if (other == null) return false;
            for (int i = 0; i < other._tags.Count; i++)
                if (HasTag(other._tags[i])) return true;
            return false;
        }

        public bool HasAll(GameTagContainer other)
        {
            if (other == null) return true;
            for (int i = 0; i < other._tags.Count; i++)
                if (!HasTag(other._tags[i])) return false;
            return true;
        }

        public bool HasAnyExact(GameTagContainer other)
        {
            if (other == null) return false;
            for (int i = 0; i < other._tags.Count; i++)
                if (HasTagExact(other._tags[i])) return true;
            return false;
        }

        public bool HasAllExact(GameTagContainer other)
        {
            if (other == null) return true;
            for (int i = 0; i < other._tags.Count; i++)
                if (!HasTagExact(other._tags[i])) return false;
            return true;
        }
    }
}
