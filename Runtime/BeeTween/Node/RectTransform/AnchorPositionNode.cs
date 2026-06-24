using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class AnchorPositionNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public IRectTransformProvider TargetProvider { get; set; }
        public Vector2 TargetPosition;
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider Duration { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }

        public AnchorPositionNode()
        {
            TargetProvider = new RectTransformDirectProvider();
            Duration       = new FloatConstantProvider();
            Ease           = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var rectTransform = TargetProvider?.GetValue();
            if (rectTransform == null) return;

            var easeProvider  = Ease ?? new EaseFuncProvider();
            var startPosition = rectTransform.anchoredPosition;
            var duration      = Duration.GetValue();
            var elapsedTime   = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                rectTransform.anchoredPosition = MathFunc.Lerp.LerpVector2Unclamped(startPosition, TargetPosition, easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / duration)));
            }

            rectTransform.anchoredPosition = TargetPosition;
        }
    }
}
