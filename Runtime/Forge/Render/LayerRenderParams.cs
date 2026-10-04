using Unity.Mathematics;

namespace DataKeeper.Forge.Render
{
    public struct CurveRef
    {
        public int Start;
        public int Count;
        public float Min;
        public float Max;
    }

    public struct LayerRenderParams
    {
        public SourceType Source;
        public Waveform Waveform;
        public NoiseColor NoiseColor;
        public float Pitch;
        public float Gain;
        public float Pan;
        public int StartFrame;
        public int EndFrame;
        public int VoiceFrames;
        public FilterType Filter;
        public float CutoffHz;
        public float FilterK;
        public uint Seed;

        public float FmRatio;
        public float FmIndex;
        public float FmIndexEnvelope;

        public int WavetableOffset;
        public float WavetablePosition;

        public int SampleOffset;
        public int SampleLength;
        public float SampleStep;
        public float SampleStart;
        public bool SampleReverse;
        public bool SampleCubic;

        public int GrainFrames;
        public float GrainInterval;
        public float GrainSpray;
        public float GrainPitchRandom;

        public Waveform LfoShape;
        public float LfoRateHz;
        public float4 LfoDepth;
        public float4 EnvelopeDepth;

        public CurveRef AmpCurve;
        public CurveRef PitchCurve;
        public CurveRef CutoffCurve;
        public CurveRef PanCurve;
    }
}
