using System.Collections.Generic;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class ShepardTests
    {
        private const int SampleRate = SfxRecipe.DefaultSampleRate;

        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        [Test]
        public void Defaults_AreRateOneWidthHalfEightPartials()
        {
            var shepard = new Layer().Source.Shepard;
            Assert.AreEqual(1f, shepard.RateOctaves);
            Assert.AreEqual(0.5f, shepard.Width);
            Assert.AreEqual(8, shepard.Partials);
            Assert.AreEqual(ShepardSettings.Default, SourceSettings.Default.Shepard);
        }

        // Shepard-free recipes take none of the new code: settings on other sources change nothing.
        [Test]
        public void ShepardSettingsOnOtherSources_RenderBitIdentical()
        {
            using var reference = new SfxRenderer();
            reference.Render(MixedRecipe());

            var recipe = MixedRecipe();
            foreach (var layer in recipe.Layers)
                layer.Source.Shepard = new ShepardSettings { RateOctaves = -3f, Width = 0.9f, Partials = 5 };
            recipe.Layers[0].Source.Shepard = default;

            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void ZeroSettings_RenderNonSilentFiniteAndAsTheMinimums()
        {
            var layer = ShepardLayer(0f);
            layer.Source.Shepard = default;
            using var zero = new SfxRenderer();
            zero.Render(Recipe(1000f, layer));

            zero.LayerFrameRange(0, out var start, out var end);
            var output = zero.LayerOutput(0);
            var peak = 0f;
            for (var i = start * SfxRenderer.Channels; i < end * SfxRenderer.Channels; i++)
            {
                Assert.IsTrue(math.isfinite(output[i]), $"Sample {i}");
                peak = math.max(peak, math.abs(output[i]));
            }

            Assert.Greater(peak, 0.1f);

            using var minimums = new SfxRenderer();
            minimums.Render(Recipe(1000f, ShepardLayer(0f, 0f, ShepardSettings.MinPartials)));
            AssertIdentical(minimums, zero);
        }

        [Test]
        public void Unison_IsIgnored()
        {
            using var reference = new SfxRenderer();
            reference.Render(Recipe(500f, ShepardLayer(1f)));

            var layer = ShepardLayer(1f);
            layer.Unison = new UnisonSettings { Voices = 5, DetuneCents = 40f, Spread = 1f };
            using var rendered = new SfxRenderer();
            rendered.Render(Recipe(500f, layer));

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void RateZero_SpectrumPeaksOnlyAtOctavesOfTheCentre()
        {
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(800f, ShepardLayer(0f)));

            const int size = 32768;
            var power = PowerSpectrum(renderer, 0, size, size);
            var binHz = (float)SampleRate / size;
            var total = 0.0;
            var onOctaves = 0.0;
            var centre = 0.0;
            var octaveAbove = 0.0;
            for (var bin = 1; bin < power.Length; bin++)
            {
                total += power[bin];
                var octaves = math.log2(bin * binHz / AudioMath.ReferenceHz);
                var nearest = math.round(octaves);
                if (math.abs(bin * binHz - AudioMath.ReferenceHz * math.exp2(nearest)) > 4f * binHz) continue;

                onOctaves += power[bin];
                if (nearest == 0f) centre += power[bin];
                if (nearest == 1f) octaveAbove += power[bin];
            }

            Assert.Greater(onOctaves / total, 0.999);
            Assert.Greater(centre / total, 0.1, "the centre partial");
            Assert.Greater(octaveAbove / total, 0.1, "a stack, not a single sine");
        }

        // One octave of travel lands on the same partials at the same levels; half an octave does not.
        [Test]
        public void RateOne_SpectrumRepeatsEverySecond([Values(1f, -1f)] float rate)
        {
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(3000f, ShepardLayer(rate, decayMs: 3000f)));

            const int window = SampleRate / 10;
            const int size = 8192;
            var first = PowerSpectrum(renderer, SampleRate / 2 - window / 2, window, size);
            var octaveLater = PowerSpectrum(renderer, SampleRate * 3 / 2 - window / 2, window, size);
            var twoOctavesLater = PowerSpectrum(renderer, SampleRate * 5 / 2 - window / 2, window, size);
            var halfOctaveLater = PowerSpectrum(renderer, SampleRate - window / 2, window, size);

            Assert.Greater(MagnitudeCorrelation(first, octaveLater), 0.95);
            Assert.Greater(MagnitudeCorrelation(first, twoOctavesLater), 0.95);
            Assert.Less(MagnitudeCorrelation(first, halfOctaveLater), 0.5);
        }

        // A partial dropped or added at a non-zero level, or a phase lost in the slot rotation,
        // would jump far past the steepest slope the partials can produce.
        [Test]
        public void Wraps_DoNotClick([Values(4f, -4f)] float rate)
        {
            const int partials = 4;
            const float pitch = -36f;
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(1000f, ShepardLayer(rate, 1f, partials, pitch)));

            var topIncrement = AudioMath.SemitonesToHz(pitch) * math.exp2(partials * 0.5f) / SampleRate;
            var bound = 2f * math.PI * topIncrement * math.sqrt(partials);

            renderer.LayerFrameRange(0, out var start, out var end);
            var output = renderer.LayerOutput(0);
            var largestStep = 0f;
            for (var frame = start + 1; frame < end; frame++)
                largestStep = math.max(largestStep, math.abs(output[frame * 2] - output[(frame - 1) * 2]));

            Assert.Less(largestStep, bound);
        }

        [Test]
        public void HighCentre_PutsNoEnergyAboveTheFadeLimit([Values(0f, 1f)] float rate)
        {
            // 5.9 kHz: the partial two octaves up sits at 23.7 kHz, inside the band that must stay empty.
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(800f, ShepardLayer(rate, 1f, pitch: 45f)));

            const int size = 32768;
            var power = PowerSpectrum(renderer, 0, size, size);
            var limitBin = (int)(0.45f * size);
            var total = 0.0;
            var above = 0.0;
            for (var bin = 1; bin < power.Length; bin++)
            {
                total += power[bin];
                if (bin >= limitBin) above += power[bin];
            }

            Assert.Greater(total, 0.0);
            Assert.Less(above / total, 1e-6);
        }

        [Test]
        public void FullStack_PeaksNearOne([Values(0f, 0.5f, 1f)] float width)
        {
            using var renderer = new SfxRenderer();
            renderer.Render(Recipe(1000f, ShepardLayer(1f, width, ShepardSettings.MaxPartials)));

            renderer.LayerFrameRange(0, out var start, out var end);
            var output = renderer.LayerOutput(0);
            var peak = 0f;
            for (var frame = start; frame < end; frame++) peak = math.max(peak, math.abs(output[frame * 2]));

            Assert.That(peak / AudioMath.ConstantPowerPan(0f).x, Is.InRange(0.6f, 1.25f));
        }

        [Test]
        public void Randomize_NeverGeneratesShepardAndFillsItsDefaults()
        {
            foreach (var template in ForgeTestRecipes.LoadTemplates().Values)
            {
                for (var seed = 1u; seed <= 8u; seed++)
                {
                    var recipe = ScriptableObject.CreateInstance<SfxRecipe>();
                    _created.Add(recipe);
                    SfxRandomizer.Randomize(recipe, template, seed);

                    foreach (var layer in recipe.Layers)
                    {
                        Assert.AreNotEqual(SourceType.Shepard, layer.Source.Type, $"{template.Category} seed {seed}");
                        Assert.AreEqual(ShepardSettings.Default, layer.Source.Shepard);
                    }
                }
            }
        }

        private static Layer ShepardLayer(float rate, float width = 0.5f, int partials = 8, float pitch = 0f,
            float decayMs = 1000f)
        {
            var layer = ForgeTestRecipes.Flat(new Layer
            {
                Source = SourceSettings.Default,
                Pitch = pitch,
                LevelDb = 0f,
                DecayMs = decayMs,
            });
            layer.Source.Type = SourceType.Shepard;
            layer.Source.Shepard = new ShepardSettings { RateOctaves = rate, Width = width, Partials = partials };
            return layer;
        }

        private SfxRecipe MixedRecipe()
        {
            var saw = ForgeTestRecipes.Oscillator(Waveform.Saw, -5f, -6f);
            var wavetable = ForgeTestRecipes.Oscillator(Waveform.Sine, 7f, -9f);
            wavetable.Source.Type = SourceType.Wavetable;
            wavetable.Source.Wavetable.Position = 0.4f;
            var fm = ForgeTestRecipes.Oscillator(Waveform.Sine, 0f, -9f);
            fm.Source.Type = SourceType.FM;
            fm.Source.Fm = FmSettings.Default;
            fm.Unison = new UnisonSettings { Voices = 3, DetuneCents = 20f, Spread = 0.5f };
            return Recipe(400f, saw, wavetable, fm, ForgeTestRecipes.Noise(NoiseColor.Pink, -12f));
        }

        private SfxRecipe Recipe(float lengthMs, params Layer[] layers)
        {
            var recipe = ForgeTestRecipes.Create(lengthMs, layers);
            recipe.Seed = 42;
            _created.Add(recipe);
            return recipe;
        }

        // Left channel of layer 0, Hann-windowed and zero-padded to `size`.
        private static double[] PowerSpectrum(SfxRenderer renderer, int startFrame, int length, int size)
        {
            var output = renderer.LayerOutput(0);
            var re = new double[size];
            var im = new double[size];
            for (var i = 0; i < length; i++)
            {
                var hann = 0.5 - 0.5 * System.Math.Cos(2.0 * System.Math.PI * i / (length - 1));
                re[i] = output[(startFrame + i) * SfxRenderer.Channels] * hann;
            }

            Fft.Transform(re, im, false);

            var power = new double[size / 2];
            for (var bin = 0; bin < power.Length; bin++) power[bin] = re[bin] * re[bin] + im[bin] * im[bin];
            return power;
        }

        private static double MagnitudeCorrelation(double[] a, double[] b)
        {
            var n = a.Length;
            var meanA = 0.0;
            var meanB = 0.0;
            for (var i = 0; i < n; i++)
            {
                meanA += System.Math.Sqrt(a[i]);
                meanB += System.Math.Sqrt(b[i]);
            }

            meanA /= n;
            meanB /= n;
            var covariance = 0.0;
            var varianceA = 0.0;
            var varianceB = 0.0;
            for (var i = 0; i < n; i++)
            {
                var da = System.Math.Sqrt(a[i]) - meanA;
                var db = System.Math.Sqrt(b[i]) - meanB;
                covariance += da * db;
                varianceA += da * da;
                varianceB += db * db;
            }

            return covariance / System.Math.Sqrt(varianceA * varianceB);
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
