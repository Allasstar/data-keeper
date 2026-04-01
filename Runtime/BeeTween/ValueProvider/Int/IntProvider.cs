using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface IntProvider
    {
        int GetValue(IBeeTweenContext context);
    }
    
    public class IntValueProvider : IntProvider
    {
        [field: SerializeField] public int Value { get; private set; }
        
        public int GetValue(IBeeTweenContext context)
        {
            return Value;
        }
    }
    
    public class FrameCountProvider : IntProvider
    {
        public int GetValue(IBeeTweenContext context)
        {
            return Time.frameCount;
        }
    }
}