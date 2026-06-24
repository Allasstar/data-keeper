using System;
using DataKeeper.Attributes;
using DataKeeper.ServiceLocatorPattern;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.ValueProviders
{
    // Integration providers: resolve a service from the ServiceLocator via a GetInContext
    // (the read-side counterpart to ServiceLocatorRegister.RegInContext). The context is
    // picked in the inspector through the SerializeReference selector. Only reference types
    // are supported because Register.Get<T>() is class-constrained — services are Components,
    // so this covers the Transform/RectTransform/Image markers.

    [Serializable]
    public class TransformServiceLocatorProvider : ITransformProvider
    {
        [SerializeReference, SerializeReferenceSelector] private GetInContext _context = new GetInGlobalContext();

        public Transform GetValue() => _context?.Get<Transform>();
    }

    [Serializable]
    public class RectTransformServiceLocatorProvider : IRectTransformProvider
    {
        [SerializeReference, SerializeReferenceSelector] private GetInContext _context = new GetInGlobalContext();

        public RectTransform GetValue() => _context?.Get<RectTransform>();
    }

    [Serializable]
    public class ImageServiceLocatorProvider : IImageProvider
    {
        [SerializeReference, SerializeReferenceSelector] private GetInContext _context = new GetInGlobalContext();

        public Image GetValue() => _context?.Get<Image>();
    }
}
