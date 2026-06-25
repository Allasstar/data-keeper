using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DataKeeper.ValueProviders
{
    // Integration providers: pull values from a blackboard. These depend on Blackboard/GameTag,
    // so they live outside the dependency-free core. [MovedFrom] preserves SerializeReference
    // data (namespace-only move; class names unchanged).

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class BoolBlackboardProvider : IBoolProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public bool GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetBool(_key) ?? default;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class FloatBlackboardProvider : IFloatProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public float GetValue()
        {
            var bb = _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard;
            return bb.GetFloat(_key);
        }
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class IntBlackboardProvider : IIntProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public int GetValue()
        {
            return _blackboardSource.Cast<IBlackboardOwner>().Blackboard.GetInt(_key);
        }
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class Vector3BlackboardProvider : IVector3Provider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public Vector3 GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetVector3(_key) ?? default;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class TransformBlackboardProvider : ITransformProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public TransformBlackboardProvider() { }
        public TransformBlackboardProvider(GameTag key) => _key = key;

        public Transform GetValue()
        {
            return _blackboardSource.Cast<IBlackboardOwner>().Blackboard.GetObject<Transform>(_key);
        }
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class RectTransformBlackboardProvider : IRectTransformProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public RectTransformBlackboardProvider() { }
        public RectTransformBlackboardProvider(GameTag key) => _key = key;

        public RectTransform GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetObject<RectTransform>(_key);
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class ImageBlackboardProvider : IImageProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public ImageBlackboardProvider() { }
        public ImageBlackboardProvider(GameTag key) => _key = key;

        public Image GetValue()
        {
            return _blackboardSource.Cast<IBlackboardOwner>().Blackboard.GetObject<Image>(_key);
        }
    }

    [Serializable]
    public class StringBlackboardProvider : IStringProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public string GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetString(_key);
    }

    [Serializable]
    public class Vector2BlackboardProvider : IVector2Provider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public Vector2 GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetVector2(_key) ?? default;
    }

    [Serializable]
    public class ColorBlackboardProvider : IColorProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private Object _blackboardSource;

        public Color GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetColor(_key) ?? default;
    }
}
