using System;
using DataKeeper.Attributes;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace DataKeeper.ValueProviders
{
    // Computed providers: derive a value at call time. Dependency-free (UnityEngine +
    // SerializeReferenceSelector only); the transform-derived ones compose ITransformProvider.

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class DeltaTimeProvider : IFloatProvider
    {
        public float GetValue() => Time.deltaTime;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class FixedDeltaTimeProvider : IFloatProvider
    {
        public float GetValue() => Time.fixedDeltaTime;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class TimeProvider : IFloatProvider
    {
        public float GetValue() => Time.time;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class TimeScaleProvider : IFloatProvider
    {
        public float GetValue() => Time.timeScale;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class SinTimeProvider : IFloatProvider
    {
        public float GetValue() => Mathf.Sin(Time.time);
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class CosTimeProvider : IFloatProvider
    {
        public float GetValue() => Mathf.Cos(Time.time);
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class FrameCountProvider : IIntProvider
    {
        public int GetValue() => Time.frameCount;
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
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

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class TargetVector3Provider : IVector3Provider
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider Target { get; set; }
        [field: SerializeField] public Vector3Type Vector3Type { get; set; }
        [field: SerializeField] public SpaceType SpaceType { get; set; }
        [field: SerializeField] public Vector3 Offset { get; set; }

        public Vector3 GetValue()
        {
            var t = Target?.GetValue();
            if (t == null) return Vector3.zero;
            return Vector3Type switch
            {
                Vector3Type.Position      => SpaceType == SpaceType.Global ? t.position      + Offset : t.localPosition      + Offset,
                Vector3Type.EulerRotation => SpaceType == SpaceType.Global ? t.eulerAngles   + Offset : t.localEulerAngles   + Offset,
                Vector3Type.Scale         => SpaceType == SpaceType.Global ? t.lossyScale    + Offset : t.localScale         + Offset,
                _                         => Vector3.zero
            };
        }
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class DirectionProvider : IVector3Provider
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider From { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider To   { get; set; }
        [field: SerializeField] public bool IsNormalized { get; set; }

        public Vector3 GetValue()
        {
            var from = From?.GetValue();
            var to   = To?.GetValue();
            if (from == null || to == null) return Vector3.zero;
            var dir = to.position - from.position;
            return IsNormalized ? dir.normalized : dir;
        }
    }

    [Serializable]
    [MovedFrom(true, "DataKeeper.BeeTween")]
    public class TargetNotNullProvider : IBoolProvider
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider Target { get; set; }

        public bool GetValue() => Target?.GetValue() != null;
    }
}
