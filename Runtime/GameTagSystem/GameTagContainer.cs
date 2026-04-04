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

        public bool HasTag(GameTag gameTag) => _tags.Contains(gameTag);
        public bool HasStartWith(GameTag gameTag) => _tags.Any(t => t.StartsWith(gameTag));

        public void AddTag(GameTag gameTag) { if (!_tags.Contains(gameTag)) _tags.Add(gameTag); }
        public void RemoveTag(GameTag gameTag) => _tags.Remove(gameTag);
        
        public IEnumerable<GameTag> GetTagsStartsWith(GameTag gameTag) => _tags.Where(t => t.StartsWith(gameTag.Value));
        public IEnumerable<GameTag> GetTagsStartsWith(string tag) => _tags.Where(t => t.StartsWith(tag));
    }
}