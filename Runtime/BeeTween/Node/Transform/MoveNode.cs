using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class MoveNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public ITransformProvider TargetProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IVector3Provider EndPositionProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider DurationProvider { get; set; }
        [field: SerializeReference, SerializeReferenceSelector] public IEaseProvider Ease { get; set; }

        [SerializeField, ReadOnlyInspector] private float _duration;
        [SerializeField, ReadOnlyInspector] private Vector3 _startPoint;
        [SerializeField, ReadOnlyInspector] private Vector3 _endPoint;

        public MoveNode()
        {
            TargetProvider      = new TransformValueProvider();
            EndPositionProvider = new Vector3ValueProvider();
            DurationProvider    = new FloatValueProvider();
            Ease                = new EaseFuncProvider();
        }

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            var target = TargetProvider?.GetValue();
            if (target == null) return;

            _duration   = DurationProvider.GetValue();
            _startPoint = target.position;
            _endPoint   = EndPositionProvider.GetValue();
            var easeProvider = Ease ?? new EaseFuncProvider();
            var elapsedTime  = 0f;

            while (elapsedTime < _duration)
            {
                elapsedTime += Time.deltaTime;
                target.position = MathFunc.Lerp.LerpVector3Unclamped(_startPoint, _endPoint, easeProvider.Evaluate(Mathf.Clamp01(elapsedTime / _duration)));
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
            }

            target.position = _endPoint;
        }
    }
}
