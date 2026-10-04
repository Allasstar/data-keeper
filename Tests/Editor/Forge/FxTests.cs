using System.Diagnostics;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Dsp.Fx;
using DataKeeper.Forge.Render;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge.Tests
{
    public class FxTests
    {
        private const int SampleRate = 48000;
        private const int Frames = SampleRate;

        private NativeArray<float> _buffer;
        private NativeArray<float> _scratch;

        [SetUp]
        public void SetUp()
        {
            _buffer = new NativeArray<float>(Frames * 2, Allocator.Persistent);
            _scratch = new NativeArray<float>(Frames * 2, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            _buffer.Dispose();
            _scratch.Dispose();
        }

        // ── Stability under extreme settings ────────────────────────────────────────

        [Test]
        public void TransientShaper_MaxSettingsStayBounded()
        {
            FillNoise(1f);
            TransientShaper.Process(_buffer, Frames, SampleRate, 1f, 1f);
            AssertFiniteAndBelow(16.5f);
        }

        [Test]
        public void Distortion_MaxDriveStaysBounded([Values] DistortionMode mode)
        {
            FillNoise(1f);
            Distortion.Process(_buffer, Frames, _scratch, mode, AudioMath.DbToLinear(DistortionSettings.MaxDriveDb), 1f);
            // Shaped noise is near +-1 every sample; the decimation filter's absolute tap sum
            // (about 1.44) is the worst case it can ring to.
            AssertFiniteAndBelow(1.45f);
        }

        [Test]
        public void Delay_MaxFeedbackStaysBounded([Values] bool pingPong)
        {
            FillNoise(1f);
            var delayFrames = FxParams.DelayFramesFor(20f, SampleRate);
            using var line = new NativeArray<float>(StereoDelay.LineSize(delayFrames), Allocator.Temp);

            StereoDelay.Process(_buffer, Frames, line, delayFrames, DelaySettings.MaxFeedback, 1f, pingPong);

            AssertFiniteAndBelow(12f);
        }

        [Test]
        public void Reverb_MaxSizeStaysBounded()
        {
            FillNoise(1f);
            using var memory = new NativeArray<float>(Reverb.MemorySize(SampleRate), Allocator.Temp);

            Reverb.Process(_buffer, Frames, _scratch, memory, SampleRate, 1f, 0f, 1f);

            AssertFiniteAndBelow(50f);
        }

        // ── Behaviour ───────────────────────────────────────────────────────────────

        [Test]
        public void Limiter_NeverExceedsCeiling()
        {
            FillNoise(4f);
            var ceiling = AudioMath.DbToLinear(-1f);

            Limiter.Process(_buffer, Frames, _scratch, SampleRate, ceiling, 60f);

            for (var i = 0; i < _buffer.Length; i++)
                Assert.LessOrEqual(math.abs(_buffer[i]), ceiling + 1e-6f, $"Sample {i}");
        }

        [Test]
        public void Limiter_LeavesQuietSignalUntouched()
        {
            FillNoise(0.5f);
            var before = _buffer.ToArray();

            Limiter.Process(_buffer, Frames, _scratch, SampleRate, AudioMath.DbToLinear(-1f), 60f);

            for (var i = 0; i < _buffer.Length; i++) Assert.AreEqual(before[i], _buffer[i]);
        }

        [Test]
        public void Delay_EchoArrivesAfterDelayTime()
        {
            Clear();
            _buffer[0] = 1f;
            _buffer[1] = 1f;
            var delayFrames = FxParams.DelayFramesFor(10f, SampleRate);
            using var line = new NativeArray<float>(StereoDelay.LineSize(delayFrames), Allocator.Temp);

            StereoDelay.Process(_buffer, Frames, line, delayFrames, 0f, 1f, false);

            Assert.AreEqual(480, delayFrames);
            for (var frame = 1; frame < delayFrames; frame++) Assert.AreEqual(0f, _buffer[frame * 2], $"frame {frame}");
            Assert.AreEqual(1f, _buffer[delayFrames * 2]);
            Assert.AreEqual(1f, _buffer[delayFrames * 2 + 1]);
        }

        [Test]
        public void Reverb_ImpulseLeavesADecayingTail()
        {
            Clear();
            _buffer[0] = 1f;
            _buffer[1] = 1f;
            using var memory = new NativeArray<float>(Reverb.MemorySize(SampleRate), Allocator.Temp);

            Reverb.Process(_buffer, Frames, _scratch, memory, SampleRate, 0.8f, 0.5f, 0.5f);

            var early = Energy(Frames / 10, Frames / 4);
            var late = Energy(Frames * 3 / 4, Frames);
            Assert.Greater(early, 0f);
            Assert.Greater(early, late * 4f);
        }

        [Test]
        public void Distortion_CleanAtLowLevelAndUnityDrive()
        {
            for (var frame = 0; frame < Frames; frame++)
            {
                var x = 0.01f * math.sin(2f * math.PI * 1000f * frame / SampleRate);
                _buffer[frame * 2] = x;
                _buffer[frame * 2 + 1] = x;
            }

            var before = _buffer.ToArray();
            Distortion.Process(_buffer, Frames, _scratch, DistortionMode.Tanh, 1f, 1f);

            // Edges excluded: the filter sees implicit zeros beyond the buffer.
            for (var i = 64; i < _buffer.Length - 64; i++) Assert.AreEqual(before[i], _buffer[i], 2e-4f, $"Sample {i}");
        }

        // ── Through the renderer ────────────────────────────────────────────────────

        [Test]
        public void FullChain_IsDeterministic()
        {
            var recipe = FullRecipe();
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();

            a.Render(recipe);
            b.Render(recipe);

            var outA = a.Output;
            var outB = b.Output;
            for (var i = 0; i < outA.Length; i++)
                Assert.AreEqual(System.BitConverter.SingleToInt32Bits(outA[i]), System.BitConverter.SingleToInt32Bits(outB[i]), $"Sample {i}");

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void FullChain_OneSecondFourLayersRendersQuickly()
        {
            var recipe = FullRecipe();
            using var renderer = new SfxRenderer();
            renderer.Render(recipe);

            const int runs = 5;
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < runs; i++) renderer.Render(recipe);
            stopwatch.Stop();

            var average = stopwatch.Elapsed.TotalMilliseconds / runs;
            Debug.Log($"Forge: 1 s, 4 layers, full FX renders in {average:0.00} ms (target 10 ms)");
            // Loose bound so a busy CI machine does not fail it; the log shows the real figure.
            Assert.Less(average, 100.0);
            Object.DestroyImmediate(recipe);
        }

        private static SfxRecipe FullRecipe()
        {
            var fm = ForgeTestRecipes.Oscillator(Waveform.Sine, -5f, -8f);
            fm.Source.Type = SourceType.FM;
            fm.Source.Fm = FmSettings.Default;
            var wavetable = ForgeTestRecipes.Oscillator(Waveform.Sine, 7f, -10f);
            wavetable.Source.Type = SourceType.Wavetable;
            wavetable.Source.Wavetable = new WavetableSettings { Bank = WavetableBank.Harmonic, Position = 0.7f };
            var noise = ForgeTestRecipes.Noise(NoiseColor.Pink, -12f);
            noise.Filter = new FilterSettings { Type = FilterType.BandPass, CutoffHz = 1500f, Resonance = 0.5f };

            var recipe = ForgeTestRecipes.Create(1000f, ForgeTestRecipes.Oscillator(Waveform.Saw, -12f, -6f), fm, wavetable, noise);
            foreach (var layer in recipe.Layers) layer.DecayMs = 1000f;

            recipe.Fx.Transient.Enabled = true;
            recipe.Fx.Distortion.Enabled = true;
            recipe.Fx.Delay.Enabled = true;
            recipe.Fx.Reverb.Enabled = true;
            recipe.Fx.Limiter.Enabled = true;
            return recipe;
        }

        private void FillNoise(float amplitude)
        {
            var random = new Random(17u);
            for (var i = 0; i < _buffer.Length; i++) _buffer[i] = random.NextFloat(-amplitude, amplitude);
        }

        private void Clear()
        {
            for (var i = 0; i < _buffer.Length; i++) _buffer[i] = 0f;
        }

        private float Energy(int fromFrame, int toFrame)
        {
            var energy = 0f;
            for (var i = fromFrame * 2; i < toFrame * 2; i++) energy += _buffer[i] * _buffer[i];
            return energy;
        }

        private void AssertFiniteAndBelow(float bound)
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                Assert.IsTrue(float.IsFinite(_buffer[i]), $"Non-finite sample at {i}");
                Assert.Less(math.abs(_buffer[i]), bound, $"Sample {i}");
            }
        }
    }
}
