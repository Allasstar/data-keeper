using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class FlipFlopNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public BoolProvider IsA;
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode ANode;
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode BNode;
        
        [SerializeField, ReadOnlyInspector] private bool _curState;
        private bool _isInitialized;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if(ANode == null || BNode == null || IsA == null) return;
            
            if (!_isInitialized)
            {
                _curState = IsA.GetValue(context);
                _isInitialized = true;
            }
            
            if (_curState)
            {
                await ANode.ExecuteAsync(context, cancellationToken);
            }
            else
            {
                await BNode.ExecuteAsync(context, cancellationToken);
            }
            
            _curState = !_curState;
        }
    }
}