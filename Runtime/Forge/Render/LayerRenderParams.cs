using Unity.Collections;
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

    public struct LfoParams
    {
        public Waveform Shape;
        public float RateHz;
        public float Phase;
        public LfoMode Mode;
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

        // Always Voices entries, even for one voice: the mono path reads its start phase here.
        public int Voices;
        public FixedList64Bytes<float> VoiceRatios;
        public FixedList64Bytes<float> VoicePhases;
        public FixedList128Bytes<float2> VoiceGains;

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

        // Layer start in seconds: Free LFOs, Env 2/3 and moving Rnd run on the sound's timeline.
        public float StartSeconds;
        public LfoParams Lfo1;
        public LfoParams Lfo2;
        public LfoParams Lfo3;
        public RandomMode RandomMode;
        public float RandomRateHz;

        public float4 LfoDepth;
        public float4 Lfo2Depth;
        public float4 Lfo3Depth;
        public float4 EnvelopeDepth;
        public float4 Env2Depth;
        public float4 Env3Depth;
        public float4 RandomDepth;

        public CurveRef Env2Curve;
        public CurveRef Env3Curve;
        public CurveRef AmpCurve;
        public CurveRef PitchCurve;
        public CurveRef CutoffCurve;
        public CurveRef PanCurve;
    }
}
