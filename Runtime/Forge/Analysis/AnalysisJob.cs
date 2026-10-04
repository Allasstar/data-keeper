using DataKeeper.Forge.Dsp;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace DataKeeper.Forge.Analysis
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
    public struct AnalysisJob : IJob
    {
        public const int FftSize = 2048;
        public const int TruePeakPhases = 4;
        public const int TruePeakTaps = 16;

        private const float BlockSeconds = 0.4f;
        private const int StepsPerBlock = 4;
        private const double AbsoluteGateLufs = -70.0;
        private const double RelativeGateLu = -10.0;

        // Inter-sample peaks only overshoot noticeably next to samples that are already large,
        // so quiet stretches skip the oversampling filter.
        private const float TruePeakSearchRatio = 0.5f;

        [ReadOnly] public NativeArray<float> Samples;
        public int Channels;
        public int SampleRate;
        public KWeighting Weighting;

        [ReadOnly] public NativeArray<float2> Twiddles;
        [ReadOnly] public NativeArray<float> Window;
        [ReadOnly] public NativeArray<float> TruePeakKernel;
        public NativeArray<float> FftRe;
        public NativeArray<float> FftIm;
        public NativeArray<double> StepPower;
        public NativeArray<SfxAnalysis> Result;

        public void Execute()
        {
            var frames = Samples.Length / Channels;
            var result = new SfxAnalysis { LengthMs = frames * 1000f / SampleRate };

            if (frames == 0)
            {
                result.LoudnessLufs = AudioMath.SilenceDb;
                Result[0] = result;
                return;
            }

            var stepFrames = math.max(1, (int)math.round(BlockSeconds / StepsPerBlock * SampleRate));
            var steps = (frames + stepFrames - 1) / stepFrames;
            for (var s = 0; s < steps; s++) StepPower[s] = 0.0;

            var sumSquares = 0.0;
            for (var c = 0; c < Channels; c++)
            {
                var weighting = Weighting;
                weighting.Reset();
                var sum = 0.0;

                for (var f = 0; f < frames; f++)
                {
                    var x = Samples[f * Channels + c];
                    var magnitude = math.abs(x);
                    if (!math.isfinite(x) || magnitude >= SfxAnalysis.ClipThreshold) result.ClippedSamples++;
                    if (!math.isfinite(x)) continue;

                    result.Peak = math.max(result.Peak, magnitude);
                    sum += x;
                    sumSquares += (double)x * x;

                    var weighted = weighting.Process(x);
                    StepPower[f / stepFrames] += weighted * weighted;
                }

                result.DcOffset = math.max(result.DcOffset, (float)math.abs(sum / frames));
            }

            result.Rms = (float)math.sqrt(sumSquares / ((double)frames * Channels));
            result.LoudnessLufs = (float)IntegratedLoudness(frames, stepFrames);
            result.EffectiveLengthMs = EffectiveFrames(frames, result.Peak) * 1000f / SampleRate;
            result.TruePeak = TruePeak(frames, result.Peak);
            result.SpectralCentroidHz = SpectralCentroid(frames);
            Result[0] = result;
        }

        // BS.1770 gated integrated loudness over 400 ms blocks with 75% overlap. Sounds shorter
        // than one block are measured as a single block, which is the useful answer for SFX.
        private double IntegratedLoudness(int frames, int stepFrames)
        {
            var fullSteps = frames / stepFrames;
            var blockFrames = (double)stepFrames * StepsPerBlock;

            if (fullSteps < StepsPerBlock)
            {
                var total = 0.0;
                for (var s = 0; s < (frames + stepFrames - 1) / stepFrames; s++) total += StepPower[s];
                return Loudness(total / frames);
            }

            var blocks = fullSteps - StepsPerBlock + 1;
            var gatedSum = 0.0;
            var gatedCount = 0;
            for (var b = 0; b < blocks; b++)
            {
                var power = BlockPower(b) / blockFrames;
                if (Loudness(power) <= AbsoluteGateLufs) continue;
                gatedSum += power;
                gatedCount++;
            }

            if (gatedCount == 0) return AudioMath.SilenceDb;

            var relativeGate = Loudness(gatedSum / gatedCount) + RelativeGateLu;
            var sum = 0.0;
            var count = 0;
            for (var b = 0; b < blocks; b++)
            {
                var power = BlockPower(b) / blockFrames;
                var loudness = Loudness(power);
                if (loudness <= AbsoluteGateLufs || loudness <= relativeGate) continue;
                sum += power;
                count++;
            }

            return count > 0 ? Loudness(sum / count) : AudioMath.SilenceDb;
        }

        private double BlockPower(int block)
        {
            var power = 0.0;
            for (var s = 0; s < StepsPerBlock; s++) power += StepPower[block + s];
            return power;
        }

        private static double Loudness(double meanSquare) =>
            meanSquare > 0.0 ? -0.691 + 10.0 * math.log10(meanSquare) : AudioMath.SilenceDb;

        private int EffectiveFrames(int frames, float peak)
        {
            if (peak <= 0f) return 0;

            var threshold = peak * AudioMath.DbToLinear(SfxAnalysis.EffectiveLengthDb);
            for (var f = frames - 1; f >= 0; f--)
            {
                for (var c = 0; c < Channels; c++)
                {
                    if (math.abs(Samples[f * Channels + c]) >= threshold) return f + 1;
                }
            }

            return 0;
        }

        private float TruePeak(int frames, float samplePeak)
        {
            var truePeak = samplePeak;
            var searchLevel = samplePeak * TruePeakSearchRatio;
            const int before = TruePeakTaps / 2 - 1;

            for (var c = 0; c < Channels; c++)
            {
                for (var f = 0; f + 1 < frames; f++)
                {
                    if (math.max(math.abs(Sample(f, c, frames)), math.abs(Sample(f + 1, c, frames))) < searchLevel)
                        continue;

                    for (var phase = 1; phase < TruePeakPhases; phase++)
                    {
                        var y = 0f;
                        var kernel = phase * TruePeakTaps;
                        for (var t = 0; t < TruePeakTaps; t++)
                            y += TruePeakKernel[kernel + t] * Sample(f - before + t, c, frames);
                        truePeak = math.max(truePeak, math.abs(y));
                    }
                }
            }

            return truePeak;
        }

        private float Sample(int frame, int channel, int frames)
        {
            if (frame < 0 || frame >= frames) return 0f;
            var x = Samples[frame * Channels + channel];
            return math.isfinite(x) ? x : 0f;
        }

        // Magnitude-weighted over all Hann-windowed frames of the mono mix, so loud parts of
        // the sound dominate the result the way they dominate perceived brightness.
        private float SpectralCentroid(int frames)
        {
            var weighted = 0.0;
            var total = 0.0;
            var binHz = (double)SampleRate / FftSize;

            for (var start = 0; start < frames; start += FftSize)
            {
                for (var i = 0; i < FftSize; i++)
                {
                    var mono = 0f;
                    for (var c = 0; c < Channels; c++) mono += Sample(start + i, c, frames);
                    FftRe[i] = mono / Channels * Window[i];
                    FftIm[i] = 0f;
                }

                Fft.Forward(FftRe, FftIm, Twiddles, FftSize);

                for (var bin = 1; bin < FftSize / 2; bin++)
                {
                    var magnitude = math.sqrt(FftRe[bin] * FftRe[bin] + FftIm[bin] * FftIm[bin]);
                    weighted += bin * binHz * magnitude;
                    total += magnitude;
                }
            }

            return total > 0.0 ? (float)(weighted / total) : 0f;
        }
    }
}
