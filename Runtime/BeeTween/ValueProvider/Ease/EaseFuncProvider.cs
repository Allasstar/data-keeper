using System;
using DataKeeper.MathFunc;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class EaseFuncProvider : EaseProvider
    {
        [field: SerializeField] public EaseType EaseType { get; private set; }

        public EaseFuncProvider()
        {
            EaseType = EaseType.Linear;
        }
        
        public EaseFuncProvider(EaseType easeType)
        {
            EaseType = easeType;
        }

        public float Evaluate(IBeeTweenContext context, float t)
        {
            return MathFunc.Easing.Apply(t, EaseType);
        }
    }
}