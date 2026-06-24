using System;
using System.Threading;
using DataKeeper.Signals;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class SignalChannelNode : IBeeTweenNode
    {
        [SerializeField] private SignalChannel Channel;

        public SignalChannelNode() { }
        public SignalChannelNode(SignalChannel channel) => Channel = channel;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            // Signal isolates per-listener exceptions internally.
            Channel?.Invoke();
        }
    }
}
