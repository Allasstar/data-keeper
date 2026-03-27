using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Color tween node - tweens an Image's color
    /// </summary>
    [Serializable]
    public class ColorNode : IBeeTweenNode
    {
        public Color TargetColor;
        public float Duration;
        
        [field: SerializeReference, SerializeReferenceSelector]
        public EaseProvider Ease { get; set; }

        public ColorNode()
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
            var startColor = image.color;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = easeProvider.Evaluate(context, t);
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