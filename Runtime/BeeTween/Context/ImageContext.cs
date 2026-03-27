using System;
using DataKeeper.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Context for controlling UI Images
    /// </summary>
    [Serializable]
    public class ImageContext : IBeeTweenContext<Image>
    {
        [SerializeField] private Image target;
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode rootNode;

        public Image Target => target;
        public IBeeTweenNode RootNode => rootNode;
        object IBeeTweenContext.Target => target;

        public ImageContext() { }
        
        public ImageContext(Image target, IBeeTweenNode rootNode)
        {
            this.target = target;
            this.rootNode = rootNode;
        }

        public bool IsValid() => target != null && rootNode != null;
    }
}