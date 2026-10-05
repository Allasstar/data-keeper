using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Forge
{
    [CreateAssetMenu(menuName = "DataKeeper/Forge/SFX Recipe", fileName = "SFX Recipe")]
    public class SfxRecipe : ScriptableObject
    {
        public const int MaxLayers = 6;
        public const float MinLengthMs = 50f;
        public const float MaxLengthMs = 4000f;
        public const int DefaultSampleRate = 48000;
        public const int DefaultRootNote = 69;
        public const int MinRootNote = 24;
        public const int MaxRootNote = 108;

        public SfxCategory Category;
        public uint Seed = 1;
        public float LengthMs = 500f;
        public int SampleRate = DefaultSampleRate;

        // MIDI note; every layer is transposed by its distance from A4, so layer Pitch stays relative.
        public int RootNote = DefaultRootNote;
        public List<Layer> Layers = new() { new Layer() };
        public Macros Macros = new();
        public LfoSettings Lfo = new();
        public LfoSettings Lfo2 = new();
        public LfoSettings Lfo3 = new();

        // Shared by every layer and spanning the whole sound, unlike Env 1 (each layer's amp curve).
        public Curve Env2 = Curve.DefaultModEnvelope();
        public Curve Env3 = Curve.DefaultModEnvelope();
        public RandomSettings Random = new();
        public List<ModRoute> Routes = ModRoute.Defaults();
        public FxChain Fx = new();
        public RandomizerSettings Randomizer = new();

        public string ToJson() => JsonUtility.ToJson(this, true);

        public void FromJson(string json) => JsonUtility.FromJsonOverwrite(json, this);

        private void OnValidate()
        {
            LengthMs = Mathf.Clamp(LengthMs, MinLengthMs, MaxLengthMs);
            SampleRate = Mathf.Clamp(SampleRate, 8000, 192000);
            if (Seed == 0) Seed = 1;
            Randomizer.CandidateCount = Mathf.Clamp(Randomizer.CandidateCount, 1, Analysis.CandidateFilter.MaxCandidates);
        }
    }
}
