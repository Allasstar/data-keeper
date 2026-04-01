using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DeltaUpdateNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] private FloatProvider DeltaTimeProvider = new DeltaTimeProvider();
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode updateNode;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            while (cancellationToken.Token.IsCancellationRequested == false)
            {
                updateNode?.ExecuteAsync(context, cancellationToken);
                await Awaitable.WaitForSecondsAsync(DeltaTimeProvider.GetValue(context), cancellationToken.Token);
            }
        }
    }
}