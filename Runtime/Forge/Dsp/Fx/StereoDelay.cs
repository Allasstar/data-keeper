using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp.Fx
{
    public static class StereoDelay
    {
        // One-pole lowpass in the feedback path so repeats darken like a tape or room would.
        private const float FeedbackDamping = 0.35f;

        public static int LineSize(int delayFrames) => delayFrames * 2;

        public static void Process(NativeArray<float> buffer, int frames, NativeArray<float> line, int delayFrames,
            float feedback, float mix, bool pingPong)
        {
            for (var i = 0; i < delayFrames * 2; i++) line[i] = 0f;

            var write = 0;
            var dampedLeft = 0f;
            var dampedRight = 0f;

            for (var frame = 0; frame < frames; frame++)
            {
                var delayedLeft = line[write * 2];
                var delayedRight = line[write * 2 + 1];
                dampedLeft += (delayedLeft - dampedLeft) * (1f - FeedbackDamping);
                dampedRight += (delayedRight - dampedRight) * (1f - FeedbackDamping);

                var inLeft = buffer[frame * 2];
                var inRight = buffer[frame * 2 + 1];

                // Ping-pong feeds each side's repeats into the other side.
                line[write * 2] = inLeft + (pingPong ? dampedRight : dampedLeft) * feedback;
                line[write * 2 + 1] = inRight + (pingPong ? dampedLeft : dampedRight) * feedback;

                buffer[frame * 2] = inLeft + delayedLeft * mix;
                buffer[frame * 2 + 1] = inRight + delayedRight * mix;

                write = write + 1 == delayFrames ? 0 : write + 1;
            }
        }
    }
}
