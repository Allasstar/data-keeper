using System.Threading;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface IBeeTweenNode
    {
        Awaitable ExecuteAsync(CancellationTokenSource cancellationToken);
    }
}
