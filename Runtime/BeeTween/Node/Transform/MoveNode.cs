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
        [field: SerializeReference, SerializeReferenceSelector]
        public Vector3Provider EndPositionProvider { get; set; }
        
        [field: SerializeReference, SerializeReferenceSelector]
        public FloatProvider DurationProvider { get; set; }
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public MoveNode()
        {
            EndPositionProvider = new Vector3ValueProvider();
            DurationProvider = new FloatValueProvider();
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<Transform> trContext || trContext.Target == null) return;
            
            var easeProvider = Ease ?? new EaseFuncProvider();
            var startPosition = trContext.Target.position;
            var elapsedTime = 0f;

            while (elapsedTime < DurationProvider.GetValue(context))
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / DurationProvider.GetValue(context));
                var easeT = easeProvider.Evaluate(context, t);
                trContext.Target.position = MathFunc.Lerp.LerpVector3Unclamped(startPosition, EndPositionProvider.GetValue(context), easeT);
            }

            trContext.Target.position = EndPositionProvider.GetValue(context);
        }
    }
    
}