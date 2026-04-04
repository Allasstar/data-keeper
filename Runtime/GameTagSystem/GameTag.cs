using System;
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

        private string[] _nodes;
        
        public GameTag(string value, bool autoRegister = false)
        {
            _autoRegister = autoRegister;
            _value = value;
            _nodes = Array.Empty<string>();

            if (!_autoRegister) return;
            GameTagRegistry.RegisterTag(_value);
        }

        private string[] GetNodes()
        {
            if (_nodes == null || _nodes.Length == 0)
            {
                _nodes = _value != null
                    ? _value.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    : Array.Empty<string>();
            }
            
            return _nodes;
        }

        public bool StartsWith(GameTag other) => _value.StartsWith(other._value + SEPARATOR);
        public bool StartsWith(string other) => _value.StartsWith(other + SEPARATOR);
        
        public bool Equals(GameTag other) => _value == other._value;
        public bool Equals(string other) => _value == other;
        
        public override string ToString() => _value ?? string.Empty;
    }
}