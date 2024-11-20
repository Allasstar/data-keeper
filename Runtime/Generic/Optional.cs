using System;
using UnityEngine;

namespace DataKeeper.Generic
{
    [Serializable]
    public struct Optional<T>
    {
        [SerializeField] private bool enabled;
        [SerializeField] private T value;

        public bool Enabled => enabled;
        public T Value => value;

        public Optional(T initialValue)
        {
            enabled = true;
            value = initialValue;
        }

        public Optional(T initialValue, bool isEnabled)
        {
            enabled = isEnabled;
            value = initialValue;
        }
    }
}
