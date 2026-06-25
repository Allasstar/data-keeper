using System;
using DataKeeper.Attributes;
using DataKeeper.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DataKeeper.ValueProviders
{
    // Integration providers: read from a ScriptableObject asset that implements IValueProvider<T>.
    // The field is a UnityEngine.Object constrained by [RequireInterface] so the inspector only
    // accepts assets implementing the matching IValueProvider<T> (e.g. the built-in FloatProvider,
    // ColorProvider, TransformProvider, ... assets, or any custom one). This lets several nodes
    // share a single asset-backed value that can be authored / swapped in one place.

    [Serializable]
    public class BoolScriptableObjectProvider : IBoolProvider
    {
        [RequireInterface(typeof(IValueProvider<bool>))]
        [SerializeField] private Object _asset;

        public bool GetValue() => _asset.Cast<IValueProvider<bool>>()?.GetValue() ?? default;
    }

    [Serializable]
    public class FloatScriptableObjectProvider : IFloatProvider
    {
        [RequireInterface(typeof(IValueProvider<float>))]
        [SerializeField] private Object _asset;

        public float GetValue() => _asset.Cast<IValueProvider<float>>()?.GetValue() ?? default;
    }

    [Serializable]
    public class IntScriptableObjectProvider : IIntProvider
    {
        [RequireInterface(typeof(IValueProvider<int>))]
        [SerializeField] private Object _asset;

        public int GetValue() => _asset.Cast<IValueProvider<int>>()?.GetValue() ?? default;
    }

    [Serializable]
    public class StringScriptableObjectProvider : IStringProvider
    {
        [RequireInterface(typeof(IValueProvider<string>))]
        [SerializeField] private Object _asset;

        public string GetValue() => _asset.Cast<IValueProvider<string>>()?.GetValue();
    }

    [Serializable]
    public class Vector2ScriptableObjectProvider : IVector2Provider
    {
        [RequireInterface(typeof(IValueProvider<Vector2>))]
        [SerializeField] private Object _asset;

        public Vector2 GetValue() => _asset.Cast<IValueProvider<Vector2>>()?.GetValue() ?? default;
    }

    [Serializable]
    public class Vector3ScriptableObjectProvider : IVector3Provider
    {
        [RequireInterface(typeof(IValueProvider<Vector3>))]
        [SerializeField] private Object _asset;

        public Vector3 GetValue() => _asset.Cast<IValueProvider<Vector3>>()?.GetValue() ?? default;
    }

    [Serializable]
    public class ColorScriptableObjectProvider : IColorProvider
    {
        [RequireInterface(typeof(IValueProvider<Color>))]
        [SerializeField] private Object _asset;

        public Color GetValue() => _asset.Cast<IValueProvider<Color>>()?.GetValue() ?? default;
    }

    [Serializable]
    public class TransformScriptableObjectProvider : ITransformProvider
    {
        [RequireInterface(typeof(IValueProvider<Transform>))]
        [SerializeField] private Object _asset;

        public Transform GetValue() => _asset.Cast<IValueProvider<Transform>>()?.GetValue();
    }

    [Serializable]
    public class RectTransformScriptableObjectProvider : IRectTransformProvider
    {
        [RequireInterface(typeof(IValueProvider<RectTransform>))]
        [SerializeField] private Object _asset;

        public RectTransform GetValue() => _asset.Cast<IValueProvider<RectTransform>>()?.GetValue();
    }

    [Serializable]
    public class ImageScriptableObjectProvider : IImageProvider
    {
        [RequireInterface(typeof(IValueProvider<Image>))]
        [SerializeField] private Object _asset;

        public Image GetValue() => _asset.Cast<IValueProvider<Image>>()?.GetValue();
    }
}
