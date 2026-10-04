using DataKeeper.Forge.Dsp;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace DataKeeper.Forge.Render
{
    // CompileSynchronously keeps the editor from running the managed fallback while Burst
    // compiles in the background, which would break bit-identical output between renders.
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
    public struct LayerRenderJob : IJobParallelFor
    {
        public const int ControlRate = 32;

        public int FrameCount;
        public int SampleRate;
        [ReadOnly] public NativeArray<LayerRenderParams> Layers;
        [ReadOnly] public NativeArray<Breakpoint> Breakpoints;
        [ReadOnly] public NativeArray<float> Wavetables;
        [ReadOnly] public NativeArray<float> SampleData;

        // Interleaved stereo; each index writes only its own 2 * FrameCount region.
        [NativeDisableParallelForRestriction] public NativeArray<float> LayerBuffer;

        private struct ControlFrame
        {
            public float Envelope;
            public float Amp;
            public float PhaseIncrement;
            public float SampleStep;
            public float FilterG;
            public float2 PanGains;
        }

        public void Execute(int layerIndex)
        {
            var p = Layers[layerIndex];
            if (p.Gain <= 0f) return;

            var offset = layerIndex * FrameCount * 2;
            var start = p.StartFrame;
            var end = p.EndFrame;
            if (end <= start) return;

            var oscillator = new PolyBlepOscillator();
            var noise = new NoiseGenerator(p.Seed);
            var wavetable = new WavetableOscillator();
            var fm = new FmOperator();
            var sampler = new SamplePlayer { Position = p.SampleStart };
            var granular = new GranularPlayer(p.Seed, p.SampleStart);
            var filter = new StateVariableFilter();
            var invVoiceFrames = 1f / p.VoiceFrames;
            var previous = EvaluateControl(p, 0f);

            for (var block = start; block < end; block += ControlRate)
            {
                var blockEnd = math.min(block + ControlRate, end);
                var next = EvaluateControl(p, (blockEnd - start) * invVoiceFrames);
                var invBlockLength = 1f / (blockEnd - block);

                for (var i = block; i < blockEnd; i++)
                {
                    var f = (i - block) * invBlockLength;
                    var phaseIncrement = math.lerp(previous.PhaseIncrement, next.PhaseIncrement, f);
                    var filterG = math.lerp(previous.FilterG, next.FilterG, f);
                    var amp = math.lerp(previous.Amp, next.Amp, f);
                    var panGains = math.lerp(previous.PanGains, next.PanGains, f);

                    float sample;
                    switch (p.Source)
                    {
                        case SourceType.Noise:
                            sample = noise.Next(p.NoiseColor);
                            break;
                        case SourceType.Wavetable:
                            sample = wavetable.Next(Wavetables, p.WavetableOffset, p.WavetablePosition, phaseIncrement);
                            break;
                        case SourceType.FM:
                            var envelope = math.lerp(previous.Envelope, next.Envelope, f);
                            var index = p.FmIndex * math.lerp(1f, envelope, p.FmIndexEnvelope);
                            sample = fm.Next(phaseIncrement, p.FmRatio, index);
                            break;
                        case SourceType.Sample:
                            var step = math.lerp(previous.SampleStep, next.SampleStep, f);
                            sample = sampler.Next(SampleData, p.SampleOffset, p.SampleLength, step, p.SampleReverse, p.SampleCubic);
                            break;
                        case SourceType.Granular:
                            var grainStep = math.lerp(previous.SampleStep, next.SampleStep, f);
                            sample = granular.Next(SampleData, p.SampleOffset, p.SampleLength, p.SampleStep, grainStep,
                                p.GrainFrames, p.GrainInterval, p.GrainSpray, p.GrainPitchRandom, p.SampleReverse, p.SampleCubic);
                            break;
                        default:
                            sample = oscillator.Next(p.Waveform, phaseIncrement);
                            break;
                    }

                    sample = filter.Process(sample, filterG, p.FilterK, p.Filter) * amp;
                    LayerBuffer[offset + i * 2] = sample * panGains.x;
                    LayerBuffer[offset + i * 2 + 1] = sample * panGains.y;
                }

                previous = next;
            }
        }

        private ControlFrame EvaluateControl(in LayerRenderParams p, float t)
        {
            var envelope = Evaluate(p.AmpCurve, t);
            var seconds = t * p.VoiceFrames / SampleRate;
            var mod = p.LfoDepth * ModMatrix.Lfo(p.LfoShape, seconds * p.LfoRateHz) + p.EnvelopeDepth * envelope;

            var pitch = p.Pitch + Evaluate(p.PitchCurve, t) + mod.x;
            var frequency = math.min(AudioMath.SemitonesToHz(pitch), SampleRate * PolyBlepOscillator.MaxPhaseIncrement);
            var cutoff = p.CutoffHz * math.exp2(Evaluate(p.CutoffCurve, t) + mod.y);
            var pan = math.clamp(p.Pan + Evaluate(p.PanCurve, t) + mod.w, -1f, 1f);
            // Skipping the pow when unmodulated keeps unrouted recipes bit-identical to before.
            var level = mod.z == 0f ? 1f : AudioMath.DbToLinear(mod.z);

            return new ControlFrame
            {
                Envelope = envelope,
                Amp = envelope * p.Gain * level,
                PhaseIncrement = frequency / SampleRate,
                SampleStep = p.SampleStep * AudioMath.SemitonesToRatio(pitch),
                FilterG = StateVariableFilter.CutoffToG(cutoff, SampleRate),
                PanGains = AudioMath.ConstantPowerPan(pan),
            };
        }

        private float Evaluate(in CurveRef curve, float t) =>
            math.lerp(curve.Min, curve.Max, CurveEvaluator.Evaluate(Breakpoints, curve.Start, curve.Count, t));
    }
}
