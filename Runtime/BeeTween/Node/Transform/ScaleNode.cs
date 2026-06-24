using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class ScaleNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider TargetProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IVector3Provider TargetScaleProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider DurationProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }

        public ScaleNode()
        {
            TargetProvider      = new TransformDirectProvider();
            TargetScaleProvider = new Vector3ConstantProvider();
            DurationProvider    = new FloatConstantProvider();
            Ease                = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var target = TargetProvider?.GetValue();
            if (target == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startScale   = target.localScale;
            var targetScale  = TargetScaleProvider.GetValue();
            var duration     = DurationProvider.GetValue();
            var elapsedTime  = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                target.localScale = MathFunc.Lerp.LerpVector3Unclamped(startScale, targetScale, easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / duration)));
            }

            target.localScale = targetScale;
        }
    }
}
