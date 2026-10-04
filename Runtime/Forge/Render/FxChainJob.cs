using DataKeeper.Forge.Dsp.Fx;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace DataKeeper.Forge.Render
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
    public struct FxChainJob : IJob
    {
        public int FrameCount;
        public int SampleRate;
        public FxParams Fx;
        public NativeArray<float> Buffer;
        public NativeArray<float> Scratch;
        public NativeArray<float> DelayLine;
        public NativeArray<float> ReverbMemory;

        public void Execute()
        {
            if (Fx.TransientEnabled)
                TransientShaper.Process(Buffer, FrameCount, SampleRate, Fx.TransientAttack, Fx.TransientSustain);

            if (Fx.DistortionEnabled)
                Distortion.Process(Buffer, FrameCount, Scratch, Fx.DistortionMode, Fx.DistortionDrive, Fx.DistortionMix);

            if (Fx.DelayEnabled)
                StereoDelay.Process(Buffer, FrameCount, DelayLine, Fx.DelayFrames, Fx.DelayFeedback, Fx.DelayMix, Fx.DelayPingPong);

            if (Fx.ReverbEnabled)
                Reverb.Process(Buffer, FrameCount, Scratch, ReverbMemory, SampleRate, Fx.ReverbSize, Fx.ReverbDamping, Fx.ReverbMix);

            if (Fx.LimiterEnabled)
                Limiter.Process(Buffer, FrameCount, Scratch, SampleRate, Fx.LimiterCeiling, Fx.LimiterReleaseMs);
        }
    }
}
