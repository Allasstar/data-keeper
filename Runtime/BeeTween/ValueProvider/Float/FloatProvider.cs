using System;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface FloatProvider
    {
        float GetValue(IBeeTweenContext context);
    }
    
    [Serializable]
    public class FloatValueProvider : FloatProvider
    {
        [field: SerializeField] public float Value { get; set; }
        
        public float GetValue(IBeeTweenContext context)
        {
            return Value;
        }
    }
    
    [Serializable]
    public class DeltaTimeProvider : FloatProvider
    {
        public float GetValue(IBeeTweenContext context)
        {
            return Time.deltaTime;
        }
    }
    
    [Serializable]
    public class FixedDeltaTimeProvider : FloatProvider
    {
        public float GetValue(IBeeTweenContext context)
        {
            return Time.fixedDeltaTime;
        }
    }
    
    [Serializable]
    public class TimeProvider : FloatProvider
    {
        public float GetValue(IBeeTweenContext context)
        {
            return Time.time;
        }
    }
    
    [Serializable]
    public class TimeScaleProvider : FloatProvider
    {
        public float GetValue(IBeeTweenContext context)
        {
            return Time.timeScale;
        }
    }
    
    [Serializable]
    public class SinTimeProvider : FloatProvider
    {
        public float GetValue(IBeeTweenContext context)
        {
            return Mathf.Sin(Time.time);
        }
    }
    
    [Serializable]
    public class CosTimeProvider : FloatProvider
    {
        public float GetValue(IBeeTweenContext context)
        {
            return Mathf.Cos(Time.time);
        }
    }
    
    [Serializable]
    public class DistanceProvider : FloatProvider
    {
        [field: SerializeField] public Optional<Transform> OverrideTarget { get; set; } = new Optional<Transform>(null, false);
        [field: SerializeField] public Transform OtherTarget { get; set; }
        
        public float GetValue(IBeeTweenContext context)
        {
            if(OtherTarget == null) return 0;
            
            if(OverrideTarget.Enabled && OverrideTarget.Value != null)
            {
                return Vector3.Distance(OverrideTarget.Value.position, OtherTarget.position);
            }
            
            if (context.Target is Transform transform)
            {
                return Vector3.Distance(transform.position, OtherTarget.position);
            }
         
            return 0;
        }
    }
}