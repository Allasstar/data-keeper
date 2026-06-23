using System;
using DataKeeper.Attributes;
using DataKeeper.BlackboardSystem;
using DataKeeper.Extensions;
using DataKeeper.GameTagSystem;
using UnityEngine;

namespace DataKeeper.BeeTween
{
    public interface IIntProvider
    {
        int GetValue();
    }

    [Serializable]
    public class IntValueProvider : IIntProvider
    {
        [field: SerializeField] public int Value { get; set; }

        public int GetValue() => Value;
    }

    [Serializable]
    public class IntBlackboardProvider : IIntProvider
    {
        [SerializeField] private GameTag _key;
        [RequireInterface(typeof(IBlackboardOwner))]
        [SerializeField] private MonoBehaviour _blackboardSource;

        public int GetValue()
        {
            return _blackboardSource.Cast<IBlackboardOwner>().Blackboard.GetInt(_key);
        }
    }

    [Serializable]
    public class FrameCountProvider : IIntProvider
    {
        public int GetValue() => Time.frameCount;
    }
}
