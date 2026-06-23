using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class SizeDeltaNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public IRectTransformProvider TargetProvider { get; set; }
        public Vector2 TargetSize;
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider Duration { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }

        public SizeDeltaNode()
        {
            TargetProvider = new RectTransformValueProvider();
            Duration       = new FloatValueProvider();
            Ease           = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var rectTransform = TargetProvider?.GetValue();
            if (rectTransform == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startSize    = rectTransform.sizeDelta;
            var duration     = Duration.GetValue();
            var elapsedTime  = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                rectTransform.sizeDelta = MathFunc.Lerp.LerpVector2Unclamped(startSize, TargetSize, easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / duration)));
            }

            rectTransform.sizeDelta = TargetSize;
        }
    }
}
