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
    /// <summary>
    /// Typed key-value store keyed by <see cref="GameTag"/>. Dictionaries are created lazily on first Set.
    /// Reference types have dedicated stores; anything else derived from <see cref="Object"/> goes through
    /// <see cref="SetObject"/> / <see cref="GetObject{T}"/>.
    /// </summary>
    /// <example>
    /// Extend with custom stores in a project-side partial file:
    /// <code>
    /// namespace DataKeeper.BlackboardSystem
    /// {
    ///     public partial class Blackboard
    ///     {
    ///         private Dictionary&lt;int, MyData&gt; _myData;
    ///
    ///         public bool HasMyData(GameTag key) => _myData != null &amp;&amp; _myData.ContainsKey(key.Hash);
    ///         public MyData GetMyData(GameTag key) => _myData != null &amp;&amp; _myData.TryGetValue(key.Hash, out var v) ? v : default;
    ///         public void SetMyData(GameTag key, MyData value) => (_myData ??= new())[key.Hash] = value;
    ///
    ///         // Called by Clear() so custom stores are reset too.
    ///         partial void ClearExtra()
    ///         {
    ///             _myData?.Clear();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public partial class Blackboard
    {
        [SerializeReference, SerializeReferenceSelector]
        public List<IBlackboardEntry> Entries = new();

        // Value types (lazy — created on first Set)
        private Dictionary<int, float> _floats;
        private Dictionary<int, int> _ints;
        private Dictionary<int, bool> _bools;
        private Dictionary<int, string> _strings;
        private Dictionary<int, Vector2> _vector2s;
        private Dictionary<int, Vector3> _vector3s;
        private Dictionary<int, Vector4> _vector4s;
        private Dictionary<int, Vector2Int> _vector2Ints;
        private Dictionary<int, Vector3Int> _vector3Ints;
        private Dictionary<int, Quaternion> _quaternions;
        private Dictionary<int, Color> _colors;
        private Dictionary<int, Rect> _rects;
        private Dictionary<int, Bounds> _bounds;
        private Dictionary<int, GameTagContainer> _gameTagContainers;

        // Reference types (lazy — created on first Set)
        private Dictionary<int, GameObject> _gameObjects;
        private Dictionary<int, Transform> _transforms;
        private Dictionary<int, RectTransform> _rectTransforms;
        private Dictionary<int, TMP_Text> _tmpTexts;
        private Dictionary<int, Image> _images;
        private Dictionary<int, Sprite> _sprites;

        // Fallback store for any other UnityEngine.Object; GetObject<T> resolves the concrete type
        private Dictionary<int, Object> _objects;

        public void Initialize()
        {
            foreach (var entry in Entries)
                entry?.Apply(this);
        }

        // float
        public bool HasFloat(GameTag key) => _floats != null && _floats.ContainsKey(key.Hash);
        public float GetFloat(GameTag key) => _floats != null && _floats.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetFloat(GameTag key, float value) => (_floats ??= new())[key.Hash] = value;

        // int
        public bool HasInt(GameTag key) => _ints != null && _ints.ContainsKey(key.Hash);
        public int GetInt(GameTag key) => _ints != null && _ints.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetInt(GameTag key, int value) => (_ints ??= new())[key.Hash] = value;

        // bool
        public bool HasBool(GameTag key) => _bools != null && _bools.ContainsKey(key.Hash);
        public bool GetBool(GameTag key) => _bools != null && _bools.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetBool(GameTag key, bool value) => (_bools ??= new())[key.Hash] = value;

        // string
        public bool HasString(GameTag key) => _strings != null && _strings.ContainsKey(key.Hash);
        public string GetString(GameTag key) => _strings != null && _strings.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetString(GameTag key, string value) => (_strings ??= new())[key.Hash] = value;

        // Vector2
        public bool HasVector2(GameTag key) => _vector2s != null && _vector2s.ContainsKey(key.Hash);
        public Vector2 GetVector2(GameTag key) => _vector2s != null && _vector2s.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector2(GameTag key, Vector2 value) => (_vector2s ??= new())[key.Hash] = value;

        // Vector3
        public bool HasVector3(GameTag key) => _vector3s != null && _vector3s.ContainsKey(key.Hash);
        public Vector3 GetVector3(GameTag key) => _vector3s != null && _vector3s.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector3(GameTag key, Vector3 value) => (_vector3s ??= new())[key.Hash] = value;

        // Vector4
        public bool HasVector4(GameTag key) => _vector4s != null && _vector4s.ContainsKey(key.Hash);
        public Vector4 GetVector4(GameTag key) => _vector4s != null && _vector4s.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector4(GameTag key, Vector4 value) => (_vector4s ??= new())[key.Hash] = value;

        // Vector2Int
        public bool HasVector2Int(GameTag key) => _vector2Ints != null && _vector2Ints.ContainsKey(key.Hash);
        public Vector2Int GetVector2Int(GameTag key) => _vector2Ints != null && _vector2Ints.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector2Int(GameTag key, Vector2Int value) => (_vector2Ints ??= new())[key.Hash] = value;

        // Vector3Int
        public bool HasVector3Int(GameTag key) => _vector3Ints != null && _vector3Ints.ContainsKey(key.Hash);
        public Vector3Int GetVector3Int(GameTag key) => _vector3Ints != null && _vector3Ints.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetVector3Int(GameTag key, Vector3Int value) => (_vector3Ints ??= new())[key.Hash] = value;

        // Quaternion
        public bool HasQuaternion(GameTag key) => _quaternions != null && _quaternions.ContainsKey(key.Hash);
        public Quaternion GetQuaternion(GameTag key) => _quaternions != null && _quaternions.TryGetValue(key.Hash, out var v) ? v : Quaternion.identity;
        public void SetQuaternion(GameTag key, Quaternion value) => (_quaternions ??= new())[key.Hash] = value;

        // Color
        public bool HasColor(GameTag key) => _colors != null && _colors.ContainsKey(key.Hash);
        public Color GetColor(GameTag key) => _colors != null && _colors.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetColor(GameTag key, Color value) => (_colors ??= new())[key.Hash] = value;

        // Rect
        public bool HasRect(GameTag key) => _rects != null && _rects.ContainsKey(key.Hash);
        public Rect GetRect(GameTag key) => _rects != null && _rects.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetRect(GameTag key, Rect value) => (_rects ??= new())[key.Hash] = value;

        // Bounds
        public bool HasBounds(GameTag key) => _bounds != null && _bounds.ContainsKey(key.Hash);
        public Bounds GetBounds(GameTag key) => _bounds != null && _bounds.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetBounds(GameTag key, Bounds value) => (_bounds ??= new())[key.Hash] = value;

        // GameTagContainer
        public bool HasGameTagContainer(GameTag key) => _gameTagContainers != null && _gameTagContainers.ContainsKey(key.Hash);
        public GameTagContainer GetGameTagContainer(GameTag key) => _gameTagContainers != null && _gameTagContainers.TryGetValue(key.Hash, out var v) ? v : default;
        public void SetGameTagContainer(GameTag key, GameTagContainer gameTagContainer) => (_gameTagContainers ??= new())[key.Hash] = gameTagContainer;

        // Typed getters fall back to the generic object store so values stored via SetObject still resolve.

        // GameObject
        public bool HasGameObject(GameTag key) => (_gameObjects != null && _gameObjects.ContainsKey(key.Hash)) || GetObject<GameObject>(key) != null;
        public GameObject GetGameObject(GameTag key) => _gameObjects != null && _gameObjects.TryGetValue(key.Hash, out var v) ? v : GetObject<GameObject>(key);
        public void SetGameObject(GameTag key, GameObject value) => (_gameObjects ??= new())[key.Hash] = value;

        // Transform (also resolves keys stored as RectTransform)
        public bool HasTransform(GameTag key) => 
            (_transforms != null && _transforms.ContainsKey(key.Hash)) || HasRectTransform(key) || GetObject<Transform>(key) != null;
        public Transform GetTransform(GameTag key) => _transforms != null && _transforms.TryGetValue(key.Hash, out var v) 
            ? v 
            : _rectTransforms != null && _rectTransforms.TryGetValue(key.Hash, out var v2) 
                ? v2 
                : GetObject<Transform>(key);
        
        public void SetTransform(GameTag key, Transform value) => (_transforms ??= new())[key.Hash] = value;

        // RectTransform
        public bool HasRectTransform(GameTag key) => (_rectTransforms != null && _rectTransforms.ContainsKey(key.Hash)) || GetObject<RectTransform>(key) != null;
        public RectTransform GetRectTransform(GameTag key) => _rectTransforms != null && _rectTransforms.TryGetValue(key.Hash, out var v) ? v : GetObject<RectTransform>(key);
        public void SetRectTransform(GameTag key, RectTransform value) => (_rectTransforms ??= new())[key.Hash] = value;

        // TMP_Text
        public bool HasTMPText(GameTag key) => (_tmpTexts != null && _tmpTexts.ContainsKey(key.Hash)) || GetObject<TMP_Text>(key) != null;
        public TMP_Text GetTMPText(GameTag key) => _tmpTexts != null && _tmpTexts.TryGetValue(key.Hash, out var v) ? v : GetObject<TMP_Text>(key);
        public void SetTMPText(GameTag key, TMP_Text value) => (_tmpTexts ??= new())[key.Hash] = value;

        // Image
        public bool HasImage(GameTag key) => (_images != null && _images.ContainsKey(key.Hash)) || GetObject<Image>(key) != null;
        public Image GetImage(GameTag key) => _images != null && _images.TryGetValue(key.Hash, out var v) ? v : GetObject<Image>(key);
        public void SetImage(GameTag key, Image value) => (_images ??= new())[key.Hash] = value;

        // Sprite
        public bool HasSprite(GameTag key) => (_sprites != null && _sprites.ContainsKey(key.Hash)) || GetObject<Sprite>(key) != null;
        public Sprite GetSprite(GameTag key) => _sprites != null && _sprites.TryGetValue(key.Hash, out var v) ? v : GetObject<Sprite>(key);
        public void SetSprite(GameTag key, Sprite value) => (_sprites ??= new())[key.Hash] = value;

        // UnityEngine.Object (generic store for any other reference types)
        public bool HasObject(GameTag key) => _objects != null && _objects.ContainsKey(key.Hash);
        public T GetObject<T>(GameTag key) where T : Object => _objects != null && _objects.TryGetValue(key.Hash, out var v) ? v as T : null;
        public void SetObject(GameTag key, Object value) => (_objects ??= new())[key.Hash] = value;


        public void Clear()
        {
            _floats?.Clear();
            _ints?.Clear();
            _bools?.Clear();
            _strings?.Clear();
            _vector2s?.Clear();
            _vector3s?.Clear();
            _vector4s?.Clear();
            _vector2Ints?.Clear();
            _vector3Ints?.Clear();
            _quaternions?.Clear();
            _colors?.Clear();
            _rects?.Clear();
            _bounds?.Clear();
            _gameTagContainers?.Clear();
            _gameObjects?.Clear();
            _transforms?.Clear();
            _rectTransforms?.Clear();
            _tmpTexts?.Clear();
            _images?.Clear();
            _sprites?.Clear();
            _objects?.Clear();
            ClearExtra();
        }

        // Implement in a project-side partial file to clear custom stores.
        partial void ClearExtra();
    }
}
