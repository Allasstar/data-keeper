using System;
using System.Linq;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    [Serializable]
    public struct GameTag : IEquatable<GameTag>
    {
        public const string SEPARATOR = "/";
    
        [SerializeField] private string _value;
        [SerializeField] private bool _autoRegister;
        
        public string Value => _value;

        private GameTag[] _nodes;
        
        public GameTag(string value, bool autoRegister = false)
        {
            _autoRegister = autoRegister;
            _value = value;
            _nodes = Array.Empty<GameTag>();

            if (!_autoRegister) return;
            GameTagRegistry.RegisterTag(_value);
        }

        private GameTag[] GetNodes()
        {
            if (_nodes == null || _nodes.Length == 0)
            {
                _nodes = _value != null
                    ? _value.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries).Select(s => new GameTag(s)).ToArray()
                    : Array.Empty<GameTag>();
            }
            
            return _nodes;
        }
        
        public bool StartsWith(GameTag other) => _value.StartsWith(other._value + SEPARATOR);
        public bool StartsWith(string other) => _value.StartsWith(other + SEPARATOR);
        
        public bool Contains(GameTag other) => _value.Contains(other._value);
        public bool Contains(string other) => _value.Contains(other);
        
        public bool Equals(GameTag other) => _value == other._value;
        public bool Equals(string other) => _value == other;
        
        public override string ToString() => _value ?? string.Empty;
    }
}