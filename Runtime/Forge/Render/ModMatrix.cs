using Unity.Mathematics;

namespace DataKeeper.Forge.Render
{
    public struct LayerModulation
    {
        public float Pitch;
        public float CutoffOctaves;
        public float LevelDb;
        public float Pan;
        public float DecayOctaves;
        public float Resonance;

        // x pitch (st), y cutoff (oct), z level (dB), w pan; applied at control rate.
        public float4 LfoDepth;
        public float4 EnvelopeDepth;
    }

    public struct GlobalModulation
    {
        public float LengthOctaves;
        public float DriveDb;
        public float ReverbMix;
        public float DelayMix;
        public float TransientAttack;
        public float LfoRateOctaves;
        public float LfoDepth;
    }

    // Static sources (macros, random) become fixed offsets before rendering; the LFO and the
    // envelope become per-layer depths the render job applies every control block.
    public static class ModMatrix
    {
        private const float MaxLfoDepthScale = 2f;

        public static GlobalModulation Evaluate(SfxRecipe recipe, uint seed, LayerModulation[] layers, int layerCount)
        {
            for (var i = 0; i < layerCount; i++) layers[i] = default;
            var global = new GlobalModulation();
            var routes = recipe.Routes;

            for (var r = 0; r < routes.Count; r++)
            {
                var route = routes[r];
                if (!route.Enabled || route.Amount == 0f || ModTargets.IsContinuous(route.Source)) continue;

                if (!ModTargets.IsPerLayer(route.Target))
                {
                    AddGlobal(ref global, route.Target, StaticValue(route.Source, recipe.Macros, seed, r, -1) * route.Amount);
                    continue;
                }

                for (var layer = 0; layer < layerCount; layer++)
                {
                    if (!Applies(route, layer)) continue;
                    AddLayer(ref layers[layer], route.Target, StaticValue(route.Source, recipe.Macros, seed, r, layer) * route.Amount);
                }
            }

            var lfoScale = math.clamp(1f + global.LfoDepth, 0f, MaxLfoDepthScale);
            for (var r = 0; r < routes.Count; r++)
            {
                var route = routes[r];
                if (!route.Enabled || route.Amount == 0f || !ModTargets.IsContinuous(route.Source)) continue;
                if (!ModTargets.IsContinuous(route.Target)) continue;

                var isLfo = route.Source == ModSource.Lfo;
                var amount = isLfo ? route.Amount * lfoScale : route.Amount;
                for (var layer = 0; layer < layerCount; layer++)
                {
                    if (!Applies(route, layer)) continue;
                    var depth = Component((int)route.Target, amount);
                    if (isLfo) layers[layer].LfoDepth += depth;
                    else layers[layer].EnvelopeDepth += depth;
                }
            }

            return global;
        }

        // Bipolar so that a macro at its 0.5 default contributes exactly nothing.
        public static float StaticValue(ModSource source, Macros macros, uint seed, int route, int layer) => source switch
        {
            ModSource.Size => macros.Size * 2f - 1f,
            ModSource.Energy => macros.Energy * 2f - 1f,
            ModSource.Tone => macros.Tone * 2f - 1f,
            ModSource.Motion => macros.Motion * 2f - 1f,
            ModSource.Random => math.hash(new uint3(seed, (uint)route, (uint)(layer + 1))) / (float)uint.MaxValue * 2f - 1f,
            _ => 0f,
        };

        public static float Lfo(Waveform shape, float cycles)
        {
            var phase = cycles - math.floor(cycles);
            return shape switch
            {
                Waveform.Saw => 2f * phase - 1f,
                Waveform.Square => phase < 0.5f ? 1f : -1f,
                Waveform.Triangle => 1f - 4f * math.abs(phase - 0.5f),
                _ => math.sin(2f * math.PI * phase),
            };
        }

        private static bool Applies(ModRoute route, int layer) => route.Layer < 0 || route.Layer == layer;

        private static float4 Component(int index, float value)
        {
            var result = float4.zero;
            result[index] = value;
            return result;
        }

        private static void AddLayer(ref LayerModulation mod, ModTarget target, float value)
        {
            switch (target)
            {
                case ModTarget.Pitch: mod.Pitch += value; break;
                case ModTarget.Cutoff: mod.CutoffOctaves += value; break;
                case ModTarget.Level: mod.LevelDb += value; break;
                case ModTarget.Pan: mod.Pan += value; break;
                case ModTarget.Decay: mod.DecayOctaves += value; break;
                case ModTarget.Resonance: mod.Resonance += value; break;
            }
        }

        private static void AddGlobal(ref GlobalModulation mod, ModTarget target, float value)
        {
            switch (target)
            {
                case ModTarget.Length: mod.LengthOctaves += value; break;
                case ModTarget.Drive: mod.DriveDb += value; break;
                case ModTarget.ReverbMix: mod.ReverbMix += value; break;
                case ModTarget.DelayMix: mod.DelayMix += value; break;
                case ModTarget.TransientAttack: mod.TransientAttack += value; break;
                case ModTarget.LfoRate: mod.LfoRateOctaves += value; break;
                case ModTarget.LfoDepth: mod.LfoDepth += value; break;
            }
        }
    }
}
