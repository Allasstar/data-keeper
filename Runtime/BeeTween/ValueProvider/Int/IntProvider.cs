using System;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface IntProvider
    {
        int GetValue(IBeeTweenContext context);
    }
    
    [Serializable]
    public class IntValueProvider : IntProvider
    {
        [field: SerializeField] public int Value { get; set; }
        
        public int GetValue(IBeeTweenContext context)
        {
            return Value;
        }
    }
    
    [Serializable]
    public class FrameCountProvider : IntProvider
    {
        public int GetValue(IBeeTweenContext context)
        {
            return Time.frameCount;
        }
    }
}