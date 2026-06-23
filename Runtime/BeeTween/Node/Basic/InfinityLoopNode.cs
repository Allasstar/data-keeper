using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class InfinityLoopNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode updateNode;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            if (updateNode == null) return;
            while (!cancellationToken.Token.IsCancellationRequested)
                await updateNode.ExecuteAsync(cancellationToken);
        }
    }
}
