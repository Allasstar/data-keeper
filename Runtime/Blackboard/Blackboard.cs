using System;
using System.Collections.Generic;
using DataKeeper.Attributes;
using DataKeeper.GameTagSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DataKeeper.BlackboardSystem
{
    [Serializable]
    public class Blackboard
    {
        [SerializeReference, SerializeReferenceSelector]
        public List<IBlackboardEntry> Entries = new();

        // Value types
        private readonly Dictionary<int, float> _floats = new();
        private readonly Dictionary<int, int> _ints = new();
        private readonly Dictionary<int, bool> _bools = new();
        private readonly Dictionary<int, string> _strings = new();
        private readonly Dictionary<int, Vector2> _vector2s = new();
        private readonly Dictionary<int, Vector3> _vector3s = new();
        private readonly Dictionary<int, Vector4> _vector4s = new();
        private readonly Dictionary<int, Vector2Int> _vector2Ints = new();
        private readonly Dictionary<int, Vector3Int> _vector3Ints = new();
        private readonly Dictionary<int, Quaternion> _quaternions = new();
        private readonly Dictionary<int, Color> _colors = new();
        private readonly Dictionary<int, Rect> _rects = new();
        private readonly Dictionary<int, Bounds> _bounds = new();

        // Reference types (all UnityEngine.Object share one store; GetObject<T> resolves the concrete type)
        private readonly Dictionary<int, Object> _objects = new();

        public void Initialize()
        {
            foreach (var entry in Entries)
                entry?.Apply(this);
        }

        // float
        public float GetFloat(GameTag key) => _floats.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetFloat(GameTag key, float value) => _floats[key.Hash] = value;

        // int
        public int GetInt(GameTag key) => _ints.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetInt(GameTag key, int value) => _ints[key.Hash] = value;

        // bool
        public bool GetBool(GameTag key) => _bools.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetBool(GameTag key, bool value) => _bools[key.Hash] = value;

        // string
        public string GetString(GameTag key) => _strings.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetString(GameTag key, string value) => _strings[key.Hash] = value;

        // Vector2
        public Vector2 GetVector2(GameTag key) => _vector2s.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector2(GameTag key, Vector2 value) => _vector2s[key.Hash] = value;

        // Vector3
        public Vector3 GetVector3(GameTag key) => _vector3s.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector3(GameTag key, Vector3 value) => _vector3s[key.Hash] = value;

        // Vector4
        public Vector4 GetVector4(GameTag key) => _vector4s.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector4(GameTag key, Vector4 value) => _vector4s[key.Hash] = value;

        // Vector2Int
        public Vector2Int GetVector2Int(GameTag key) => _vector2Ints.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector2Int(GameTag key, Vector2Int value) => _vector2Ints[key.Hash] = value;

        // Vector3Int
        public Vector3Int GetVector3Int(GameTag key) => _vector3Ints.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector3Int(GameTag key, Vector3Int value) => _vector3Ints[key.Hash] = value;

        // Quaternion
        public Quaternion GetQuaternion(GameTag key) =>
            _quaternions.TryGetValue(key.Hash, out var v) ? v : Quaternion.identity;

        public void SetQuaternion(GameTag key, Quaternion value) => _quaternions[key.Hash] = value;

        // Color
        public Color GetColor(GameTag key) => _colors.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetColor(GameTag key, Color value) => _colors[key.Hash] = value;

        // Rect
        public Rect GetRect(GameTag key) => _rects.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetRect(GameTag key, Rect value) => _rects[key.Hash] = value;

        // Bounds
        public Bounds GetBounds(GameTag key) => _bounds.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetBounds(GameTag key, Bounds value) => _bounds[key.Hash] = value;

        // UnityEngine.Object (generic store for all reference types)
        public T GetObject<T>(GameTag key) where T : Object
            => _objects.TryGetValue(key.Hash, out var v) ? v as T : null;

        public void SetObject(GameTag key, Object value) => _objects[key.Hash] = value;

        public bool HasObject(GameTag key)
            => _objects.TryGetValue(key.Hash, out var v) && v != null;

        // Typed reference convenience getters
        public GameObject GetGameObject(GameTag key) => GetObject<GameObject>(key);
        public Transform GetTransform(GameTag key) => GetObject<Transform>(key);
        public RectTransform GetRectTransform(GameTag key) => GetObject<RectTransform>(key);
        public Image GetImage(GameTag key) => GetObject<Image>(key);
        public TMP_Text GetText(GameTag key) => GetObject<TMP_Text>(key);
        public Sprite GetSprite(GameTag key) => GetObject<Sprite>(key);

        public void Clear()
        {
            _floats.Clear();
            _ints.Clear();
            _bools.Clear();
            _strings.Clear();
            _vector2s.Clear();
            _vector3s.Clear();
            _vector4s.Clear();
            _vector2Ints.Clear();
            _vector3Ints.Clear();
            _quaternions.Clear();
            _colors.Clear();
            _rects.Clear();
            _bounds.Clear();
            _objects.Clear();
        }
    }
}