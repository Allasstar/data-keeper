using System;

namespace DataKeeper.Forge
{
    // Fixed processing order: transient shaper, distortion, delay, reverb, limiter.
    [Serializable]
    public class FxChain
    {
        public TransientSettings Transient = new() { Attack = 0.3f };
        public DistortionSettings Distortion = new() { DriveDb = 6f, Mix = 1f };
        public DelaySettings Delay = new() { TimeMs = 180f, Feedback = 0.35f, Mix = 0.25f };
        public ReverbSettings Reverb = new() { Size = 0.5f, Damping = 0.5f, Mix = 0.2f };
        public LimiterSettings Limiter = new() { Enabled = true, CeilingDb = -1f, ReleaseMs = 60f };
    }

    [Serializable]
    public struct TransientSettings
    {
        public bool Enabled;
        public float Attack;
        public float Sustain;
    }

    [Serializable]
    public struct DistortionSettings
    {
        public const float MaxDriveDb = 36f;

        public bool Enabled;
        public DistortionMode Mode;
        public float DriveDb;
        public float Mix;
    }

    [Serializable]
    public struct DelaySettings
    {
        public const float MinTimeMs = 10f;
        public const float MaxTimeMs = 1000f;
        public const float MaxFeedback = 0.9f;

        public bool Enabled;
        public float TimeMs;
        public float Feedback;
        public float Mix;
        public bool PingPong;
    }

    [Serializable]
    public struct ReverbSettings
    {
        public bool Enabled;
        public float Size;
        public float Damping;
        public float Mix;
    }

    [Serializable]
    public struct LimiterSettings
    {
        public const float MinCeilingDb = -12f;
        public const float MinReleaseMs = 5f;
        public const float MaxReleaseMs = 500f;

        public bool Enabled;
        public float CeilingDb;
        public float ReleaseMs;
    }
}
