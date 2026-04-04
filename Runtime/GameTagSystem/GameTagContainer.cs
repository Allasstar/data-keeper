using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    [Serializable]
    public class GameTagContainer
    {
        [SerializeField] private List<GameTag> _tags = new();

        public IReadOnlyList<GameTag> Tags => _tags;

        public bool HasTag(GameTag tag) => _tags.Contains(tag);
        public bool HasStartWith(GameTag tag) => _tags.Any(t => t.StartsWith(tag));

        public void AddTag(GameTag tag) { if (!_tags.Contains(tag)) _tags.Add(tag); }
        public void RemoveTag(GameTag tag) => _tags.Remove(tag);
    }
}