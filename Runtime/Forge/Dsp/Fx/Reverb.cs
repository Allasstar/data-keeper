using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp.Fx
{
    // Freeverb: eight damped combs in parallel then four allpasses in series per channel, the
    // right channel's delays offset by a fixed spread for width. Tunings are at 44.1 kHz.
    public static class Reverb
    {
        private static readonly int[] CombTunings = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
        private static readonly int[] AllpassTunings = { 556, 441, 341, 225 };

        private const int StereoSpread = 23;
        private const float FixedGain = 0.015f;
        private const float AllpassFeedback = 0.5f;
        private const float WetScale = 3f;

        public static int MemorySize(int sampleRate)
        {
            var total = 0;
            for (var channel = 0; channel < 2; channel++)
            {
                for (var i = 0; i < CombTunings.Length; i++) total += Scaled(CombTunings[i], channel, sampleRate);
                for (var i = 0; i < AllpassTunings.Length; i++) total += Scaled(AllpassTunings[i], channel, sampleRate);
            }

            return total;
        }

        public static void Process(NativeArray<float> buffer, int frames, NativeArray<float> wet,
            NativeArray<float> memory, int sampleRate, float size, float damping, float mix)
        {
            var feedback = size * 0.28f + 0.7f;
            var damp = damping * 0.4f;
            var memorySize = MemorySize(sampleRate);
            for (var i = 0; i < memorySize; i++) memory[i] = 0f;

            var offset = 0;
            for (var channel = 0; channel < 2; channel++)
            {
                for (var frame = 0; frame < frames; frame++) wet[frame * 2 + channel] = 0f;

                for (var c = 0; c < CombTunings.Length; c++)
                {
                    var length = Scaled(CombTunings[c], channel, sampleRate);
                    var index = 0;
                    var store = 0f;

                    for (var frame = 0; frame < frames; frame++)
                    {
                        var input = (buffer[frame * 2] + buffer[frame * 2 + 1]) * FixedGain;
                        var output = memory[offset + index];
                        store = output * (1f - damp) + store * damp;
                        memory[offset + index] = input + store * feedback;
                        index = index + 1 == length ? 0 : index + 1;
                        wet[frame * 2 + channel] += output;
                    }

                    offset += length;
                }

                for (var a = 0; a < AllpassTunings.Length; a++)
                {
                    var length = Scaled(AllpassTunings[a], channel, sampleRate);
                    var index = 0;

                    for (var frame = 0; frame < frames; frame++)
                    {
                        var input = wet[frame * 2 + channel];
                        var delayed = memory[offset + index];
                        wet[frame * 2 + channel] = delayed - input;
                        memory[offset + index] = input + delayed * AllpassFeedback;
                        index = index + 1 == length ? 0 : index + 1;
                    }

                    offset += length;
                }
            }

            var wetGain = mix * WetScale;
            for (var i = 0; i < frames * 2; i++) buffer[i] += wet[i] * wetGain;
        }

        private static int Scaled(int tuning, int channel, int sampleRate) =>
            math.max(1, (int)((tuning + StereoSpread * channel) * (long)sampleRate / 44100));
    }
}
