using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class SourceOutputTests
    {
        private const int SampleRate = 48000;

        [Test]
        public void Oscillator_StaysFiniteAndBounded(
            [Values] Waveform waveform,
            [Values(20f, 440f, 5000f, 15000f)] float frequency)
        {
            var oscillator = new PolyBlepOscillator();
            var increment = frequency / SampleRate;

            for (var i = 0; i < SampleRate; i++)
            {
                var y = oscillator.Next(waveform, increment);
                Assert.IsTrue(float.IsFinite(y), $"Non-finite sample at {i}");
                Assert.LessOrEqual(Mathf.Abs(y), 1.05f, $"Sample {i} out of range");
            }
        }

        [Test]
        public void Noise_StaysFiniteAndBounded([Values] NoiseColor color)
        {
            var noise = new NoiseGenerator(1234u);
            var peak = 0f;

            for (var i = 0; i < SampleRate * 4; i++)
            {
                var y = noise.Next(color);
                Assert.IsTrue(float.IsFinite(y), $"Non-finite sample at {i}");
                peak = Mathf.Max(peak, Mathf.Abs(y));
            }

            Assert.LessOrEqual(peak, 1.5f);
            Assert.Greater(peak, 0.05f);
        }

        [Test]
        public void Renderer_FullScaleLayer_PeaksAtConstantPowerCentre([Values] Waveform waveform)
        {
            var recipe = ForgeTestRecipes.Create(250f, ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(waveform)));
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var output = renderer.Output;
            var peak = 0f;
            for (var i = 0; i < output.Length; i++)
            {
                Assert.IsTrue(float.IsFinite(output[i]));
                peak = Mathf.Max(peak, Mathf.Abs(output[i]));
            }

            Assert.That(peak, Is.InRange(0.5f, 0.75f));
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Renderer_MutedAndSoloRules()
        {
            var muted = ForgeTestRecipes.Oscillator(Waveform.Saw);
            muted.Mute = true;
            var recipe = ForgeTestRecipes.Create(100f, muted);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);
            AssertSilent(renderer);

            var soloed = ForgeTestRecipes.Oscillator(Waveform.Sine);
            soloed.Solo = true;
            soloed.Mute = true;
            recipe.Layers = new System.Collections.Generic.List<Layer>
                { ForgeTestRecipes.Oscillator(Waveform.Square), soloed };

            renderer.Render(recipe);
            AssertSilent(renderer);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Renderer_StartOffset_KeepsLeadingSilence()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.White));
            layer.StartOffsetMs = 50f;
            var recipe = ForgeTestRecipes.Create(200f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var output = renderer.Output;
            var offsetSamples = 50 * SampleRate / 1000 * SfxRenderer.Channels;
            for (var i = 0; i < offsetSamples; i++) Assert.AreEqual(0f, output[i]);

            var tailPeak = 0f;
            for (var i = offsetSamples; i < output.Length; i++) tailPeak = Mathf.Max(tailPeak, Mathf.Abs(output[i]));
            Assert.Greater(tailPeak, 0.1f);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Renderer_Decay_SilencesLayerAfterVoiceEnds()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.White));
            layer.StartOffsetMs = 20f;
            layer.DecayMs = 100f;
            var recipe = ForgeTestRecipes.Create(400f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var output = renderer.Output;
            var voiceEnd = 120 * SampleRate / 1000 * SfxRenderer.Channels;
            var voicePeak = 0f;
            for (var i = 0; i < voiceEnd; i++) voicePeak = Mathf.Max(voicePeak, Mathf.Abs(output[i]));
            for (var i = voiceEnd; i < output.Length; i++) Assert.AreEqual(0f, output[i], $"Sample {i} after decay");

            Assert.Greater(voicePeak, 0.1f);
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Renderer_LowPassFilter_ReducesNoiseEnergy()
        {
            var open = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.White));
            var filtered = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.White));
            filtered.Filter = new FilterSettings { Type = FilterType.LowPass, CutoffHz = 300f, Resonance = 0f };

            var recipe = ForgeTestRecipes.Create(300f, open);
            using var renderer = new SfxRenderer();
            renderer.Render(recipe);
            var openRms = Rms(renderer);

            recipe.Layers[0] = filtered;
            renderer.Render(recipe);
            var filteredRms = Rms(renderer);

            Assert.Less(filteredRms, openRms * 0.3f);
            Object.DestroyImmediate(recipe);
        }

        private static float Rms(SfxRenderer renderer)
        {
            var output = renderer.Output;
            var sum = 0.0;
            for (var i = 0; i < output.Length; i++) sum += output[i] * output[i];
            return (float)System.Math.Sqrt(sum / output.Length);
        }

        private static void AssertSilent(SfxRenderer renderer)
        {
            var output = renderer.Output;
            for (var i = 0; i < output.Length; i++) Assert.AreEqual(0f, output[i]);
        }
    }
}
