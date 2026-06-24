using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DeltaUpdateNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IFloatProvider deltaTimeProvider = new DeltaTimeProvider();
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode updateNode;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            while (!cancellationToken.Token.IsCancellationRequested)
            {
                updateNode?.ExecuteAsync(cancellationToken);
                await Awaitable.WaitForSecondsAsync(deltaTimeProvider.GetValue(), cancellationToken.Token);
            }
        }
    }
}
