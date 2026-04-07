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
            _value = value?.TrimEnd()?.TrimEnd(SEPARATOR[0]);

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
        public bool Equals(string str) => _value == str;
        public new bool Equals(object other)
        {
            if (other is GameTag tag) return Equals(tag);
            return false;
        }

        public bool StartsWithAndNotEquals(GameTag other)
        {
            return _value.Length > other._value.Length
                   && _value[other._value.Length] == SEPARATOR[0]
                   && _value.StartsWith(other._value);
        }
        
        public bool StartsWithOrEquals(GameTag other) => StartsWithAndNotEquals(other) || Equals(other);

        public override string ToString() => _value ?? string.Empty;
    }
}