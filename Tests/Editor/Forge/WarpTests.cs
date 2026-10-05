using System.Collections.Generic;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class WarpTests
    {
        private const int SampleRate = SfxRecipe.DefaultSampleRate;

        private static readonly ModSource[] ContinuousSources =
        {
            ModSource.Lfo, ModSource.Lfo2, ModSource.Lfo3,
            ModSource.Envelope, ModSource.Env2, ModSource.Env3,
            ModSource.Random, ModSource.Random2, ModSource.Random3,
        };

        private readonly List<Object> _created = new();
        private readonly LayerModulation[] _modulation = new LayerModulation[SfxRecipe.MaxLayers];

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        [Test]
        public void Defaults_AreOffAtZero()
        {
            Assert.AreEqual(default(WarpSettings), new Layer().Warp);
            Assert.AreEqual(WarpMode.Off, default(WarpSettings).Mode);
        }

        [Test]
        public void ModTargets_WarpIsPerLayerContinuousAndUnitRange()
        {
            Assert.IsTrue(ModTargets.IsPerLayer(ModTarget.Warp));
            Assert.IsTrue(ModTargets.IsContinuous(ModTarget.Warp));
            Assert.AreEqual(1f, ModTargets.MaxAmount(ModTarget.Warp));
            Assert.IsFalse(ModTargets.IsPerLayer(ModTarget.LfoDepth));
            Assert.IsFalse(ModTargets.IsContinuous(ModTarget.Decay));
        }

        // Warp Off with an amount and routes on the Warp lane changes nothing, mono or unison.
        [Test]
        public void WarpOff_RendersBitIdentical([Values(1, 3)] int voices)
        {
            using var reference = new SfxRenderer();
            reference.Render(TonalRecipe(voices));

            var recipe = TonalRecipe(voices);
            foreach (var layer in recipe.Layers) layer.Warp = new WarpSettings { Mode = WarpMode.Off, Amount = 0.8f };
            recipe.Routes.Add(new ModRoute(ModSource.Lfo, ModTarget.Warp, 0.5f));
            recipe.Routes.Add(new ModRoute(ModSource.Envelope, ModTarget.Warp, 0.5f));
            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void SyncOnSourcesWithoutWarp_RendersBitIdentical()
        {
            using var reference = new SfxRenderer();
            reference.Render(UnsupportedRecipe());

            var recipe = UnsupportedRecipe();
            foreach (var layer in recipe.Layers) layer.Warp = new WarpSettings { Mode = WarpMode.Sync, Amount = 0.8f };
            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        // Every continuous source routed to Warp with depths that cancel, or are zero, adds nothing
        // to a synced layer, and the float4 lanes beside it keep their output.
        [Test]
        public void ZeroWarpLaneDepths_RenderBitIdentical()
        {
            using var reference = new SfxRenderer();
            reference.Render(LaneRecipe());

            var recipe = LaneRecipe();
            foreach (var source in ContinuousSources)
            {
                recipe.Routes.Add(new ModRoute(source, ModTarget.Warp, 0.3f));
                recipe.Routes.Add(new ModRoute(source, ModTarget.Warp, -0.3f));
                recipe.Routes.Add(new ModRoute(source, ModTarget.Warp, 0f));
            }

            ModMatrix.Evaluate(recipe, recipe.Seed, _modulation, recipe.Layers.Count);
            Assert.AreEqual(0f, _modulation[0].LfoWarpDepth);
            Assert.AreEqual(0f, _modulation[0].Random3WarpDepth);

            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void ContinuousWarpRoute_FillsOnlyTheWarpLane()
        {
            var recipe = Recipe(500f, SyncLayer(SourceType.Oscillator, Waveform.Saw, 0f));
            recipe.Routes = new List<ModRoute> { new(ModSource.Env2, ModTarget.Warp, 0.7f) { Layer = 0 } };

            ModMatrix.Evaluate(recipe, recipe.Seed, _modulation, 1);

            Assert.AreEqual(0.7f, _modulation[0].Env2WarpDepth);
            Assert.AreEqual(float4.zero, _modulation[0].Env2Depth);
            Assert.AreEqual(0f, _modulation[0].Warp);
        }

        [Test]
        public void SyncAmountZero_OnWavetableMatchesWarpOff([Values(1, 3)] int voices)
        {
            var off = WavetableLayer(voices);
            using var reference = new SfxRenderer();
            reference.Render(Recipe(400f, off));

            var synced = WavetableLayer(voices);
            synced.Warp = new WarpSettings { Mode = WarpMode.Sync };
            using var rendered = new SfxRenderer();
            rendered.Render(Recipe(400f, synced));

            reference.LayerFrameRange(0, out var start, out var end);
            var a = reference.LayerOutput(0);
            var b = rendered.LayerOutput(0);
            for (var i = start * SfxRenderer.Channels; i < end * SfxRenderer.Channels; i++)
                Assert.AreEqual(a[i], b[i], 1e-5f, $"Sample {i}");
        }

        // The Classic tables carry the PolyBLEP shapes at their own amplitude.
        [Test]
        public void SyncAmountZero_OnOscillatorIsAsLoudAsWarpOff([Values] Waveform waveform)
        {
            using var reference = new SfxRenderer();
            reference.Render(Recipe(400f, SyncLayer(SourceType.Oscillator, waveform, 0f, WarpMode.Off)));
            using var rendered = new SfxRenderer();
            rendered.Render(Recipe(400f, SyncLayer(SourceType.Oscillator, waveform, 0f)));

            Assert.AreEqual(0f, AudioMath.LinearToDb(LayerRms(rendered) / LayerRms(reference)), 0.25f);
        }

        [TestCase(SourceType.Oscillator, Waveform.Sine)]
        [TestCase(SourceType.Oscillator, Waveform.Saw)]
        [TestCase(SourceType.Wavetable, Waveform.Sine)]
        public void Centroid_RisesWithSyncAmount(SourceType source, Waveform waveform)
        {
            using var analyzer = new SfxAnalyzer();
            var previous = 0f;
            foreach (var amount in new[] { 0f, 0.5f, 1f })
            {
                using var renderer = new SfxRenderer();
                renderer.Render(Recipe(500f, SyncLayer(source, waveform, amount)));
                var centroid = LayerCentroid(renderer, analyzer, 0f, 1f);

                if (amount > 0f) Assert.Greater(centroid, previous * 1.15f, $"Amount {amount}");
                previous = centroid;
            }
        }

        [Test]
        public void Env2RampOnWarp_BrightensLaterWindows()
        {
            var recipe = Recipe(600f, SyncLayer(SourceType.Oscillator, Waveform.Saw, 0f));
            recipe.Env2 = new Curve
            {
                Unit = CurveUnit.Gain,
                Points = new List<Breakpoint> { new(0f, 0f), new(1f, 1f) },
            };
            recipe.Routes = new List<ModRoute> { new(ModSource.Env2, ModTarget.Warp, 1f) };
            using var renderer = new SfxRenderer();
            using var analyzer = new SfxAnalyzer();

            renderer.Render(recipe);
            var early = LayerCentroid(renderer, analyzer, 0f, 0.25f);
            var late = LayerCentroid(renderer, analyzer, 0.75f, 1f);

            Assert.Greater(late, early * 1.2f);
        }

        // A macro route moves Warp exactly like turning the Amount knob by the same amount.
        [Test]
        public void MacroRouteOnWarp_IsAStaticOffset()
        {
            var routed = Recipe(400f, SyncLayer(SourceType.Oscillator, Waveform.Saw, 0.25f));
            routed.Macros.Size = 1f;
            routed.Routes = new List<ModRoute> { new(ModSource.Size, ModTarget.Warp, 0.5f) };
            ModMatrix.Evaluate(routed, routed.Seed, _modulation, 1);
            Assert.AreEqual(0.5f, _modulation[0].Warp);
            Assert.AreEqual(0f, _modulation[0].LfoWarpDepth);

            var turned = Recipe(400f, SyncLayer(SourceType.Oscillator, Waveform.Saw, 0.75f));
            turned.Routes = new List<ModRoute>();

            using var a = new SfxRenderer();
            using var b = new SfxRenderer();
            a.Render(routed);
            b.Render(turned);

            AssertIdentical(b, a);
        }

        [Test]
        public void SyncAtFullAmount_IsFiniteAndBounded(
            [Values] Waveform waveform,
            [Values(-36f, 0f, 30f)] float pitch)
        {
            AssertFiniteAndBounded(SyncLayer(SourceType.Oscillator, waveform, 1f, pitch: pitch));
        }

        [Test]
        public void SyncAtFullAmount_OnWavetableIsFiniteAndBounded(
            [Values] WavetableBank bank,
            [Values(-36f, 0f, 30f)] float pitch)
        {
            var layer = SyncLayer(SourceType.Wavetable, Waveform.Sine, 1f, pitch: pitch);
            layer.Source.Wavetable = new WavetableSettings { Bank = bank, Position = 0.7f };
            AssertFiniteAndBounded(layer);
        }

        [Test]
        public void Randomize_KeepsWarpByLayerIndex()
        {
            var template = ForgeTestRecipes.LoadTemplates()[SfxCategory.Laser];
            var recipe = ScriptableObject.CreateInstance<SfxRecipe>();
            _created.Add(recipe);
            SfxRandomizer.Randomize(recipe, template, 1u);

            var warp = new WarpSettings { Mode = WarpMode.Sync, Amount = 0.6f };
            recipe.Layers[0].Warp = warp;

            SfxRandomizer.Randomize(recipe, template, 2u);
            Assert.AreEqual(warp, recipe.Layers[0].Warp);

            SfxRandomizer.Mutate(recipe, 3u);
            Assert.AreEqual(warp, recipe.Layers[0].Warp);
        }

        private void AssertFiniteAndBounded(Layer layer)
        {
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(300f, layer));

            renderer.LayerFrameRange(0, out var start, out var end);
            var output = renderer.LayerOutput(0);
            var peak = 0f;
            for (var i = start * SfxRenderer.Channels; i < end * SfxRenderer.Channels; i++)
            {
                Assert.IsTrue(math.isfinite(output[i]), $"Sample {i}");
                peak = math.max(peak, math.abs(output[i]));
            }

            // Full sync on a high note leaves only the bank's fundamental under Nyquist, and Formant's
            // fundamental sits near 0.023 of its full-band peak.
            Assert.Greater(peak, 0.01f);
            Assert.LessOrEqual(peak / AudioMath.ConstantPowerPan(0f).x, 1.5f);
        }

        private static Layer SyncLayer(SourceType source, Waveform waveform, float amount, WarpMode mode = WarpMode.Sync,
            float pitch = -24f)
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(waveform, pitch));
            layer.Source = SourceSettings.Default;
            layer.Source.Type = source;
            layer.Source.Oscillator.Waveform = waveform;
            layer.Source.Wavetable.Position = 0.5f;
            layer.DecayMs = 2000f;
            layer.Warp = new WarpSettings { Mode = mode, Amount = amount };
            return layer;
        }

        private static Layer WavetableLayer(int voices)
        {
            var layer = ForgeTestRecipes.Oscillator(Waveform.Sine, 0f, -6f);
            layer.Source.Type = SourceType.Wavetable;
            layer.Source.Wavetable = new WavetableSettings { Bank = WavetableBank.Metallic, Position = 0.3f };
            layer.Filter = new FilterSettings { Type = FilterType.LowPass, CutoffHz = 4000f, Resonance = 0.3f };
            layer.Unison = new UnisonSettings { Voices = voices, DetuneCents = 25f, Spread = 0.8f };
            return layer;
        }

        private SfxRecipe TonalRecipe(int voices)
        {
            var saw = ForgeTestRecipes.Oscillator(Waveform.Saw, -5f, -6f);
            saw.Filter = new FilterSettings { Type = FilterType.LowPass, CutoffHz = 3000f, Resonance = 0.3f };
            var square = ForgeTestRecipes.Oscillator(Waveform.Square, 3f, -9f);
            var wavetable = WavetableLayer(1);
            foreach (var layer in new[] { saw, square, wavetable })
                layer.Unison = new UnisonSettings { Voices = voices, DetuneCents = 20f, Spread = 0.5f };
            return Recipe(400f, saw, square, wavetable);
        }

        private SfxRecipe UnsupportedRecipe()
        {
            var fm = ForgeTestRecipes.Oscillator(Waveform.Sine, 0f, -9f);
            fm.Source.Type = SourceType.FM;
            fm.Source.Fm = FmSettings.Default;
            fm.Unison = new UnisonSettings { Voices = 3, DetuneCents = 20f, Spread = 0.5f };
            var shepard = ForgeTestRecipes.Oscillator(Waveform.Sine, 0f, -9f);
            shepard.Source.Type = SourceType.Shepard;
            shepard.Source.Shepard = ShepardSettings.Default;
            return Recipe(400f, fm, shepard, ForgeTestRecipes.Noise(NoiseColor.Pink, -12f));
        }

        private SfxRecipe LaneRecipe()
        {
            var saw = SyncLayer(SourceType.Oscillator, Waveform.Saw, 0.4f);
            var wavetable = SyncLayer(SourceType.Wavetable, Waveform.Sine, 0.6f);
            wavetable.Unison = new UnisonSettings { Voices = 3, DetuneCents = 20f, Spread = 0.5f };
            var recipe = Recipe(400f, saw, wavetable);
            recipe.Random.Mode = RandomMode.Smooth;
            recipe.Random2.Mode = RandomMode.SampleHold;
            recipe.Random3.Mode = RandomMode.Smooth;
            recipe.Routes = new List<ModRoute>
            {
                new(ModSource.Lfo, ModTarget.Pitch, 1f),
                new(ModSource.Random, ModTarget.Cutoff, 0.5f),
            };
            return recipe;
        }

        private SfxRecipe Recipe(float lengthMs, params Layer[] layers)
        {
            var recipe = ForgeTestRecipes.Create(lengthMs, layers);
            recipe.Seed = 42;
            _created.Add(recipe);
            return recipe;
        }

        private static float LayerCentroid(SfxRenderer renderer, SfxAnalyzer analyzer, float from, float to)
        {
            renderer.LayerFrameRange(0, out var start, out var end);
            var first = start + (int)((end - start) * from);
            var last = start + (int)((end - start) * to);
            var window = renderer.LayerOutput(0).GetSubArray(first * SfxRenderer.Channels,
                (last - first) * SfxRenderer.Channels);
            return analyzer.Analyze(window, SfxRenderer.Channels, SampleRate).SpectralCentroidHz;
        }

        private static float LayerRms(SfxRenderer renderer)
        {
            renderer.LayerFrameRange(0, out var start, out var end);
            var output = renderer.LayerOutput(0);
            var sum = 0.0;
            for (var frame = start; frame < end; frame++) sum += output[frame * 2] * output[frame * 2];
            return (float)math.sqrt(sum / (end - start));
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
