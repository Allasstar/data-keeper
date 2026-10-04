using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DataKeeper.Forge
{
    [Serializable]
    public class CategoryTemplate
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Converters = { new StringEnumConverter() },
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        public SfxCategory Category;
        public FloatRange LengthMs = new(200f, 800f);

        // Carrier:modulator ratios for FM layers; consumed once the FM source exists.
        public List<float> FmRatios = new();
        public FxTemplate Fx = new();
        public List<LayerTemplate> Layers = new();

        public static CategoryTemplate FromJson(string json) =>
            JsonConvert.DeserializeObject<CategoryTemplate>(json, JsonSettings);

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented, JsonSettings);
    }

    [Serializable]
    public class LayerTemplate
    {
        public string Name = "Layer";
        public Band Band = Band.Body;
        public float Probability = 1f;
        public bool Tonal;
        public List<SourceType> Sources = new();
        public List<Waveform> Waveforms = new();
        public List<NoiseColor> NoiseColors = new();
        public List<WavetableBank> Wavetables = new();
        public FloatRange WavetablePosition = new(0f, 1f);
        public FloatRange FmIndex = new(1f, 4f);
        public FloatRange FmIndexEnvelope = new(0.3f, 1f);
        public FloatRange Pitch = new(-12f, 12f);
        public List<FilterType> Filters = new();
        public FloatRange CutoffHz = new(200f, 8000f);
        public FloatRange Resonance = new(0f, 0.3f);
        public FloatRange LevelDb = new(-12f, -6f);
        public FloatRange Pan;
        public FloatRange DecayMs = new(100f, 500f);
        public FloatRange OffsetMs;
        public CurveTemplate AmpCurve;
        public CurveTemplate PitchCurve;
        public CurveTemplate CutoffCurve;
        public CurveTemplate PanCurve;

        public CurveTemplate GetCurve(CurveTarget target) => target switch
        {
            CurveTarget.Pitch => PitchCurve,
            CurveTarget.Cutoff => CutoffCurve,
            CurveTarget.Pan => PanCurve,
            _ => AmpCurve,
        };
    }

    // A zero range leaves that effect off; Energy drives transients and drive, Size the space.
    [Serializable]
    public class FxTemplate
    {
        public FloatRange TransientAttack;
        public FloatRange DriveDb;
        public List<DistortionMode> DistortionModes = new();
        public float DelayProbability;
        public FloatRange DelayTimeMs = new(80f, 300f);
        public FloatRange DelayFeedback = new(0.2f, 0.5f);
        public FloatRange DelayMix;
        public FloatRange ReverbMix;
        public FloatRange ReverbSize = new(0.3f, 0.8f);
        public FloatRange ReverbDamping = new(0.3f, 0.7f);
    }

    [Serializable]
    public class CurveTemplate
    {
        public List<Breakpoint> Points = new();
        public float TimeJitter;
        public float ValueJitter;
        public float TensionJitter;

        // Index of the breakpoint that ends the attack; Energy moves it earlier. -1 disables.
        public int AttackIndex = -1;

        // Output range of the curve; left equal, the target's default range is used.
        public float Min;
        public float Max;

        // Pitch curves only: values land on the harmony mode's scale degrees.
        public bool SnapToHarmony;
    }
}
