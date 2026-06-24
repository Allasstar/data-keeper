using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace DataKeeper.ValueProviders
{
    // Literal / direct providers: hold a serialized value (or object reference) and return it.
    // [MovedFrom] preserves existing SerializeReference data after the namespace + rename move
    // (former DataKeeper.BeeTween.*ValueProvider).

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween", null, "BoolValueProvider")]
    public class BoolConstantProvider : IBoolProvider
    {
        [field: SerializeField] public bool Value { get; set; }

        public bool GetValue() => Value;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween", null, "FloatValueProvider")]
    public class FloatConstantProvider : IFloatProvider
    {
        [field: SerializeField] public float Value { get; set; }

        public float GetValue() => Value;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween", null, "IntValueProvider")]
    public class IntConstantProvider : IIntProvider
    {
        [field: SerializeField] public int Value { get; set; }

        public int GetValue() => Value;
    }

    [Serializable]
    public class StringConstantProvider : IStringProvider
    {
        [field: SerializeField] public string Value { get; set; }

        public string GetValue() => Value;
    }

    [Serializable]
    public class Vector2ConstantProvider : IVector2Provider
    {
        [field: SerializeField] public Vector2 Value { get; set; }

        public Vector2 GetValue() => Value;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween", null, "Vector3ValueProvider")]
    public class Vector3ConstantProvider : IVector3Provider
    {
        [field: SerializeField] public Vector3 Value { get; set; }

        public Vector3 GetValue() => Value;
    }

    [Serializable]
    public class ColorConstantProvider : IColorProvider
    {
        [field: SerializeField] public Color Value { get; set; } = Color.white;

        public Color GetValue() => Value;
    }

    [Serializable]
    public class QuaternionConstantProvider : IQuaternionProvider
    {
        [field: SerializeField] public Quaternion Value { get; set; } = Quaternion.identity;

        public Quaternion GetValue() => Value;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween", null, "TransformValueProvider")]
    public class TransformDirectProvider : ITransformProvider
    {
        [SerializeField] public Transform target;

        public Transform GetValue() => target;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween", null, "RectTransformValueProvider")]
    public class RectTransformDirectProvider : IRectTransformProvider
    {
        [SerializeField] public RectTransform target;

        public RectTransform GetValue() => target;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween", null, "ImageValueProvider")]
    public class ImageDirectProvider : IImageProvider
    {
        [SerializeField] public Image target;

        public Image GetValue() => target;
    }
}
