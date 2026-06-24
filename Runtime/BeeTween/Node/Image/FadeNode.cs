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
        public float TargetAlpha;
        public float Duration;
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }

        public FadeNode()
        {
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var image = TargetProvider?.GetValue();
            if (image == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startAlpha = image.color.a;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                var easeT = easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / Duration));
                var color = image.color;
                color.a = MathFunc.Lerp.FloatUnclamped(startAlpha, TargetAlpha, easeT);
                image.color = color;
            }

            var final = image.color;
            final.a = TargetAlpha;
            image.color = final;
        }
    }
}
