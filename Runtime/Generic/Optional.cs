using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Generic
{
    [Serializable]
    public struct Optional<T> : IEquatable<Optional<T>>
    {
        [SerializeField] private bool enabled;
        [SerializeField] private T value;

        public bool Enabled => enabled;
        public T Value => value;

        // Add safe value access
        public T ValueOrDefault => enabled ? value : default;
        
        // Allow value access with fallback
        public T ValueOr(T fallback) => enabled ? value : fallback;

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

        // Implicit conversion from T to Optional<T>
        public static implicit operator Optional<T>(T value) => new Optional<T>(value);

        // Explicit conversion from Optional<T> to T (throws if not enabled)
        public static explicit operator T(Optional<T> optional)
        {
            if (!optional.enabled)
                throw new InvalidOperationException("Cannot get value from disabled Optional");
            return optional.value;
        }

        // Enable fluent modification
        public Optional<T> WithValue(T newValue)
        {
            value = newValue;
            return this;
        }

        public Optional<T> Enable()
        {
            enabled = true;
            return this;
        }

        public Optional<T> Disable()
        {
            enabled = false;
            return this;
        }

        // Without this, EqualityComparer<Optional<T>>.Default falls back to reflection-based
        // ValueType.Equals, which boxes both operands on every comparison.
        public bool Equals(Optional<T> other)
        {
            return enabled == other.enabled && EqualityComparer<T>.Default.Equals(value, other.value);
        }

        public override bool Equals(object obj) => obj is Optional<T> other && Equals(other);

        public override int GetHashCode()
        {
            return ((enabled ? 1 : 0) * 397) ^ (value == null ? 0 : EqualityComparer<T>.Default.GetHashCode(value));
        }
    }
}