using System;
using System.Collections.Generic;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Dsp.Fx;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DataKeeper.Forge.Render
{
    public sealed class SfxRenderer : IDisposable
    {
        public const int Channels = 2;

        private const uint PhaseSalt = 0x9A5Eu;

        private readonly Dictionary<AudioClip, float[]> _clipCache = new();
        private readonly LayerModulation[] _modulation = new LayerModulation[SfxRecipe.MaxLayers];

        private NativeArray<LayerRenderParams> _layers;
        private NativeArray<Breakpoint> _breakpoints;
        private NativeArray<float> _layerBuffer;
        private NativeArray<float> _output;
        private NativeArray<float> _wavetables;
        private NativeArray<float> _sampleData;
        private NativeArray<float> _fxScratch;
        private NativeArray<float> _delayLine;
        private NativeArray<float> _reverbMemory;
        private bool _wavetablesBuilt;

        public int FrameCount { get; private set; }
        public int SampleRate { get; private set; }
        public int LayerCount { get; private set; }

        public NativeArray<float> Output => _output.GetSubArray(0, FrameCount * Channels);

        // Per-layer audio before mixing and FX. Only the frames inside LayerFrameRange were
        // written by the last render; the rest holds stale data from earlier renders.
        public NativeArray<float> LayerOutput(int layer) =>
            _layerBuffer.GetSubArray(layer * FrameCount * Channels, FrameCount * Channels);

        public bool IsLayerAudible(int layer)
        {
            var p = _layers[layer];
            return p.Gain > 0f && p.EndFrame > p.StartFrame;
        }

        public void LayerFrameRange(int layer, out int startFrame, out int endFrame)
        {
            startFrame = _layers[layer].StartFrame;
            endFrame = _layers[layer].EndFrame;
        }

        public SfxRenderer()
        {
            _layers = new NativeArray<LayerRenderParams>(SfxRecipe.MaxLayers, Allocator.Persistent);
        }

        public static int FramesFor(float lengthMs, int sampleRate) =>
            (int)math.round(math.clamp(lengthMs, SfxRecipe.MinLengthMs, SfxRecipe.MaxLengthMs)
                            * sampleRate / 1000.0);

        public void Render(SfxRecipe recipe) => Render(recipe, recipe.Seed);

        public void Render(SfxRecipe recipe, uint seed)
        {
            SampleRate = recipe.SampleRate;
            var layerCount = math.min(recipe.Layers.Count, SfxRecipe.MaxLayers);
            LayerCount = layerCount;
            var global = ModMatrix.Evaluate(recipe, seed, _modulation, layerCount);
            FrameCount = FramesFor(recipe.LengthMs * math.exp2(global.LengthOctaves), SampleRate);

            EnsureCapacity(ref _layerBuffer, FrameCount * Channels * SfxRecipe.MaxLayers);
            EnsureCapacity(ref _output, FrameCount * Channels);
            EnsureCapacity(ref _breakpoints, CountBreakpoints(recipe, layerCount));
            PrepareSources(recipe.Layers, layerCount);
            BuildLayers(recipe, layerCount, seed, global);

            var fx = FxParams.From(recipe.Fx, SampleRate, global);
            PrepareFx(fx);

            var layerJob = new LayerRenderJob
            {
                FrameCount = FrameCount,
                SampleRate = SampleRate,
                Layers = _layers,
                Breakpoints = _breakpoints,
                Wavetables = _wavetables,
                SampleData = _sampleData,
                LayerBuffer = _layerBuffer,
            }.Schedule(layerCount, 1);

            var mixJob = new MixJob
            {
                FrameCount = FrameCount,
                LayerCount = layerCount,
                Layers = _layers,
                LayerBuffer = _layerBuffer,
                Output = _output,
            }.Schedule(layerJob);

            new FxChainJob
            {
                FrameCount = FrameCount,
                SampleRate = SampleRate,
                Fx = fx,
                Buffer = _output,
                Scratch = _fxScratch,
                DelayLine = _delayLine,
                ReverbMemory = _reverbMemory,
            }.Schedule(mixJob).Complete();
        }

        // Forget decoded clip data, e.g. after a clip was reimported.
        public void ClearSampleCache() => _clipCache.Clear();

        public void Dispose()
        {
            if (_layers.IsCreated) _layers.Dispose();
            if (_breakpoints.IsCreated) _breakpoints.Dispose();
            if (_layerBuffer.IsCreated) _layerBuffer.Dispose();
            if (_output.IsCreated) _output.Dispose();
            if (_wavetables.IsCreated) _wavetables.Dispose();
            if (_sampleData.IsCreated) _sampleData.Dispose();
            if (_fxScratch.IsCreated) _fxScratch.Dispose();
            if (_delayLine.IsCreated) _delayLine.Dispose();
            if (_reverbMemory.IsCreated) _reverbMemory.Dispose();
            _clipCache.Clear();
        }

        // Wavetables are built on first use; sample data for this render is packed into one
        // buffer because a job cannot take a variable number of arrays.
        private void PrepareSources(List<Layer> layers, int layerCount)
        {
            var needsWavetables = false;
            var sampleTotal = 0;
            for (var i = 0; i < layerCount; i++)
            {
                var source = layers[i].Source;
                needsWavetables |= source.Type == SourceType.Wavetable || WarpModeOf(layers[i]) != WarpMode.Off;
                if (SourceSettings.UsesClip(source.Type) && source.Sample.Clip != null)
                    sampleTotal += ClipData(source.Sample.Clip)?.Length ?? 0;
            }

            if (needsWavetables && !_wavetablesBuilt)
            {
                EnsureCapacity(ref _wavetables, Wavetables.TotalSize);
                Wavetables.BuildAll(_wavetables);
                _wavetablesBuilt = true;
            }
            else
            {
                EnsureCapacity(ref _wavetables, 1);
            }

            EnsureCapacity(ref _sampleData, sampleTotal);
        }

        private void PrepareFx(FxParams fx)
        {
            EnsureCapacity(ref _fxScratch, FrameCount * Channels);
            EnsureCapacity(ref _delayLine, fx.DelayEnabled ? StereoDelay.LineSize(fx.DelayFrames) : 1);
            EnsureCapacity(ref _reverbMemory, fx.ReverbEnabled ? Reverb.MemorySize(SampleRate) : 1);
        }

        private void BuildLayers(SfxRecipe recipe, int layerCount, uint seed, in GlobalModulation global)
        {
            var layers = recipe.Layers;
            var transpose = (float)(math.clamp(recipe.RootNote, SfxRecipe.MinRootNote, SfxRecipe.MaxRootNote)
                                    - SfxRecipe.DefaultRootNote);
            // The LFO Rate target moves LFO 1 only.
            var lfo1 = PackLfo(recipe.Lfo, global.LfoRateOctaves);
            var lfo2 = PackLfo(recipe.Lfo2, 0f);
            var lfo3 = PackLfo(recipe.Lfo3, 0f);
            var anySolo = false;
            for (var i = 0; i < layerCount; i++)
                anySolo |= layers[i].Enabled && layers[i].Solo;

            // Env 2/3 are shared by every layer, so they are copied once.
            var cursor = 0;
            var env2 = CopyCurve(recipe.Env2, ref cursor);
            var env3 = CopyCurve(recipe.Env3, ref cursor);
            var sampleCursor = 0;
            for (var i = 0; i < layerCount; i++)
            {
                var layer = layers[i];
                var source = layer.Source;
                var mod = _modulation[i];
                var audible = layer.Enabled && !layer.Mute && (!anySolo || layer.Solo);
                var startFrame = math.min(MsToFrames(math.max(0f, layer.StartOffsetMs)), FrameCount);
                var voiceFrames = math.max(1, MsToFrames(layer.DecayMs * math.exp2(mod.DecayOctaves)));
                var layerSeed = math.hash(new uint2(seed, (uint)i));

                var parameters = new LayerRenderParams
                {
                    Source = source.Type,
                    Waveform = source.Oscillator.Waveform,
                    NoiseColor = source.Noise.Color,
                    Pitch = layer.Pitch + mod.Pitch + transpose,
                    Gain = audible ? AudioMath.DbToLinear(layer.LevelDb + mod.LevelDb) : 0f,
                    Pan = math.clamp(layer.Pan + mod.Pan, -1f, 1f),
                    StartFrame = startFrame,
                    EndFrame = math.min(startFrame + voiceFrames, FrameCount),
                    VoiceFrames = voiceFrames,
                    Filter = layer.Filter.Type,
                    CutoffHz = layer.Filter.CutoffHz * math.exp2(mod.CutoffOctaves),
                    FilterK = StateVariableFilter.ResonanceToK(math.saturate(layer.Filter.Resonance + mod.Resonance)),
                    Seed = layerSeed,
                    FmRatio = math.clamp(source.Fm.Ratio, FmSettings.MinRatio, FmSettings.MaxRatio),
                    FmIndex = math.clamp(source.Fm.Index, 0f, FmSettings.MaxIndex),
                    FmIndexEnvelope = math.saturate(source.Fm.IndexEnvelope),
                    WavetableOffset = Wavetables.BankOffset(source.Wavetable.Bank),
                    WavetablePosition = math.saturate(source.Wavetable.Position),
                    ShepardRate = math.clamp(source.Shepard.RateOctaves, -ShepardSettings.MaxRateOctaves,
                        ShepardSettings.MaxRateOctaves),
                    ShepardWidthOctaves = ShepardOscillator.WidthOctaves(source.Shepard.Width),
                    ShepardPartials = math.clamp(source.Shepard.Partials, ShepardSettings.MinPartials,
                        ShepardSettings.MaxPartials),
                    Warp = WarpModeOf(layer),
                    WarpAmount = math.saturate(layer.Warp.Amount) + mod.Warp,
                    AmpCurve = CopyCurve(layer.AmpCurve, ref cursor),
                    PitchCurve = CopyCurve(layer.PitchCurve, ref cursor),
                    CutoffCurve = CopyCurve(layer.CutoffCurve, ref cursor),
                    PanCurve = CopyCurve(layer.PanCurve, ref cursor),
                    StartSeconds = startFrame / (float)SampleRate,
                    Lfo1 = lfo1,
                    Lfo2 = lfo2,
                    Lfo3 = lfo3,
                    Random1 = PackRandom(recipe, ModSource.Random, layerSeed),
                    Random2 = PackRandom(recipe, ModSource.Random2, layerSeed),
                    Random3 = PackRandom(recipe, ModSource.Random3, layerSeed),
                    LfoDepth = mod.LfoDepth,
                    Lfo2Depth = mod.Lfo2Depth,
                    Lfo3Depth = mod.Lfo3Depth,
                    EnvelopeDepth = mod.EnvelopeDepth,
                    Env2Depth = mod.Env2Depth,
                    Env3Depth = mod.Env3Depth,
                    RandomDepth = mod.RandomDepth,
                    Random2Depth = mod.Random2Depth,
                    Random3Depth = mod.Random3Depth,
                    LfoWarpDepth = mod.LfoWarpDepth,
                    Lfo2WarpDepth = mod.Lfo2WarpDepth,
                    Lfo3WarpDepth = mod.Lfo3WarpDepth,
                    EnvelopeWarpDepth = mod.EnvelopeWarpDepth,
                    Env2WarpDepth = mod.Env2WarpDepth,
                    Env3WarpDepth = mod.Env3WarpDepth,
                    RandomWarpDepth = mod.RandomWarpDepth,
                    Random2WarpDepth = mod.Random2WarpDepth,
                    Random3WarpDepth = mod.Random3WarpDepth,
                    Env2Curve = env2,
                    Env3Curve = env3,
                };

                SetVoices(layer, ref parameters);
                if (SourceSettings.UsesClip(source.Type)) CopySample(source.Sample, ref parameters, ref sampleCursor);
                if (source.Type == SourceType.Granular) SetGrains(source.Granular, ref parameters);
                _layers[i] = parameters;
            }
        }

        // Only Sync is implemented so far; the other modes, and sources without warp, play as Off.
        private static WarpMode WarpModeOf(Layer layer) =>
            WarpSettings.Supports(layer.Source.Type) && layer.Warp.Mode == WarpMode.Sync ? WarpMode.Sync : WarpMode.Off;

        private static LfoParams PackLfo(LfoSettings lfo, float rateOctaves) => new()
        {
            Shape = lfo.Shape,
            RateHz = math.clamp(lfo.RateHz * math.exp2(rateOctaves), LfoSettings.MinRateHz, LfoSettings.MaxRateHz),
            Phase = math.saturate(lfo.Phase),
            Mode = lfo.Mode,
        };

        private static RandomParams PackRandom(SfxRecipe recipe, ModSource source, uint layerSeed)
        {
            var random = recipe.RandomSettingsOf(source);
            return new RandomParams
            {
                Mode = random.Mode,
                RateHz = math.clamp(random.RateHz, RandomSettings.MinRateHz, RandomSettings.MaxRateHz),
                Seed = ModMatrix.RandomSeed(layerSeed, source, random.Seed),
            };
        }

        private static void SetVoices(Layer layer, ref LayerRenderParams parameters)
        {
            var voices = SourceSettings.IsTonal(layer.Source.Type)
                ? math.clamp(layer.Unison.Voices, UnisonSettings.MinVoices, UnisonSettings.MaxVoices)
                : 1;
            var detuneOctaves = math.clamp(layer.Unison.DetuneCents, 0f, UnisonSettings.MaxDetuneCents) / 1200f;
            var spread = math.saturate(layer.Unison.Spread);
            var start = math.frac(math.saturate(layer.Phase.Start));
            // √2 undoes the constant-power centre dip, so a centred voice matches the mono path
            // before the 1/√N level; the layer pan is applied after the filter as usual.
            var gain = math.SQRT2 / math.sqrt(voices);

            parameters.Voices = voices;
            for (var v = 0; v < voices; v++)
            {
                var position = voices == 1 ? 0f : (float)v / (voices - 1) - 0.5f;
                parameters.VoiceRatios.Add(math.exp2(position * detuneOctaves));
                parameters.VoicePhases.Add(layer.Phase.Random ? RandomPhase(parameters.Seed, v) : start);
                parameters.VoiceGains.Add(AudioMath.ConstantPowerPan(spread * SpreadPosition(v, voices)) * gain);
            }
        }

        // Even voices take the left positions from the outside in and odd voices the right ones,
        // so neighbouring detunes land on opposite sides.
        private static float SpreadPosition(int voice, int voices)
        {
            if (voices == 1) return 0f;
            var slot = (voice & 1) == 0 ? voice / 2 : voices - 1 - voice / 2;
            return 2f * slot / (voices - 1) - 1f;
        }

        private static float RandomPhase(uint layerSeed, int voice) =>
            (ModMatrix.Avalanche(math.hash(new uint3(layerSeed, (uint)voice, PhaseSalt))) >> 8) * (1f / (1 << 24));

        private void CopySample(SampleSettings sample, ref LayerRenderParams parameters, ref int cursor)
        {
            var data = sample.Clip != null ? ClipData(sample.Clip) : null;
            if (data == null || data.Length == 0)
            {
                // Missing or unreadable clip: the layer renders as silence.
                parameters.Gain = 0f;
                return;
            }

            NativeArray<float>.Copy(data, 0, _sampleData, cursor, data.Length);
            var clipRate = sample.Clip.frequency;

            parameters.SampleOffset = cursor;
            parameters.SampleLength = data.Length;
            parameters.SampleStep = clipRate / (float)SampleRate;
            parameters.SampleStart = math.clamp(sample.StartMs, 0f, SampleSettings.MaxStartMs) * clipRate / 1000f;
            parameters.SampleReverse = sample.Reverse;
            parameters.SampleCubic = sample.Interpolation == SampleInterpolation.Cubic;
            cursor += data.Length;
        }

        private void SetGrains(GranularSettings granular, ref LayerRenderParams parameters)
        {
            var grainMs = math.clamp(granular.GrainMs, GranularSettings.MinGrainMs, GranularSettings.MaxGrainMs);
            var density = math.clamp(granular.Density, GranularSettings.MinDensity, GranularSettings.MaxDensity);
            var clipRate = parameters.SampleStep * SampleRate;

            parameters.GrainFrames = math.max(2, MsToFrames(grainMs));
            parameters.GrainInterval = SampleRate / density;
            parameters.GrainSpray = math.clamp(granular.SprayMs, 0f, GranularSettings.MaxSprayMs) * clipRate / 1000f;
            parameters.GrainPitchRandom = math.clamp(granular.PitchRandom, 0f, GranularSettings.MaxPitchRandom);
        }

        // Decoded once per clip and mixed to mono. A failed read is not cached, so it is retried
        // on the next render, e.g. once the clip finishes loading.
        private float[] ClipData(AudioClip clip)
        {
            if (_clipCache.TryGetValue(clip, out var cached)) return cached;

            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();

            var channels = clip.channels;
            var raw = new float[clip.samples * channels];
            if (!clip.GetData(raw, 0)) return null;

            var mono = new float[clip.samples];
            for (var frame = 0; frame < mono.Length; frame++)
            {
                var sum = 0f;
                for (var c = 0; c < channels; c++) sum += raw[frame * channels + c];
                mono[frame] = sum / channels;
            }

            _clipCache[clip] = mono;
            return mono;
        }

        private CurveRef CopyCurve(Curve curve, ref int cursor)
        {
            var count = curve.Points.Count;
            for (var p = 0; p < count; p++) _breakpoints[cursor + p] = curve.Points[p];
            SortByTime(cursor, count);

            var reference = new CurveRef { Start = cursor, Count = count, Min = curve.Min, Max = curve.Max };
            cursor += count;
            return reference;
        }

        private int MsToFrames(float ms) => (int)math.round(ms * SampleRate / 1000f);

        // Insertion sort: point lists are tiny and usually already ordered, and it allocates nothing.
        private void SortByTime(int start, int count)
        {
            for (var i = start + 1; i < start + count; i++)
            {
                var key = _breakpoints[i];
                var j = i - 1;
                while (j >= start && _breakpoints[j].Time > key.Time)
                {
                    _breakpoints[j + 1] = _breakpoints[j];
                    j--;
                }

                _breakpoints[j + 1] = key;
            }
        }

        private static int CountBreakpoints(SfxRecipe recipe, int layerCount)
        {
            var layers = recipe.Layers;
            var count = recipe.Env2.Points.Count + recipe.Env3.Points.Count;
            for (var i = 0; i < layerCount; i++)
            {
                var layer = layers[i];
                count += layer.AmpCurve.Points.Count + layer.PitchCurve.Points.Count
                         + layer.CutoffCurve.Points.Count + layer.PanCurve.Points.Count;
            }

            return count;
        }

        private static void EnsureCapacity<T>(ref NativeArray<T> array, int required) where T : struct
        {
            required = math.max(required, 1);
            if (array.IsCreated && array.Length >= required) return;
            if (array.IsCreated) array.Dispose();
            array = new NativeArray<T>(required, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
    }
}
