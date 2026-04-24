using System;
using System.Threading;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DebugPrintLogNode : IBeeTweenNode
    {
        public string message;

        public DebugPrintLogNode() => message = "Hello, World!";
        public DebugPrintLogNode(string message) => this.message = message;
        
        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            DebugPrint.Log(message);
        }
    }
}