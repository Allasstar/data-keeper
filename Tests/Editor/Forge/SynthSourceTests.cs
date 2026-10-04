using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge.Tests
{
    public class SynthSourceTests
    {
        private const int SampleRate = 48000;

        // ── FFT ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Fft_RoundTripRestoresSignal()
        {
            var random = new Random(3u);
            var re = new double[256];
            var im = new double[256];
            var original = new double[256];
            for (var i = 0; i < re.Length; i++) original[i] = re[i] = random.NextFloat(-1f, 1f);

            Fft.Transform(re, im, false);
            Fft.Transform(re, im, true);

            for (var i = 0; i < re.Length; i++)
            {
                Assert.AreEqual(original[i], re[i], 1e-9);
                Assert.AreEqual(0.0, im[i], 1e-9);
            }
        }

        [Test]
        public void Fft_SineLandsInItsBin()
        {
            var re = new double[512];
            var im = new double[512];
            for (var i = 0; i < re.Length; i++) re[i] = math.sin(2.0 * System.Math.PI * 12 * i / re.Length);

            Fft.Transform(re, im, false);

            for (var bin = 1; bin < re.Length / 2; bin++)
            {
                var magnitude = math.sqrt(re[bin] * re[bin] + im[bin] * im[bin]);
                if (bin == 12) Assert.AreEqual(re.Length / 2.0, magnitude, 1e-6);
                else Assert.Less(magnitude, 1e-6, $"Leakage into bin {bin}");
            }
        }

        // ── Wavetables ──────────────────────────────────────────────────────────────

        [TestCase(20f)]
        [TestCase(110f)]
        [TestCase(440f)]
        [TestCase(2000f)]
        [TestCase(8000f)]
        public void Wavetable_MipLevelKeepsHarmonicsBelowNyquist(float frequency)
        {
            var level = Wavetables.MipLevel(frequency / SampleRate);
            var highestHarmonic = (Wavetables.MaxHarmonics >> level) - 1;

            if (highestHarmonic >= 1)
                Assert.LessOrEqual(highestHarmonic * frequency, SampleRate / 2f, $"level {level}");
        }

        [Test]
        public void Wavetable_TablesAreFiniteAndNormalised()
        {
            using var tables = new NativeArray<float>(Wavetables.TotalSize, Allocator.Temp);
            Wavetables.BuildAll(tables);

            for (var bank = 0; bank < Wavetables.BankCount; bank++)
            {
                for (var frame = 0; frame < Wavetables.Frames; frame++)
                {
                    for (var level = 0; level < Wavetables.Levels; level++)
                    {
                        var offset = Wavetables.TableOffset(bank * Wavetables.BankSize, frame, level);
                        var peak = 0f;
                        for (var i = 0; i <= Wavetables.TableSize; i++)
                        {
                            Assert.IsTrue(float.IsFinite(tables[offset + i]));
                            peak = math.max(peak, math.abs(tables[offset + i]));
                        }

                        var label = $"{(WavetableBank)bank} frame {frame} level {level}";
                        if (level == 0) Assert.AreEqual(1f, peak, 1e-4f, label);
                        else Assert.LessOrEqual(peak, 1.3f, label);
                        Assert.AreEqual(tables[offset], tables[offset + Wavetables.TableSize], label + " guard");
                    }
                }
            }
        }

        [Test]
        public void WavetableOscillator_StaysBounded(
            [Values] WavetableBank bank,
            [Values(0f, 0.5f, 1f)] float position,
            [Values(55f, 880f, 9000f)] float frequency)
        {
            using var tables = new NativeArray<float>(Wavetables.TotalSize, Allocator.Temp);
            Wavetables.BuildAll(tables);
            var oscillator = new WavetableOscillator();

            for (var i = 0; i < SampleRate / 4; i++)
            {
                var y = oscillator.Next(tables, Wavetables.BankOffset(bank), position, frequency / SampleRate);
                Assert.IsTrue(float.IsFinite(y));
                Assert.LessOrEqual(math.abs(y), 1.3f);
            }
        }

        // ── FM ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Fm_ZeroIndexIsASine()
        {
            var fm = new FmOperator();
            var phase = 0f;
            const float increment = 440f / SampleRate;

            for (var i = 0; i < 2000; i++)
            {
                Assert.AreEqual(math.sin(2f * math.PI * phase), fm.Next(increment, 3.5f, 0f), 1e-6f);
                phase = math.frac(phase + increment);
            }
        }

        [Test]
        public void Fm_ExtremeSettingsStayFinite()
        {
            var fm = new FmOperator();
            for (var i = 0; i < SampleRate; i++)
            {
                var y = fm.Next(0.2f, FmSettings.MaxRatio, FmSettings.MaxIndex);
                Assert.IsTrue(float.IsFinite(y));
                Assert.LessOrEqual(math.abs(y), 1f);
            }
        }

        [Test]
        public void Renderer_FmAndWavetableLayersProduceSound([Values(SourceType.FM, SourceType.Wavetable)] SourceType type)
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine));
            layer.Source.Type = type;
            layer.Source.Wavetable = new WavetableSettings { Bank = WavetableBank.Formant, Position = 0.6f };
            layer.Source.Fm = FmSettings.Default;
            var recipe = ForgeTestRecipes.Create(200f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var output = renderer.Output;
            var peak = 0f;
            for (var i = 0; i < output.Length; i++)
            {
                Assert.IsTrue(float.IsFinite(output[i]));
                peak = math.max(peak, math.abs(output[i]));
            }

            Assert.Greater(peak, 0.2f);
            Object.DestroyImmediate(recipe);
        }

        // ── Samples ─────────────────────────────────────────────────────────────────

        [Test]
        public void Sample_PlaysForwardReverseAndOffset([Values] SampleInterpolation interpolation)
        {
            var clip = SmoothClip(4800);
            var data = new float[clip.samples];
            clip.GetData(data, 0);

            var forward = RenderSample(clip, interpolation, false, 0f, 0f);
            var reverse = RenderSample(clip, interpolation, true, 0f, 0f);
            var offset = RenderSample(clip, interpolation, false, 10f, 0f);
            var gain = AudioMath.ConstantPowerPan(0f).x;

            for (var i = 0; i < 4000; i++)
            {
                Assert.AreEqual(data[i] * gain, forward[i * 2], 1e-5f, $"forward {i}");
                Assert.AreEqual(data[data.Length - 1 - i] * gain, reverse[i * 2], 1e-5f, $"reverse {i}");
                if (i < 4000 - 480) Assert.AreEqual(data[i + 480] * gain, offset[i * 2], 1e-5f, $"offset {i}");
            }

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void Sample_OctaveUpPlaysTwiceAsFast()
        {
            var clip = SmoothClip(9600);
            var data = new float[clip.samples];
            clip.GetData(data, 0);

            var output = RenderSample(clip, SampleInterpolation.Cubic, false, 0f, 12f);
            var gain = AudioMath.ConstantPowerPan(0f).x;

            for (var i = 0; i < 4000; i++)
                Assert.AreEqual(data[i * 2] * gain, output[i * 2], 1e-3f, $"frame {i}");

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void Sample_MissingClipIsSilent()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine));
            layer.Source.Type = SourceType.Sample;
            var recipe = ForgeTestRecipes.Create(100f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var output = renderer.Output;
            for (var i = 0; i < output.Length; i++) Assert.AreEqual(0f, output[i]);
            Object.DestroyImmediate(recipe);
        }

        // A low sine with a slow drift: smooth enough that interpolation error is tiny.
        private static AudioClip SmoothClip(int length)
        {
            var data = new float[length];
            for (var i = 0; i < length; i++)
                data[i] = 0.8f * math.sin(2f * math.PI * 110f * i / SampleRate) + 0.1f * i / length;

            var clip = AudioClip.Create("Forge Test Clip", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float[] RenderSample(AudioClip clip, SampleInterpolation interpolation, bool reverse,
            float startMs, float pitch)
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine, pitch));
            layer.DecayMs = 200f;
            layer.Source.Type = SourceType.Sample;
            layer.Source.Sample = new SampleSettings
            {
                Clip = clip, Reverse = reverse, StartMs = startMs, Interpolation = interpolation,
            };

            var recipe = ForgeTestRecipes.Create(200f, layer);
            recipe.Fx.Limiter.Enabled = false;
            using var renderer = new SfxRenderer();
            renderer.Render(recipe);

            var copy = renderer.Output.ToArray();
            Object.DestroyImmediate(recipe);
            return copy;
        }
    }
}
