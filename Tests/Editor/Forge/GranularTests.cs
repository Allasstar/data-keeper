using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class GranularTests
    {
        private const int SampleRate = 48000;

        private AudioClip _clip;
        private SfxRecipe _recipe;

        [SetUp]
        public void SetUp()
        {
            // 100 ms of 440 Hz.
            var data = new float[SampleRate / 10];
            for (var i = 0; i < data.Length; i++) data[i] = 0.8f * math.sin(2f * math.PI * 440f * i / SampleRate);
            _clip = AudioClip.Create("Forge Grain Clip", data.Length, 1, SampleRate, false);
            _clip.SetData(data, 0);

            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine));
            layer.DecayMs = 400f;
            layer.Source.Type = SourceType.Granular;
            layer.Source.Sample = new SampleSettings { Clip = _clip, Interpolation = SampleInterpolation.Cubic };
            layer.Source.Granular = new GranularSettings { GrainMs = 20f, Density = 200f, SprayMs = 0f };

            _recipe = ForgeTestRecipes.Create(400f, layer);
            _recipe.Fx.Limiter.Enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_clip);
        }

        [Test]
        public void Granular_ExtremeSettingsStayFiniteAndBounded(
            [Values(GranularSettings.MinGrainMs, GranularSettings.MaxGrainMs)] float grainMs,
            [Values(GranularSettings.MinDensity, GranularSettings.MaxDensity)] float density)
        {
            var granular = _recipe.Layers[0].Source;
            granular.Granular = new GranularSettings
            {
                GrainMs = grainMs, Density = density, SprayMs = GranularSettings.MaxSprayMs,
                PitchRandom = GranularSettings.MaxPitchRandom,
            };
            _recipe.Layers[0].Source = granular;
            using var renderer = new SfxRenderer();

            renderer.Render(_recipe);

            var output = renderer.Output;
            for (var i = 0; i < output.Length; i++)
            {
                Assert.IsTrue(float.IsFinite(output[i]), $"sample {i}");
                Assert.LessOrEqual(math.abs(output[i]), 4f, $"sample {i}");
            }
        }

        [Test]
        public void Granular_IsDeterministicPerSeed()
        {
            using var renderer = new SfxRenderer();
            _recipe.Layers[0].Source.Granular.SprayMs = 30f;

            renderer.Render(_recipe, 3u);
            var first = renderer.Output.ToArray();
            renderer.Render(_recipe, 3u);
            var again = renderer.Output.ToArray();
            renderer.Render(_recipe, 4u);
            var other = renderer.Output.ToArray();

            CollectionAssert.AreEqual(first, again);
            CollectionAssert.AreNotEqual(first, other);
        }

        // Grains replay at the layer pitch while the read head moves in real time, so an octave
        // up doubles the frequency but keeps the clip's length, unlike plain sample playback.
        [Test]
        public void Granular_PitchDoesNotChangeDuration()
        {
            var layer = _recipe.Layers[0];
            layer.Pitch = 12f;
            using var renderer = new SfxRenderer();
            using var analyzer = new SfxAnalyzer();

            renderer.Render(_recipe);
            var granular = analyzer.Analyze(renderer.Output, SfxRenderer.Channels, SampleRate);

            var source = layer.Source;
            source.Type = SourceType.Sample;
            layer.Source = source;
            renderer.Render(_recipe);
            var sample = analyzer.Analyze(renderer.Output, SfxRenderer.Channels, SampleRate);

            Assert.AreEqual(880f, granular.SpectralCentroidHz, 880f * 0.15f);
            Assert.Greater(granular.EffectiveLengthMs, 80f);
            Assert.Less(sample.EffectiveLengthMs, 55f);
        }

        [Test]
        public void Granular_MissingClipIsSilent()
        {
            _recipe.Layers[0].Source.Sample.Clip = null;
            using var renderer = new SfxRenderer();

            renderer.Render(_recipe);

            var output = renderer.Output;
            for (var i = 0; i < output.Length; i++) Assert.AreEqual(0f, output[i]);
        }
    }
}
