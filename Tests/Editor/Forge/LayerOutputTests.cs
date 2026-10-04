using DataKeeper.Forge.Render;
using NUnit.Framework;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class LayerOutputTests
    {
        [Test]
        public void AudibleLayers_SumToOutput_WhenFxIsOff()
        {
            var saw = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Saw, levelDb: -6f));
            var noise = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.Pink, levelDb: -12f));
            noise.StartOffsetMs = 50f;
            var recipe = ForgeTestRecipes.Create(300f, saw, noise);
            recipe.Fx.Limiter.Enabled = false;

            using var renderer = new SfxRenderer();
            renderer.Render(recipe);
            var output = renderer.Output;
            var channels = SfxRenderer.Channels;

            Assert.AreEqual(2, renderer.LayerCount);
            for (var sample = 0; sample < output.Length; sample++)
            {
                var frame = sample / channels;
                var sum = 0f;
                for (var layer = 0; layer < renderer.LayerCount; layer++)
                {
                    Assert.IsTrue(renderer.IsLayerAudible(layer));
                    renderer.LayerFrameRange(layer, out var start, out var end);
                    if (frame >= start && frame < end) sum += renderer.LayerOutput(layer)[sample];
                }

                Assert.AreEqual(output[sample], sum, 1e-5f, $"Sample {sample}");
            }

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void MutedLayer_IsNotAudible()
        {
            var playing = ForgeTestRecipes.Oscillator(Waveform.Sine);
            var muted = ForgeTestRecipes.Oscillator(Waveform.Square);
            muted.Mute = true;
            var recipe = ForgeTestRecipes.Create(200f, playing, muted);

            using var renderer = new SfxRenderer();
            renderer.Render(recipe);

            Assert.IsTrue(renderer.IsLayerAudible(0));
            Assert.IsFalse(renderer.IsLayerAudible(1));

            Object.DestroyImmediate(recipe);
        }
    }
}
