using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Anchor position tween node - tweens a RectTransform's anchoredPosition
    /// </summary>
    [Serializable]
    public class AnchorPositionNode : IBeeTweenNode
    {
        public Vector2 TargetPosition;
        public float Duration;
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public AnchorPositionNode()
        {
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            RectTransform rectTransform = null;

            if (context is IBeeTweenContext<RectTransform> rtContext)
                rectTransform = rtContext.Target;
            else if (context is IBeeTweenContext<GameObject> goContext && goContext.Target != null)
                rectTransform = goContext.Target.GetComponent<RectTransform>();

            if (rectTransform == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startPosition = rectTransform.anchoredPosition;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = easeProvider.Evaluate(context, t);
                rectTransform.anchoredPosition = MathFunc.Lerp.LerpVector2Unclamped(startPosition, TargetPosition, easeT);
            }

            rectTransform.anchoredPosition = TargetPosition;
        }
    }
}