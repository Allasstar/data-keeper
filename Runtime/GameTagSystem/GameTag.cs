using System;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    [Serializable]
    public struct GameTag : IEquatable<GameTag>
    {
        public const string SEPARATOR = "/";
    
        [SerializeField] private string _value;
        [SerializeField] private string[] _nodes;
        
        [SerializeField] private bool _autoRegister;
        
        public string Value => _value;
        public string[] Nodes => _nodes;

        public GameTag(string value, bool autoRegister = false)
        {
            _autoRegister = autoRegister;
            _value = value;
            _nodes = _value != null
                ? _value.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            Register();
        }
        
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // private static void Initialize()
        // {
        //      Register();
        // }
        
        private void Register()
        {
            if (!_autoRegister) return;
            if (!string.IsNullOrEmpty(_value))
            {
                if (GameTagRegistry.Default != null)
                {
                    GameTagRegistry.Default.Add(_value);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(GameTagRegistry.Default);
#endif
                }
            }
        }

        public bool StartsWith(GameTag other) => _value.StartsWith(other._value);
        public bool Equals(GameTag other) => _value == other._value;
        public override string ToString() => _value ?? string.Empty;
    }
}