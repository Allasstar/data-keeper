using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp.Fx
{
    // OTT-style: three Linkwitz-Riley bands, each pushed down above one threshold and pulled up
    // below another, so quiet detail and loud peaks meet in the middle. The low band also runs
    // the high crossover's allpass, so the untouched bands sum to an allpass of the input. Depth
    // blends from that sum rather than from the raw input, which would notch the crossovers at
    // partial Depth (FS2-D25).
    public static class Compressor
    {
        public const float LowSplitHz = 88f;
        public const float HighSplitHz = 2500f;
        public const float DownThresholdDb = -24f;
        public const float DownRatio = 6f;
        public const float UpThresholdDb = -30f;
        public const float UpRatio = 4f;
        public const float MaxBoostDb = 24f;
        public const float FloorDb = -70f;
        public const float FloorFadeDb = 10f;
        public const float MakeupDb = 10f;

        private const float LowAttackMs = 40f;
        private const float MidAttackMs = 20f;
        private const float HighAttackMs = 10f;
        private const float LowReleaseMs = 280f;
        private const float MidReleaseMs = 200f;
        private const float HighReleaseMs = 130f;
        private const float FloorReleaseMs = 50f;
        private const float TimeRangeOctaves = 2f;
        private const float MinEnvelope = 1e-10f;
        private const double ButterworthQ = 0.70710678118654752;

        public static void Process(NativeArray<float> buffer, int frames, int sampleRate, float depth, float time,
            float upward, float downward, float gain)
        {
            if (depth <= 0f) return;

            var lowA = Biquad.LowPass(LowSplitHz, sampleRate);
            var lowB = lowA;
            var restA = Biquad.HighPass(LowSplitHz, sampleRate);
            var restB = restA;
            var midA = Biquad.LowPass(HighSplitHz, sampleRate);
            var midB = midA;
            var highA = Biquad.HighPass(HighSplitHz, sampleRate);
            var highB = highA;
            var lowAllpass = Biquad.AllPass(HighSplitHz, sampleRate);

            var framesPerMs = sampleRate * 0.001f * math.exp2(math.lerp(-TimeRangeOctaves, TimeRangeOctaves, time));
            var attack = math.exp(-1f / (new float3(LowAttackMs, MidAttackMs, HighAttackMs) * framesPerMs));
            var release = math.exp(-1f / (new float3(LowReleaseMs, MidReleaseMs, HighReleaseMs) * framesPerMs));
            var floorRelease = math.exp(-1f / (FloorReleaseMs * sampleRate * 0.001f));

            var envelope = float3.zero;
            var floorEnvelope = float3.zero;
            var gainDb = float3.zero;
            for (var frame = 0; frame < frames; frame++)
            {
                var x = new float2(buffer[frame * 2], buffer[frame * 2 + 1]);
                var below = lowB.Tick(lowA.Tick(x));
                var above = restB.Tick(restA.Tick(x));
                var low = lowAllpass.Tick(below);
                var mid = midB.Tick(midA.Tick(above));
                var high = highB.Tick(highA.Tick(above));

                var level = new float3(math.cmax(math.abs(low)), math.cmax(math.abs(mid)), math.cmax(math.abs(high)));
                envelope = math.max(level, envelope * release);
                // The floor has its own fast detector: the slow one lags a falling tail and would
                // keep boosting it after it has dropped below the floor.
                floorEnvelope = math.max(level, floorEnvelope * floorRelease);
                var target = GainDb(ToDb(envelope), ToDb(floorEnvelope), upward, downward);
                // The detector jumps to a new peak at once, so the attack is applied to the gain:
                // an onset out of silence then starts at unity instead of at the full upward boost.
                gainDb = target + (gainDb - target) * attack;
                var bandGain = math.lerp(1f, math.exp10(gainDb * 0.05f) * gain, depth);

                var y = low * bandGain.x + mid * bandGain.y + high * bandGain.z;
                buffer[frame * 2] = y.x;
                buffer[frame * 2 + 1] = y.y;
            }
        }

        // Static curve per band, also drawn by the FX graph. Makeup comes with Downward so that
        // Upward 0 and Downward 0 is exactly unity, and it fades with the floor like the boost.
        public static float3 GainDb(float3 levelDb, float upward, float downward) =>
            GainDb(levelDb, levelDb, upward, downward);

        private static float3 GainDb(float3 levelDb, float3 floorLevelDb, float upward, float downward)
        {
            var cut = math.max(levelDb - DownThresholdDb, 0f) * (downward * (1f - 1f / DownRatio));
            var boost = math.min(math.max(UpThresholdDb - levelDb, 0f) * (upward * (1f - 1f / UpRatio)), MaxBoostDb);
            var floor = math.saturate((floorLevelDb - FloorDb) / FloorFadeDb);
            return floor * (boost + downward * MakeupDb) - cut;
        }

        private static float3 ToDb(float3 envelope) => 20f * math.log10(math.max(envelope, MinEnvelope));

        private struct Biquad
        {
            private float _b0, _b1, _b2, _a1, _a2;
            private float2 _z1, _z2;

            public float2 Tick(float2 x)
            {
                var y = _b0 * x + _z1;
                _z1 = _b1 * x - _a1 * y + _z2;
                _z2 = _b2 * x - _a2 * y;
                return y;
            }

            public static Biquad LowPass(float hz, int sampleRate)
            {
                Prewarp(hz, sampleRate, out var cos, out var alpha);
                return Normalized((1.0 - cos) * 0.5, 1.0 - cos, (1.0 - cos) * 0.5, cos, alpha);
            }

            public static Biquad HighPass(float hz, int sampleRate)
            {
                Prewarp(hz, sampleRate, out var cos, out var alpha);
                return Normalized((1.0 + cos) * 0.5, -(1.0 + cos), (1.0 + cos) * 0.5, cos, alpha);
            }

            public static Biquad AllPass(float hz, int sampleRate)
            {
                Prewarp(hz, sampleRate, out var cos, out var alpha);
                return Normalized(1.0 - alpha, -2.0 * cos, 1.0 + alpha, cos, alpha);
            }

            private static void Prewarp(float hz, int sampleRate, out double cos, out double alpha)
            {
                var w0 = 2.0 * math.PI_DBL * hz / sampleRate;
                cos = math.cos(w0);
                alpha = math.sin(w0) / (2.0 * ButterworthQ);
            }

            private static Biquad Normalized(double b0, double b1, double b2, double cos, double alpha)
            {
                var a0 = 1.0 + alpha;
                return new Biquad
                {
                    _b0 = (float)(b0 / a0),
                    _b1 = (float)(b1 / a0),
                    _b2 = (float)(b2 / a0),
                    _a1 = (float)(-2.0 * cos / a0),
                    _a2 = (float)((1.0 - alpha) / a0),
                };
            }
        }
    }
}
