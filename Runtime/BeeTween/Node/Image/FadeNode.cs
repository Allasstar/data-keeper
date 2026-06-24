using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class FadeNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public IImageProvider TargetProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider TargetAlphaProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider DurationProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }

        public FadeNode()
        {
            TargetProvider      = new ImageDirectProvider();
            TargetAlphaProvider = new FloatConstantProvider();
            DurationProvider    = new FloatConstantProvider();
            Ease                = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var image = TargetProvider?.GetValue();
            if (image == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startAlpha   = image.color.a;
            var targetAlpha  = TargetAlphaProvider.GetValue();
            var duration     = DurationProvider.GetValue();
            var elapsedTime  = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                var easeT = easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / duration));
                var color = image.color;
                color.a = MathFunc.Lerp.FloatUnclamped(startAlpha, targetAlpha, easeT);
                image.color = color;
            }

            var final = image.color;
            final.a = targetAlpha;
            image.color = final;
        }
    }
}
