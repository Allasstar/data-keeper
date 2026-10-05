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
        public float Warp;

        // One depth per continuous source, x pitch (st), y cutoff (oct), z level (dB), w pan;
        // applied at control rate. LfoDepth and EnvelopeDepth are LFO 1 and Env 1.
        public float4 LfoDepth;
        public float4 Lfo2Depth;
        public float4 Lfo3Depth;
        public float4 EnvelopeDepth;
        public float4 Env2Depth;
        public float4 Env3Depth;
        public float4 RandomDepth;
        public float4 Random2Depth;
        public float4 Random3Depth;

        // The Warp lane: one depth per continuous source, kept out of the float4s (FS2-D3).
        public float LfoWarpDepth;
        public float Lfo2WarpDepth;
        public float Lfo3WarpDepth;
        public float EnvelopeWarpDepth;
        public float Env2WarpDepth;
        public float Env3WarpDepth;
        public float RandomWarpDepth;
        public float Random2WarpDepth;
        public float Random3WarpDepth;
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
        public float CompressorDepth;
    }

    // Static sources (macros, Constant random) become fixed offsets before rendering; the LFOs,
    // envelopes and moving random become per-layer depths the render job applies every control block.
    public static class ModMatrix
    {
        private const float MaxLfoDepthScale = 2f;
        private const uint RandomSalt = 0x52A4Du;
        private const uint RandomSourceSalt = 0x9E3779B9u;

        public static GlobalModulation Evaluate(SfxRecipe recipe, uint seed, LayerModulation[] layers, int layerCount)
        {
            for (var i = 0; i < layerCount; i++) layers[i] = default;
            var global = new GlobalModulation();
            var routes = recipe.Routes;

            for (var r = 0; r < routes.Count; r++)
            {
                var route = routes[r];
                if (!route.Enabled || route.Amount == 0f || ModTargets.IsContinuous(route.Source, recipe.RandomModeOf(route.Source))) continue;

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
                if (!route.Enabled || route.Amount == 0f || !ModTargets.IsContinuous(route.Source, recipe.RandomModeOf(route.Source))) continue;
                if (!ModTargets.IsContinuous(route.Target)) continue;

                // The LFO Depth target scales LFO 1 only.
                var amount = route.Source == ModSource.Lfo ? route.Amount * lfoScale : route.Amount;
                for (var layer = 0; layer < layerCount; layer++)
                {
                    if (!Applies(route, layer)) continue;
                    if (route.Target == ModTarget.Warp) AddWarpDepth(ref layers[layer], route.Source, amount);
                    else AddDepth(ref layers[layer], route.Source, Component((int)route.Target, amount));
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
            ModSource.Random or ModSource.Random2 or ModSource.Random3 =>
                Avalanche(math.hash(new uint3(RandomSeed(seed, source), (uint)route, (uint)(layer + 1)))) / (float)uint.MaxValue * 2f - 1f,
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

        // Rnd 1 keeps the seed as is, so recipes from before Rnd 2/3 render unchanged.
        public static uint RandomSeed(uint seed, ModSource source) =>
            source is ModSource.Random2 or ModSource.Random3 ? Avalanche(seed ^ (uint)source * RandomSourceSalt) : seed;

        // -1..1 for a whole step, from the layer seed.
        public static float RandomStep(uint seed, int step) =>
            (Avalanche(math.hash(new uint3(seed, (uint)step, RandomSalt))) >> 8) * (2f / (1 << 24)) - 1f;

        // lowbias32 (Chris Wellons). Every hash of neighbouring integers (steps, voices, routes,
        // seeds) goes through it, because math.hash alone is a linear combination of its inputs.
        public static uint Avalanche(uint x)
        {
            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;
            return x;
        }

        // steps = seconds × rate. Smooth is value noise: smoothstep between neighbouring steps,
        // so it is continuous and its slope never exceeds 1.5 × the step difference.
        public static float RandomSignal(RandomMode mode, uint seed, float steps)
        {
            var step = (int)math.floor(steps);
            var held = RandomStep(seed, step);
            if (mode != RandomMode.Smooth) return held;

            var f = steps - step;
            return math.lerp(held, RandomStep(seed, step + 1), f * f * (3f - 2f * f));
        }

        private static bool Applies(ModRoute route, int layer) => route.Layer < 0 || route.Layer == layer;

        private static float4 Component(int index, float value)
        {
            var result = float4.zero;
            result[index] = value;
            return result;
        }

        private static void AddDepth(ref LayerModulation mod, ModSource source, float4 depth)
        {
            switch (source)
            {
                case ModSource.Lfo: mod.LfoDepth += depth; break;
                case ModSource.Lfo2: mod.Lfo2Depth += depth; break;
                case ModSource.Lfo3: mod.Lfo3Depth += depth; break;
                case ModSource.Envelope: mod.EnvelopeDepth += depth; break;
                case ModSource.Env2: mod.Env2Depth += depth; break;
                case ModSource.Env3: mod.Env3Depth += depth; break;
                case ModSource.Random: mod.RandomDepth += depth; break;
                case ModSource.Random2: mod.Random2Depth += depth; break;
                case ModSource.Random3: mod.Random3Depth += depth; break;
            }
        }

        private static void AddWarpDepth(ref LayerModulation mod, ModSource source, float depth)
        {
            switch (source)
            {
                case ModSource.Lfo: mod.LfoWarpDepth += depth; break;
                case ModSource.Lfo2: mod.Lfo2WarpDepth += depth; break;
                case ModSource.Lfo3: mod.Lfo3WarpDepth += depth; break;
                case ModSource.Envelope: mod.EnvelopeWarpDepth += depth; break;
                case ModSource.Env2: mod.Env2WarpDepth += depth; break;
                case ModSource.Env3: mod.Env3WarpDepth += depth; break;
                case ModSource.Random: mod.RandomWarpDepth += depth; break;
                case ModSource.Random2: mod.Random2WarpDepth += depth; break;
                case ModSource.Random3: mod.Random3WarpDepth += depth; break;
            }
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
                case ModTarget.Warp: mod.Warp += value; break;
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
                case ModTarget.CompressorDepth: mod.CompressorDepth += value; break;
            }
        }
    }
}
