using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public enum Vector3Type { Position = 0, EulerRotation = 1, Scale = 2 }
    public enum SpaceType   { Global = 0, Local = 1 }
    public enum LookAtType  { ToTarget = 0, ToOther = 1 }

    public interface IVector3Provider
    {
        Vector3 GetValue();
    }

    [Serializable]
    public class Vector3ValueProvider : IVector3Provider
    {
        [field: SerializeField] public Vector3 Value { get; set; }

        public Vector3 GetValue() => Value;
    }

    [Serializable]
    public class Vector3BlackboardProvider : IVector3Provider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private MonoBehaviour _blackboardSource;

        public Vector3 GetValue() => _blackboardSource.Cast<IBlackboardOwner>()?.Blackboard.GetVector3(_key) ?? default;
    }

    [Serializable]
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
}
