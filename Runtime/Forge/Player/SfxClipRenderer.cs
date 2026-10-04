using System;
using DataKeeper.Forge.Export;
using DataKeeper.Forge.Render;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Forge.Runtime
{
    // Renders recipes into AudioClips at runtime. Keep one instance around to render many
    // clips; the static Render is for one-offs and frees its buffers straight away.
    public sealed class SfxClipRenderer : IDisposable
    {
        private readonly SfxRenderer _renderer = new();
        private readonly SfxRecipe _scratch;
        private float[] _clipData = Array.Empty<float>();

        public SfxClipRenderer()
        {
            _scratch = ScriptableObject.CreateInstance<SfxRecipe>();
            _scratch.hideFlags = HideFlags.HideAndDontSave;
        }

        public static AudioClip Render(SfxRecipe recipe, uint seed, float variation)
        {
            using var renderer = new SfxClipRenderer();
            return renderer.RenderClip(recipe, seed, variation);
        }

        // Variation 0 renders the recipe as designed (the seed still drives noise, grains and
        // random routes); above 0 the recipe is first mutated by that amount with the same seed.
        public AudioClip RenderClip(SfxRecipe recipe, uint seed, float variation, string clipName = null)
        {
            var source = recipe;
            if (variation > 0f)
            {
                _scratch.FromJson(recipe.ToJson());
                _scratch.Randomizer.VariationAmount = Mathf.Clamp01(variation);
                SfxRandomizer.Mutate(_scratch, seed);
                source = _scratch;
            }

            _renderer.Render(source, seed);

            var output = _renderer.Output;
            var channels = ExportProcessor.IsMono(output, SfxRenderer.Channels) ? 1 : SfxRenderer.Channels;
            var frames = _renderer.FrameCount;
            if (_clipData.Length != frames * channels) _clipData = new float[frames * channels];

            if (channels == SfxRenderer.Channels)
            {
                output.CopyTo(_clipData);
            }
            else
            {
                for (var f = 0; f < frames; f++) _clipData[f] = output[f * SfxRenderer.Channels];
            }

            var clip = AudioClip.Create(clipName ?? $"{recipe.name} {seed}", frames, channels, _renderer.SampleRate, false);
            clip.SetData(_clipData, 0);
            return clip;
        }

        public void Dispose()
        {
            _renderer.Dispose();
            if (_scratch != null) Object.DestroyImmediate(_scratch);
        }
    }
}
