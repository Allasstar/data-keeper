using System;
using System.Collections.Generic;

namespace DataKeeper.Forge
{
    // Amount is in the target's unit (semitones, octaves, dB, pan, mix). Macro and random
    // sources are bipolar (-1..1, a macro at 0.5 is neutral), the LFOs swing -1..1, Env 1 is
    // the layer's amp curve and Env 2/3 are the recipe's curves over the whole sound (0..1).
    [Serializable]
    public class ModRoute
    {
        public const int AllLayers = -1;

        public bool Enabled = true;
        public ModSource Source;
        public ModTarget Target;
        public int Layer = AllLayers;
        public float Amount;

        public ModRoute()
        {
        }

        public ModRoute(ModSource source, ModTarget target, float amount)
        {
            Source = source;
            Target = target;
            Amount = amount;
        }

        // What the spec asks of the macros: Size lowers pitch, lengthens and adds space;
        // Energy sharpens, drives and brightens; Tone tilts brightness; Motion adds movement.
        public static List<ModRoute> Defaults() => new()
        {
            new ModRoute(ModSource.Size, ModTarget.Pitch, -5f),
            new ModRoute(ModSource.Size, ModTarget.Decay, 1f),
            new ModRoute(ModSource.Size, ModTarget.Length, 0.5f),
            new ModRoute(ModSource.Size, ModTarget.ReverbMix, 0.3f),
            new ModRoute(ModSource.Energy, ModTarget.TransientAttack, 0.6f),
            new ModRoute(ModSource.Energy, ModTarget.Drive, 12f),
            new ModRoute(ModSource.Energy, ModTarget.Cutoff, 1f),
            new ModRoute(ModSource.Tone, ModTarget.Cutoff, 2f),
            new ModRoute(ModSource.Motion, ModTarget.LfoDepth, 1f),
            new ModRoute(ModSource.Motion, ModTarget.DelayMix, 0.25f),
        };
    }

    [Serializable]
    public class LfoSettings
    {
        public const float MinRateHz = 0.05f;
        public const float MaxRateHz = 40f;

        public Waveform Shape = Waveform.Sine;
        public float RateHz = 4f;
        public float Phase;
        public LfoMode Mode;
    }

    // Constant is one fixed value per route and layer; the moving modes make that Rnd a
    // continuous source with one signal per layer.
    [Serializable]
    public class RandomSettings
    {
        public const float MinRateHz = 0.1f;
        public const float MaxRateHz = 40f;

        public RandomMode Mode;
        public float RateHz = 4f;
    }

    public static class ModTargets
    {
        public static bool IsPerLayer(ModTarget target) => target <= ModTarget.Resonance;

        // Only these are evaluated at control rate, so only they can follow a continuous source.
        public static bool IsContinuous(ModTarget target) => target <= ModTarget.Pan;

        public static bool IsContinuous(ModSource source, RandomMode randomMode) => source switch
        {
            ModSource.Lfo or ModSource.Lfo2 or ModSource.Lfo3 => true,
            ModSource.Envelope or ModSource.Env2 or ModSource.Env3 => true,
            ModSource.Random or ModSource.Random2 or ModSource.Random3 => randomMode != RandomMode.Constant,
            _ => false,
        };

        public static bool IsRandom(ModSource source) =>
            source is ModSource.Random or ModSource.Random2 or ModSource.Random3;

        public static float MaxAmount(ModTarget target) => target switch
        {
            ModTarget.Pitch => 24f,
            ModTarget.Cutoff => 4f,
            ModTarget.Level => 24f,
            ModTarget.Decay => 2f,
            ModTarget.Length => 2f,
            ModTarget.Drive => 24f,
            ModTarget.LfoRate => 3f,
            _ => 1f,
        };
    }
}
