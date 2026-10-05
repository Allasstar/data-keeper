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

    // Seed is already the layer's seed for this Rnd source.
    public struct RandomParams
    {
        public RandomMode Mode;
        public float RateHz;
        public uint Seed;
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

        // Octaves per second, the window's Gaussian sigma in octaves, and the octave count.
        public float ShepardRate;
        public float ShepardWidthOctaves;
        public int ShepardPartials;

        // Off for sources without warp and for modes not implemented yet. WarpAmount is the knob
        // plus static routes; the job adds the lane and saturates.
        public WarpMode Warp;
        public float WarpAmount;

        // Layer start in seconds: Free LFOs, Env 2/3 and moving Rnd run on the sound's timeline.
        public float StartSeconds;
        public LfoParams Lfo1;
        public LfoParams Lfo2;
        public LfoParams Lfo3;
        public RandomParams Random1;
        public RandomParams Random2;
        public RandomParams Random3;

        public float4 LfoDepth;
        public float4 Lfo2Depth;
        public float4 Lfo3Depth;
        public float4 EnvelopeDepth;
        public float4 Env2Depth;
        public float4 Env3Depth;
        public float4 RandomDepth;
        public float4 Random2Depth;
        public float4 Random3Depth;

        public float LfoWarpDepth;
        public float Lfo2WarpDepth;
        public float Lfo3WarpDepth;
        public float EnvelopeWarpDepth;
        public float Env2WarpDepth;
        public float Env3WarpDepth;
        public float RandomWarpDepth;
        public float Random2WarpDepth;
        public float Random3WarpDepth;

        public CurveRef Env2Curve;
        public CurveRef Env3Curve;
        public CurveRef AmpCurve;
        public CurveRef PitchCurve;
        public CurveRef CutoffCurve;
        public CurveRef PanCurve;
    }
}
