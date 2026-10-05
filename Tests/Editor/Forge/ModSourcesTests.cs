using System.Collections.Generic;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class ModSourcesTests
    {
        private const int SampleRate = 48000;

        private readonly LayerModulation[] _layers = new LayerModulation[SfxRecipe.MaxLayers];
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        [Test]
        public void NewSettings_DefaultToTodaysBehaviour()
        {
            var recipe = SineRecipe(500f);

            foreach (var lfo in new[] { recipe.Lfo, recipe.Lfo2, recipe.Lfo3 })
            {
                Assert.AreEqual(0f, lfo.Phase);
                Assert.AreEqual(LfoMode.Retrigger, lfo.Mode);
            }

            foreach (var random in new[] { recipe.Random, recipe.Random2, recipe.Random3 })
                Assert.AreEqual(RandomMode.Constant, random.Mode);
            foreach (var env in new[] { recipe.Env2, recipe.Env3 })
            {
                Assert.AreEqual(CurveUnit.Gain, env.Unit);
                Assert.AreEqual(0f, env.Min);
                Assert.AreEqual(1f, env.Max);
                Assert.AreEqual(2, env.Points.Count);
                Assert.AreEqual(0f, env.Points[0].Value);
                Assert.AreEqual(0f, env.Points[1].Value);
            }
        }

        [Test]
        public void ModSource_AppendsNewValuesWithoutRenumbering()
        {
            Assert.AreEqual(4, (int)ModSource.Lfo);
            Assert.AreEqual(5, (int)ModSource.Envelope);
            Assert.AreEqual(6, (int)ModSource.Random);
            Assert.AreEqual(7, (int)ModSource.Lfo2);
            Assert.AreEqual(8, (int)ModSource.Lfo3);
            Assert.AreEqual(9, (int)ModSource.Env2);
            Assert.AreEqual(10, (int)ModSource.Env3);
            Assert.AreEqual(11, (int)ModSource.Random2);
            Assert.AreEqual(12, (int)ModSource.Random3);
        }

        [Test]
        public void IsContinuous_RandomOnlyInMovingModes()
        {
            foreach (var source in new[] { ModSource.Lfo, ModSource.Lfo2, ModSource.Lfo3, ModSource.Envelope, ModSource.Env2, ModSource.Env3 })
            {
                Assert.IsTrue(ModTargets.IsContinuous(source, RandomMode.Constant), source.ToString());
                Assert.IsTrue(ModTargets.IsContinuous(source, RandomMode.Smooth), source.ToString());
            }

            foreach (var source in new[] { ModSource.Random, ModSource.Random2, ModSource.Random3 })
            {
                Assert.IsFalse(ModTargets.IsContinuous(source, RandomMode.Constant), source.ToString());
                Assert.IsTrue(ModTargets.IsContinuous(source, RandomMode.SampleHold), source.ToString());
                Assert.IsTrue(ModTargets.IsContinuous(source, RandomMode.Smooth), source.ToString());
            }

            Assert.IsFalse(ModTargets.IsContinuous(ModSource.Size, RandomMode.Smooth));
        }

        [Test]
        public void ExplicitDefaults_RenderBitIdenticalToUntouchedRecipe()
        {
            using var reference = new SfxRenderer();
            reference.Render(RoutedRecipe());

            var recipe = RoutedRecipe();
            recipe.Lfo.Phase = 0f;
            recipe.Lfo.Mode = LfoMode.Retrigger;
            recipe.Lfo2 = new LfoSettings();
            recipe.Lfo3 = new LfoSettings();
            recipe.Env2 = Curve.DefaultModEnvelope();
            recipe.Env3 = Curve.DefaultModEnvelope();
            recipe.Random = new RandomSettings { Mode = RandomMode.Constant };
            recipe.Random2 = new RandomSettings();
            recipe.Random3 = new RandomSettings();
            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void UnroutedNewSources_DoNotChangeTheRender()
        {
            using var reference = new SfxRenderer();
            reference.Render(RoutedRecipe());

            var recipe = RoutedRecipe();
            recipe.Lfo2 = new LfoSettings { Shape = Waveform.Square, RateHz = 13f, Phase = 0.3f, Mode = LfoMode.Free };
            recipe.Lfo3 = new LfoSettings { Shape = Waveform.Saw, RateHz = 0.7f, Phase = 0.9f };
            recipe.Env2 = Ramp(0f, 1f);
            recipe.Env3 = Ramp(1f, 0.2f);
            recipe.Random.RateHz = 17f;
            recipe.Random2 = new RandomSettings { Mode = RandomMode.Smooth, RateHz = 3f };
            recipe.Random3 = new RandomSettings { Mode = RandomMode.SampleHold, RateHz = 11f };
            using var rendered = new SfxRenderer();
            rendered.Render(recipe);

            AssertIdentical(reference, rendered);
        }

        [TestCase(ModSource.Lfo2)]
        [TestCase(ModSource.Lfo3)]
        public void ExtraLfoRoute_RendersLikeTheSameSettingsOnLfo1(ModSource source)
        {
            var settings = new LfoSettings { Shape = Waveform.Triangle, RateHz = 7f, Phase = 0.2f };

            var onLfo1 = SineRecipe(500f);
            onLfo1.Lfo = settings;
            onLfo1.Routes = new List<ModRoute> { new(ModSource.Lfo, ModTarget.Pitch, 3f), new(ModSource.Lfo, ModTarget.Pan, 0.5f) };
            using var reference = new SfxRenderer();
            reference.Render(onLfo1);

            var onOther = SineRecipe(500f);
            if (source == ModSource.Lfo2) onOther.Lfo2 = settings;
            else onOther.Lfo3 = settings;
            onOther.Routes = new List<ModRoute> { new(source, ModTarget.Pitch, 3f), new(source, ModTarget.Pan, 0.5f) };
            using var rendered = new SfxRenderer();
            rendered.Render(onOther);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void LfoDepthTarget_ScalesLfo1Only()
        {
            var recipe = SineRecipe(500f);
            recipe.Macros.Motion = 1f;
            recipe.Routes = new List<ModRoute>
            {
                new(ModSource.Motion, ModTarget.LfoDepth, 1f),
                new(ModSource.Lfo, ModTarget.Pitch, 2f),
                new(ModSource.Lfo2, ModTarget.Pitch, 2f),
                new(ModSource.Env2, ModTarget.Cutoff, 3f),
            };

            ModMatrix.Evaluate(recipe, 1u, _layers, 1);

            Assert.AreEqual(4f, _layers[0].LfoDepth.x, 1e-6f);
            Assert.AreEqual(2f, _layers[0].Lfo2Depth.x, 1e-6f);
            Assert.AreEqual(3f, _layers[0].Env2Depth.y, 1e-6f);
        }

        [Test]
        public void FreeMode_RunsOnTheSoundTimeline()
        {
            // At 4 Hz the square is high for the first 125 ms of its clock. The layer starts at
            // 125 ms, so Retrigger is high there and Free is low.
            var retrigger = SquareLfoOnLevel(125f, LfoMode.Retrigger, 0f);
            var free = SquareLfoOnLevel(125f, LfoMode.Free, 0f);
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();
            a.Render(retrigger);
            b.Render(free);

            var high = Rms(a.LayerOutput(0), 0.145f, 0.225f);
            var low = Rms(b.LayerOutput(0), 0.145f, 0.225f);
            Assert.AreEqual(24f, AudioMath.LinearToDb(high / low), 0.5f);
        }

        [Test]
        public void FreeMode_WithoutStartOffset_IsBitIdenticalToRetrigger()
        {
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();
            a.Render(SquareLfoOnLevel(0f, LfoMode.Retrigger, 0.1f));
            b.Render(SquareLfoOnLevel(0f, LfoMode.Free, 0.1f));

            AssertIdentical(a, b);
        }

        [Test]
        public void SquareLfoPhaseHalf_InvertsIt()
        {
            using var renderer = new SfxRenderer();
            renderer.Render(SquareLfoOnLevel(0f, LfoMode.Retrigger, 0.5f));

            // Phase 0 is high for 0-125 ms then low; half a cycle later it is the other way round.
            var first = Rms(renderer.LayerOutput(0), 0.02f, 0.10f);
            var second = Rms(renderer.LayerOutput(0), 0.145f, 0.225f);
            Assert.AreEqual(24f, AudioMath.LinearToDb(second / first), 0.5f);
        }

        [Test]
        public void Env2RampOnPitch_RisesOverTheWholeSound()
        {
            var recipe = SineRecipe(500f);
            recipe.Env2 = Ramp(0f, 1f);
            recipe.Routes = new List<ModRoute> { new(ModSource.Env2, ModTarget.Pitch, 12f) };
            using var renderer = new SfxRenderer();
            renderer.Render(recipe);

            var output = renderer.LayerOutput(0);
            var early = ZeroCrossings(output, 0.01f, 0.06f);
            var late = ZeroCrossings(output, 0.44f, 0.49f);

            // About +0.8 st early and +11.2 st late.
            Assert.Greater(late, early * 1.6f);
        }

        [Test]
        public void Env2_IsSharedAcrossTheSoundNotRestartedPerLayer()
        {
            var atStart = Env2Recipe(0f);
            var offset = Env2Recipe(250f);
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();
            a.Render(atStart);
            b.Render(offset);

            // The offset layer begins halfway up the ramp (about +6 st); the first one at +0.
            var fromStart = ZeroCrossings(a.LayerOutput(0), 0f, 0.05f);
            var fromMiddle = ZeroCrossings(b.LayerOutput(0), 0.25f, 0.30f);
            Assert.Greater(fromMiddle, fromStart * 1.3f);
        }

        [Test]
        public void ConstantRandom_IsTheSeededStaticOffset()
        {
            var recipe = SineRecipe(500f);
            recipe.Random.RateHz = 9f;
            recipe.Routes = new List<ModRoute> { new(ModSource.Random, ModTarget.Pitch, 12f), new(ModSource.Random, ModTarget.Decay, 1f) };

            ModMatrix.Evaluate(recipe, 5u, _layers, 1);

            Assert.AreEqual(ModMatrix.StaticValue(ModSource.Random, recipe.Macros, 5u, 0, 0) * 12f, _layers[0].Pitch);
            Assert.AreEqual(ModMatrix.StaticValue(ModSource.Random, recipe.Macros, 5u, 1, 0), _layers[0].DecayOctaves);
            Assert.AreEqual(float4.zero, _layers[0].RandomDepth);
        }

        [TestCase(RandomMode.SampleHold)]
        [TestCase(RandomMode.Smooth)]
        public void MovingRandom_IsAContinuousDepthAndSkipsStaticTargets(RandomMode mode)
        {
            var recipe = SineRecipe(500f);
            recipe.Random.Mode = mode;
            recipe.Routes = new List<ModRoute> { new(ModSource.Random, ModTarget.Pitch, 12f), new(ModSource.Random, ModTarget.Decay, 1f) };

            ModMatrix.Evaluate(recipe, 5u, _layers, 1);

            Assert.AreEqual(0f, _layers[0].Pitch);
            Assert.AreEqual(0f, _layers[0].DecayOctaves);
            Assert.AreEqual(12f, _layers[0].RandomDepth.x);
        }

        [TestCase(ModSource.Random2)]
        [TestCase(ModSource.Random3)]
        public void ExtraMovingRandom_UsesItsOwnModeAndDepth(ModSource source)
        {
            var recipe = SineRecipe(500f);
            recipe.RandomSettingsOf(source).Mode = RandomMode.Smooth;
            recipe.Routes = new List<ModRoute>
            {
                new(source, ModTarget.Pitch, 12f),
                new(ModSource.Random, ModTarget.Decay, 1f),
            };

            ModMatrix.Evaluate(recipe, 5u, _layers, 1);

            var depth = source == ModSource.Random2 ? _layers[0].Random2Depth : _layers[0].Random3Depth;
            Assert.AreEqual(12f, depth.x);
            Assert.AreEqual(float4.zero, _layers[0].RandomDepth);
            Assert.AreEqual(0f, _layers[0].Pitch);
            Assert.AreEqual(ModMatrix.StaticValue(ModSource.Random, recipe.Macros, 5u, 1, 0), _layers[0].DecayOctaves);
        }

        [Test]
        public void RandomSources_HaveIndependentValues()
        {
            var macros = new Macros();
            Assert.AreEqual(7u, ModMatrix.RandomSeed(7u, ModSource.Random));
            Assert.AreNotEqual(ModMatrix.RandomSeed(7u, ModSource.Random2), ModMatrix.RandomSeed(7u, ModSource.Random3));

            var matches = 0;
            for (var seed = 1u; seed <= 64u; seed++)
            {
                var rnd1 = ModMatrix.StaticValue(ModSource.Random, macros, seed, 0, 0);
                var rnd2 = ModMatrix.StaticValue(ModSource.Random2, macros, seed, 0, 0);
                var rnd3 = ModMatrix.StaticValue(ModSource.Random3, macros, seed, 0, 0);
                if (rnd1 == rnd2 || rnd1 == rnd3 || rnd2 == rnd3) matches++;
            }

            Assert.AreEqual(0, matches);
        }

        [Test]
        public void RandomSourceSeed_ZeroKeepsRenderSeed_OtherValuesReroll()
        {
            var macros = new Macros();
            foreach (var source in new[] { ModSource.Random, ModSource.Random2, ModSource.Random3 })
            {
                Assert.AreEqual(ModMatrix.RandomSeed(7u, source), ModMatrix.RandomSeed(7u, source, 0u));
                Assert.AreNotEqual(ModMatrix.RandomSeed(7u, source, 1u), ModMatrix.RandomSeed(7u, source, 2u));
                Assert.AreNotEqual(ModMatrix.StaticValue(source, macros, 7u, 0, 0),
                    ModMatrix.StaticValue(source, macros, 7u, 0, 0, 3u));
            }
        }

        [Test]
        public void RandomSourceSeed_RerollsConstantRoutesOfThatSourceOnly()
        {
            var recipe = SineRecipe(500f);
            recipe.Routes = new List<ModRoute> { new(ModSource.Random, ModTarget.Pitch, 12f), new(ModSource.Random2, ModTarget.Decay, 1f) };
            ModMatrix.Evaluate(recipe, 5u, _layers, 1);
            var pitch = _layers[0].Pitch;
            var decay = _layers[0].DecayOctaves;

            recipe.Random.Seed = 42u;
            ModMatrix.Evaluate(recipe, 5u, _layers, 1);
            Assert.AreNotEqual(pitch, _layers[0].Pitch);
            Assert.AreEqual(decay, _layers[0].DecayOctaves);
        }

        [TestCase(ModSource.Random)]
        [TestCase(ModSource.Random2)]
        [TestCase(ModSource.Random3)]
        public void RandomSampleHold_HoldsStepsOfOneOverRate(ModSource source)
        {
            const float rate = 10f;
            var recipe = RandomOnLevel(RandomMode.SampleHold, rate, source);
            using var renderer = new SfxRenderer();
            renderer.Render(recipe);

            var output = renderer.LayerOutput(0);
            var layerSeed = ModMatrix.RandomSeed(math.hash(new uint2(recipe.Seed, 0u)), source);
            var firstDb = AudioMath.LinearToDb(Peak(output, 0.005f, 0.045f));
            var spread = 0f;
            for (var step = 0; step < 9; step++)
            {
                var from = step / rate;
                var head = AudioMath.LinearToDb(Peak(output, from + 0.005f, from + 0.045f));
                var tail = AudioMath.LinearToDb(Peak(output, from + 0.055f, from + 0.095f));
                Assert.AreEqual(head, tail, 0.05f, $"step {step} is not held");

                var expected = 12f * (ModMatrix.RandomStep(layerSeed, step) - ModMatrix.RandomStep(layerSeed, 0));
                Assert.AreEqual(expected, head - firstDb, 0.05f, $"step {step}");
                spread = math.max(spread, math.abs(head - firstDb));
            }

            Assert.Greater(spread, 3f);
        }

        [Test]
        public void RandomSmooth_SignalSlopeIsBounded()
        {
            const float rate = 4f;
            var bound = 3f * rate / SampleRate * 1.001f;
            var seed = math.hash(new uint2(1u, 0u));
            var previous = ModMatrix.RandomSignal(RandomMode.Smooth, seed, 0f);
            var largestHoldJump = 0f;
            var heldPrevious = ModMatrix.RandomSignal(RandomMode.SampleHold, seed, 0f);
            for (var n = 1; n <= SampleRate; n++)
            {
                var steps = n / (float)SampleRate * rate;
                var value = ModMatrix.RandomSignal(RandomMode.Smooth, seed, steps);
                Assert.LessOrEqual(math.abs(value - previous), bound, $"sample {n}");
                Assert.That(value, Is.InRange(-1f, 1f));
                previous = value;

                var held = ModMatrix.RandomSignal(RandomMode.SampleHold, seed, steps);
                largestHoldJump = math.max(largestHoldJump, math.abs(held - heldPrevious));
                heldPrevious = held;
            }

            Assert.Greater(largestHoldJump, 0.1f);
        }

        [Test]
        public void RandomSmoothOnLevel_HasNoJumps()
        {
            var recipe = RandomOnLevel(RandomMode.Smooth, 4f);
            using var renderer = new SfxRenderer();
            renderer.Render(recipe);

            // 5 ms windows hold two cycles of 440 Hz. At 4 Hz × 12 dB the level moves at most
            // 1.5 × 2 × 4 × 12 = 144 dB/s, so neighbouring windows differ by well under 2 dB.
            var output = renderer.LayerOutput(0);
            const float window = 0.005f;
            var previous = AudioMath.LinearToDb(Peak(output, 0f, window));
            var lowest = previous;
            var highest = previous;
            for (var w = 1; w < 190; w++)
            {
                var db = AudioMath.LinearToDb(Peak(output, w * window, (w + 1) * window));
                Assert.Less(math.abs(db - previous), 2f, $"window {w}");
                previous = db;
                lowest = math.min(lowest, db);
                highest = math.max(highest, db);
            }

            Assert.Greater(highest - lowest, 3f);
        }

        private SfxRecipe SineRecipe(float lengthMs, float startOffsetMs = 0f)
        {
            var layer = ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine, 0f, -30f));
            layer.StartOffsetMs = startOffsetMs;
            layer.DecayMs = lengthMs;
            var recipe = ForgeTestRecipes.Create(lengthMs, layer);
            recipe.Fx.Limiter.Enabled = false;
            recipe.Routes = new List<ModRoute>();
            _created.Add(recipe);
            return recipe;
        }

        // Exercises the paths that existed before: LFO 1, Env 1, Constant Rnd and the LFO Depth target.
        private SfxRecipe RoutedRecipe()
        {
            var offset = ForgeTestRecipes.Oscillator(Waveform.Saw, -5f, -12f);
            offset.StartOffsetMs = 100f;
            var recipe = ForgeTestRecipes.Create(600f,
                ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine, 0f, -12f)),
                offset);
            recipe.Seed = 9;
            recipe.Macros.Motion = 0.7f;
            recipe.Lfo = new LfoSettings { Shape = Waveform.Triangle, RateHz = 5f };
            recipe.Routes = new List<ModRoute>
            {
                new(ModSource.Motion, ModTarget.LfoDepth, 1f),
                new(ModSource.Lfo, ModTarget.Pitch, 2f),
                new(ModSource.Envelope, ModTarget.Cutoff, 1f),
                new(ModSource.Random, ModTarget.Pitch, 3f),
                new(ModSource.Random, ModTarget.Decay, 0.5f),
            };
            _created.Add(recipe);
            return recipe;
        }

        private SfxRecipe SquareLfoOnLevel(float startOffsetMs, LfoMode mode, float phase)
        {
            var recipe = SineRecipe(500f, startOffsetMs);
            recipe.Lfo = new LfoSettings { Shape = Waveform.Square, RateHz = 4f, Phase = phase, Mode = mode };
            recipe.Routes = new List<ModRoute> { new(ModSource.Lfo, ModTarget.Level, 12f) };
            return recipe;
        }

        private SfxRecipe Env2Recipe(float startOffsetMs)
        {
            var recipe = SineRecipe(500f, startOffsetMs);
            recipe.Env2 = Ramp(0f, 1f);
            recipe.Routes = new List<ModRoute> { new(ModSource.Env2, ModTarget.Pitch, 12f) };
            return recipe;
        }

        private SfxRecipe RandomOnLevel(RandomMode mode, float rateHz, ModSource source = ModSource.Random)
        {
            var recipe = SineRecipe(1000f);
            var random = recipe.RandomSettingsOf(source);
            random.Mode = mode;
            random.RateHz = rateHz;
            recipe.Routes = new List<ModRoute> { new(source, ModTarget.Level, 12f) };
            return recipe;
        }

        private static Curve Ramp(float from, float to)
        {
            var curve = Curve.DefaultModEnvelope();
            curve.Points = new List<Breakpoint> { new(0f, from), new(1f, to) };
            return curve;
        }

        private static float Rms(NativeArray<float> interleaved, float fromSeconds, float toSeconds)
        {
            var first = (int)(fromSeconds * SampleRate);
            var last = (int)(toSeconds * SampleRate);
            var sum = 0.0;
            for (var f = first; f < last; f++) sum += interleaved[f * 2] * interleaved[f * 2];
            return (float)math.sqrt(sum / (last - first));
        }

        private static float Peak(NativeArray<float> interleaved, float fromSeconds, float toSeconds)
        {
            var first = (int)(fromSeconds * SampleRate);
            var last = (int)(toSeconds * SampleRate);
            var peak = 0f;
            for (var f = first; f < last; f++) peak = math.max(peak, math.abs(interleaved[f * 2]));
            return peak;
        }

        private static int ZeroCrossings(NativeArray<float> interleaved, float fromSeconds, float toSeconds)
        {
            var first = (int)(fromSeconds * SampleRate);
            var last = (int)(toSeconds * SampleRate);
            var count = 0;
            for (var f = first + 1; f < last; f++)
            {
                if (interleaved[(f - 1) * 2] < 0f != interleaved[f * 2] < 0f) count++;
            }

            return count;
        }

        private static void AssertIdentical(SfxRenderer expected, SfxRenderer actual)
        {
            var a = expected.Output;
            var b = actual.Output;
            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) Assert.Fail($"Sample {i} differs: {a[i]:R} vs {b[i]:R}");
            }
        }
    }
}
