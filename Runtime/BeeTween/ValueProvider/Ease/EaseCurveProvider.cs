using System;
using UnityEngine;

namespace DataKeeper.BeeTween
{
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
}