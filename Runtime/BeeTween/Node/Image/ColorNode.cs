using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class ColorNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public IImageProvider TargetProvider { get; set; }
        public Color TargetColor;
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider Duration { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }

        public ColorNode()
        {
            TargetProvider = new ImageValueProvider();
            Duration       = new FloatValueProvider();
            Ease           = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var image = TargetProvider?.GetValue();
            if (image == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startColor   = image.color;
            var duration     = Duration.GetValue();
            var elapsedTime  = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                var easeT = easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / duration));
                image.color = new Color(
                    MathFunc.Lerp.FloatUnclamped(startColor.r, TargetColor.r, easeT),
                    MathFunc.Lerp.FloatUnclamped(startColor.g, TargetColor.g, easeT),
                    MathFunc.Lerp.FloatUnclamped(startColor.b, TargetColor.b, easeT),
                    MathFunc.Lerp.FloatUnclamped(startColor.a, TargetColor.a, easeT)
                );
            }

            image.color = TargetColor;
        }
    }
}
