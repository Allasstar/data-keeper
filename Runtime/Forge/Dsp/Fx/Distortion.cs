using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp.Fx
{
    // Waveshaping at 2x: zero-stuff, halfband interpolate, shape, halfband filter, decimate.
    // The filter is symmetric and non-causal, which is free offline and adds no latency.
    public static class Distortion
    {
        // Odd taps of a 31-tap Blackman-windowed halfband (offsets 1, 3, 5 ... 13); even taps
        // are zero except the 0.5 centre.
        private static readonly float[] HalfbandTaps =
        {
            0.312633322f, -0.090106922f, 0.040107418f, -0.017917030f, 0.007100857f, -0.002230286f, 0.000410323f,
        };

        private const float CenterTap = 0.5f;

        public static void Process(NativeArray<float> buffer, int frames, NativeArray<float> scratch,
            DistortionMode mode, float drive, float mix)
        {
            for (var channel = 0; channel < 2; channel++)
            {
                for (var n = 0; n < frames; n++)
                {
                    var odd = 0f;
                    for (var k = 0; k < HalfbandTaps.Length; k++)
                        odd += HalfbandTaps[k] * (Input(buffer, frames, channel, n - k) + Input(buffer, frames, channel, n + k + 1));

                    scratch[n * 2] = Shape(Input(buffer, frames, channel, n) * drive, mode);
                    scratch[n * 2 + 1] = Shape(2f * odd * drive, mode);
                }

                var oversampled = frames * 2;
                for (var n = 0; n < frames; n++)
                {
                    var center = n * 2;
                    var filtered = CenterTap * scratch[center];
                    for (var k = 0; k < HalfbandTaps.Length; k++)
                    {
                        var offset = 2 * k + 1;
                        filtered += HalfbandTaps[k] * (Oversampled(scratch, oversampled, center - offset)
                                                       + Oversampled(scratch, oversampled, center + offset));
                    }

                    var index = n * 2 + channel;
                    buffer[index] = math.lerp(buffer[index], filtered, mix);
                }
            }
        }

        public static float Shape(float x, DistortionMode mode) =>
            mode == DistortionMode.Foldback ? Fold(x) : math.tanh(x);

        // Triangle-wave fold: identity inside [-1, 1], reflected back from the rails beyond.
        private static float Fold(float x)
        {
            var phase = (x + 1f) * 0.25f;
            return 4f * math.abs(phase - math.floor(phase + 0.5f)) - 1f;
        }

        private static float Input(NativeArray<float> buffer, int frames, int channel, int frame) =>
            (uint)frame < (uint)frames ? buffer[frame * 2 + channel] : 0f;

        private static float Oversampled(NativeArray<float> scratch, int length, int index) =>
            (uint)index < (uint)length ? scratch[index] : 0f;
    }
}
