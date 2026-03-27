using System;
using System.Threading;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DebugLogNode : IBeeTweenNode
    {
        public string message;

        public DebugLogNode() => message = "Hello, World!";
        public DebugLogNode(string message) => this.message = message;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            Debug.Log(message);
        }
    }
}