using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface IBoolProvider
    {
        bool GetValue();
    }

    [Serializable]
    public class BoolValueProvider : IBoolProvider
    {
        [field: SerializeField] public bool Value { get; set; }

        public bool GetValue() => Value;
    }

    [Serializable]
    public class BoolBlackboardProvider : IBoolProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private MonoBehaviour _blackboardSource;

        public bool GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetBool(_key) ?? default;
    }

    [Serializable]
    public class TargetNotNullProvider : IBoolProvider
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider Target { get; set; }

        public bool GetValue() => Target?.GetValue() != null;
    }
}
