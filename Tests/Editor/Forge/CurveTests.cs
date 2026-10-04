using System.Collections.Generic;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge.Tests
{
    public class CurveTests
    {
        private const int SampleRate = 48000;

        // ── Curve math ──────────────────────────────────────────────────────────────

        [Test]
        public void ManagedEvaluate_MatchesJobEvaluate()
        {
            var random = new Random(5u);
            var points = new List<Breakpoint> { new(0f, random.NextFloat()) };
            var time = 0f;
            for (var i = 0; i < 6; i++)
            {
                time += random.NextFloat(0.05f, 0.15f);
                points.Add(new Breakpoint(time, random.NextFloat(), random.NextFloat(-1f, 1f)));
            }
            points.Add(new Breakpoint(1f, random.NextFloat()));

            using var native = new NativeArray<Breakpoint>(points.ToArray(), Allocator.Temp);
            for (var i = 0; i <= 200; i++)
            {
                var t = i / 200f;
                Assert.AreEqual(CurveEvaluator.Evaluate(native, 0, native.Length, t), CurveEvaluator.Evaluate(points, t));
            }
        }

        [Test]
        public void DefaultFlatCurves_MapToZeroOffset()
        {
            foreach (var curve in new[] { Curve.DefaultPitch(), Curve.DefaultCutoff(), Curve.DefaultPan() })
            {
                var value = CurveEvaluator.Evaluate(curve.Points, 0.37f);
                Assert.AreEqual(0f, Mathf.Lerp(curve.Min, curve.Max, value), curve.Unit.ToString());
            }
        }

        [Test]
        public void Simplify_StraightLineCollapsesToEndpoints()
        {
            var samples = new List<Breakpoint>();
            for (var i = 0; i <= 100; i++) samples.Add(new Breakpoint(i / 100f, 0.2f + 0.5f * i / 100f));

            var simplified = CurveSimplifier.Simplify(samples, 0.005f);

            Assert.AreEqual(2, simplified.Count);
            Assert.AreEqual(0f, simplified[0].Time);
            Assert.AreEqual(1f, simplified[1].Time);
        }

        [Test]
        public void Simplify_KeepsCornersAndStaysWithinEpsilon()
        {
            var samples = new List<Breakpoint>();
            for (var i = 0; i <= 100; i++)
            {
                var t = i / 100f;
                samples.Add(new Breakpoint(t, t < 0.5f ? t * 2f : 2f - t * 2f));
            }

            const float epsilon = 0.01f;
            var simplified = CurveSimplifier.Simplify(samples, epsilon);

            Assert.AreEqual(3, simplified.Count);
            Assert.AreEqual(0.5f, simplified[1].Time, 1e-4f);
            foreach (var sample in samples)
                Assert.AreEqual(sample.Value, CurveEvaluator.Evaluate(simplified, sample.Time), epsilon * 2f);
        }

        [TestCase(HarmonyMode.Major, 5f, 4f)]
        [TestCase(HarmonyMode.Major, 11.6f, 12f)]
        [TestCase(HarmonyMode.Major, -1f, 0f)]
        [TestCase(HarmonyMode.Minor, 4f, 3f)]
        [TestCase(HarmonyMode.Fifths, 9f, 7f)]
        [TestCase(HarmonyMode.Dissonant, 3f, 1f)]
        [TestCase(HarmonyMode.Unison, 5f, 0f)]
        [TestCase(HarmonyMode.Unison, 7f, 12f)]
        public void HarmonySnap_PicksNearestScaleDegree(HarmonyMode mode, float input, float expected)
        {
            Assert.AreEqual(expected, HarmonySets.Snap(input, mode), 1e-4f);
        }

        // ── Curve-driven rendering ──────────────────────────────────────────────────

        [Test]
        public void FlatPitchCurve_PlaysLayerPitch()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine));
            layer.DecayMs = 1000f;
            var recipe = ForgeTestRecipes.Create(1000f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var crossings = ZeroCrossings(renderer.Output, 0, renderer.FrameCount);
            Assert.AreEqual(880, crossings, 3, "A4 for one second crosses zero ~880 times");
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void PitchCurve_SweepsUp()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine));
            layer.DecayMs = 1000f;
            layer.PitchCurve = Ramp(-12f, 12f, CurveUnit.Semitones);
            var recipe = ForgeTestRecipes.Create(1000f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var quarter = renderer.FrameCount / 4;
            var early = ZeroCrossings(renderer.Output, 0, quarter);
            var late = ZeroCrossings(renderer.Output, quarter * 3, quarter);
            Assert.Greater(late, early * 2.5f);
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void PanCurve_MovesLeftToRight()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.White));
            layer.DecayMs = 500f;
            layer.PanCurve = Ramp(-1f, 1f, CurveUnit.Pan);
            var recipe = ForgeTestRecipes.Create(500f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var tenth = renderer.FrameCount / 10;
            ChannelEnergy(renderer.Output, 0, tenth, out var earlyLeft, out var earlyRight);
            ChannelEnergy(renderer.Output, renderer.FrameCount - tenth, tenth, out var lateLeft, out var lateRight);
            Assert.Greater(earlyLeft, earlyRight * 4f);
            Assert.Greater(lateRight, lateLeft * 4f);
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void CutoffCurve_OpensFilter()
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.White));
            layer.DecayMs = 500f;
            layer.Filter = new FilterSettings { Type = FilterType.LowPass, CutoffHz = 200f };
            layer.CutoffCurve = Ramp(0f, 6f, CurveUnit.Octaves);
            var recipe = ForgeTestRecipes.Create(500f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var tenth = renderer.FrameCount / 10;
            var early = Brightness(renderer.Output, 0, tenth);
            var late = Brightness(renderer.Output, renderer.FrameCount - tenth, tenth);
            Assert.Greater(late, early * 10f);
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void ExtremeCutoffCurve_StaysFiniteAndBounded()
        {
            var zigzag = new Curve { Min = -4f, Max = 4f, Unit = CurveUnit.Octaves };
            for (var i = 0; i <= 40; i++) zigzag.Points.Add(new Breakpoint(i / 40f, i % 2 == 0 ? 0f : 1f));

            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Noise(NoiseColor.White));
            layer.DecayMs = 1000f;
            layer.Filter = new FilterSettings { Type = FilterType.LowPass, CutoffHz = 1000f, Resonance = 1f };
            layer.CutoffCurve = zigzag;
            var recipe = ForgeTestRecipes.Create(1000f, layer);
            using var renderer = new SfxRenderer();

            renderer.Render(recipe);

            var output = renderer.Output;
            var peak = 0f;
            for (var i = 0; i < output.Length; i++)
            {
                Assert.IsTrue(float.IsFinite(output[i]), $"Non-finite sample at {i}");
                peak = Mathf.Max(peak, Mathf.Abs(output[i]));
            }

            Assert.Less(peak, 100f);
            Object.DestroyImmediate(recipe);
        }

        private static Curve Ramp(float min, float max, CurveUnit unit) => new()
        {
            Min = min,
            Max = max,
            Unit = unit,
            Points = new List<Breakpoint> { new(0f, 0f), new(1f, 1f) },
        };

        private static int ZeroCrossings(NativeArray<float> interleaved, int firstFrame, int frames)
        {
            var count = 0;
            for (var frame = firstFrame + 1; frame < firstFrame + frames; frame++)
            {
                var previous = interleaved[(frame - 1) * 2];
                var current = interleaved[frame * 2];
                if ((previous < 0f) != (current < 0f)) count++;
            }

            return count;
        }

        private static void ChannelEnergy(NativeArray<float> interleaved, int firstFrame, int frames,
            out float left, out float right)
        {
            left = 0f;
            right = 0f;
            for (var frame = firstFrame; frame < firstFrame + frames; frame++)
            {
                left += interleaved[frame * 2] * interleaved[frame * 2];
                right += interleaved[frame * 2 + 1] * interleaved[frame * 2 + 1];
            }
        }

        // Energy of the first difference: a crude high-frequency measure.
        private static float Brightness(NativeArray<float> interleaved, int firstFrame, int frames)
        {
            var energy = 0f;
            for (var frame = firstFrame + 1; frame < firstFrame + frames; frame++)
            {
                var delta = interleaved[frame * 2] - interleaved[(frame - 1) * 2];
                energy += delta * delta;
            }

            return energy;
        }
    }
}
