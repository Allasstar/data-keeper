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

            // Separate loop so a single voice keeps the original mono path and its exact output.
            if (p.Voices > 1)
            {
                RenderUnison(p, offset);
                return;
            }

            var phase = p.VoicePhases[0];
            var oscillator = new PolyBlepOscillator { Phase = phase };
            var noise = new NoiseGenerator(p.Seed);
            var wavetable = new WavetableOscillator { Phase = phase };
            var fm = StartFm(phase, p.FmRatio);
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

        // Tonal sources only (the renderer packs one voice for the rest). Voices sum to stereo
        // before the filter, so each channel needs its own filter state.
        private void RenderUnison(in LayerRenderParams p, int offset)
        {
            var voices = p.Voices;
            // Local copies: indexing the lists through the readonly parameter would copy them per access.
            var ratios = p.VoiceRatios;
            var gains = p.VoiceGains;
            var phases = p.VoicePhases;
            var oscillators = new FixedList64Bytes<PolyBlepOscillator>();
            var wavetables = new FixedList64Bytes<WavetableOscillator>();
            var operators = new FixedList128Bytes<FmOperator>();
            for (var v = 0; v < voices; v++)
            {
                var phase = phases[v];
                oscillators.Add(new PolyBlepOscillator { Phase = phase });
                wavetables.Add(new WavetableOscillator { Phase = phase });
                operators.Add(StartFm(phase, p.FmRatio));
            }

            var filterLeft = new StateVariableFilter();
            var filterRight = new StateVariableFilter();
            var start = p.StartFrame;
            var end = p.EndFrame;
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

                    var mix = float2.zero;
                    switch (p.Source)
                    {
                        case SourceType.Wavetable:
                            for (var v = 0; v < voices; v++)
                            {
                                var wavetable = wavetables[v];
                                mix += gains[v] * wavetable.Next(Wavetables, p.WavetableOffset, p.WavetablePosition,
                                    phaseIncrement * ratios[v]);
                                wavetables[v] = wavetable;
                            }
                            break;
                        case SourceType.FM:
                            var envelope = math.lerp(previous.Envelope, next.Envelope, f);
                            var index = p.FmIndex * math.lerp(1f, envelope, p.FmIndexEnvelope);
                            for (var v = 0; v < voices; v++)
                            {
                                var fm = operators[v];
                                mix += gains[v] * fm.Next(phaseIncrement * ratios[v], p.FmRatio, index);
                                operators[v] = fm;
                            }
                            break;
                        default:
                            for (var v = 0; v < voices; v++)
                            {
                                var oscillator = oscillators[v];
                                mix += gains[v] * oscillator.Next(p.Waveform, phaseIncrement * ratios[v]);
                                oscillators[v] = oscillator;
                            }
                            break;
                    }

                    var left = filterLeft.Process(mix.x, filterG, p.FilterK, p.Filter) * amp;
                    var right = filterRight.Process(mix.y, filterG, p.FilterK, p.Filter) * amp;
                    LayerBuffer[offset + i * 2] = left * panGains.x;
                    LayerBuffer[offset + i * 2 + 1] = right * panGains.y;
                }

                previous = next;
            }
        }

        // The modulator starts at the same point in time as the carrier, so Start shifts the
        // whole FM waveform rather than changing its timbre.
        private static FmOperator StartFm(float phase, float ratio) =>
            new() { CarrierPhase = phase, ModulatorPhase = math.frac(phase * ratio) };

        private ControlFrame EvaluateControl(in LayerRenderParams p, float t)
        {
            var envelope = Evaluate(p.AmpCurve, t);
            var seconds = t * p.VoiceFrames / SampleRate;

            // Unrouted slots are skipped, so a recipe without continuous routes does no extra math.
            // LFO 1 is assigned before Env 1 is added to keep the original summation order.
            var mod = float4.zero;
            if (math.any(p.LfoDepth != 0f)) mod = p.LfoDepth * Lfo(p.Lfo1, seconds, p.StartSeconds);
            if (math.any(p.EnvelopeDepth != 0f)) mod += p.EnvelopeDepth * envelope;
            if (math.any(p.Lfo2Depth != 0f)) mod += p.Lfo2Depth * Lfo(p.Lfo2, seconds, p.StartSeconds);
            if (math.any(p.Lfo3Depth != 0f)) mod += p.Lfo3Depth * Lfo(p.Lfo3, seconds, p.StartSeconds);

            var soundT = (p.StartFrame + t * p.VoiceFrames) / FrameCount;
            if (math.any(p.Env2Depth != 0f)) mod += p.Env2Depth * Evaluate(p.Env2Curve, soundT);
            if (math.any(p.Env3Depth != 0f)) mod += p.Env3Depth * Evaluate(p.Env3Curve, soundT);
            if (math.any(p.RandomDepth != 0f)) mod += p.RandomDepth * Random(p.Random1, seconds, p.StartSeconds);
            if (math.any(p.Random2Depth != 0f)) mod += p.Random2Depth * Random(p.Random2, seconds, p.StartSeconds);
            if (math.any(p.Random3Depth != 0f)) mod += p.Random3Depth * Random(p.Random3, seconds, p.StartSeconds);

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

        // Retrigger runs on the layer's own time, Free on the sound's.
        private static float Lfo(in LfoParams lfo, float seconds, float startSeconds)
        {
            var time = lfo.Mode == LfoMode.Free ? seconds + startSeconds : seconds;
            return ModMatrix.Lfo(lfo.Shape, time * lfo.RateHz + lfo.Phase);
        }

        private static float Random(in RandomParams random, float seconds, float startSeconds) =>
            ModMatrix.RandomSignal(random.Mode, random.Seed, (seconds + startSeconds) * random.RateHz);

        private float Evaluate(in CurveRef curve, float t) =>
            math.lerp(curve.Min, curve.Max, CurveEvaluator.Evaluate(Breakpoints, curve.Start, curve.Count, t));
    }
}
