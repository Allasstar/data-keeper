using System;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Context for controlling GameObjects
    /// </summary>
    [Serializable]
    public class TransformContext : IBeeTweenContext<Transform>
    {
        [SerializeField] private Transform target;
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode rootNode;

        public Transform Target => target;
        public IBeeTweenNode RootNode => rootNode;
        object IBeeTweenContext.Target => target;

        public TransformContext() { }
        
        public TransformContext(Transform target, IBeeTweenNode rootNode)
        {
            this.target = target;
            this.rootNode = rootNode;
        }

        public bool IsValid() => target != null && rootNode != null;
    }
}