using System;
using DataKeeper.Forge.Dsp;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace DataKeeper.Forge.Analysis
{
    public sealed class SfxAnalyzer : IDisposable
    {
        private NativeArray<float2> _twiddles;
        private NativeArray<float> _window;
        private NativeArray<float> _truePeakKernel;
        private NativeArray<float> _fftRe;
        private NativeArray<float> _fftIm;
        private NativeArray<double> _stepPower;
        private NativeArray<SfxAnalysis> _result;

        public SfxAnalyzer()
        {
            const int size = AnalysisJob.FftSize;
            _twiddles = new NativeArray<float2>(size / 2, Allocator.Persistent);
            _window = new NativeArray<float>(size, Allocator.Persistent);
            _truePeakKernel = new NativeArray<float>(AnalysisJob.TruePeakPhases * AnalysisJob.TruePeakTaps, Allocator.Persistent);
            _fftRe = new NativeArray<float>(size, Allocator.Persistent);
            _fftIm = new NativeArray<float>(size, Allocator.Persistent);
            _result = new NativeArray<SfxAnalysis>(1, Allocator.Persistent);

            Fft.BuildTwiddles(_twiddles, size);
            for (var i = 0; i < size; i++) _window[i] = (float)(0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / size));
            BuildTruePeakKernel(_truePeakKernel);
        }

        public SfxAnalysis Analyze(NativeArray<float> interleaved, int channels, int sampleRate)
        {
            var frames = interleaved.Length / channels;
            var stepFrames = math.max(1, (int)math.round(0.1f * sampleRate));
            EnsureCapacity(ref _stepPower, frames / stepFrames + 2);

            new AnalysisJob
            {
                Samples = interleaved,
                Channels = channels,
                SampleRate = sampleRate,
                Weighting = KWeighting.Create(sampleRate),
                Twiddles = _twiddles,
                Window = _window,
                TruePeakKernel = _truePeakKernel,
                FftRe = _fftRe,
                FftIm = _fftIm,
                StepPower = _stepPower,
                Result = _result,
            }.Run();

            return _result[0];
        }

        public void Dispose()
        {
            if (_twiddles.IsCreated) _twiddles.Dispose();
            if (_window.IsCreated) _window.Dispose();
            if (_truePeakKernel.IsCreated) _truePeakKernel.Dispose();
            if (_fftRe.IsCreated) _fftRe.Dispose();
            if (_fftIm.IsCreated) _fftIm.Dispose();
            if (_stepPower.IsCreated) _stepPower.Dispose();
            if (_result.IsCreated) _result.Dispose();
        }

        // Hann-windowed sinc per fractional phase; normalised so each phase has unity DC gain.
        private static void BuildTruePeakKernel(NativeArray<float> kernel)
        {
            const int taps = AnalysisJob.TruePeakTaps;
            const int before = taps / 2 - 1;
            const double halfWidth = taps / 2.0;

            for (var phase = 0; phase < AnalysisJob.TruePeakPhases; phase++)
            {
                var fraction = phase / (double)AnalysisJob.TruePeakPhases;
                var sum = 0.0;
                for (var t = 0; t < taps; t++)
                {
                    var x = fraction - (t - before);
                    var sinc = Math.Abs(x) < 1e-12 ? 1.0 : Math.Sin(Math.PI * x) / (Math.PI * x);
                    var window = 0.5 + 0.5 * Math.Cos(Math.PI * x / halfWidth);
                    var value = sinc * window;
                    kernel[phase * taps + t] = (float)value;
                    sum += value;
                }

                for (var t = 0; t < taps; t++) kernel[phase * taps + t] = (float)(kernel[phase * taps + t] / sum);
            }
        }

        private static void EnsureCapacity<T>(ref NativeArray<T> array, int required) where T : struct
        {
            if (array.IsCreated && array.Length >= required) return;
            if (array.IsCreated) array.Dispose();
            array = new NativeArray<T>(required, Allocator.Persistent);
        }
    }
}
