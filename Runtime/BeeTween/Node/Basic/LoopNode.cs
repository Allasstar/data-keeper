using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class LoopNode : IBeeTweenNode
    {
        [field: SerializeReference, SerializeReferenceSelector] public IIntProvider LoopCountProvider { get; set; }
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode updateNode;

        [SerializeField, ReadOnlyInspector] private int _loopCount;
        [SerializeField, ReadOnlyInspector] private int _curLoopCount;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            if (updateNode == null) return;
            _loopCount = LoopCountProvider.GetValue();
            _curLoopCount = 0;
            while (_curLoopCount < _loopCount && !cancellationToken.Token.IsCancellationRequested)
            {
                await updateNode.ExecuteAsync(cancellationToken);
                _curLoopCount++;
            }
        }
    }
}
