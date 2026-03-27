using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Fade node - fades an Image's alpha
    /// </summary>
    [Serializable]
    public class FadeNode : IBeeTweenNode
    {
        public float TargetAlpha;
        public float Duration;
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public FadeNode()
        {
            Ease = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            Image image = null;
            
            if (context is IBeeTweenContext<Image> imageContext)
                image = imageContext.Target;
            else if (context is IBeeTweenContext<GameObject> goContext && goContext.Target != null)
                image = goContext.Target.GetComponent<Image>();

            if (image == null) return;

            var easeProvider = Ease ?? new EaseFuncProvider();
            var startAlpha = image.color.a;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = easeProvider.Evaluate(context, t);
                var color = image.color;
                color.a = MathFunc.Lerp.FloatUnclamped(startAlpha, TargetAlpha, easeT);
                image.color = color;
            }

            var finalColor = image.color;
            finalColor.a = TargetAlpha;
            image.color = finalColor;
        }
    }
}