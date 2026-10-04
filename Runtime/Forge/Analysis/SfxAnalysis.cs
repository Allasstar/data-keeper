using DataKeeper.Forge.Dsp;

namespace DataKeeper.Forge.Analysis
{
    public struct SfxAnalysis
    {
        public const float ClipThreshold = 0.999f;
        public const float EffectiveLengthDb = -60f;

        public float LengthMs;
        public float Peak;
        public float TruePeak;
        public float Rms;
        public float LoudnessLufs;
        public float SpectralCentroidHz;
        public float EffectiveLengthMs;
        public float DcOffset;
        public int ClippedSamples;

        public float PeakDb => AudioMath.LinearToDb(Peak);
        public float TruePeakDb => AudioMath.LinearToDb(TruePeak);
        public float RmsDb => AudioMath.LinearToDb(Rms);
        public float CrestFactorDb => Rms > 0f ? PeakDb - RmsDb : 0f;
        public bool IsClipping => ClippedSamples > 0;
        public bool IsSilent => Peak <= 0f;
    }
}
