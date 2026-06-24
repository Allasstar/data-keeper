using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DelayNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector]
        public IFloatProvider Duration;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            await Awaitable.WaitForSecondsAsync(Duration.GetValue(), cancellationToken.Token);
        }
    }
}
