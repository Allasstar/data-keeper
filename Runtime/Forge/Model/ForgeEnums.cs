using UnityEngine;

namespace DataKeeper.Forge
{
    // Explicit values: these are serialized as ints in recipe assets and JSON presets.
    public enum SfxCategory
    {
        Impact = 0,
        Whoosh = 1,
        Magic = 2,
        UIClick = 3,
        Pickup = 4,
        Laser = 5,
        Explosion = 6,
        Footstep = 7,
    }

    public enum SourceType
    {
        Oscillator = 0,
        Noise = 1,
        Wavetable = 2,
        FM = 3,
        Sample = 4,
        Granular = 5,
    }

    public enum WavetableBank
    {
        Basic = 0,
        Harmonic = 1,
        Pulse = 2,
        Formant = 3,
        Metallic = 4,
    }

    public enum SampleInterpolation
    {
        Linear = 0,
        Cubic = 1,
    }

    public enum DistortionMode
    {
        Tanh = 0,
        Foldback = 1,
    }

    public enum Waveform
    {
        Sine = 0,
        Saw = 1,
        Square = 2,
        Triangle = 3,
    }

    public enum NoiseColor
    {
        White = 0,
        Pink = 1,
        Brown = 2,
    }

    public enum FilterType
    {
        Off = 0,
        LowPass = 1,
        HighPass = 2,
        BandPass = 3,
        Notch = 4,
    }

    public enum Band
    {
        Sub = 0,
        Body = 1,
        Click = 2,
        Air = 3,
    }

    public enum HarmonyMode
    {
        Unison = 0,
        Fifths = 1,
        Major = 2,
        Minor = 3,
        Dissonant = 4,
    }

    // Per-parameter randomizer locks on a layer. Values are bit flags stored as an int.
    [System.Flags]
    public enum LayerParam
    {
        None = 0,
        Source = 1 << 0,
        Pitch = 1 << 1,
        FilterType = 1 << 2,
        Cutoff = 1 << 3,
        Resonance = 1 << 4,
        Level = 1 << 5,
        Pan = 1 << 6,
        Decay = 1 << 7,
        Offset = 1 << 8,
    }

    public enum CurveUnit
    {
        Normalized = 0,
        Gain = 1,
        Semitones = 2,
        Hertz = 3,
        Pan = 4,
        Octaves = 5,
    }

    public enum CurveTarget
    {
        Pitch = 0,
        Cutoff = 1,
        Amp = 2,
        Pan = 3,
    }

    // InspectorName gives the editor's EnumFields (route rows, Inspector) the names the source
    // bar uses; they must match ForgeModulation's source names.
    public enum ModSource
    {
        Size = 0,
        Energy = 1,
        Tone = 2,
        Motion = 3,
        [InspectorName("LFO 1")] Lfo = 4,
        [InspectorName("Env 1")] Envelope = 5,
        [InspectorName("Rnd 1")] Random = 6,
        [InspectorName("LFO 2")] Lfo2 = 7,
        [InspectorName("LFO 3")] Lfo3 = 8,
        [InspectorName("Env 2")] Env2 = 9,
        [InspectorName("Env 3")] Env3 = 10,
        [InspectorName("Rnd 2")] Random2 = 11,
        [InspectorName("Rnd 3")] Random3 = 12,
    }

    public enum LfoMode
    {
        Retrigger = 0,
        Free = 1,
    }

    public enum RandomMode
    {
        Constant = 0,
        [InspectorName("Sample & Hold")] SampleHold = 1,
        Smooth = 2,
    }

    public enum ModTarget
    {
        Pitch = 0,
        Cutoff = 1,
        Level = 2,
        Pan = 3,
        Decay = 4,
        Resonance = 5,
        Length = 6,
        Drive = 7,
        ReverbMix = 8,
        DelayMix = 9,
        TransientAttack = 10,
        LfoRate = 11,
        LfoDepth = 12,
    }
}
