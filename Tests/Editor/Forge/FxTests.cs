using System.Collections.Generic;
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
            Distortion.Process(_buffer, Frames, _scratch, mode, AudioMath.DbToLinear(DistortionSettings.MaxDriveDb),
                DistortionSettings.MaxDriveDb, 1f);
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

        [Test]
        public void Compressor_MaxSettingsStayBounded([Values(0f, 1f)] float time)
        {
            FillNoise(1f);
            Compressor.Process(_buffer, Frames, SampleRate, 1f, time, 1f, 1f, AudioMath.DbToLinear(CompressorSettings.MaxGainDb));
            // +12 dB of output gain on full-scale noise; the managed run peaked at 5.3.
            AssertFiniteAndBelow(8f);
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
            Distortion.Process(_buffer, Frames, _scratch, DistortionMode.Tanh, 1f, 0f, 1f);

            // Edges excluded: the filter sees implicit zeros beyond the buffer.
            for (var i = 64; i < _buffer.Length - 64; i++) Assert.AreEqual(before[i], _buffer[i], 2e-4f, $"Sample {i}");
        }

        [Test]
        public void Distortion_TanhAndFoldbackMatchTheOriginalAlgorithm(
            [Values(DistortionMode.Tanh, DistortionMode.Foldback)] DistortionMode mode)
        {
            FillNoise(1f);
            var expected = _buffer.ToArray();
            var drive = AudioMath.DbToLinear(18f);
            OriginalDistortion(expected, Frames, mode, drive, 0.7f);

            Distortion.Process(_buffer, Frames, _scratch, mode, drive, 18f, 0.7f);

            for (var i = 0; i < _buffer.Length; i++)
                Assert.AreEqual(System.BitConverter.SingleToInt32Bits(expected[i]), System.BitConverter.SingleToInt32Bits(_buffer[i]), $"Sample {i}");
        }

        [Test]
        public void Distortion_HardClipOvershootsOnlyByHalfbandRinging()
        {
            for (var frame = 0; frame < Frames; frame++)
            {
                var x = 0.9f * math.sin(2f * math.PI * 220f * frame / SampleRate);
                _buffer[frame * 2] = x;
                _buffer[frame * 2 + 1] = x;
            }

            Distortion.Process(_buffer, Frames, _scratch, DistortionMode.HardClip, AudioMath.DbToLinear(18f), 18f, 1f);

            // A prototype peaked at 1.009 here. Higher notes and full drive ring further, up to the
            // tap-sum bound the max-drive test covers, so this stays a low note at 18 dB.
            var peak = 0f;
            for (var i = 0; i < _buffer.Length; i++) peak = math.max(peak, math.abs(_buffer[i]));
            Assert.LessOrEqual(peak, 1.1f);
            Assert.Greater(peak, 0.95f);
        }

        [Test]
        public void Distortion_SineFoldIsOddSymmetric()
        {
            for (var x = -4f; x <= 4f; x += 0.01f)
                Assert.AreEqual(-Distortion.Shape(x, DistortionMode.SineFold), Distortion.Shape(-x, DistortionMode.SineFold), 1e-6f, $"x {x}");
            Assert.AreEqual(-1f, Distortion.Shape(3f, DistortionMode.SineFold), 1e-5f, "folds back beyond the rails");

            FillNoise(1f);
            var input = _buffer.ToArray();
            var drive = AudioMath.DbToLinear(12f);
            Distortion.Process(_buffer, Frames, _scratch, DistortionMode.SineFold, drive, 12f, 1f);
            var positive = _buffer.ToArray();

            for (var i = 0; i < _buffer.Length; i++) _buffer[i] = -input[i];
            Distortion.Process(_buffer, Frames, _scratch, DistortionMode.SineFold, drive, 12f, 1f);

            for (var i = 0; i < _buffer.Length; i++) Assert.AreEqual(-positive[i], _buffer[i], 1e-6f, $"Sample {i}");
        }

        [Test]
        public void Distortion_BitCrushAtMaxDriveHasAtMostFourLevels()
        {
            FillNoise(1f);

            Distortion.Process(_buffer, Frames, _scratch, DistortionMode.BitCrush,
                AudioMath.DbToLinear(DistortionSettings.MaxDriveDb), DistortionSettings.MaxDriveDb, 1f);

            var sorted = _buffer.ToArray();
            System.Array.Sort(sorted);
            var levels = 1;
            for (var i = 1; i < sorted.Length; i++)
                if (sorted[i] != sorted[i - 1]) levels++;
            Assert.LessOrEqual(levels, 4);
            Assert.Greater(levels, 1);
        }

        [Test]
        public void Distortion_DownsampleAt18DbHoldsRunsOfEight()
        {
            FillNoise(1f);
            var before = _buffer.ToArray();

            Distortion.Process(_buffer, Frames, _scratch, DistortionMode.Downsample, AudioMath.DbToLinear(18f), 18f, 1f);

            Assert.AreEqual(8, Distortion.HoldFor(18f));
            for (var frame = 0; frame < Frames; frame++)
            {
                var held = frame - frame % 8;
                Assert.AreEqual(before[held * 2], _buffer[frame * 2], $"Left frame {frame}");
                Assert.AreEqual(before[held * 2 + 1], _buffer[frame * 2 + 1], $"Right frame {frame}");
            }
        }

        [Test]
        public void Distortion_ZeroDriveRendersFinite([Values] DistortionMode mode)
        {
            FillNoise(1f);
            Distortion.Process(_buffer, Frames, _scratch, mode, 1f, 0f, 1f);
            AssertFiniteAndBelow(1.45f);
        }

        [Test]
        public void Distortion_CrushModesAreNearlyCleanAtZeroDrive(
            [Values(DistortionMode.BitCrush, DistortionMode.Downsample)] DistortionMode mode)
        {
            FillNoise(1f);
            var before = _buffer.ToArray();

            Distortion.Process(_buffer, Frames, _scratch, mode, 1f, 0f, 1f);

            // 16 bits is 32767 steps a side, so at most half a step (1.5e-5) of error.
            for (var i = 0; i < _buffer.Length; i++) Assert.AreEqual(before[i], _buffer[i], 2e-5f, $"Sample {i}");
        }

        [Test]
        public void Compressor_NeutralBandsSumFlat([Values(30f, 88f, 200f, 1000f, 2500f, 6000f, 16000f)] float hz)
        {
            FillSine(hz, 0.5f);
            var before = Rms(Frames / 2, Frames);

            Compressor.Process(_buffer, Frames, SampleRate, 1f, 0.5f, 0f, 0f, 1f);

            // The crossover sums to an allpass, so the phase moves but the level must not. Even
            // frequencies put whole cycles in the half-second window. The managed run was within 0.002 dB.
            Assert.AreEqual(0f, AudioMath.LinearToDb(Rms(Frames / 2, Frames) / before), 0.1f);
        }

        [Test]
        public void Compressor_DownwardOnlyTurnsALoudSineDown()
        {
            FillSine(1000f, AudioMath.DbToLinear(-6f));
            var before = Rms(Frames / 2, Frames);

            Compressor.Process(_buffer, Frames, SampleRate, 1f, 0.5f, 0f, 1f, 1f);

            // The managed run gave -3.9 dB, after the makeup gain.
            Assert.Less(AudioMath.LinearToDb(Rms(Frames / 2, Frames) / before), -2f);
        }

        [Test]
        public void Compressor_UpwardOnlyBringsAQuietSineUp()
        {
            FillSine(1000f, AudioMath.DbToLinear(-40f));
            var before = Rms(Frames / 2, Frames);

            Compressor.Process(_buffer, Frames, SampleRate, 1f, 0.5f, 1f, 0f, 1f);

            // The static curve gives +7.5 dB at -40 dB, and so did the managed run.
            Assert.Greater(AudioMath.LinearToDb(Rms(Frames / 2, Frames) / before), 3f);
        }

        [Test]
        public void Compressor_LeavesSilenceAndTheFloorAlone()
        {
            Clear();
            Compressor.Process(_buffer, Frames, SampleRate, 1f, 0.5f, 1f, 1f, AudioMath.DbToLinear(CompressorSettings.MaxGainDb));
            for (var i = 0; i < _buffer.Length; i++) Assert.AreEqual(0f, _buffer[i], $"Sample {i}");

            FillSine(1000f, AudioMath.DbToLinear(-80f));
            var before = Rms(Frames / 2, Frames);
            Compressor.Process(_buffer, Frames, SampleRate, 1f, 0.5f, 1f, 1f, 1f);
            Assert.LessOrEqual(AudioMath.LinearToDb(Rms(Frames / 2, Frames) / before), 0.1f);
        }

        [Test]
        public void Compressor_DepthZeroLeavesTheBufferUntouched()
        {
            FillNoise(0.5f);
            var before = _buffer.ToArray();

            Compressor.Process(_buffer, Frames, SampleRate, 0f, 0.5f, 1f, 1f, AudioMath.DbToLinear(CompressorSettings.MaxGainDb));

            for (var i = 0; i < _buffer.Length; i++)
                Assert.AreEqual(System.BitConverter.SingleToInt32Bits(before[i]), System.BitConverter.SingleToInt32Bits(_buffer[i]), $"Sample {i}");
        }

        [Test]
        public void CompressorDepthTarget_IsGlobalStaticAndMovesDepth()
        {
            Assert.IsFalse(ModTargets.IsPerLayer(ModTarget.CompressorDepth));
            Assert.IsFalse(ModTargets.IsContinuous(ModTarget.CompressorDepth));
            Assert.AreEqual(1f, ModTargets.MaxAmount(ModTarget.CompressorDepth));

            var recipe = FullRecipe();
            recipe.Routes = new List<ModRoute> { new(ModSource.Size, ModTarget.CompressorDepth, -0.4f) };
            recipe.Macros.Size = 1f;
            var global = ModMatrix.Evaluate(recipe, 1u, new LayerModulation[SfxRecipe.MaxLayers], recipe.Layers.Count);

            Assert.AreEqual(-0.4f, global.CompressorDepth, 1e-6f);
            Assert.AreEqual(0.6f, FxParams.From(recipe.Fx, SampleRate, global).CompressorDepth, 1e-6f);
            Object.DestroyImmediate(recipe);
        }

        // ── Through the renderer ────────────────────────────────────────────────────

        [Test]
        public void Compressor_DefaultIsOffAndDisabledSettingsRenderBitIdentical()
        {
            Assert.IsFalse(new FxChain().Compressor.Enabled);
            Assert.IsFalse(FxParams.From(new FxChain(), SampleRate).CompressorEnabled);

            var reference = FullRecipe();
            reference.Fx.Compressor = default;
            var disabled = FullRecipe();
            disabled.Fx.Compressor = new CompressorSettings { Depth = 0.7f, Time = 0.2f, Upward = 0.4f, Downward = 0.9f, GainDb = 6f };
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();

            a.Render(reference);
            b.Render(disabled);

            AssertIdentical(a, b);
            Object.DestroyImmediate(reference);
            Object.DestroyImmediate(disabled);
        }

        [Test]
        public void Compressor_DepthZeroRendersBitIdenticalToOff()
        {
            var off = FullRecipe();
            var zero = FullRecipe();
            zero.Fx.Compressor.Enabled = true;
            zero.Fx.Compressor.Depth = 0f;
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();

            a.Render(off);
            b.Render(zero);

            AssertIdentical(a, b);
            Object.DestroyImmediate(off);
            Object.DestroyImmediate(zero);
        }

        [Test]
        public void Compressor_FullChainIsDeterministicAndChangesTheSound()
        {
            var recipe = FullRecipe();
            using var off = new SfxRenderer();
            off.Render(recipe);
            recipe.Fx.Compressor.Enabled = true;
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();

            a.Render(recipe);
            b.Render(recipe);

            AssertIdentical(a, b);
            var dry = off.Output;
            var wet = a.Output;
            var changed = false;
            for (var i = 0; i < wet.Length && !changed; i++) changed = wet[i] != dry[i];
            Assert.IsTrue(changed);
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Compressor_RandomizeAndMutateLeaveItAlone()
        {
            var recipe = FullRecipe();
            var settings = new CompressorSettings { Enabled = true, Depth = 0.6f, Time = 0.3f, Upward = 0.2f, Downward = 0.8f, GainDb = 3f };
            recipe.Fx.Compressor = settings;

            SfxRandomizer.Randomize(recipe, ForgeTestRecipes.LoadTemplates()[SfxCategory.Impact], 7u);
            SfxRandomizer.Mutate(recipe, 8u);

            Assert.AreEqual(settings, recipe.Fx.Compressor);
            Object.DestroyImmediate(recipe);
        }


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

        // The pre-FS2 Distortion.Process, verbatim, as the bit-exact reference for the two
        // original modes.
        private static readonly float[] OriginalTaps =
        {
            0.312633322f, -0.090106922f, 0.040107418f, -0.017917030f, 0.007100857f, -0.002230286f, 0.000410323f,
        };

        private static void OriginalDistortion(float[] buffer, int frames, DistortionMode mode, float drive, float mix)
        {
            var scratch = new float[frames * 2];
            for (var channel = 0; channel < 2; channel++)
            {
                for (var n = 0; n < frames; n++)
                {
                    var odd = 0f;
                    for (var k = 0; k < OriginalTaps.Length; k++)
                        odd += OriginalTaps[k] * (OriginalInput(buffer, frames, channel, n - k) + OriginalInput(buffer, frames, channel, n + k + 1));

                    scratch[n * 2] = OriginalShape(OriginalInput(buffer, frames, channel, n) * drive, mode);
                    scratch[n * 2 + 1] = OriginalShape(2f * odd * drive, mode);
                }

                var oversampled = frames * 2;
                for (var n = 0; n < frames; n++)
                {
                    var center = n * 2;
                    var filtered = 0.5f * scratch[center];
                    for (var k = 0; k < OriginalTaps.Length; k++)
                    {
                        var offset = 2 * k + 1;
                        filtered += OriginalTaps[k] * (OriginalOversampled(scratch, oversampled, center - offset)
                                                       + OriginalOversampled(scratch, oversampled, center + offset));
                    }

                    var index = n * 2 + channel;
                    buffer[index] = math.lerp(buffer[index], filtered, mix);
                }
            }
        }

        private static float OriginalShape(float x, DistortionMode mode)
        {
            if (mode != DistortionMode.Foldback) return math.tanh(x);
            var phase = (x + 1f) * 0.25f;
            return 4f * math.abs(phase - math.floor(phase + 0.5f)) - 1f;
        }

        private static float OriginalInput(float[] buffer, int frames, int channel, int frame) =>
            (uint)frame < (uint)frames ? buffer[frame * 2 + channel] : 0f;

        private static float OriginalOversampled(float[] scratch, int length, int index) =>
            (uint)index < (uint)length ? scratch[index] : 0f;

        private void FillNoise(float amplitude)
        {
            var random = new Random(17u);
            for (var i = 0; i < _buffer.Length; i++) _buffer[i] = random.NextFloat(-amplitude, amplitude);
        }

        private void FillSine(float hz, float amplitude)
        {
            for (var frame = 0; frame < Frames; frame++)
            {
                var x = amplitude * (float)System.Math.Sin(2.0 * System.Math.PI * hz * frame / SampleRate);
                _buffer[frame * 2] = x;
                _buffer[frame * 2 + 1] = x;
            }
        }

        private float Rms(int fromFrame, int toFrame)
        {
            var sum = 0.0;
            for (var i = fromFrame * 2; i < toFrame * 2; i++) sum += (double)_buffer[i] * _buffer[i];
            return (float)System.Math.Sqrt(sum / ((toFrame - fromFrame) * 2));
        }

        private static void AssertIdentical(SfxRenderer expected, SfxRenderer actual)
        {
            Assert.AreEqual(expected.FrameCount, actual.FrameCount);
            var a = expected.Output;
            var b = actual.Output;
            for (var i = 0; i < a.Length; i++)
                Assert.AreEqual(System.BitConverter.SingleToInt32Bits(a[i]), System.BitConverter.SingleToInt32Bits(b[i]), $"Sample {i}");
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
