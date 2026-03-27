using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Chain node - waits for a node to complete, then executes another node after a delay
    /// 0 delay means delay 1 frame
    /// Negative delay means no delay
    /// </summary>
    [Serializable]
    public class ChainNode : IBeeTweenNode
    {
        public Optional<float> delayBefore;
        
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode NodeA;
        
        public Optional<float> delay;
        
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode NodeB;

        public Optional<float> delayAfter;
        
        public ChainNode()
        {
            delayBefore = new Optional<float>(1, false);
            delay = new Optional<float>(1, false);
            delayAfter = new Optional<float>(1, false);
            
            NodeA = new DebugLogNode("Chain Node A");
            NodeB = new DebugLogNode("Chain Node B");
        }

        public ChainNode(IBeeTweenNode nodeA, IBeeTweenNode nodeB)
        {
            NodeA = nodeA;
            NodeB = nodeB;
        }

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (delayBefore.Enabled)
            {
                await Awaitable.WaitForSecondsAsync(delayBefore.Value, cancellationToken.Token);
            }
            
            await NodeA.ExecuteAsync(context, cancellationToken);
            
            if (delay.Enabled)
            {
                await Awaitable.WaitForSecondsAsync(delay.Value, cancellationToken.Token);
            }
            
            await NodeB.ExecuteAsync(context, cancellationToken);
            
            if (delayAfter.Enabled)
            {
                await Awaitable.WaitForSecondsAsync(delayAfter.Value, cancellationToken.Token);
            }
        }
    }
}