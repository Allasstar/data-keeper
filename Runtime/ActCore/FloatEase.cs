using System;

namespace DataKeeper.ActCore
{
    public readonly struct FloatEase : IEquatable<FloatEase>, IComparable<FloatEase>
    {
        private readonly float _value;

        public FloatEase(float value)
        {
            _value = value;
        }

        public static implicit operator float(FloatEase target) => target._value;
        public static implicit operator FloatEase(float target) => new FloatEase(target);

        public static FloatEase operator +(FloatEase a, FloatEase b) => new FloatEase(a._value + b._value);
        public static FloatEase operator -(FloatEase a, FloatEase b) => new FloatEase(a._value - b._value);
        public static FloatEase operator *(FloatEase a, FloatEase b) => new FloatEase(a._value * b._value);
        public static FloatEase operator /(FloatEase a, FloatEase b) => new FloatEase(a._value / b._value);

        public bool Equals(FloatEase other) => _value.Equals(other._value);
        public override bool Equals(object obj) => obj is FloatEase other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public int CompareTo(FloatEase other) => _value.CompareTo(other._value);

        public override string ToString() => _value.ToString();
        public string ToString(string format) => _value.ToString(format);
    }
}