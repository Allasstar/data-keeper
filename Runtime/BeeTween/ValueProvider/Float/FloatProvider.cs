using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface IFloatProvider
    {
        float GetValue();
    }

    [Serializable]
    public class FloatValueProvider : IFloatProvider
    {
        [field: SerializeField] public float Value { get; set; }

        public float GetValue() => Value;
    }

    [Serializable]
    public class FloatBlackboardProvider : IFloatProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private MonoBehaviour _blackboardSource;

        public float GetValue()
        {
            var bb = _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard;
            return bb.GetFloat(_key);
        }
    }

    [Serializable]
    public class DeltaTimeProvider : IFloatProvider
    {
        public float GetValue() => Time.deltaTime;
    }

    [Serializable]
    public class FixedDeltaTimeProvider : IFloatProvider
    {
        public float GetValue() => Time.fixedDeltaTime;
    }

    [Serializable]
    public class TimeProvider : IFloatProvider
    {
        public float GetValue() => Time.time;
    }

    [Serializable]
    public class TimeScaleProvider : IFloatProvider
    {
        public float GetValue() => Time.timeScale;
    }

    [Serializable]
    public class SinTimeProvider : IFloatProvider
    {
        public float GetValue() => Mathf.Sin(Time.time);
    }

    [Serializable]
    public class CosTimeProvider : IFloatProvider
    {
        public float GetValue() => Mathf.Cos(Time.time);
    }

    [Serializable]
    public class DistanceProvider : IFloatProvider
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider TargetA { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider TargetB { get; set; }

        public float GetValue()
        {
            if (TargetA == null || TargetB == null) return 0;
            
            return Vector3.Distance(TargetA.GetValue().position, TargetB.GetValue().position);
        }
    }
}
