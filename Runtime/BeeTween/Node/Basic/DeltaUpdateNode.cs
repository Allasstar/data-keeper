using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DeltaUpdateNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public FloatProvider deltaTimeProvider = new DeltaTimeProvider();
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode updateNode;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            while (cancellationToken.Token.IsCancellationRequested == false)
            {
                updateNode?.ExecuteAsync(context, cancellationToken);
                await Awaitable.WaitForSecondsAsync(deltaTimeProvider.GetValue(context), cancellationToken.Token);
            }
        }
    }
}