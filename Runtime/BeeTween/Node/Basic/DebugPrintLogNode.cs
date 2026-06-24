using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DebugPrintLogNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IStringProvider MessageProvider;

        public DebugPrintLogNode() => MessageProvider = new StringConstantProvider { Value = "Hello, World!" };
        public DebugPrintLogNode(IStringProvider messageProvider) => MessageProvider = messageProvider;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            DebugPrint.Log(MessageProvider?.GetValue());
        }
    }
}
