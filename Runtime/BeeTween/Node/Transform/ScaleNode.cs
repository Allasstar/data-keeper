using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Scale node - scales a GameObject
    /// </summary>
    [Serializable]
    public class ScaleNode : IBeeTweenNode
    {
        public Vector3 TargetScale;
        public float Duration;
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public ScaleNode()
        {
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<GameObject> goContext || goContext.Target == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startScale = goContext.Target.transform.localScale;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = easeProvider.Evaluate(context, t);
                goContext.Target.transform.localScale = MathFunc.Lerp.LerpVector3Unclamped(startScale, TargetScale, easeT);
            }

            goContext.Target.transform.localScale = TargetScale;
        }
    }
}