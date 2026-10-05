using System.Collections.Generic;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge
{
    // Every parameter is drawn whether or not it is locked, so the random stream never depends
    // on lock state: locking one knob leaves every other result of the same seed unchanged.
    public static class SfxRandomizer
    {
        private const float CouplingJitter = 0.15f;
        private const float MinPointGap = 0.001f;
        private const uint RandomizeSalt = 0x5F0Bu;
        private const uint MutateSalt = 0xA11Eu;

        private const float MutateTransposeSemitones = 5f;
        private const float MutateLengthOctaves = 0.5f;
        private const float MutateCutoffOctaves = 1.5f;
        private const float MutateResonance = 0.25f;
        private const float MutateLevelDb = 4f;
        private const float MutatePan = 0.4f;
        private const float MutateDecayOctaves = 1f;
        private const float MutateOffsetMs = 30f;
        private const float MutateCurveTime = 0.05f;
        private const float MutateCurveValue = 0.15f;
        private const float MutateCurveTension = 0.4f;
        private const float MutateFmIndexOctaves = 0.5f;
        private const float MutateWavetablePosition = 0.2f;
        private const float MutateDriveDb = 3f;
        private const float MutateFxMix = 0.1f;
        private const float MutateDelayOctaves = 0.3f;

        private const float MinAudibleAttack = 0.05f;
        private const float MinAudibleDriveDb = 0.5f;
        private const float MinAudibleMix = 0.005f;

        public static void Randomize(SfxRecipe recipe, CategoryTemplate template, uint seed)
        {
            var rng = new Random(NonZero(math.hash(new uint2(seed, RandomizeSalt))));
            var settings = recipe.Randomizer;
            var coupling = math.saturate(settings.PhysicsCoupling);

            // Hidden physical character of this variation; coupling decides how strictly the
            // individual parameters follow it.
            var size = rng.NextFloat();
            var energy = rng.NextFloat();

            recipe.Category = template.Category;
            recipe.Seed = NonZero(seed);

            var length = template.LengthMs.LerpLog(Coupled(ref rng, size, coupling));
            if (!settings.LockLength) recipe.LengthMs = length;

            var harmony = new HarmonyState(settings.Harmony, Coupled(ref rng, 1f - size, coupling));

            var generated = new List<Layer>(template.Layers.Count);
            foreach (var layerTemplate in template.Layers)
                generated.Add(GenerateLayer(layerTemplate, template.FmRatios, ref rng, size, energy, coupling, ref harmony));

            recipe.Layers = MergeWithLocks(recipe.Layers, generated);
            RandomizeFx(recipe.Fx, template.Fx ?? new FxTemplate(), ref rng, size, energy, coupling, settings.LockFx);
        }

        public static void Mutate(SfxRecipe recipe, uint seed)
        {
            var rng = new Random(NonZero(math.hash(new uint2(seed, MutateSalt))));
            var settings = recipe.Randomizer;
            var amount = math.saturate(settings.VariationAmount);

            // One shared transposition for all tonal layers keeps their intervals intact.
            var transpose = math.round(rng.NextFloat(-1f, 1f) * amount * MutateTransposeSemitones);
            var lengthFactor = math.exp2(rng.NextFloat(-1f, 1f) * amount * MutateLengthOctaves);
            if (!settings.LockLength)
                recipe.LengthMs = math.clamp(recipe.LengthMs * lengthFactor, SfxRecipe.MinLengthMs, SfxRecipe.MaxLengthMs);

            MutateFx(recipe.Fx, ref rng, amount, settings.LockFx);

            foreach (var layer in recipe.Layers)
            {
                var fmIndexShift = rng.NextFloat(-1f, 1f);
                var wavetableShift = rng.NextFloat(-1f, 1f);
                var cutoffShift = rng.NextFloat(-1f, 1f);
                var resonanceShift = rng.NextFloat(-1f, 1f);
                var levelShift = rng.NextFloat(-1f, 1f);
                var panShift = rng.NextFloat(-1f, 1f);
                var decayShift = rng.NextFloat(-1f, 1f);
                var offsetShift = rng.NextFloat(-1f, 1f);

                // Pitch curve values are musical content (steps, intervals), so only their
                // timing and shape move; the shared transpose handles pitch itself.
                var ampCurve = MutateCurve(layer.AmpCurve, amount, true, ref rng);
                var pitchCurve = MutateCurve(layer.PitchCurve, amount, false, ref rng);
                var cutoffCurve = MutateCurve(layer.CutoffCurve, amount, true, ref rng);
                var panCurve = MutateCurve(layer.PanCurve, amount, true, ref rng);

                if (layer.Locked) continue;
                var locks = layer.LockedParams;

                if (SourceSettings.IsTonal(layer.Source.Type) && !Has(locks, LayerParam.Pitch) && transpose != 0f)
                    layer.Pitch = MutatePitch(layer.Pitch, transpose, layer.Band);

                if (!Has(locks, LayerParam.Source))
                {
                    layer.Source.Fm.Index = math.clamp(layer.Source.Fm.Index * math.exp2(fmIndexShift * amount * MutateFmIndexOctaves),
                        0f, FmSettings.MaxIndex);
                    layer.Source.Wavetable.Position = math.saturate(
                        layer.Source.Wavetable.Position + wavetableShift * amount * MutateWavetablePosition);
                }

                if (!Has(locks, LayerParam.Cutoff))
                    layer.Filter.CutoffHz = KeepInBand(
                        layer.Filter.CutoffHz * math.exp2(cutoffShift * amount * MutateCutoffOctaves),
                        layer.Filter.CutoffHz, BandRules.FitCutoff(FullCutoff, layer.Band), FullCutoff);

                if (!Has(locks, LayerParam.Resonance))
                    layer.Filter.Resonance = math.saturate(layer.Filter.Resonance + resonanceShift * amount * MutateResonance);

                if (!Has(locks, LayerParam.Level))
                    layer.LevelDb = math.clamp(layer.LevelDb + levelShift * amount * MutateLevelDb,
                        ParamRanges.LevelMinDb, ParamRanges.LevelMaxDb);

                if (!Has(locks, LayerParam.Pan))
                    layer.Pan = math.clamp(layer.Pan + panShift * amount * MutatePan, -1f, 1f);

                if (!Has(locks, LayerParam.Decay))
                    layer.DecayMs = math.clamp(layer.DecayMs * math.exp2(decayShift * amount * MutateDecayOctaves),
                        ParamRanges.DecayMinMs, ParamRanges.DecayMaxMs);

                if (!Has(locks, LayerParam.Offset))
                    layer.StartOffsetMs = math.clamp(layer.StartOffsetMs + offsetShift * amount * MutateOffsetMs,
                        0f, ParamRanges.OffsetMaxMs);

                ReplaceUnlessLocked(layer, CurveTarget.Amp, ampCurve);
                ReplaceUnlessLocked(layer, CurveTarget.Pitch, pitchCurve);
                ReplaceUnlessLocked(layer, CurveTarget.Cutoff, cutoffCurve);
                ReplaceUnlessLocked(layer, CurveTarget.Pan, panCurve);
            }
        }

        private static readonly FloatRange FullCutoff = new(ParamRanges.CutoffMin, ParamRanges.CutoffMax);

        private static Layer GenerateLayer(LayerTemplate t, List<float> fmRatios, ref Random rng, float size,
            float energy, float coupling, ref HarmonyState harmony)
        {
            var layer = new Layer
            {
                Name = t.Name,
                Band = t.Band,
                Enabled = rng.NextFloat() < t.Probability,
            };

            layer.Source = new SourceSettings
            {
                Type = Pick(t.Sources, ref rng, SourceType.Oscillator),
                Oscillator = new OscillatorSettings { Waveform = Pick(t.Waveforms, ref rng, Waveform.Sine) },
                Noise = new NoiseSettings { Color = Pick(t.NoiseColors, ref rng, NoiseColor.White) },
                Wavetable = new WavetableSettings
                {
                    Bank = Pick(t.Wavetables, ref rng, WavetableBank.Basic),
                    Position = t.WavetablePosition.Lerp(Coupled(ref rng, energy, coupling)),
                },
                // Ratios come from the category: integer sets read as clean tones, odd ones as metal.
                Fm = new FmSettings
                {
                    Ratio = Pick(fmRatios, ref rng, 1f),
                    Index = t.FmIndex.Lerp(Coupled(ref rng, energy, coupling)),
                    IndexEnvelope = t.FmIndexEnvelope.Lerp(rng.NextFloat()),
                },
                Sample = new SampleSettings { Interpolation = SampleInterpolation.Cubic },
                Granular = GranularSettings.Default,
            };

            var pitchRange = BandRules.FitPitch(t.Pitch, t.Band);
            var freePitch = Coupled(ref rng, 1f - size, coupling);
            var intervals = HarmonySets.Intervals(harmony.Mode);
            var interval = intervals[rng.NextInt(intervals.Length)];
            layer.Pitch = t.Tonal ? harmony.Place(pitchRange, interval) : pitchRange.Lerp(freePitch);

            layer.Filter = new FilterSettings
            {
                Type = Pick(t.Filters, ref rng, FilterType.Off),
                CutoffHz = BandRules.FitCutoff(t.CutoffHz, t.Band).LerpLog(Coupled(ref rng, energy, coupling)),
                Resonance = t.Resonance.Lerp(rng.NextFloat()),
            };

            // Size feeds the low end, Energy everything else.
            layer.LevelDb = t.LevelDb.Lerp(Coupled(ref rng, t.Band == Band.Sub ? size : energy, coupling));
            layer.Pan = t.Pan.Lerp(rng.NextFloat());
            layer.DecayMs = t.DecayMs.LerpLog(Coupled(ref rng, size, coupling));
            layer.StartOffsetMs = t.OffsetMs.Lerp(rng.NextFloat());

            layer.AmpCurve = PerturbCurve(t.AmpCurve, Curve.DefaultAmp(), ref rng);
            SharpenAttack(layer.AmpCurve, t.AmpCurve, energy, coupling);

            layer.PitchCurve = PerturbCurve(t.PitchCurve, Curve.DefaultPitch(), ref rng);
            if (t.PitchCurve is { SnapToHarmony: true }) SnapToHarmony(layer.PitchCurve, harmony.Mode);

            layer.CutoffCurve = PerturbCurve(t.CutoffCurve, Curve.DefaultCutoff(), ref rng);
            layer.PanCurve = PerturbCurve(t.PanCurve, Curve.DefaultPan(), ref rng);
            return layer;
        }

        private static List<Layer> MergeWithLocks(List<Layer> existing, List<Layer> generated)
        {
            var count = generated.Count;
            for (var i = 0; i < existing.Count; i++)
                if (existing[i].Locked) count = math.max(count, i + 1);
            count = math.min(count, SfxRecipe.MaxLayers);

            var result = new List<Layer>(count);
            for (var i = 0; i < count; i++)
            {
                var old = i < existing.Count ? existing[i] : null;
                if (old != null && old.Locked)
                {
                    result.Add(old);
                    continue;
                }

                if (i >= generated.Count) continue;

                var fresh = generated[i];
                if (old != null) CarryLockedParams(old, fresh);
                result.Add(fresh);
            }

            return result;
        }

        private static void CarryLockedParams(Layer from, Layer to)
        {
            var locks = from.LockedParams;
            to.LockedParams = locks;
            to.Mute = from.Mute;
            to.Solo = from.Solo;
            // Unison and phase are the user's voicing, not generated content (FSF-D4).
            to.Unison = from.Unison;
            to.Phase = from.Phase;

            if (Has(locks, LayerParam.Source)) to.Source = from.Source;
            if (Has(locks, LayerParam.Pitch)) to.Pitch = from.Pitch;
            if (Has(locks, LayerParam.FilterType)) to.Filter.Type = from.Filter.Type;
            if (Has(locks, LayerParam.Cutoff)) to.Filter.CutoffHz = from.Filter.CutoffHz;
            if (Has(locks, LayerParam.Resonance)) to.Filter.Resonance = from.Filter.Resonance;
            if (Has(locks, LayerParam.Level)) to.LevelDb = from.LevelDb;
            if (Has(locks, LayerParam.Pan)) to.Pan = from.Pan;
            if (Has(locks, LayerParam.Decay)) to.DecayMs = from.DecayMs;
            if (Has(locks, LayerParam.Offset)) to.StartOffsetMs = from.StartOffsetMs;
            foreach (var target in CurveTargets)
                if (from.GetCurve(target).Locked) to.SetCurve(target, from.GetCurve(target));
        }

        // Energy sharpens transients and adds drive; Size adds delay time and reverb.
        private static void RandomizeFx(FxChain fx, FxTemplate t, ref Random rng, float size, float energy,
            float coupling, bool locked)
        {
            var attack = t.TransientAttack.Lerp(Coupled(ref rng, energy, coupling));
            var drive = t.DriveDb.Lerp(Coupled(ref rng, energy, coupling));
            var mode = Pick(t.DistortionModes, ref rng, DistortionMode.Tanh);
            var delayOn = rng.NextFloat() < t.DelayProbability;
            var delayTime = t.DelayTimeMs.LerpLog(Coupled(ref rng, size, coupling));
            var delayFeedback = t.DelayFeedback.Lerp(rng.NextFloat());
            var delayMix = t.DelayMix.Lerp(rng.NextFloat());
            var reverbMix = t.ReverbMix.Lerp(Coupled(ref rng, size, coupling));
            var reverbSize = t.ReverbSize.Lerp(Coupled(ref rng, size, coupling));
            var reverbDamping = t.ReverbDamping.Lerp(rng.NextFloat());
            var pingPong = rng.NextFloat() < 0.5f;

            if (locked) return;

            fx.Transient = new TransientSettings { Enabled = attack > MinAudibleAttack, Attack = attack };
            fx.Distortion = new DistortionSettings
            {
                Enabled = drive > MinAudibleDriveDb, Mode = mode, DriveDb = drive, Mix = 1f,
            };
            fx.Delay = new DelaySettings
            {
                Enabled = delayOn && delayMix > MinAudibleMix,
                TimeMs = delayTime,
                Feedback = delayFeedback,
                Mix = delayMix,
                PingPong = pingPong,
            };
            fx.Reverb = new ReverbSettings
            {
                Enabled = reverbMix > MinAudibleMix, Size = reverbSize, Damping = reverbDamping, Mix = reverbMix,
            };
        }

        private static void MutateFx(FxChain fx, ref Random rng, float amount, bool locked)
        {
            var driveShift = rng.NextFloat(-1f, 1f);
            var delayTimeShift = rng.NextFloat(-1f, 1f);
            var delayMixShift = rng.NextFloat(-1f, 1f);
            var reverbMixShift = rng.NextFloat(-1f, 1f);
            var reverbSizeShift = rng.NextFloat(-1f, 1f);

            if (locked) return;

            fx.Distortion.DriveDb = math.clamp(fx.Distortion.DriveDb + driveShift * amount * MutateDriveDb,
                0f, DistortionSettings.MaxDriveDb);
            fx.Delay.TimeMs = math.clamp(fx.Delay.TimeMs * math.exp2(delayTimeShift * amount * MutateDelayOctaves),
                DelaySettings.MinTimeMs, DelaySettings.MaxTimeMs);
            fx.Delay.Mix = math.saturate(fx.Delay.Mix + delayMixShift * amount * MutateFxMix);
            fx.Reverb.Mix = math.saturate(fx.Reverb.Mix + reverbMixShift * amount * MutateFxMix);
            fx.Reverb.Size = math.saturate(fx.Reverb.Size + reverbSizeShift * amount * MutateFxMix);
        }

        private static readonly CurveTarget[] CurveTargets =
            { CurveTarget.Amp, CurveTarget.Pitch, CurveTarget.Cutoff, CurveTarget.Pan };

        private static Curve MutateCurve(Curve source, float amount, bool moveValues, ref Random rng)
        {
            var template = new CurveTemplate
            {
                Points = source.Points,
                TimeJitter = MutateCurveTime * amount,
                ValueJitter = moveValues ? MutateCurveValue * amount : 0f,
                TensionJitter = MutateCurveTension * amount,
            };
            return PerturbCurve(template, source, ref rng);
        }

        private static void ReplaceUnlessLocked(Layer layer, CurveTarget target, Curve replacement)
        {
            if (!layer.GetCurve(target).Locked) layer.SetCurve(target, replacement);
        }

        private static void SnapToHarmony(Curve curve, HarmonyMode mode)
        {
            for (var i = 0; i < curve.Points.Count; i++)
            {
                var point = curve.Points[i];
                var snapped = HarmonySets.Snap(math.lerp(curve.Min, curve.Max, point.Value), mode);
                point.Value = math.saturate(math.unlerp(curve.Min, curve.Max, snapped));
                curve.Points[i] = point;
            }
        }

        // Moves template breakpoints within their jitter limits; never invents a new shape.
        // The fallback supplies the shape when the template has none, and the range and unit
        // when the template does not set its own.
        private static Curve PerturbCurve(CurveTemplate template, Curve fallback, ref Random rng)
        {
            var source = template != null && template.Points.Count > 0 ? template.Points : fallback.Points;
            var timeJitter = template?.TimeJitter ?? 0f;
            var valueJitter = template?.ValueJitter ?? 0f;
            var tensionJitter = template?.TensionJitter ?? 0f;
            var hasRange = template != null && template.Max > template.Min;

            var curve = new Curve
            {
                Unit = fallback.Unit,
                Min = hasRange ? template.Min : fallback.Min,
                Max = hasRange ? template.Max : fallback.Max,
                Points = new List<Breakpoint>(source.Count),
            };
            var last = source.Count - 1;

            for (var i = 0; i <= last; i++)
            {
                var p = source[i];
                var time = p.Time + rng.NextFloat(-1f, 1f) * timeJitter;
                var value = p.Value + rng.NextFloat(-1f, 1f) * valueJitter;
                var tension = p.Tension + rng.NextFloat(-1f, 1f) * tensionJitter;

                if (i == 0 || i == last) time = p.Time;
                else time = ClampBetween(time, curve.Points[i - 1].Time + MinPointGap, source[i + 1].Time - MinPointGap);

                curve.Points.Add(new Breakpoint(time, math.saturate(value), math.clamp(tension, -1f, 1f)));
            }

            return curve;
        }

        private static void SharpenAttack(Curve curve, CurveTemplate template, float energy, float coupling)
        {
            var index = template?.AttackIndex ?? -1;
            if (index <= 0 || index >= curve.Points.Count - 1) return;

            var factor = math.lerp(1f, math.lerp(2f, 0.35f, energy), coupling);
            var p = curve.Points[index];
            p.Time = ClampBetween(p.Time * factor,
                curve.Points[index - 1].Time + MinPointGap, curve.Points[index + 1].Time - MinPointGap);
            curve.Points[index] = p;
        }

        private static float MutatePitch(float pitch, float transpose, Band band)
        {
            var bandRange = BandRules.Semitones(band);
            var shifted = pitch + transpose;
            // Only enforce the band on layers already inside it; an out-of-band pitch was a choice.
            return bandRange.Contains(pitch)
                ? BandRules.FoldOctaves(shifted, bandRange)
                : math.clamp(shifted, ParamRanges.PitchMin, ParamRanges.PitchMax);
        }

        private static float KeepInBand(float value, float previous, FloatRange band, FloatRange full) =>
            band.Contains(previous) ? band.Clamp(value) : full.Clamp(value);

        // Blends an independent draw toward a physical target; coupling 0 ignores the target.
        private static float Coupled(ref Random rng, float target, float coupling)
        {
            var free = rng.NextFloat();
            var jitter = rng.NextFloat(-CouplingJitter, CouplingJitter);
            return math.saturate(math.lerp(free, target + jitter, coupling));
        }

        private static T Pick<T>(List<T> options, ref Random rng, T fallback)
        {
            var index = rng.NextInt(math.max(1, options.Count));
            return options.Count == 0 ? fallback : options[index];
        }

        private static float ClampBetween(float value, float min, float max) =>
            max < min ? min : math.clamp(value, min, max);

        private static bool Has(LayerParam locks, LayerParam param) => (locks & param) != 0;

        private static uint NonZero(uint value) => value == 0 ? 1u : value;

        // The first tonal layer sets the root; later ones take an interval from the harmony set,
        // folded by octaves into their own range so the interval class is preserved.
        private struct HarmonyState
        {
            public readonly HarmonyMode Mode;
            private readonly float _rootPosition;
            private bool _hasRoot;
            private float _root;

            public HarmonyState(HarmonyMode mode, float rootPosition)
            {
                Mode = mode;
                _rootPosition = rootPosition;
                _hasRoot = false;
                _root = 0f;
            }

            public float Place(FloatRange range, float interval)
            {
                if (_hasRoot) return BandRules.FoldOctaves(_root + interval, range);

                _hasRoot = true;
                _root = range.Lerp(_rootPosition);
                return _root;
            }
        }
    }
}
