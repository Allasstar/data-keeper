using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class UpdateNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode updateNode;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            while (!cancellationToken.Token.IsCancellationRequested)
            {
                updateNode?.ExecuteAsync(cancellationToken);
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
            }
        }
    }
}
