using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BeeTween
{
    public interface IImageProvider
    {
        Image GetValue();
    }

    [Serializable]
    public class ImageValueProvider : IImageProvider
    {
        [SerializeField] public Image target;

        public Image GetValue() => target;
    }

    [Serializable]
    public class ImageBlackboardProvider : IImageProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private MonoBehaviour _blackboardSource;

        public ImageBlackboardProvider() { }
        public ImageBlackboardProvider(GameTag key) => _key = key;

        public Image GetValue()
        {
            return _blackboardSource.Cast<IBlackboardOwner>().Blackboard.GetObject<Image>(_key);
        }
    }
}
