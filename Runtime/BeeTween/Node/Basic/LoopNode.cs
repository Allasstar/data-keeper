using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class LoopNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public IntProvider LoopCountProvider { get; set; }
        
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode updateNode;
        
        [SerializeField, ReadOnlyInspector] private int _loopCount;
        [SerializeField, ReadOnlyInspector] private int _curLoopCount;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if(updateNode == null) return;
            
            _loopCount = LoopCountProvider.GetValue(context);
            _curLoopCount = 0;
            
            while (_curLoopCount < _loopCount && cancellationToken.Token.IsCancellationRequested == false)
            {
                await updateNode.ExecuteAsync(context, cancellationToken);
                _curLoopCount++;
            }
        }
    }
}