using System;
using System.Collections.Generic;

namespace DataKeeper.Forge
{
    // Amount is in the target's unit (semitones, octaves, dB, pan, mix). Macro and random
    // sources are bipolar (-1..1, a macro at 0.5 is neutral), the LFO swings -1..1 and the
    // envelope is the layer's amp curve (0..1).
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
    }

    public static class ModTargets
    {
        public static bool IsPerLayer(ModTarget target) => target <= ModTarget.Resonance;

        // Only these are evaluated at control rate, so only they can follow the LFO or envelope.
        public static bool IsContinuous(ModTarget target) => target <= ModTarget.Pan;

        public static bool IsContinuous(ModSource source) => source == ModSource.Lfo || source == ModSource.Envelope;

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
