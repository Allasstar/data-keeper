using System.Collections.Generic;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Dsp.Fx;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class ModulationTests
    {
        private const int SampleRate = 48000;

        private readonly LayerModulation[] _layers = new LayerModulation[SfxRecipe.MaxLayers];
        private SfxRecipe _recipe;

        [SetUp]
        public void SetUp()
        {
            _recipe = ForgeTestRecipes.Create(500f,
                ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine, 0f, -30f)),
                ForgeTestRecipes.Flat(ForgeTestRecipes.Oscillator(Waveform.Sine, 7f, -30f)));
            _recipe.Fx.Limiter.Enabled = false;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_recipe);

        [Test]
        public void DefaultRoutes_AtNeutralMacrosRenderBitIdenticalToNoRoutes()
        {
            Assert.IsNotEmpty(_recipe.Routes);
            using var withRoutes = new SfxRenderer();
            using var without = new SfxRenderer();

            withRoutes.Render(_recipe);
            _recipe.Routes = new List<ModRoute>();
            without.Render(_recipe);

            Assert.AreEqual(without.FrameCount, withRoutes.FrameCount);
            var a = withRoutes.Output;
            var b = without.Output;
            for (var i = 0; i < a.Length; i++) Assert.AreEqual(b[i], a[i], $"sample {i}");
        }

        [Test]
        public void Evaluate_MacrosScaleBipolarAroundTheMiddle()
        {
            _recipe.Routes = new List<ModRoute> { new(ModSource.Size, ModTarget.Pitch, -6f) };

            _recipe.Macros.Size = 1f;
            ModMatrix.Evaluate(_recipe, 1u, _layers, 2);
            Assert.AreEqual(-6f, _layers[0].Pitch, 1e-6f);
            Assert.AreEqual(-6f, _layers[1].Pitch, 1e-6f);

            _recipe.Macros.Size = 0f;
            ModMatrix.Evaluate(_recipe, 1u, _layers, 2);
            Assert.AreEqual(6f, _layers[0].Pitch, 1e-6f);

            _recipe.Macros.Size = 0.75f;
            ModMatrix.Evaluate(_recipe, 1u, _layers, 2);
            Assert.AreEqual(-3f, _layers[0].Pitch, 1e-6f);
        }

        [Test]
        public void Evaluate_LayerScopedRouteOnlyTouchesItsLayer()
        {
            _recipe.Macros.Tone = 1f;
            _recipe.Routes = new List<ModRoute> { new(ModSource.Tone, ModTarget.Cutoff, 2f) { Layer = 1 } };

            ModMatrix.Evaluate(_recipe, 1u, _layers, 2);

            Assert.AreEqual(0f, _layers[0].CutoffOctaves);
            Assert.AreEqual(2f, _layers[1].CutoffOctaves, 1e-6f);
        }

        [Test]
        public void Evaluate_GlobalTargetsReachTheFxChain()
        {
            _recipe.Macros.Energy = 1f;
            _recipe.Fx.Distortion.DriveDb = 6f;
            _recipe.Routes = new List<ModRoute> { new(ModSource.Energy, ModTarget.Drive, 12f) };

            var global = ModMatrix.Evaluate(_recipe, 1u, _layers, 2);
            var fx = FxParams.From(_recipe.Fx, SampleRate, global);

            Assert.AreEqual(12f, global.DriveDb, 1e-6f);
            Assert.AreEqual(AudioMath.DbToLinear(18f), fx.DistortionDrive, 1e-4f);
        }

        [Test]
        public void Evaluate_MotionScalesLfoDepthAndContinuousSourcesSkipStaticTargets()
        {
            _recipe.Macros.Motion = 1f;
            _recipe.Routes = new List<ModRoute>
            {
                new(ModSource.Motion, ModTarget.LfoDepth, 1f),
                new(ModSource.Lfo, ModTarget.Pitch, 2f),
                new(ModSource.Envelope, ModTarget.Cutoff, 3f),
                new(ModSource.Lfo, ModTarget.Decay, 1f),
            };

            ModMatrix.Evaluate(_recipe, 1u, _layers, 2);

            Assert.AreEqual(4f, _layers[0].LfoDepth.x, 1e-6f);
            Assert.AreEqual(3f, _layers[0].EnvelopeDepth.y, 1e-6f);
            Assert.AreEqual(0f, _layers[0].DecayOctaves);
        }

        [Test]
        public void Evaluate_RandomSourceFollowsTheSeed()
        {
            _recipe.Routes = new List<ModRoute> { new(ModSource.Random, ModTarget.Pitch, 12f) };

            ModMatrix.Evaluate(_recipe, 5u, _layers, 2);
            var first = _layers[0].Pitch;
            var otherLayer = _layers[1].Pitch;
            ModMatrix.Evaluate(_recipe, 5u, _layers, 2);
            Assert.AreEqual(first, _layers[0].Pitch);

            ModMatrix.Evaluate(_recipe, 6u, _layers, 2);
            Assert.AreNotEqual(first, _layers[0].Pitch);
            Assert.AreNotEqual(first, otherLayer);
            Assert.That(first, Is.InRange(-12f, 12f));
        }

        // An unmixed math.hash steps by a fixed amount between consecutive seeds (FSF-O3).
        [Test]
        public void StaticRandom_IsNotEvenlySteppedAcrossSeeds()
        {
            var macros = new Macros();
            var steps = new float[3];
            for (var i = 0; i < steps.Length; i++)
            {
                var delta = ModMatrix.StaticValue(ModSource.Random, macros, (uint)(i + 2), 0, 0)
                            - ModMatrix.StaticValue(ModSource.Random, macros, (uint)(i + 1), 0, 0);
                steps[i] = delta < 0f ? delta + 2f : delta;
            }

            Assert.IsFalse(Mathf.Approximately(steps[0], steps[1]) && Mathf.Approximately(steps[1], steps[2]));
        }

        [Test]
        public void Length_RouteChangesTheRenderedLength()
        {
            _recipe.Macros.Size = 1f;
            _recipe.Routes = new List<ModRoute> { new(ModSource.Size, ModTarget.Length, 1f) };
            using var renderer = new SfxRenderer();

            renderer.Render(_recipe);

            Assert.AreEqual(SfxRenderer.FramesFor(1000f, SampleRate), renderer.FrameCount);
        }

        [Test]
        public void SquareLfoOnLevel_SwitchesBetweenTwoLevels()
        {
            _recipe.Layers.RemoveAt(1);
            _recipe.Lfo = new LfoSettings { Shape = Waveform.Square, RateHz = 4f };
            _recipe.Routes = new List<ModRoute> { new(ModSource.Lfo, ModTarget.Level, 12f) };
            using var renderer = new SfxRenderer();

            renderer.Render(_recipe);

            // First half period (0-125 ms) is +12 dB, the second -12 dB; skip the transitions.
            var loud = Rms(renderer.Output, 0.02f, 0.10f);
            var quiet = Rms(renderer.Output, 0.145f, 0.225f);
            Assert.AreEqual(24f, AudioMath.LinearToDb(loud / quiet), 0.5f);
        }

        [Test]
        public void EnvelopeOnPitch_BendsWithTheAmpCurve()
        {
            _recipe.Layers.RemoveAt(1);
            var layer = _recipe.Layers[0];
            layer.AmpCurve = new Curve { Points = new List<Breakpoint> { new(0f, 1f), new(1f, 0.001f) } };
            _recipe.Routes = new List<ModRoute> { new(ModSource.Envelope, ModTarget.Pitch, 12f) };
            using var renderer = new SfxRenderer();
            using var analyzer = new SfxAnalyzer();

            renderer.Render(_recipe);
            var output = renderer.Output;
            var eighth = output.Length / 8 & ~1;
            var start = analyzer.Analyze(output.GetSubArray(0, eighth), 2, SampleRate);
            var end = analyzer.Analyze(output.GetSubArray(output.Length - eighth, eighth), 2, SampleRate);

            // The envelope is near 1 at the start (about +12 st) and near 0 at the end (about +0 st).
            Assert.Greater(start.SpectralCentroidHz, end.SpectralCentroidHz * 1.5f);
        }

        private static float Rms(NativeArray<float> interleaved, float fromSeconds, float toSeconds)
        {
            var first = (int)(fromSeconds * SampleRate);
            var last = (int)(toSeconds * SampleRate);
            var sum = 0.0;
            for (var f = first; f < last; f++) sum += interleaved[f * 2] * interleaved[f * 2];
            return (float)math.sqrt(sum / (last - first));
        }
    }
}
