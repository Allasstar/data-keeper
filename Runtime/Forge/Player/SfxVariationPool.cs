using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge.Runtime
{
    // Pre-renders variations of a recipe on load and plays them from a shuffle bag: every
    // variation plays once per round, and a new round never starts with the one just heard.
    [AddComponentMenu("DataKeeper/Forge/SFX Variation Pool")]
    public class SfxVariationPool : MonoBehaviour
    {
        public const int MaxPoolSize = 32;

        [SerializeField] private SfxRecipe _recipe;
        [SerializeField, Range(1, MaxPoolSize)] private int _poolSize = 8;
        [SerializeField, Range(0f, 1f)] private float _variation = 0.3f;
        [SerializeField] private uint _seed = 1;
        [SerializeField] private bool _includeOriginal = true;
        [SerializeField] private AudioSource _source;

        private readonly List<AudioClip> _clips = new();
        private int[] _order = Array.Empty<int>();
        private int _cursor;
        private int _last = -1;
        private Random _random;

        public IReadOnlyList<AudioClip> Clips => _clips;
        public AudioSource Source => _source;

        private void Awake()
        {
            if (_source == null) _source = GetComponent<AudioSource>();
            Build();
        }

        private void OnDestroy() => ReleaseClips();

        // Same recipe, seed and settings always render the same set of clips.
        public void Build()
        {
            ReleaseClips();
            if (_recipe == null) return;

            using (var renderer = new SfxClipRenderer())
            {
                for (var i = 0; i < _poolSize; i++)
                {
                    var seed = math.max(1u, math.hash(new uint2(_seed, (uint)i)));
                    var variation = _includeOriginal && i == 0 ? 0f : _variation;
                    _clips.Add(renderer.RenderClip(_recipe, seed, variation, $"{_recipe.name} {i + 1}"));
                }
            }

            _order = new int[_clips.Count];
            for (var i = 0; i < _order.Length; i++) _order[i] = i;
            _cursor = _order.Length;
        }

        public void SetRecipe(SfxRecipe recipe, bool rebuild = true)
        {
            _recipe = recipe;
            if (rebuild) Build();
        }

        public AudioClip Next()
        {
            if (_clips.Count == 0) return null;
            if (_cursor >= _order.Length) Shuffle();

            _last = _order[_cursor++];
            return _clips[_last];
        }

        public void Play(float volumeScale = 1f)
        {
            var clip = Next();
            if (clip != null) _source.PlayOneShot(clip, volumeScale);
        }

        private void Shuffle()
        {
            if (_random.state == 0) _random = new Random((uint)Environment.TickCount | 1u);
            for (var i = _order.Length - 1; i > 0; i--)
            {
                var j = _random.NextInt(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }

            if (_order.Length > 1 && _order[0] == _last)
                (_order[0], _order[_order.Length - 1]) = (_order[_order.Length - 1], _order[0]);
            _cursor = 0;
        }

        private void ReleaseClips()
        {
            foreach (var clip in _clips)
            {
                if (Application.isPlaying) Destroy(clip);
                else DestroyImmediate(clip);
            }

            _clips.Clear();
            _order = Array.Empty<int>();
            _last = -1;
        }
    }
}
