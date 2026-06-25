using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DataKeeper.ValueProviders
{
    // Random providers: generate a fresh value each call. Dependency-free.
    // Handy for tween juice — randomized durations, delays, target offsets, tints, rotations.

    [Serializable]
    public class RandomFloatProvider : IFloatProvider
    {
        [SerializeField] private float _min = 0f;
        [SerializeField] private float _max = 1f;

        public float GetValue() => Random.Range(_min, _max);
    }

    [Serializable]
    public class RandomIntProvider : IIntProvider
    {
        [SerializeField] private int _minInclusive = 0;
        [SerializeField] private int _maxExclusive = 100;

        public int GetValue() => Random.Range(_minInclusive, _maxExclusive);
    }

    [Serializable]
    public class RandomBoolProvider : IBoolProvider
    {
        [SerializeField, Range(0f, 1f)] private float _trueChance = 0.5f;

        public bool GetValue() => Random.value < _trueChance;
    }

    [Serializable]
    public class RandomVector2Provider : IVector2Provider
    {
        [SerializeField] private Vector2 _min = Vector2.zero;
        [SerializeField] private Vector2 _max = Vector2.one;

        public Vector2 GetValue() => new Vector2(
            Random.Range(_min.x, _max.x),
            Random.Range(_min.y, _max.y));
    }

    [Serializable]
    public class RandomVector3Provider : IVector3Provider
    {
        [SerializeField] private Vector3 _min = Vector3.zero;
        [SerializeField] private Vector3 _max = Vector3.one;

        public Vector3 GetValue() => new Vector3(
            Random.Range(_min.x, _max.x),
            Random.Range(_min.y, _max.y),
            Random.Range(_min.z, _max.z));
    }

    [Serializable]
    public class RandomColorProvider : IColorProvider
    {
        [SerializeField] private Color _min = Color.black;
        [SerializeField] private Color _max = Color.white;

        public Color GetValue() => new Color(
            Random.Range(_min.r, _max.r),
            Random.Range(_min.g, _max.g),
            Random.Range(_min.b, _max.b),
            Random.Range(_min.a, _max.a));
    }
}
