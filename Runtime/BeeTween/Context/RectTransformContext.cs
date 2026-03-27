using System;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Context for controlling RectTransforms (UI elements)
    /// </summary>
    [Serializable]
    public class RectTransformContext : IBeeTweenContext<RectTransform>
    {
        [SerializeField] private RectTransform target;
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode rootNode;

        public RectTransform Target => target;
        public IBeeTweenNode RootNode => rootNode;
        object IBeeTweenContext.Target => target;

        public RectTransformContext() { }
        
        public RectTransformContext(RectTransform target, IBeeTweenNode rootNode)
        {
            this.target = target;
            this.rootNode = rootNode;
        }

        public bool IsValid() => target != null && rootNode != null;
    }
}