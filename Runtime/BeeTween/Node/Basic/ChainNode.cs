using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Chain node - waits for a node to complete, then executes another node after a delay
    /// </summary>
    [Serializable]
    public class ChainNode : IBeeTweenNode
    {
        [field: SerializeField] public Optional<float> DelayNextNode { get; private set; }
        
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode[] Nodes;

        public ChainNode()
        {
            Nodes = Array.Empty<IBeeTweenNode>();
            DelayNextNode = new Optional<float>(0, false);
        }

        public ChainNode(params IBeeTweenNode[] nodes)
        {
            Nodes = nodes;
            DelayNextNode = new Optional<float>(0, false);
        }
        
        public ChainNode(float delay, params IBeeTweenNode[] nodes)
        {
            Nodes = nodes;
            DelayNextNode = new Optional<float>(delay, true);
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            foreach (var node in Nodes)
            {
                await node.ExecuteAsync(context, cancellationToken);
                if (DelayNextNode.Enabled)
                {
                    await Awaitable.WaitForSecondsAsync(DelayNextNode.Value, cancellationToken.Token);
                }
            }
        }
    }
}