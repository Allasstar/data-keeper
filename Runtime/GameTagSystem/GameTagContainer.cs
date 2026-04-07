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

        public bool HasTag(GameTag gameTag) => _tags.Any(t => t.Equals(gameTag));
        
        public void AddTag(GameTag gameTag) { if (!HasTag(gameTag)) _tags.Add(gameTag); }
        public bool RemoveTag(GameTag gameTag) => _tags.Remove(gameTag);
        
        public bool HasStartsWithAndNotEquals(GameTag gameTag) => _tags.Any(t => t.StartsWithAndNotEquals(gameTag));
        public bool HasStartWithOrEquals(GameTag gameTag) => _tags.Any(t => t.StartsWithOrEquals(gameTag));
        
        public IEnumerable<GameTag> GetTagsStartsWithAndNotEquals(GameTag gameTag) => _tags.Where(t => t.StartsWithAndNotEquals(gameTag));
        public IEnumerable<GameTag> GetTagsStartsWithOrEquals(GameTag gameTag) => _tags.Where(t => t.StartsWithOrEquals(gameTag));
    }
}