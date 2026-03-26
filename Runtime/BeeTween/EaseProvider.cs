using System;
using DataKeeper.MathFunc;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface EaseProvider
    {
        float Evaluate(IBeeTweenContext context, float t);
    }

    [Serializable]
    public class EaseCurveProvider : EaseProvider
    {
        [field: SerializeField] public AnimationCurve Curve { get; private set; }

        public EaseCurveProvider()
        {
            Curve = AnimationCurve.Linear(0, 0, 1, 1);
        }
        
        public EaseCurveProvider(AnimationCurve curve)
        {
            Curve = curve;
        }

        public float Evaluate(IBeeTweenContext context, float t)
        {
            return Curve.Evaluate(t);
        }
    }
    
    [Serializable]
    public class EaseValueProvider : EaseProvider
    {
        [field: SerializeField] public EaseType EaseType { get; private set; }

        public EaseValueProvider()
        {
            EaseType = EaseType.Linear;
        }
        
        public EaseValueProvider(EaseType easeType)
        {
            EaseType = easeType;
        }

        public float Evaluate(IBeeTweenContext context, float t)
        {
            var v = MathFunc.Easing.Apply(t, EaseType);
            
            Debug.Log($"EaseValueProvider: {EaseType} and t: {t} -> {v}");
            return MathFunc.Easing.Apply(t, EaseType);
        }
    }
}