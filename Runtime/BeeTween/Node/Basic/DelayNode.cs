using System;
using System.Threading;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Delay node - waits for a specified duration
    /// </summary>
    [Serializable]
    public class DelayNode : IBeeTweenNode
    {
        public float Duration;

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            await Awaitable.WaitForSecondsAsync(Duration, cancellationToken.Token);
        }
    }
}