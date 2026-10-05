using System;
using UnityEngine;

namespace DataKeeper.Forge
{
    // One flat struct with a sub-struct per variant instead of [SerializeReference] polymorphism:
    // it converts to blittable render params trivially and stays Undo- and JSON-friendly.
    [Serializable]
    public struct SourceSettings
    {
        public SourceType Type;
        public OscillatorSettings Oscillator;
        public NoiseSettings Noise;
        public WavetableSettings Wavetable;
        public FmSettings Fm;
        public SampleSettings Sample;
        public GranularSettings Granular;
        public ShepardSettings Shepard;

        public static bool IsTonal(SourceType type) =>
            type == SourceType.Oscillator || type == SourceType.Wavetable || type == SourceType.FM;

        public static SourceSettings Default => new()
        {
            Type = SourceType.Oscillator,
            Oscillator = new OscillatorSettings { Waveform = Waveform.Sine },
            Noise = new NoiseSettings { Color = NoiseColor.White },
            Wavetable = new WavetableSettings { Bank = WavetableBank.Basic },
            Fm = FmSettings.Default,
            Sample = new SampleSettings { Interpolation = SampleInterpolation.Cubic },
            Granular = GranularSettings.Default,
            Shepard = ShepardSettings.Default,
        };

        public static bool UsesClip(SourceType type) => type == SourceType.Sample || type == SourceType.Granular;
    }

    [Serializable]
    public struct OscillatorSettings
    {
        public Waveform Waveform;
    }

    [Serializable]
    public struct NoiseSettings
    {
        public NoiseColor Color;
    }

    [Serializable]
    public struct WavetableSettings
    {
        public WavetableBank Bank;
        public float Position;
    }

    [Serializable]
    public struct FmSettings
    {
        public const float MinRatio = 0.25f;
        public const float MaxRatio = 16f;
        public const float MaxIndex = 20f;

        public float Ratio;
        public float Index;

        // How much the modulation index follows the amp curve: classic FM brightness decay.
        public float IndexEnvelope;

        public static FmSettings Default => new() { Ratio = 2f, Index = 2f, IndexEnvelope = 0.5f };
    }

    // Layer.Pitch transposes a sample relative to its recorded pitch instead of setting an
    // absolute note, so 0 plays the clip as recorded.
    [Serializable]
    public struct SampleSettings
    {
        public const float MaxStartMs = 2000f;

        public AudioClip Clip;
        public float StartMs;
        public bool Reverse;
        public SampleInterpolation Interpolation;
    }

    // Grains read the layer's sample settings (clip, start, reverse, interpolation). The read
    // position moves through the clip in real time while each grain plays at the layer pitch,
    // so pitch and time are independent.
    [Serializable]
    public struct GranularSettings
    {
        public const float MinGrainMs = 5f;
        public const float MaxGrainMs = 500f;
        public const float MinDensity = 1f;
        public const float MaxDensity = 400f;
        public const float MaxSprayMs = 1000f;
        public const float MaxPitchRandom = 12f;

        public float GrainMs;

        // Grains started per second.
        public float Density;
        public float SprayMs;

        // Semitones of random detune per grain.
        public float PitchRandom;

        public static GranularSettings Default => new() { GrainMs = 60f, Density = 30f, SprayMs = 20f };
    }

    // Old data deserializes as zeros: the renderer plays Partials 0 as MinPartials and Width 0
    // as the narrowest window, and Rate 0 is a static octave stack.
    [Serializable]
    public struct ShepardSettings
    {
        public const float MaxRateOctaves = 4f;
        public const int MinPartials = 4;
        public const int MaxPartials = 10;

        // Octaves per second every partial glides; negative falls.
        public float RateOctaves;
        public float Width;
        public int Partials;

        public static ShepardSettings Default => new() { RateOctaves = 1f, Width = 0.5f, Partials = 8 };
    }

    // Tonal sources only. Old data deserializes Voices as 0, which the renderer reads as 1.
    [Serializable]
    public struct UnisonSettings
    {
        public const int MinVoices = 1;
        public const int MaxVoices = 8;
        public const float MaxDetuneCents = 100f;

        public int Voices;

        // Total width: voices spread evenly across ±DetuneCents / 2.
        public float DetuneCents;
        public float Spread;

        public static UnisonSettings Default => new() { Voices = MinVoices };
    }

    [Serializable]
    public struct PhaseSettings
    {
        // Fraction of a cycle; Random replaces it with a per-voice phase from the layer seed.
        public float Start;
        public bool Random;
    }

    // Oscillator and Wavetable only. Old data deserializes as Off, which renders as before.
    [Serializable]
    public struct WarpSettings
    {
        public WarpMode Mode;
        public float Amount;

        public static bool Supports(SourceType type) => type == SourceType.Oscillator || type == SourceType.Wavetable;
    }
}
