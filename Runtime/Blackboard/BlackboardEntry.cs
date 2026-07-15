using System;
using DataKeeper.Attributes;
using DataKeeper.GameTagSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BlackboardSystem
{
    public interface IBlackboardEntry
    {
        void Apply(Blackboard blackboard);
    }

    // ---- Value types ----

    [Serializable]
    public class FloatEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public float value;

        public void Apply(Blackboard bb) => bb.SetFloat(key, value);
    }

    [Serializable]
    public class IntEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public int value;

        public void Apply(Blackboard bb) => bb.SetInt(key, value);
    }

    [Serializable]
    public class BoolEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public bool value;

        public void Apply(Blackboard bb) => bb.SetBool(key, value);
    }

    [Serializable]
    public class StringEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public string value;

        public void Apply(Blackboard bb) => bb.SetString(key, value);
    }

    [Serializable]
    public class Vector2Entry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Vector2 value;

        public void Apply(Blackboard bb) => bb.SetVector2(key, value);
    }

    [Serializable]
    public class Vector3Entry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Vector3 value;

        public void Apply(Blackboard bb) => bb.SetVector3(key, value);
    }

    [Serializable]
    public class Vector4Entry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Vector4 value;

        public void Apply(Blackboard bb) => bb.SetVector4(key, value);
    }

    [Serializable]
    public class Vector2IntEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Vector2Int value;

        public void Apply(Blackboard bb) => bb.SetVector2Int(key, value);
    }

    [Serializable]
    public class Vector3IntEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Vector3Int value;

        public void Apply(Blackboard bb) => bb.SetVector3Int(key, value);
    }

    [Serializable]
    public class QuaternionEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Vector3 eulerAngles;

        public void Apply(Blackboard bb) => bb.SetQuaternion(key, Quaternion.Euler(eulerAngles));
    }

    [Serializable]
    public class ColorEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Color value = Color.white;

        public void Apply(Blackboard bb) => bb.SetColor(key, value);
    }

    [Serializable]
    public class RectEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Rect value;

        public void Apply(Blackboard bb) => bb.SetRect(key, value);
    }

    [Serializable]
    public class BoundsEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Bounds value;

        public void Apply(Blackboard bb) => bb.SetBounds(key, value);
    }

    // ---- Reference types ----
    // Typed fields so the inspector accepts/extracts the correct component
    // (e.g. a dropped GameObject yields its Transform), instead of always landing as a GameObject.

    [Serializable]
    public class ObjectEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [ObjectComponentPicker]
        [SerializeField] public UnityEngine.Object value;

        public void Apply(Blackboard bb) => bb.SetObject(key, value);
    }

    [Serializable]
    public class GameObjectEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public GameObject value;

        public void Apply(Blackboard bb) => bb.SetGameObject(key, value);
    }

    [Serializable]
    public class TransformEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Transform value;

        public void Apply(Blackboard bb) => bb.SetTransform(key, value);
    }

    [Serializable]
    public class RectTransformEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public RectTransform value;

        public void Apply(Blackboard bb) => bb.SetRectTransform(key, value);
    }

    [Serializable]
    public class ImageEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Image value;

        public void Apply(Blackboard bb) => bb.SetImage(key, value);
    }

    [Serializable]
    public class TextEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public TMP_Text value;

        public void Apply(Blackboard bb) => bb.SetTMPText(key, value);
    }

    [Serializable]
    public class SpriteEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public Sprite value;

        public void Apply(Blackboard bb) => bb.SetSprite(key, value);
    }
    
    [Serializable]
    public class GameTagContainerEntry : IBlackboardEntry
    {
        [SerializeField] public GameTag key;
        [SerializeField] public GameTagContainer value;

        public void Apply(Blackboard bb) => bb.SetGameTagContainer(key, value);
    }
}
