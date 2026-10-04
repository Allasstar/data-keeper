using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp.Fx
{
    // Differential envelopes: a fast follower running ahead of a slow one marks an attack, a
    // long-release follower lingering above the fast one marks the sustain. Stereo-linked.
    public static class TransientShaper
    {
        private const float MaxBoostDb = 12f;
        private const float Epsilon = 1e-6f;

        public static void Process(NativeArray<float> buffer, int frames, int sampleRate, float attack, float sustain)
        {
            var fastAttack = Coefficient(0.5f, sampleRate);
            var slowAttack = Coefficient(20f, sampleRate);
            var release = Coefficient(50f, sampleRate);
            var longRelease = Coefficient(200f, sampleRate);

            var fast = 0f;
            var slow = 0f;
            var tail = 0f;

            for (var frame = 0; frame < frames; frame++)
            {
                var level = math.max(math.abs(buffer[frame * 2]), math.abs(buffer[frame * 2 + 1]));
                fast = Follow(fast, level, fastAttack, release);
                slow = Follow(slow, level, slowAttack, release);
                tail = Follow(tail, level, fastAttack, longRelease);

                var transient = math.saturate((fast - slow) / (fast + Epsilon));
                var sustained = math.saturate((tail - fast) / (tail + Epsilon));
                var gain = AudioMath.DbToLinear(MaxBoostDb * (attack * transient + sustain * sustained));

                buffer[frame * 2] *= gain;
                buffer[frame * 2 + 1] *= gain;
            }
        }

        private static float Follow(float envelope, float level, float attack, float release)
        {
            var coefficient = level > envelope ? attack : release;
            return level + (envelope - level) * coefficient;
        }

        private static float Coefficient(float ms, int sampleRate) => math.exp(-1f / (ms * 0.001f * sampleRate));
    }
}
