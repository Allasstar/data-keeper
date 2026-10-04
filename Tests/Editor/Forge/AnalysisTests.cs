using System;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge.Tests
{
    public class AnalysisTests
    {
        private const int SampleRate = 48000;

        private SfxAnalyzer _analyzer;
        private NativeArray<float> _buffer;

        [SetUp]
        public void SetUp() => _analyzer = new SfxAnalyzer();

        [TearDown]
        public void TearDown()
        {
            _analyzer.Dispose();
            if (_buffer.IsCreated) _buffer.Dispose();
        }

        [Test]
        public void Fft_FloatVersionMatchesDoubleVersion()
        {
            const int size = 256;
            var twiddles = new NativeArray<float2>(size / 2, Allocator.Temp);
            var re = new NativeArray<float>(size, Allocator.Temp);
            var im = new NativeArray<float>(size, Allocator.Temp);
            var reference = new double[size];
            var referenceIm = new double[size];
            var random = new Random(5u);
            for (var i = 0; i < size; i++) reference[i] = re[i] = random.NextFloat(-1f, 1f);

            Fft.BuildTwiddles(twiddles, size);
            Fft.Forward(re, im, twiddles, size);
            Fft.Transform(reference, referenceIm, false);

            for (var i = 0; i < size; i++)
            {
                Assert.AreEqual(reference[i], re[i], 1e-4, $"re {i}");
                Assert.AreEqual(referenceIm[i], im[i], 1e-4, $"im {i}");
            }

            twiddles.Dispose();
            re.Dispose();
            im.Dispose();
        }

        [Test]
        public void Sine_PeakRmsCrestAndDc()
        {
            var analysis = Analyze(Sine(SampleRate, 1000f, 0.5f), 2);

            Assert.AreEqual(1000f, analysis.LengthMs, 1e-3f);
            Assert.AreEqual(0.5f, analysis.Peak, 1e-3f);
            Assert.AreEqual(0.5f / math.sqrt(2f), analysis.Rms, 1e-3f);
            Assert.AreEqual(3.01f, analysis.CrestFactorDb, 0.05f);
            Assert.Less(analysis.DcOffset, 1e-4f);
            Assert.AreEqual(0, analysis.ClippedSamples);
        }

        [Test]
        public void Loudness_StereoSineAtMinus23DbfsReadsMinus23Lufs()
        {
            var analysis = Analyze(Sine(SampleRate * 3, 1000f, AudioMath.DbToLinear(-23f)), 2);
            Assert.AreEqual(-23f, analysis.LoudnessLufs, 0.1f);
        }

        [Test]
        public void Loudness_SoundShorterThanABlockIsStillMeasured()
        {
            var analysis = Analyze(Sine(SampleRate / 10, 1000f, AudioMath.DbToLinear(-23f)), 2);
            Assert.AreEqual(-23f, analysis.LoudnessLufs, 0.3f);
        }

        [Test]
        public void Loudness_GatingIgnoresTrailingSilence()
        {
            // 2 s of sine then 2 s of silence. Ungated this reads about -26 LUFS. Gated, only the
            // 17 fully loud blocks plus the three blocks straddling the end (3/4, 1/2, 1/4 loud)
            // count, which BS.1770 puts at -23 + 10 * log10(18.5 / 20).
            var frames = SampleRate * 4;
            var samples = Sine(frames, 1000f, AudioMath.DbToLinear(-23f));
            for (var i = frames; i < frames * 2; i++) samples[i] = 0f;

            var analysis = Analyze(samples, 2);
            Assert.AreEqual(-23f + 10f * math.log10(18.5f / 20f), analysis.LoudnessLufs, 0.1f);
        }

        [Test]
        public void Silence_IsReportedAsSilent()
        {
            var analysis = Analyze(new float[SampleRate], 2);

            Assert.IsTrue(analysis.IsSilent);
            Assert.AreEqual(AudioMath.SilenceDb, analysis.LoudnessLufs);
            Assert.AreEqual(0f, analysis.EffectiveLengthMs);
            Assert.AreEqual(0f, analysis.SpectralCentroidHz);
        }

        [Test]
        public void TruePeak_FindsThePeakBetweenSamples()
        {
            // A quarter-rate sine at 45 degrees only ever samples at 0.707 of its real peak. The
            // edges are faded because a hard start or stop really does overshoot between samples.
            const int fade = 480;
            var samples = new float[SampleRate * 2];
            for (var i = 0; i < SampleRate; i++)
            {
                var edge = Math.Min(i, SampleRate - 1 - i);
                var gain = edge < fade ? 0.5 - 0.5 * Math.Cos(Math.PI * edge / fade) : 1.0;
                samples[i * 2] = samples[i * 2 + 1] = (float)(Math.Sin(0.5 * Math.PI * i + 0.25 * Math.PI) * 0.9 * gain);
            }

            var analysis = Analyze(samples, 2);

            Assert.AreEqual(-3.01f + AudioMath.LinearToDb(0.9f), analysis.PeakDb, 0.05f);
            Assert.AreEqual(AudioMath.LinearToDb(0.9f), analysis.TruePeakDb, 0.1f);
        }

        [Test]
        public void EffectiveLength_EndsWhereTheSoundFallsBelowMinus60Db()
        {
            var samples = Sine(SampleRate, 440f, 0.8f);
            var end = SampleRate * 3 / 10;
            for (var i = end * 2; i < samples.Length; i++) samples[i] = 0f;

            var analysis = Analyze(samples, 2);
            Assert.AreEqual(300f, analysis.EffectiveLengthMs, 0.5f);
        }

        [TestCase(250f)]
        [TestCase(1000f)]
        [TestCase(5000f)]
        public void SpectralCentroid_OfASineIsItsFrequency(float frequency)
        {
            var analysis = Analyze(Sine(AnalysisJob.FftSize * 20, frequency, 0.5f), 2);
            Assert.AreEqual(frequency, analysis.SpectralCentroidHz, frequency * 0.01f);
        }

        [Test]
        public void SpectralCentroid_OfWhiteNoiseIsAQuarterOfTheSampleRate()
        {
            var random = new Random(9u);
            var samples = new float[AnalysisJob.FftSize * 20];
            for (var i = 0; i < samples.Length; i++) samples[i] = random.NextFloat(-0.5f, 0.5f);

            var analysis = Analyze(samples, 1);
            Assert.AreEqual(SampleRate / 4f, analysis.SpectralCentroidHz, SampleRate * 0.02f);
        }

        [Test]
        public void DcOffset_IsTheLargestChannelMean()
        {
            var samples = Sine(SampleRate, 1000f, 0.3f);
            for (var i = 1; i < samples.Length; i += 2) samples[i] += 0.1f;

            Assert.AreEqual(0.1f, Analyze(samples, 2).DcOffset, 1e-3f);
        }

        [Test]
        public void Clipping_CountsFullScaleAndNonFiniteSamples()
        {
            var samples = Sine(SampleRate, 1000f, 0.3f);
            for (var i = 0; i < 10; i++) samples[i * 100] = i % 2 == 0 ? 1f : -1.2f;
            samples[5001] = float.NaN;

            var analysis = Analyze(samples, 2);

            Assert.AreEqual(11, analysis.ClippedSamples);
            Assert.IsTrue(float.IsFinite(analysis.Rms));
            Assert.AreEqual(1.2f, analysis.Peak, 1e-6f);
        }

        private SfxAnalysis Analyze(float[] samples, int channels)
        {
            if (_buffer.IsCreated) _buffer.Dispose();
            _buffer = new NativeArray<float>(samples, Allocator.Persistent);
            return _analyzer.Analyze(_buffer, channels, SampleRate);
        }

        private static float[] Sine(int frames, float frequency, float amplitude)
        {
            var samples = new float[frames * 2];
            for (var i = 0; i < frames; i++)
            {
                var phase = 2.0 * Math.PI * frequency * i / SampleRate;
                samples[i * 2] = samples[i * 2 + 1] = (float)(amplitude * Math.Sin(phase));
            }

            return samples;
        }
    }
}
