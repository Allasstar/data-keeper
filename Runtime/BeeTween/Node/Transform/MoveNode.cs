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

        [SerializeField, ReadOnlyInspector] private float _duratuion;
        [SerializeField, ReadOnlyInspector] private Vector3 _startPoint;
        [SerializeField, ReadOnlyInspector] private Vector3 _endPoint;

        public MoveNode()
        {
            EndPositionProvider = new Vector3ValueProvider();
            DurationProvider = new FloatValueProvider();
            Ease = new EaseFuncProvider();
        }

        void UpdateValues(IBeeTweenContext context, Transform target)
        {
            _duratuion = DurationProvider.GetValue(context);
            _startPoint = target.position;
            _endPoint = EndPositionProvider.GetValue(context);
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<Transform> trContext || trContext.Target == null) return;
            
            var easeProvider = Ease ?? new EaseFuncProvider();
            var elapsedTime = 0f;
            
            UpdateValues(context, trContext.Target);

            while (elapsedTime < _duratuion)
            {
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / _duratuion);
                var easeT = easeProvider.Evaluate(context, t);
                trContext.Target.position = MathFunc.Lerp.LerpVector3Unclamped(_startPoint, _endPoint, easeT);
                
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
            }

            trContext.Target.position = _endPoint;
        }
    }
}