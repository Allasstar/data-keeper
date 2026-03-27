using System.Threading;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Base interface for all tween nodes
    /// </summary>
    public interface IBeeTweenNode
    {
        Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken);
    }
    
    /// <summary>
    /// Generic node interface for type-safe node operations
    /// </summary>
    public interface IBeeTweenNode<T> : IBeeTweenNode where T : class
    {
        new Awaitable ExecuteAsync(IBeeTweenContext<T> context, CancellationTokenSource cancellationToken);
    }
}