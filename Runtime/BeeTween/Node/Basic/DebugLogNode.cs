using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class DebugLogNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IStringProvider MessageProvider;

        public DebugLogNode() => MessageProvider = new StringConstantProvider { Value = "Hello, World!" };
        public DebugLogNode(IStringProvider messageProvider) => MessageProvider = messageProvider;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            Debug.Log(MessageProvider?.GetValue());
        }
    }
}
