using System;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Export
{
    public struct ExportResult
    {
        public int Frames;
        public int Channels;
        public float Gain;
        public bool GainLimitedByPeak;
        public SfxAnalysis Analysis;
    }

    // Prepares a rendered buffer for writing: channel layout, trailing-silence trim, fade-out
    // and normalisation, in that order, so normalisation measures exactly what gets written.
    public sealed class ExportProcessor : IDisposable
    {
        private const float MonoTolerance = 1e-5f;
        private const float TrimPaddingMs = 10f;

        private readonly SfxAnalyzer _analyzer = new();
        private NativeArray<float> _buffer;
        private ExportResult _result;

        public NativeArray<float> Output => _buffer.GetSubArray(0, _result.Frames * _result.Channels);

        public ExportResult Process(NativeArray<float> interleaved, int sourceChannels, int sampleRate,
            ExportSettings settings)
        {
            var frames = interleaved.Length / sourceChannels;
            var channels = OutputChannels(interleaved, sourceChannels, settings.Channels);
            EnsureCapacity(frames * channels);
            CopyChannels(interleaved, sourceChannels, _buffer, channels, frames);

            if (settings.TrimSilence) frames = TrimmedFrames(_buffer, frames, channels, sampleRate);
            var samples = _buffer.GetSubArray(0, frames * channels);
            ApplyFadeOut(samples, channels, (int)math.round(math.clamp(settings.FadeOutMs, 0f, ExportSettings.MaxFadeOutMs) * sampleRate / 1000f));

            var analysis = _analyzer.Analyze(samples, channels, sampleRate);
            var gain = NormalizeGain(analysis, settings, out var limited);
            if (gain != 1f)
            {
                for (var i = 0; i < samples.Length; i++) samples[i] *= gain;
                analysis = _analyzer.Analyze(samples, channels, sampleRate);
            }

            _result = new ExportResult
            {
                Frames = frames,
                Channels = channels,
                Gain = gain,
                GainLimitedByPeak = limited,
                Analysis = analysis,
            };
            return _result;
        }

        public void Dispose()
        {
            _analyzer.Dispose();
            if (_buffer.IsCreated) _buffer.Dispose();
        }

        public static int OutputChannels(NativeArray<float> interleaved, int sourceChannels, ExportChannels mode) => mode switch
        {
            ExportChannels.Mono => 1,
            ExportChannels.Stereo => 2,
            _ => IsMono(interleaved, sourceChannels) ? 1 : sourceChannels,
        };

        public static bool IsMono(NativeArray<float> interleaved, int channels)
        {
            if (channels == 1) return true;

            for (var i = 0; i < interleaved.Length; i += channels)
            {
                for (var c = 1; c < channels; c++)
                    if (math.abs(interleaved[i + c] - interleaved[i]) > MonoTolerance) return false;
            }

            return true;
        }

        // Keeps everything up to the -60 dB point relative to the peak, plus a little padding so
        // the fade-out lands in the tail instead of cutting the last audible bit.
        public static int TrimmedFrames(NativeArray<float> interleaved, int frames, int channels, int sampleRate)
        {
            var peak = 0f;
            for (var i = 0; i < frames * channels; i++) peak = math.max(peak, math.abs(interleaved[i]));
            if (peak <= 0f) return math.min(frames, 1);

            var threshold = peak * AudioMath.DbToLinear(SfxAnalysis.EffectiveLengthDb);
            var last = 0;
            for (var f = frames - 1; f >= 0 && last == 0; f--)
            {
                for (var c = 0; c < channels; c++)
                {
                    if (math.abs(interleaved[f * channels + c]) < threshold) continue;
                    last = f + 1;
                    break;
                }
            }

            var padding = (int)math.round(TrimPaddingMs * sampleRate / 1000f);
            return math.clamp(last + padding, 1, frames);
        }

        // Raised-cosine fade that reaches exactly zero on the last frame.
        public static void ApplyFadeOut(NativeArray<float> interleaved, int channels, int fadeFrames)
        {
            var frames = interleaved.Length / channels;
            fadeFrames = math.min(fadeFrames, frames);
            if (fadeFrames <= 0) return;

            var first = frames - fadeFrames;
            for (var f = first; f < frames; f++)
            {
                var t = (f - first + 1) / (float)fadeFrames;
                var gain = 0.5f + 0.5f * math.cos(math.PI * t);
                for (var c = 0; c < channels; c++) interleaved[f * channels + c] *= gain;
            }
        }

        // Loudness normalisation still respects the peak target, so a quiet, spiky sound
        // cannot be pushed into clipping to reach its loudness.
        public static float NormalizeGain(SfxAnalysis analysis, ExportSettings settings, out bool limitedByPeak)
        {
            limitedByPeak = false;
            if (settings.Normalize == NormalizeMode.None || analysis.TruePeak <= 0f) return 1f;

            var peakGain = AudioMath.DbToLinear(math.min(settings.PeakTargetDb, 0f)) / analysis.TruePeak;
            if (settings.Normalize == NormalizeMode.Peak) return peakGain;
            if (analysis.LoudnessLufs <= AudioMath.SilenceDb) return 1f;

            var loudnessGain = AudioMath.DbToLinear(settings.LoudnessTargetLufs - analysis.LoudnessLufs);
            limitedByPeak = loudnessGain > peakGain;
            return math.min(loudnessGain, peakGain);
        }

        private static void CopyChannels(NativeArray<float> source, int sourceChannels, NativeArray<float> destination,
            int channels, int frames)
        {
            for (var f = 0; f < frames; f++)
            {
                var sourceBase = f * sourceChannels;
                if (channels == 1)
                {
                    var sum = 0f;
                    for (var c = 0; c < sourceChannels; c++) sum += source[sourceBase + c];
                    destination[f] = sum / sourceChannels;
                    continue;
                }

                for (var c = 0; c < channels; c++)
                    destination[f * channels + c] = source[sourceBase + math.min(c, sourceChannels - 1)];
            }
        }

        private void EnsureCapacity(int required)
        {
            required = math.max(required, 1);
            if (_buffer.IsCreated && _buffer.Length >= required) return;
            if (_buffer.IsCreated) _buffer.Dispose();
            _buffer = new NativeArray<float>(required, Allocator.Persistent);
        }
    }
}
