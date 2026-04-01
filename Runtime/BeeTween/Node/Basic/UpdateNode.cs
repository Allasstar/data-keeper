using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class UpdateNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode updateNode;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            while (cancellationToken.Token.IsCancellationRequested == false)
            {
                updateNode?.ExecuteAsync(context, cancellationToken);
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
            }
        }
    }
}