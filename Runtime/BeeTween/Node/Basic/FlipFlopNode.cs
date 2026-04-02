using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class FlipFlopNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public BoolProvider ISA;
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode ANode;
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode BNode;
        
        [SerializeField, ReadOnlyInspector] private bool _curState;
        private bool _isInitialized;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if(ANode == null || BNode == null) return;
            
            if (!_isInitialized)
            {
                _curState = ISA.GetValue(context);
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