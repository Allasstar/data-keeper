using DataKeeper.Forge.Render;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp.Fx
{
    public struct FxParams
    {
        public bool TransientEnabled;
        public float TransientAttack;
        public float TransientSustain;

        public bool DistortionEnabled;
        public DistortionMode DistortionMode;
        public float DistortionDrive;
        public float DistortionDriveDb;
        public float DistortionMix;

        public bool CompressorEnabled;
        public float CompressorDepth;
        public float CompressorTime;
        public float CompressorUpward;
        public float CompressorDownward;
        public float CompressorGain;

        public bool DelayEnabled;
        public int DelayFrames;
        public float DelayFeedback;
        public float DelayMix;
        public bool DelayPingPong;

        public bool ReverbEnabled;
        public float ReverbSize;
        public float ReverbDamping;
        public float ReverbMix;

        public bool LimiterEnabled;
        public float LimiterCeiling;
        public float LimiterReleaseMs;

        public static int DelayFramesFor(float timeMs, int sampleRate) =>
            math.max(1, (int)math.round(
                math.clamp(timeMs, DelaySettings.MinTimeMs, DelaySettings.MaxTimeMs) * sampleRate / 1000f));

        public static FxParams From(FxChain fx, int sampleRate) => From(fx, sampleRate, default);

        public static FxParams From(FxChain fx, int sampleRate, in GlobalModulation mod) => new()
        {
            TransientEnabled = fx.Transient.Enabled,
            TransientAttack = math.clamp(fx.Transient.Attack + mod.TransientAttack, -1f, 1f),
            TransientSustain = math.clamp(fx.Transient.Sustain, -1f, 1f),

            DistortionEnabled = fx.Distortion.Enabled,
            DistortionMode = fx.Distortion.Mode,
            DistortionDrive = AudioMath.DbToLinear(DriveDbOf(fx, mod)),
            DistortionDriveDb = DriveDbOf(fx, mod),
            DistortionMix = math.saturate(fx.Distortion.Mix),

            CompressorEnabled = fx.Compressor.Enabled,
            CompressorDepth = math.saturate(fx.Compressor.Depth + mod.CompressorDepth),
            CompressorTime = math.saturate(fx.Compressor.Time),
            CompressorUpward = math.saturate(fx.Compressor.Upward),
            CompressorDownward = math.saturate(fx.Compressor.Downward),
            CompressorGain = AudioMath.DbToLinear(
                math.clamp(fx.Compressor.GainDb, -CompressorSettings.MaxGainDb, CompressorSettings.MaxGainDb)),

            DelayEnabled = fx.Delay.Enabled,
            DelayFrames = DelayFramesFor(fx.Delay.TimeMs, sampleRate),
            DelayFeedback = math.clamp(fx.Delay.Feedback, 0f, DelaySettings.MaxFeedback),
            DelayMix = math.saturate(fx.Delay.Mix + mod.DelayMix),
            DelayPingPong = fx.Delay.PingPong,

            ReverbEnabled = fx.Reverb.Enabled,
            ReverbSize = math.saturate(fx.Reverb.Size),
            ReverbDamping = math.saturate(fx.Reverb.Damping),
            ReverbMix = math.saturate(fx.Reverb.Mix + mod.ReverbMix),

            LimiterEnabled = fx.Limiter.Enabled,
            LimiterCeiling = AudioMath.DbToLinear(math.clamp(fx.Limiter.CeilingDb, LimiterSettings.MinCeilingDb, 0f)),
            LimiterReleaseMs = math.clamp(fx.Limiter.ReleaseMs, LimiterSettings.MinReleaseMs, LimiterSettings.MaxReleaseMs),
        };

        private static float DriveDbOf(FxChain fx, in GlobalModulation mod) =>
            math.clamp(fx.Distortion.DriveDb + mod.DriveDb, 0f, DistortionSettings.MaxDriveDb);
    }
}
