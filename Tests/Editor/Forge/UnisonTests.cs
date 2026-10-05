using System.Collections.Generic;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class UnisonTests
    {
        private const float Tolerance = 1e-4f;

        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        [Test]
        public void Defaults_AreOneVoiceAtPhaseZero()
        {
            var layer = new Layer();
            Assert.AreEqual(1, layer.Unison.Voices);
            Assert.AreEqual(0f, layer.Unison.DetuneCents);
            Assert.AreEqual(0f, layer.Unison.Spread);
            Assert.AreEqual(0f, layer.Phase.Start);
            Assert.IsFalse(layer.Phase.Random);
        }

        [Test]
        public void ExplicitDefaults_RenderBitIdenticalToUntouchedRecipe()
        {
            using var reference = new SfxRenderer();
            reference.Render(MixedRecipe());

            var recipe = MixedRecipe();
            foreach (var layer in recipe.Layers)
            {
                layer.Unison = new UnisonSettings { Voices = 1 };
                layer.Phase = new PhaseSettings { Start = 0f, Random = false };
            }

            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void OldDataZeroVoices_RendersBitIdenticalToOneVoice()
        {
            using var reference = new SfxRenderer();
            reference.Render(MixedRecipe());

            var recipe = MixedRecipe();
            foreach (var layer in recipe.Layers) layer.Unison = default;

            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void OneVoice_IgnoresDetuneAndSpread()
        {
            using var reference = new SfxRenderer();
            reference.Render(MixedRecipe());

            var recipe = MixedRecipe();
            foreach (var layer in recipe.Layers)
                layer.Unison = new UnisonSettings { Voices = 1, DetuneCents = 40f, Spread = 1f };

            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void NoiseLayer_IgnoresUnison()
        {
            var plain = Recipe(ForgeTestRecipes.Noise(NoiseColor.Pink, -6f));
            using var reference = new SfxRenderer();
            reference.Render(plain);

            var noise = ForgeTestRecipes.Noise(NoiseColor.Pink, -6f);
            noise.Unison = new UnisonSettings { Voices = 6, DetuneCents = 50f, Spread = 1f };
            noise.Phase = new PhaseSettings { Start = 0.3f, Random = true };
            using var rendered = new SfxRenderer();
            rendered.Render(Recipe(noise));

            AssertIdentical(reference, rendered);
        }

        // N identical voices at 1/√N each sum to √N times one voice.
        [Test]
        public void TwoIdenticalVoices_EqualOneVoiceTimesSqrt2([Values] TonalSource source)
        {
            using var single = new SfxRenderer();
            single.Render(Recipe(TonalLayer(source)));

            var layer = TonalLayer(source);
            layer.Unison = new UnisonSettings { Voices = 2 };
            using var stacked = new SfxRenderer();
            stacked.Render(Recipe(layer));

            single.LayerFrameRange(0, out var start, out var end);
            var a = single.LayerOutput(0);
            var b = stacked.LayerOutput(0);
            for (var i = start * SfxRenderer.Channels; i < end * SfxRenderer.Channels; i++)
                Assert.AreEqual(a[i] * math.SQRT2, b[i], Tolerance, $"Sample {i}");
        }

        [Test]
        public void Spread_MakesLeftAndRightDiffer()
        {
            var layer = TonalLayer(TonalSource.Oscillator);
            layer.Pan = 0f;
            layer.Unison = new UnisonSettings { Voices = 3, DetuneCents = 30f, Spread = 1f };
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(layer));

            renderer.LayerFrameRange(0, out var start, out var end);
            var output = renderer.LayerOutput(0);
            var maxDifference = 0f;
            for (var frame = start; frame < end; frame++)
                maxDifference = math.max(maxDifference, math.abs(output[frame * 2] - output[frame * 2 + 1]));

            Assert.Greater(maxDifference, 0.01f);
        }

        [Test]
        public void StartQuarter_SineBeginsAtItsPeak()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine));
            layer.Phase = new PhaseSettings { Start = 0.25f };
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(layer));

            renderer.LayerFrameRange(0, out var start, out var end);
            var output = renderer.LayerOutput(0);
            var peak = 0f;
            for (var frame = start; frame < end; frame++) peak = math.max(peak, math.abs(output[frame * 2]));

            Assert.Greater(peak, 0.1f);
            Assert.AreEqual(peak, output[start * 2], peak * 0.01f);
        }

        [Test]
        public void StartZero_SineBeginsAtZero()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine));
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(layer));

            renderer.LayerFrameRange(0, out var start, out _);
            Assert.AreEqual(0f, renderer.LayerOutput(0)[start * 2], Tolerance);
        }

        [Test]
        public void RandomPhase_FollowsTheSeed()
        {
            var layer = TonalLayer(TonalSource.Oscillator);
            layer.Unison = new UnisonSettings { Voices = 4, DetuneCents = 20f, Spread = 0.5f };
            layer.Phase = new PhaseSettings { Random = true };
            var recipe = Recipe(layer);

            using var a = new SfxRenderer();
            using var b = new SfxRenderer();
            using var c = new SfxRenderer();
            a.Render(recipe, 1);
            b.Render(recipe, 1);
            c.Render(recipe, 2);

            AssertIdentical(a, b);

            var differs = false;
            var outA = a.Output;
            var outC = c.Output;
            for (var i = 0; i < outA.Length && !differs; i++) differs = outA[i] != outC[i];
            Assert.IsTrue(differs);
        }

        [Test]
        public void Randomize_KeepsUnisonAndPhaseByLayerIndex()
        {
            var template = ForgeTestRecipes.LoadTemplates()[SfxCategory.Impact];
            var recipe = NewRecipe();
            SfxRandomizer.Randomize(recipe, template, 1u);

            var unison = new UnisonSettings { Voices = 5, DetuneCents = 30f, Spread = 1f };
            var phase = new PhaseSettings { Start = 0.25f, Random = true };
            recipe.Layers[0].Unison = unison;
            recipe.Layers[0].Phase = phase;

            SfxRandomizer.Randomize(recipe, template, 2u);
            Assert.AreEqual(unison, recipe.Layers[0].Unison);
            Assert.AreEqual(phase, recipe.Layers[0].Phase);

            SfxRandomizer.Mutate(recipe, 3u);
            Assert.AreEqual(unison, recipe.Layers[0].Unison);
            Assert.AreEqual(phase, recipe.Layers[0].Phase);
        }

        public enum TonalSource
        {
            Oscillator,
            Wavetable,
            FM,
        }

        private static Layer TonalLayer(TonalSource source)
        {
            var layer = ForgeTestRecipes.Oscillator(Waveform.Saw, -5f, -12f);
            layer.Source = SourceSettings.Default;
            layer.Source.Oscillator.Waveform = Waveform.Saw;
            layer.Source.Wavetable.Position = 0.4f;
            layer.Source.Type = source switch
            {
                TonalSource.Wavetable => SourceType.Wavetable,
                TonalSource.FM => SourceType.FM,
                _ => SourceType.Oscillator,
            };
            layer.Filter = new FilterSettings { Type = FilterType.LowPass, CutoffHz = 3000f, Resonance = 0.3f };
            layer.Pan = 0.3f;
            return layer;
        }

        private SfxRecipe MixedRecipe() => Recipe(
            TonalLayer(TonalSource.Oscillator),
            TonalLayer(TonalSource.Wavetable),
            TonalLayer(TonalSource.FM),
            ForgeTestRecipes.Noise(NoiseColor.White, -12f));

        private SfxRecipe Recipe(params Layer[] layers)
        {
            var recipe = ForgeTestRecipes.Create(300f, layers);
            recipe.Seed = 42;
            _created.Add(recipe);
            return recipe;
        }

        private SfxRecipe NewRecipe()
        {
            var recipe = ScriptableObject.CreateInstance<SfxRecipe>();
            _created.Add(recipe);
            return recipe;
        }

        private static void AssertIdentical(SfxRenderer expected, SfxRenderer actual)
        {
            var a = expected.Output;
            var b = actual.Output;
            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < a.Length; i++)
            {
                if (System.BitConverter.SingleToInt32Bits(a[i]) != System.BitConverter.SingleToInt32Bits(b[i]))
                    Assert.Fail($"Sample {i} differs: {a[i]:R} vs {b[i]:R}");
            }
        }
    }
}
