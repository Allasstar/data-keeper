using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface ITransformProvider
    {
        Transform GetValue();
    }

    [Serializable]
    public class TransformValueProvider : ITransformProvider
    {
        [SerializeField] public Transform target;

        public Transform GetValue() => target;
    }

    [Serializable]
    public class TransformBlackboardProvider : ITransformProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private MonoBehaviour _blackboardSource;

        public TransformBlackboardProvider() { }
        public TransformBlackboardProvider(GameTag key) => _key = key;

        public Transform GetValue()
        {
            return _blackboardSource.Cast<IBlackboardOwner>().Blackboard.GetObject<Transform>(_key);
        }
    }
}
