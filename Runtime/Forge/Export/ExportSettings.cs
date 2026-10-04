using System;

namespace DataKeeper.Forge.Export
{
    public enum NormalizeMode
    {
        None = 0,
        Peak = 1,
        Loudness = 2,
    }

    public enum ExportChannels
    {
        Auto = 0,
        Mono = 1,
        Stereo = 2,
    }

    [Serializable]
    public class ExportSettings
    {
        public const string DefaultTemplate = "sfx_{category}_{name}_{n}";
        public const int MaxCount = 64;
        public const float MaxFadeOutMs = 500f;

        public string Folder = "Assets/Audio/SFX";
        public string NameTemplate = DefaultTemplate;
        public int Count = 1;
        public WavFormat Format = WavFormat.Pcm16;
        public ExportChannels Channels = ExportChannels.Auto;
        public NormalizeMode Normalize = NormalizeMode.None;
        public float PeakTargetDb = -1f;
        public float LoudnessTargetLufs = -16f;
        public bool TrimSilence = true;
        public float FadeOutMs = 5f;
    }
}
