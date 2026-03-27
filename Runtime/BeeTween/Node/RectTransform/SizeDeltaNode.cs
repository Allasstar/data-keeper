using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Size delta tween node - tweens a RectTransform's sizeDelta
    /// </summary>
    [Serializable]
    public class SizeDeltaNode : IBeeTweenNode
    {
        public Vector2 TargetSize;
        public float Duration;
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public SizeDeltaNode()
        {
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            RectTransform rectTransform = null;

            if (context is IBeeTweenContext<RectTransform> rtContext)
                rectTransform = rtContext.Target;
            else if (context is IBeeTweenContext<GameObject> goContext && goContext.Target != null)
                rectTransform = goContext.Target.GetComponent<RectTransform>();

            if (rectTransform == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startSize = rectTransform.sizeDelta;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = easeProvider.Evaluate(context, t);
                rectTransform.sizeDelta = MathFunc.Lerp.LerpVector2Unclamped(startSize, TargetSize, easeT);
            }

            rectTransform.sizeDelta = TargetSize;
        }
    }
}