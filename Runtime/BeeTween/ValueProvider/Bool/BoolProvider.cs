using System;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface BoolProvider
    {
        bool GetValue(IBeeTweenContext context);
    }
    
    [Serializable]
    public class BoolValueProvider : BoolProvider
    {
        [field: SerializeField] public bool Value { get; private set; }
        
        public bool GetValue(IBeeTweenContext context)
        {
            return Value;
        }
    }
    
    [Serializable]
    public class TargetNotNullProvider : BoolProvider
    {
        public bool GetValue(IBeeTweenContext context)
        {
            return context.Target != null;
        }
    }
}