using DataKeeper.Forge.Dsp;
using NUnit.Framework;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge.Tests
{
    public class FilterTests
    {
        private const int SampleRate = 48000;

        [Test]
        public void ExtremeModulation_StaysFiniteAndBounded(
            [Values(FilterType.LowPass, FilterType.HighPass, FilterType.BandPass, FilterType.Notch)] FilterType type)
        {
            var filter = new StateVariableFilter();
            var random = new Random(99u);
            var k = StateVariableFilter.ResonanceToK(1f);
            var peak = 0f;

            for (var i = 0; i < SampleRate * 2; i++)
            {
                // Cutoff jumps across the full range every sample at maximum resonance.
                var cutoff = AudioMath.MapLog(random.NextFloat(), ParamRanges.CutoffMin, ParamRanges.CutoffMax);
                var g = StateVariableFilter.CutoffToG(cutoff, SampleRate);
                var y = filter.Process(random.NextFloat(-1f, 1f), g, k, type);

                Assert.IsTrue(float.IsFinite(y), $"Non-finite sample at {i}");
                peak = Mathf.Max(peak, Mathf.Abs(y));
            }

            Assert.Less(peak, 100f);
        }

        [Test]
        public void LowPass_AttenuatesAboveCutoff()
        {
            Assert.Less(SineGain(FilterType.LowPass, 200f, 8000f), 0.01f);
            Assert.Greater(SineGain(FilterType.LowPass, 8000f, 200f), 0.9f);
        }

        [Test]
        public void HighPass_AttenuatesBelowCutoff()
        {
            Assert.Less(SineGain(FilterType.HighPass, 8000f, 200f), 0.01f);
            Assert.Greater(SineGain(FilterType.HighPass, 200f, 8000f), 0.9f);
        }

        [Test]
        public void BandPass_PeaksAtUnityOnCutoff()
        {
            Assert.AreEqual(1f, SineGain(FilterType.BandPass, 1000f, 1000f, 0.8f), 0.05f);
            Assert.Less(SineGain(FilterType.BandPass, 1000f, 50f, 0.8f), 0.1f);
        }

        [Test]
        public void Off_PassesThrough()
        {
            var filter = new StateVariableFilter();
            Assert.AreEqual(0.37f, filter.Process(0.37f, 0.5f, 1f, FilterType.Off));
        }

        // RMS ratio after the filter has settled; sampled peaks would under-read high frequencies.
        private static float SineGain(FilterType type, float cutoff, float frequency, float resonance = 0f)
        {
            var filter = new StateVariableFilter();
            var g = StateVariableFilter.CutoffToG(cutoff, SampleRate);
            var k = StateVariableFilter.ResonanceToK(resonance);
            var inputEnergy = 0.0;
            var outputEnergy = 0.0;

            for (var i = 0; i < SampleRate; i++)
            {
                var x = Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate);
                var y = filter.Process(x, g, k, type);
                if (i < SampleRate / 2) continue;

                inputEnergy += x * x;
                outputEnergy += y * y;
            }

            return (float)System.Math.Sqrt(outputEnergy / inputEnergy);
        }
    }
}
