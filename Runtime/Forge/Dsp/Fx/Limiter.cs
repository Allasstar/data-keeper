using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp.Fx
{
    // Offline lookahead limiter. The gain each frame needs is known up front, so a backward
    // pass ramps the gain down over the lookahead before every peak and a forward pass adds
    // the release. Both passes only ever lower the required gain, so no sample can exceed the
    // ceiling.
    public static class Limiter
    {
        private const float LookaheadMs = 5f;

        public static void Process(NativeArray<float> buffer, int frames, NativeArray<float> gain,
            int sampleRate, float ceiling, float releaseMs)
        {
            if (frames == 0) return;

            for (var frame = 0; frame < frames; frame++)
            {
                var peak = math.max(math.abs(buffer[frame * 2]), math.abs(buffer[frame * 2 + 1]));
                gain[frame] = peak > ceiling ? ceiling / peak : 1f;
            }

            var attackStep = 1f / math.max(1f, LookaheadMs * 0.001f * sampleRate);
            for (var frame = frames - 2; frame >= 0; frame--)
                gain[frame] = math.min(gain[frame], gain[frame + 1] + attackStep);

            var release = 1f - math.exp(-1f / (releaseMs * 0.001f * sampleRate));
            var previous = 1f;
            for (var frame = 0; frame < frames; frame++)
            {
                var current = math.min(gain[frame], previous + (1f - previous) * release);
                gain[frame] = current;
                previous = current;
            }

            for (var frame = 0; frame < frames; frame++)
            {
                buffer[frame * 2] *= gain[frame];
                buffer[frame * 2 + 1] *= gain[frame];
            }
        }
    }
}
