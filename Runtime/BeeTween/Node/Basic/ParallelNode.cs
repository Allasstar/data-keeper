using System;
using System.Threading;
using System.Threading.Tasks;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Parallel node - executes multiple nodes in parallel
    /// </summary>
    [Serializable]
    public class ParallelNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode[] Nodes;

        public ParallelNode() => Nodes = Array.Empty<IBeeTweenNode>();
        
        public ParallelNode(params IBeeTweenNode[] nodes) => Nodes = nodes;

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (Nodes.Length == 0) return;
            
            var completionSource = new TaskCompletionSource<object>();
            int remaining = Nodes.Length;
            
            async Awaitable ExecuteAndComplete(IBeeTweenNode node)
            {
                try
                {
                    await node.ExecuteAsync(context, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        completionSource.SetException(ex);
                    }
                    return;
                }
                
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    completionSource.SetResult(null);
                }
            }
            
            foreach (var node in Nodes)
            {
                _ = ExecuteAndComplete(node);
            }
            
            await completionSource.Task;
        }
    }
}