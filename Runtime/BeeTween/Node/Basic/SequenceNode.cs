using System;
using System.Threading;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Sequence node - executes multiple nodes in sequence
    /// </summary>
    [Serializable]
    public class SequenceNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode[] Nodes;

        public SequenceNode() => Nodes = Array.Empty<IBeeTweenNode>();
        
        public SequenceNode(params IBeeTweenNode[] nodes) => Nodes = nodes;

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            foreach (var node in Nodes)
            {
                await node.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}