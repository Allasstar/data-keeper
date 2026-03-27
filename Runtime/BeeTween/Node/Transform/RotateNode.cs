using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Rotate node - rotates a GameObject
    /// </summary>
    [Serializable]
    public class RotateNode : IBeeTweenNode
    {
        public Quaternion TargetRotation;
        public float Duration;
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public RotateNode()
        {
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<Transform> goContext || goContext.Target == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startRotation = goContext.Target.transform.rotation;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = easeProvider.Evaluate(context, t);
                goContext.Target.transform.rotation = MathFunc.Lerp.LerpQuaternionUnclamped(startRotation, TargetRotation, easeT);
            }

            goContext.Target.transform.rotation = TargetRotation;
        }
    }
}