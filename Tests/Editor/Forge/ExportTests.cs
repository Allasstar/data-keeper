using System;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Export;
using NUnit.Framework;
using Unity.Collections;

namespace DataKeeper.Forge.Tests
{
    public class ExportTests
    {
        private const int SampleRate = 48000;

        private ExportProcessor _processor;
        private NativeArray<float> _buffer;

        [SetUp]
        public void SetUp() => _processor = new ExportProcessor();

        [TearDown]
        public void TearDown()
        {
            _processor.Dispose();
            if (_buffer.IsCreated) _buffer.Dispose();
        }

        // ── Naming ──────────────────────────────────────────────────────────────────

        [TestCase("sfx_{category}_{name}_{n}", SfxCategory.UIClick, "Big Hit", 3, 12, "sfx_ui_click_big_hit_03")]
        [TestCase("sfx_{category}_{name}_{n}", SfxCategory.Impact, "SFX Recipe", 7, 120, "sfx_impact_sfx_recipe_007")]
        [TestCase("{name}-{seed}", SfxCategory.Laser, "zap", 1, 1, "zap-77")]
        [TestCase("", SfxCategory.Pickup, "Coin", 1, 1, "sfx_pickup_coin_01")]
        [TestCase("a/b_{n}", SfxCategory.Magic, "x", 2, 2, "a_b_02")]
        public void Naming_ReplacesTokens(string template, SfxCategory category, string name, int n, int count, string expected)
        {
            Assert.AreEqual(expected, ExportNaming.Format(template, category, name, n, count, 77u));
        }

        [TestCase("UIClick", "ui_click")]
        [TestCase("Explosion", "explosion")]
        [TestCase("myRecipe2", "my_recipe2")]
        [TestCase("  Laser--Blast  ", "laser_blast")]
        [TestCase("HTTPRequest", "http_request")]
        public void Naming_SnakeCase(string text, string expected)
        {
            Assert.AreEqual(expected, ExportNaming.ToSnakeCase(text));
        }

        // ── Processing ──────────────────────────────────────────────────────────────

        [Test]
        public void TrimSilence_CutsTheTailAfterMinus60DbPlusPadding()
        {
            var samples = Sine(SampleRate, 0.5f);
            Silence(samples, SampleRate / 5);

            var result = Process(samples, new ExportSettings { TrimSilence = true, FadeOutMs = 0f, Channels = ExportChannels.Stereo });

            Assert.AreEqual(SampleRate / 5 + SampleRate / 100, result.Frames, 2);
            Assert.AreEqual(2, result.Channels);
        }

        [Test]
        public void FadeOut_EndsAtZeroAndLeavesTheStartAlone()
        {
            var samples = Constant(SampleRate / 10, 0.5f);
            var result = Process(samples, new ExportSettings { TrimSilence = false, FadeOutMs = 10f, Channels = ExportChannels.Stereo });

            var output = _processor.Output;
            Assert.AreEqual(SampleRate / 10, result.Frames);
            Assert.AreEqual(0f, output[output.Length - 1], 1e-6f);
            Assert.AreEqual(0.5f, output[0]);
            Assert.AreEqual(0.5f, output[(SampleRate / 10 - SampleRate / 100 - 1) * 2]);

            for (var i = 2; i < output.Length; i += 2)
                Assert.LessOrEqual(output[i], output[i - 2] + 1e-7f, $"fade not monotonic at frame {i / 2}");
        }

        [Test]
        public void Normalize_PeakHitsTheTruePeakTarget()
        {
            var settings = new ExportSettings { Normalize = NormalizeMode.Peak, PeakTargetDb = -1f, TrimSilence = false, FadeOutMs = 0f };
            var result = Process(Sine(SampleRate, 0.1f), settings);

            Assert.AreEqual(-1f, result.Analysis.TruePeakDb, 0.05f);
            Assert.IsFalse(result.GainLimitedByPeak);
        }

        [Test]
        public void Normalize_LoudnessHitsTheTarget()
        {
            var settings = new ExportSettings
            {
                Normalize = NormalizeMode.Loudness, LoudnessTargetLufs = -20f, TrimSilence = false, FadeOutMs = 0f,
                Channels = ExportChannels.Stereo,
            };
            var result = Process(Sine(SampleRate, 0.05f), settings);

            Assert.AreEqual(-20f, result.Analysis.LoudnessLufs, 0.1f);
            Assert.IsFalse(result.GainLimitedByPeak);
        }

        [Test]
        public void Normalize_LoudnessNeverPushesPastThePeakTarget()
        {
            // A lone spike: very quiet on average, so reaching -10 LUFS would need huge gain.
            var samples = new float[SampleRate * 2];
            samples[SampleRate] = samples[SampleRate + 1] = 0.2f;
            var settings = new ExportSettings
            {
                Normalize = NormalizeMode.Loudness, LoudnessTargetLufs = -10f, PeakTargetDb = -1f, TrimSilence = false, FadeOutMs = 0f,
            };

            var result = Process(samples, settings);

            Assert.IsTrue(result.GainLimitedByPeak);
            Assert.LessOrEqual(result.Analysis.TruePeakDb, -0.99f);
        }

        [Test]
        public void Channels_AutoWritesMonoOnlyWhenBothSidesMatch()
        {
            var settings = new ExportSettings { TrimSilence = false, FadeOutMs = 0f, Channels = ExportChannels.Auto };
            Assert.AreEqual(1, Process(Sine(1000, 0.5f), settings).Channels);

            var stereo = Sine(1000, 0.5f);
            stereo[501] += 0.01f;
            Assert.AreEqual(2, Process(stereo, settings).Channels);
        }

        [Test]
        public void Channels_MonoAveragesBothSides()
        {
            var samples = new float[200];
            for (var i = 0; i < samples.Length; i += 2)
            {
                samples[i] = 0.4f;
                samples[i + 1] = -0.2f;
            }

            var result = Process(samples, new ExportSettings { TrimSilence = false, FadeOutMs = 0f, Channels = ExportChannels.Mono });

            Assert.AreEqual(1, result.Channels);
            Assert.AreEqual(100, _processor.Output.Length);
            Assert.AreEqual(0.1f, _processor.Output[50], 1e-6f);
        }

        private ExportResult Process(float[] samples, ExportSettings settings)
        {
            if (_buffer.IsCreated) _buffer.Dispose();
            _buffer = new NativeArray<float>(samples, Allocator.Persistent);
            return _processor.Process(_buffer, 2, SampleRate, settings);
        }

        private static float[] Sine(int frames, float amplitude)
        {
            var samples = new float[frames * 2];
            for (var i = 0; i < frames; i++)
                samples[i * 2] = samples[i * 2 + 1] = (float)(amplitude * Math.Sin(2.0 * Math.PI * 440.0 * i / SampleRate));
            return samples;
        }

        private static float[] Constant(int frames, float value)
        {
            var samples = new float[frames * 2];
            Array.Fill(samples, value);
            return samples;
        }

        private static void Silence(float[] samples, int fromFrame)
        {
            for (var i = fromFrame * 2; i < samples.Length; i++) samples[i] = 0f;
        }
    }
}
