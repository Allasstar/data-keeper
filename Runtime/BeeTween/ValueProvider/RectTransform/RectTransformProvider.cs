using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface IRectTransformProvider
    {
        RectTransform GetValue();
    }

    [Serializable]
    public class RectTransformValueProvider : IRectTransformProvider
    {
        [SerializeField] public RectTransform target;

        public RectTransform GetValue() => target;
    }

    [Serializable]
    public class RectTransformBlackboardProvider : IRectTransformProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private MonoBehaviour _blackboardSource;

        public RectTransformBlackboardProvider() { }
        public RectTransformBlackboardProvider(GameTag key) => _key = key;

        public RectTransform GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetObject<RectTransform>(_key);
    }
}
