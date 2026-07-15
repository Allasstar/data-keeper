using System.Collections.Generic;
using DataKeeper.BlackboardSystem;
using DataKeeper.GameTagSystem;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DataKeeper.Tests.BlackboardSystem
{
    public class BlackboardTests
    {
        private GameTagRegistry _registry;
        private Blackboard _bb;
        private GameTag _a, _b;

        private GameObject _go;
        private GameObject _imageGo;
        private GameObject _textGo;
        private Texture2D _texture;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<GameTagRegistry>();
            _registry.GetOrCreate("BB/A");
            _registry.GetOrCreate("BB/B");
            GameTagRegistry.SetDefault(_registry);
            _a = GameTag.Find("BB/A");
            _b = GameTag.Find("BB/B");

            _bb = new Blackboard();

            _go = new GameObject("BlackboardTests");
            _imageGo = new GameObject("BlackboardTests_Image", typeof(Image));
            _textGo = new GameObject("BlackboardTests_Text", typeof(TextMeshProUGUI));
            _texture = new Texture2D(4, 4);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 4, 4), Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_imageGo);
            Object.DestroyImmediate(_textGo);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
            GameTagRegistry.SetDefault(null);
            Object.DestroyImmediate(_registry);
        }

        // --- Value types: Set/Get roundtrip ---
        [Test] public void Float_Roundtrip()      { _bb.SetFloat(_a, 1.5f); Assert.AreEqual(1.5f, _bb.GetFloat(_a)); }
        [Test] public void Int_Roundtrip()        { _bb.SetInt(_a, 42); Assert.AreEqual(42, _bb.GetInt(_a)); }
        [Test] public void Bool_Roundtrip()       { _bb.SetBool(_a, true); Assert.IsTrue(_bb.GetBool(_a)); }
        [Test] public void String_Roundtrip()     { _bb.SetString(_a, "hello"); Assert.AreEqual("hello", _bb.GetString(_a)); }
        [Test] public void Vector2_Roundtrip()    { _bb.SetVector2(_a, new Vector2(1, 2)); Assert.AreEqual(new Vector2(1, 2), _bb.GetVector2(_a)); }
        [Test] public void Vector3_Roundtrip()    { _bb.SetVector3(_a, new Vector3(1, 2, 3)); Assert.AreEqual(new Vector3(1, 2, 3), _bb.GetVector3(_a)); }
        [Test] public void Vector4_Roundtrip()    { _bb.SetVector4(_a, new Vector4(1, 2, 3, 4)); Assert.AreEqual(new Vector4(1, 2, 3, 4), _bb.GetVector4(_a)); }
        [Test] public void Vector2Int_Roundtrip() { _bb.SetVector2Int(_a, new Vector2Int(1, 2)); Assert.AreEqual(new Vector2Int(1, 2), _bb.GetVector2Int(_a)); }
        [Test] public void Vector3Int_Roundtrip() { _bb.SetVector3Int(_a, new Vector3Int(1, 2, 3)); Assert.AreEqual(new Vector3Int(1, 2, 3), _bb.GetVector3Int(_a)); }
        [Test] public void Quaternion_Roundtrip() { var q = Quaternion.Euler(10, 20, 30); _bb.SetQuaternion(_a, q); Assert.AreEqual(q, _bb.GetQuaternion(_a)); }
        [Test] public void Color_Roundtrip()      { _bb.SetColor(_a, Color.red); Assert.AreEqual(Color.red, _bb.GetColor(_a)); }
        [Test] public void Rect_Roundtrip()       { var r = new Rect(1, 2, 3, 4); _bb.SetRect(_a, r); Assert.AreEqual(r, _bb.GetRect(_a)); }
        [Test] public void Bounds_Roundtrip()     { var b = new Bounds(Vector3.one, Vector3.one * 2); _bb.SetBounds(_a, b); Assert.AreEqual(b, _bb.GetBounds(_a)); }

        [Test]
        public void GameTagContainer_Roundtrip()
        {
            var c = new GameTagContainer();
            c.AddTag(_a);
            _bb.SetGameTagContainer(_a, c);
            Assert.AreSame(c, _bb.GetGameTagContainer(_a));
        }

        // --- Defaults on empty blackboard ---
        [Test] public void Get_MissingFloat_ReturnsDefault()       => Assert.AreEqual(0f, _bb.GetFloat(_a));
        [Test] public void Get_MissingString_ReturnsNull()         => Assert.IsNull(_bb.GetString(_a));
        [Test] public void Get_MissingQuaternion_ReturnsIdentity() => Assert.AreEqual(Quaternion.identity, _bb.GetQuaternion(_a));
        [Test] public void Get_MissingGameObject_ReturnsNull()     => Assert.IsNull(_bb.GetGameObject(_a));
        [Test] public void Get_MissingObject_ReturnsNull()         => Assert.IsNull(_bb.GetObject<Transform>(_a));
        [Test] public void Has_MissingKey_ReturnsFalse()           => Assert.IsFalse(_bb.HasFloat(_a));

        // --- Has / overwrite / key isolation ---
        [Test]
        public void Has_AfterSet_TrueOnlyForThatKey()
        {
            _bb.SetInt(_a, 1);
            Assert.IsTrue(_bb.HasInt(_a));
            Assert.IsFalse(_bb.HasInt(_b));
        }

        [Test]
        public void Set_SameKeyTwice_Overwrites()
        {
            _bb.SetFloat(_a, 1f);
            _bb.SetFloat(_a, 2f);
            Assert.AreEqual(2f, _bb.GetFloat(_a));
        }

        [Test]
        public void SameKey_DifferentStores_DoNotCollide()
        {
            _bb.SetInt(_a, 7);
            _bb.SetFloat(_a, 3f);
            Assert.AreEqual(7, _bb.GetInt(_a));
            Assert.AreEqual(3f, _bb.GetFloat(_a));
        }

        // --- Reference types: typed stores ---
        [Test] public void GameObject_Roundtrip()    { _bb.SetGameObject(_a, _go); Assert.AreSame(_go, _bb.GetGameObject(_a)); }
        [Test] public void Transform_Roundtrip()     { _bb.SetTransform(_a, _go.transform); Assert.AreSame(_go.transform, _bb.GetTransform(_a)); }
        [Test] public void RectTransform_Roundtrip() { var rt = (RectTransform)_imageGo.transform; _bb.SetRectTransform(_a, rt); Assert.AreSame(rt, _bb.GetRectTransform(_a)); }
        [Test] public void TMPText_Roundtrip()       { var t = _textGo.GetComponent<TMP_Text>(); _bb.SetTMPText(_a, t); Assert.AreSame(t, _bb.GetTMPText(_a)); }
        [Test] public void Image_Roundtrip()         { var i = _imageGo.GetComponent<Image>(); _bb.SetImage(_a, i); Assert.AreSame(i, _bb.GetImage(_a)); }
        [Test] public void Sprite_Roundtrip()        { _bb.SetSprite(_a, _sprite); Assert.AreSame(_sprite, _bb.GetSprite(_a)); }

        [Test]
        public void GetTransform_ResolvesRectTransformStore()
        {
            var rt = (RectTransform)_imageGo.transform;
            _bb.SetRectTransform(_a, rt);
            Assert.AreSame(rt, _bb.GetTransform(_a));
            Assert.IsTrue(_bb.HasTransform(_a));
        }

        // --- Reference types: generic object store & fallback ---
        [Test]
        public void Object_Roundtrip()
        {
            _bb.SetObject(_a, _go);
            Assert.IsTrue(_bb.HasObject(_a));
            Assert.AreSame(_go, _bb.GetObject<GameObject>(_a));
        }

        [Test]
        public void GetObject_WrongType_ReturnsNull()
        {
            _bb.SetObject(_a, _go);
            Assert.IsNull(_bb.GetObject<Sprite>(_a));
        }

        [Test]
        public void TypedGetters_FallBackToObjectStore()
        {
            _bb.SetObject(_a, _go);
            _bb.SetObject(_b, _go.transform);
            Assert.AreSame(_go, _bb.GetGameObject(_a));
            Assert.AreSame(_go.transform, _bb.GetTransform(_b));
            Assert.IsTrue(_bb.HasGameObject(_a));
            Assert.IsTrue(_bb.HasTransform(_b));
        }

        [Test]
        public void TypedStore_WinsOverObjectStore()
        {
            var other = new GameObject("BlackboardTests_Other");
            try
            {
                _bb.SetObject(_a, other);
                _bb.SetGameObject(_a, _go);
                Assert.AreSame(_go, _bb.GetGameObject(_a));
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        // --- Lazy initialization ---
        [Test]
        public void Dictionaries_AreLazy_CreatedOnFirstSet()
        {
            var field = typeof(Blackboard).GetField("_floats",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNull(field.GetValue(_bb));
            _bb.GetFloat(_a);
            _bb.HasFloat(_a);
            Assert.IsNull(field.GetValue(_bb), "Get/Has must not allocate the dictionary");
            _bb.SetFloat(_a, 1f);
            Assert.IsNotNull(field.GetValue(_bb));
        }

        // --- Initialize ---
        [Test]
        public void Initialize_AppliesEntries_AndSkipsNulls()
        {
            _bb.Entries = new List<IBlackboardEntry>
            {
                new FloatEntry { key = _a, value = 5f },
                null,
                new GameObjectEntry { key = _b, value = _go },
            };

            _bb.Initialize();

            Assert.AreEqual(5f, _bb.GetFloat(_a));
            Assert.AreSame(_go, _bb.GetGameObject(_b));
        }

        // --- Clear ---
        [Test]
        public void Clear_RemovesAllValues()
        {
            _bb.SetFloat(_a, 1f);
            _bb.SetString(_a, "x");
            _bb.SetQuaternion(_a, Quaternion.Euler(1, 2, 3));
            _bb.SetGameObject(_a, _go);
            _bb.SetSprite(_a, _sprite);
            _bb.SetObject(_b, _go);

            _bb.Clear();

            Assert.IsFalse(_bb.HasFloat(_a));
            Assert.IsFalse(_bb.HasString(_a));
            Assert.IsFalse(_bb.HasGameObject(_a));
            Assert.IsFalse(_bb.HasSprite(_a));
            Assert.IsFalse(_bb.HasObject(_b));
            Assert.AreEqual(Quaternion.identity, _bb.GetQuaternion(_a));
        }

        [Test]
        public void Clear_OnEmptyBlackboard_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _bb.Clear());
        }
    }
}
