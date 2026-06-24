using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace DataKeeper.BeeTween
{
    [Serializable]
    public class UnityEventNode : IBeeTweenNode
    {
        [SerializeField] private UnityEvent OnInvoke;

        public UnityEventNode() { }
        public UnityEventNode(UnityEvent onInvoke) => OnInvoke = onInvoke;

        public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            // A throwing listener must not break the tween sequence.
            try { OnInvoke?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
