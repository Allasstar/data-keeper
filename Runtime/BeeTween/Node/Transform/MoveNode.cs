using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Move node - moves a GameObject from current position to target
    /// </summary>
    [Serializable]
    public class MoveNode : IBeeTweenNode
    {
        public Vector3 TargetPosition;
        public float Duration;
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public MoveNode()
        {
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<Transform> trContext || trContext.Target == null) return;
            
            var easeProvider = Ease ?? new EaseFuncProvider();
            var startPosition = trContext.Target.position;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = easeProvider.Evaluate(context, t);
                trContext.Target.position = MathFunc.Lerp.LerpVector3Unclamped(startPosition, TargetPosition, easeT);
            }

            trContext.Target.position = TargetPosition;
        }
    }
}