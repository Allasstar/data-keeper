using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class RotateNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider TargetProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider DurationProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }
        public Quaternion TargetRotation;

        public RotateNode()
        {
            TargetProvider   = new TransformDirectProvider();
            DurationProvider = new FloatConstantProvider();
            Ease             = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var target = TargetProvider?.GetValue();
            if (target == null) return;

            var easeProvider  = Ease ?? new EaseFuncProvider();
            var startRotation = target.rotation;
            var duration      = DurationProvider.GetValue();
            var elapsedTime   = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                target.rotation = MathFunc.Lerp.LerpQuaternionUnclamped(startRotation, TargetRotation, easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / duration)));
            }

            target.rotation = TargetRotation;
        }
    }
}
