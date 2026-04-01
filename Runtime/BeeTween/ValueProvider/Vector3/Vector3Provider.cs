using System;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public enum Vector3Type
    {
        Position = 0,
        EulerRotation = 1,
        Scale = 2,
    }
    
    public enum SpaceType
    {
        Global = 0,
        Local = 1,
    }
    
    public enum LookAtType
    {
        ToTarget = 0,
        ToOther = 1,
    }
    
    public interface Vector3Provider
    {
        Vector3 GetValue(IBeeTweenContext context);
    }
    
    [Serializable]
    public class Vector3ValueProvider : Vector3Provider
    {
        [field: SerializeField] public Vector3 Value { get; private set; }
        
        public Vector3 GetValue(IBeeTweenContext context)
        {
            return Value;
        }
    }
    
    [Serializable]
    public class TargetVector3Provider : Vector3Provider
    {
        [field: SerializeField] public Optional<Transform> OverrideTarget { get; private set; } = new Optional<Transform>(null, false);
        [field: SerializeField] public Vector3Type Vector3Type { get; private set; }
        [field: SerializeField] public SpaceType SpaceType { get; private set; }
        [field: SerializeField] public Vector3 Offset { get; private set; }
        
        public Vector3 GetValue(IBeeTweenContext context)
        {
            return Vector3Type switch
            {
                Vector3Type.Position => GetPosition(context),
                Vector3Type.EulerRotation => GetEulerRotation(context),
                Vector3Type.Scale => GetScale(context),
                _ => Vector3.zero
            };
        }

        private Vector3 GetPosition(IBeeTweenContext context)
        {
            if (OverrideTarget.Enabled && OverrideTarget.Value != null)
            {
                return SpaceType == SpaceType.Global 
                    ? OverrideTarget.Value.position 
                    : OverrideTarget.Value.localPosition
                    + Offset;
            } 
            
            if (context.Target is Transform transform)
            {
                return SpaceType == SpaceType.Global 
                    ? transform.position 
                    : transform.localPosition
                      + Offset;
            }
            
            return Vector3.zero;
        }

        private Vector3 GetEulerRotation(IBeeTweenContext context)
        {
            if (OverrideTarget.Enabled && OverrideTarget.Value != null)
            {
                return SpaceType == SpaceType.Global 
                    ? OverrideTarget.Value.eulerAngles 
                    : OverrideTarget.Value.localEulerAngles
                    + Offset;
            } 
            
            if (context.Target is Transform transform)
            {
                return SpaceType == SpaceType.Global 
                    ? transform.eulerAngles 
                    : transform.localEulerAngles
                      + Offset;
            }
            
            return Vector3.zero;
        }

        private Vector3 GetScale(IBeeTweenContext context)
        {
            if (OverrideTarget.Enabled && OverrideTarget.Value != null)
            {
                return SpaceType == SpaceType.Global 
                    ? OverrideTarget.Value.lossyScale 
                    : OverrideTarget.Value.localScale
                    + Offset;
            } 
            
            if (context.Target is Transform transform)
            {
                return SpaceType == SpaceType.Global 
                    ? transform.lossyScale 
                    : transform.localScale
                      + Offset;
            }
            
            return Vector3.zero;
        }
    }
    
    [Serializable]
    public class DirectionProvider : Vector3Provider
    {
        [field: SerializeField] public Optional<Transform> OverrideTarget { get; private set; } = new Optional<Transform>(null, false);
        [field: SerializeField] public Transform OtherTransform { get; private set; }
        [field: SerializeField] public LookAtType LookAtType { get; private set; }
        [field: SerializeField] public bool IsNormalized { get; private set; }
        
        public Vector3 GetValue(IBeeTweenContext context)
        {
            if(OtherTransform == null) return Vector3.zero;
            
            if(OverrideTarget.Enabled && OverrideTarget.Value != null)
            {
                return LookAtType switch
                {
                    LookAtType.ToTarget => IsNormalized 
                        ? (OverrideTarget.Value.position - OtherTransform.position).normalized 
                        : OverrideTarget.Value.position - OtherTransform.position,
                    LookAtType.ToOther => IsNormalized 
                        ? ( OtherTransform.position - OverrideTarget.Value.position).normalized 
                        :  OtherTransform.position - OverrideTarget.Value.position,
                    _ => Vector3.zero
                };
            }
            
            if (context.Target is Transform transform)
            {
                return LookAtType switch
                {
                    LookAtType.ToTarget => IsNormalized 
                        ? (transform.position - OtherTransform.position).normalized 
                        : transform.position - OtherTransform.position,
                    LookAtType.ToOther => IsNormalized 
                        ? (OtherTransform.position - transform.position).normalized 
                        : OtherTransform.position - transform.position,
                    _ => Vector3.zero
                };
            }
         
            return Vector3.zero;
        }
    }
}