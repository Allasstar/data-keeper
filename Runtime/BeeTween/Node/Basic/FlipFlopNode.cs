using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class FlipFlopNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IBoolProvider IsA;
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode ANode;
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode BNode;

        [SerializeField, ReadOnlyInspector] private bool _curState;
        private bool _isInitialized;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            if (ANode == null || BNode == null || IsA == null) return;
            if (!_isInitialized) { _curState = IsA.GetValue(); _isInitialized = true; }

            if (_curState) await ANode.ExecuteAsync(cancellationToken);
            else           await BNode.ExecuteAsync(cancellationToken);

            _curState = !_curState;
        }
    }
}
