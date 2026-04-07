using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    [Serializable]
    public struct GameTag : IEquatable<GameTag>
    {
        public const string SEPARATOR = "/";
        
        private static readonly Dictionary<string, GameTag[]> _nodesCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearNodesCache() => _nodesCache.Clear();
        

        [SerializeField] private string _value;
        [SerializeField] private bool _autoRegister;

        public string Value => _value;

        public GameTag(string value, bool autoRegister = false)
        {
            _autoRegister = autoRegister;
            _value = value;

            if (!_autoRegister) return;
            GameTagRegistry.RegisterTag(_value);
        }

        public IReadOnlyList<GameTag> GetNodes()
        {
            if (string.IsNullOrEmpty(_value))
                return Array.Empty<GameTag>();

            if (!_nodesCache.TryGetValue(_value, out var nodes))
            {
                nodes = _value
                    .Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => new GameTag(s))
                    .ToArray();
                
                _nodesCache[_value] = nodes;
            }

            return nodes;
        }

        public bool Equals(GameTag other) => _value == other._value;
        public bool Equals(string other) => _value == other;

        public bool StartsWith(GameTag other) => _value.StartsWith(other._value) && !Equals(other);
        public bool StartsWith(string other) => _value.StartsWith(other) && !Equals(other);

        public bool StartsWithOrEquals(GameTag other) => StartsWith(other) || Equals(other);
        public bool StartsWithOrEquals(string other) => StartsWith(other) || Equals(other);

        public override string ToString() => _value ?? string.Empty;
    }
}