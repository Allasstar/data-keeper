using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Forge.Tests
{
    public class RandomizerTests
    {
        private static readonly CurveTarget[] CurveTargets =
            { CurveTarget.Amp, CurveTarget.Pitch, CurveTarget.Cutoff, CurveTarget.Pan };

        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        // ── Templates ───────────────────────────────────────────────────────────────

        [Test]
        public void Templates_ParseAndCoverEveryCategory()
        {
            var templates = LoadTemplates();

            foreach (SfxCategory category in Enum.GetValues(typeof(SfxCategory)))
                Assert.IsTrue(templates.ContainsKey(category), $"Missing template for {category}");

            foreach (var template in templates.Values)
            {
                var name = template.Category.ToString();
                Assert.That(template.Layers.Count, Is.InRange(1, SfxRecipe.MaxLayers), name);
                AssertValidRange(template.LengthMs, name + " length");

                foreach (var layer in template.Layers)
                {
                    var label = $"{name}/{layer.Name}";
                    Assert.IsNotEmpty(layer.Sources, label + " sources");
                    Assert.IsNotEmpty(layer.Filters, label + " filters");
                    AssertValidRange(layer.Pitch, label + " pitch");
                    AssertValidRange(layer.CutoffHz, label + " cutoff");
                    AssertValidRange(layer.LevelDb, label + " level");
                    AssertValidRange(layer.DecayMs, label + " decay");
                    foreach (var target in CurveTargets)
                    {
                        var curve = layer.GetCurve(target);
                        if (curve != null) AssertValidCurve(curve.Points, $"{label} {target}");
                    }
                }
            }
        }

        // ── Randomize ───────────────────────────────────────────────────────────────

        [Test]
        public void Randomize_IsDeterministic([Values] SfxCategory category)
        {
            var template = LoadTemplates()[category];
            var a = NewRecipe();
            var b = NewRecipe();

            SfxRandomizer.Randomize(a, template, 1234u);
            SfxRandomizer.Randomize(b, template, 1234u);

            Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b));
        }

        [Test]
        public void Randomize_StaysInsideTemplateRanges(
            [Values] SfxCategory category,
            [Values(1u, 77u, 9001u, 424242u)] uint seed)
        {
            var template = LoadTemplates()[category];
            var recipe = NewRecipe();

            SfxRandomizer.Randomize(recipe, template, seed);

            Assert.AreEqual(category, recipe.Category);
            AssertIn(template.LengthMs, recipe.LengthMs, "length");
            Assert.AreEqual(template.Layers.Count, recipe.Layers.Count);

            for (var i = 0; i < template.Layers.Count; i++)
            {
                var t = template.Layers[i];
                var layer = recipe.Layers[i];
                var label = $"{category}/{t.Name}";

                Assert.AreEqual(t.Band, layer.Band, label);
                CollectionAssert.Contains(t.Sources, layer.Source.Type, label + " source");
                CollectionAssert.Contains(t.Filters, layer.Filter.Type, label + " filter");
                AssertIn(BandRules.FitPitch(t.Pitch, t.Band), layer.Pitch, label + " pitch");
                AssertIn(BandRules.FitCutoff(t.CutoffHz, t.Band), layer.Filter.CutoffHz, label + " cutoff");
                AssertIn(t.Resonance, layer.Filter.Resonance, label + " resonance");
                AssertIn(t.LevelDb, layer.LevelDb, label + " level");
                AssertIn(t.Pan, layer.Pan, label + " pan");
                AssertIn(t.DecayMs, layer.DecayMs, label + " decay");
                AssertIn(t.OffsetMs, layer.StartOffsetMs, label + " offset");
                foreach (var target in CurveTargets)
                {
                    var curve = layer.GetCurve(target);
                    AssertValidCurve(curve.Points, $"{label} {target}");
                    Assert.Less(curve.Min, curve.Max, $"{label} {target} range");
                }
            }
        }

        [Test]
        public void Randomize_KeepsLockedLayerUntouched()
        {
            var template = LoadTemplates()[SfxCategory.Impact];
            var recipe = NewRecipe();
            SfxRandomizer.Randomize(recipe, template, 1u);

            var locked = recipe.Layers[1];
            locked.Locked = true;
            var before = JsonUtility.ToJson(locked);

            SfxRandomizer.Randomize(recipe, template, 2u);

            Assert.AreSame(locked, recipe.Layers[1]);
            Assert.AreEqual(before, JsonUtility.ToJson(recipe.Layers[1]));
        }

        [Test]
        public void Randomize_KeepsLockedParamsAndCurve()
        {
            var template = LoadTemplates()[SfxCategory.Laser];
            var recipe = NewRecipe();
            SfxRandomizer.Randomize(recipe, template, 1u);

            var layer = recipe.Layers[0];
            layer.LockedParams = LayerParam.Pitch | LayerParam.Level | LayerParam.Cutoff | LayerParam.Source;
            layer.AmpCurve.Locked = true;
            layer.PitchCurve.Locked = true;
            var pitchCurve = JsonUtility.ToJson(layer.PitchCurve);
            var pitch = layer.Pitch;
            var level = layer.LevelDb;
            var cutoff = layer.Filter.CutoffHz;
            var source = JsonUtility.ToJson(layer.Source);
            var curve = JsonUtility.ToJson(layer.AmpCurve);
            var decay = layer.DecayMs;

            SfxRandomizer.Randomize(recipe, template, 2u);
            var after = recipe.Layers[0];

            Assert.AreEqual(pitch, after.Pitch);
            Assert.AreEqual(level, after.LevelDb);
            Assert.AreEqual(cutoff, after.Filter.CutoffHz);
            Assert.AreEqual(source, JsonUtility.ToJson(after.Source));
            Assert.AreEqual(curve, JsonUtility.ToJson(after.AmpCurve));
            Assert.AreEqual(pitchCurve, JsonUtility.ToJson(after.PitchCurve));
            Assert.AreEqual(layer.LockedParams, after.LockedParams);
            Assert.AreNotEqual(decay, after.DecayMs, "Unlocked parameters should still be randomized");
        }

        [Test]
        public void Randomize_LockingOneParamLeavesOtherResultsUnchanged()
        {
            var template = LoadTemplates()[SfxCategory.Magic];
            var free = NewRecipe();
            SfxRandomizer.Randomize(free, template, 1u);
            var withLock = Clone(free);
            withLock.Layers[0].LockedParams = LayerParam.Pitch;

            SfxRandomizer.Randomize(free, template, 2u);
            SfxRandomizer.Randomize(withLock, template, 2u);

            withLock.Layers[0].Pitch = free.Layers[0].Pitch;
            withLock.Layers[0].LockedParams = LayerParam.None;
            Assert.AreEqual(JsonUtility.ToJson(free), JsonUtility.ToJson(withLock));
        }

        [Test]
        public void Randomize_LockLengthKeepsLength()
        {
            var template = LoadTemplates()[SfxCategory.Explosion];
            var recipe = NewRecipe();
            recipe.LengthMs = 123f;
            recipe.Randomizer.LockLength = true;

            SfxRandomizer.Randomize(recipe, template, 5u);

            Assert.AreEqual(123f, recipe.LengthMs);
        }

        [Test]
        public void Randomize_TonalLayersFollowHarmony(
            [Values(HarmonyMode.Unison, HarmonyMode.Fifths, HarmonyMode.Major, HarmonyMode.Minor, HarmonyMode.Dissonant)]
            HarmonyMode mode,
            [Values(3u, 30u, 300u)] uint seed)
        {
            var template = new CategoryTemplate { Category = SfxCategory.Magic };
            for (var i = 0; i < 4; i++)
            {
                template.Layers.Add(new LayerTemplate
                {
                    Name = $"Tone {i}",
                    Band = Band.Body,
                    Tonal = true,
                    Sources = { SourceType.Oscillator },
                    Filters = { FilterType.Off },
                    Pitch = new FloatRange(-20f, 24f),
                });
            }

            var recipe = NewRecipe();
            recipe.Randomizer.Harmony = mode;
            SfxRandomizer.Randomize(recipe, template, seed);

            var allowed = new HashSet<int>();
            foreach (var interval in HarmonySets.Intervals(mode)) allowed.Add(PitchClass(interval));

            var root = recipe.Layers[0].Pitch;
            foreach (var layer in recipe.Layers)
                Assert.That(allowed.Contains(PitchClass(layer.Pitch - root)), $"{mode}: {layer.Pitch - root} st from root");
        }

        [Test]
        public void Randomize_SnappedPitchCurvesLandOnHarmony([Values] HarmonyMode mode, [Values(1u, 2u, 3u)] uint seed)
        {
            var template = LoadTemplates()[SfxCategory.Pickup];
            var recipe = NewRecipe();
            recipe.Randomizer.Harmony = mode;

            SfxRandomizer.Randomize(recipe, template, seed);

            var allowed = new HashSet<int>();
            foreach (var interval in HarmonySets.Intervals(mode)) allowed.Add(PitchClass(interval));

            for (var i = 0; i < template.Layers.Count; i++)
            {
                if (template.Layers[i].PitchCurve is not { SnapToHarmony: true }) continue;

                var curve = recipe.Layers[i].PitchCurve;
                foreach (var point in curve.Points)
                {
                    var semitones = Mathf.Lerp(curve.Min, curve.Max, point.Value);
                    Assert.That(allowed.Contains(PitchClass(semitones)), $"{mode}: {semitones} st is off the scale");
                }
            }
        }

        [Test]
        public void Randomize_FxStaysInsideTemplateRanges([Values] SfxCategory category, [Values(1u, 9u, 77u)] uint seed)
        {
            var template = LoadTemplates()[category];
            var recipe = NewRecipe();

            SfxRandomizer.Randomize(recipe, template, seed);

            var fx = recipe.Fx;
            var t = template.Fx;
            AssertIn(t.TransientAttack, fx.Transient.Attack, "transient");
            AssertIn(t.DriveDb, fx.Distortion.DriveDb, "drive");
            AssertIn(t.DelayTimeMs, fx.Delay.TimeMs, "delay time");
            AssertIn(t.DelayFeedback, fx.Delay.Feedback, "delay feedback");
            AssertIn(t.DelayMix, fx.Delay.Mix, "delay mix");
            AssertIn(t.ReverbMix, fx.Reverb.Mix, "reverb mix");
            AssertIn(t.ReverbSize, fx.Reverb.Size, "reverb size");
            if (t.DriveDb.Max <= 0f) Assert.IsFalse(fx.Distortion.Enabled, "distortion without a drive range");
            if (t.ReverbMix.Max <= 0f) Assert.IsFalse(fx.Reverb.Enabled, "reverb without a mix range");
            if (t.DelayProbability <= 0f) Assert.IsFalse(fx.Delay.Enabled, "delay with zero probability");
        }

        [Test]
        public void Randomize_LockFxKeepsChain()
        {
            var recipe = NewRecipe();
            recipe.Randomizer.LockFx = true;
            recipe.Fx.Reverb = new ReverbSettings { Enabled = true, Size = 0.11f, Damping = 0.22f, Mix = 0.33f };
            var before = JsonUtility.ToJson(recipe.Fx);

            SfxRandomizer.Randomize(recipe, LoadTemplates()[SfxCategory.Explosion], 4u);
            recipe.Randomizer.VariationAmount = 1f;
            SfxRandomizer.Mutate(recipe, 5u);

            Assert.AreEqual(before, JsonUtility.ToJson(recipe.Fx));
        }

        [Test]
        public void Randomize_FmAndWavetableComeFromCategorySets([Values(1u, 2u, 3u, 4u, 5u, 6u)] uint seed)
        {
            foreach (var category in new[] { SfxCategory.Laser, SfxCategory.Magic, SfxCategory.Impact })
            {
                var template = LoadTemplates()[category];
                var recipe = NewRecipe();
                SfxRandomizer.Randomize(recipe, template, seed);

                for (var i = 0; i < template.Layers.Count; i++)
                {
                    var source = recipe.Layers[i].Source;
                    var t = template.Layers[i];
                    if (source.Type == SourceType.FM)
                    {
                        CollectionAssert.Contains(template.FmRatios, source.Fm.Ratio, $"{category}/{t.Name} ratio");
                        AssertIn(t.FmIndex, source.Fm.Index, $"{category}/{t.Name} index");
                    }

                    if (source.Type == SourceType.Wavetable)
                    {
                        CollectionAssert.Contains(t.Wavetables, source.Wavetable.Bank, $"{category}/{t.Name} bank");
                        AssertIn(t.WavetablePosition, source.Wavetable.Position, $"{category}/{t.Name} position");
                    }
                }
            }
        }

        // ── Mutate ──────────────────────────────────────────────────────────────────

        [Test]
        public void Mutate_ZeroAmountChangesNothing([Values] SfxCategory category)
        {
            var recipe = NewRecipe();
            SfxRandomizer.Randomize(recipe, LoadTemplates()[category], 11u);
            recipe.Randomizer.VariationAmount = 0f;
            var before = JsonUtility.ToJson(recipe);

            SfxRandomizer.Mutate(recipe, 99u);

            Assert.AreEqual(before, JsonUtility.ToJson(recipe));
        }

        [Test]
        public void Mutate_RespectsLocksAndParamRanges([Values(1u, 2u, 3u, 4u, 5u)] uint seed)
        {
            var recipe = NewRecipe();
            SfxRandomizer.Randomize(recipe, LoadTemplates()[SfxCategory.Explosion], 8u);
            recipe.Randomizer.VariationAmount = 1f;
            recipe.Layers[0].Locked = true;
            recipe.Layers[1].LockedParams = LayerParam.Decay | LayerParam.Pan;
            recipe.Layers[2].AmpCurve.Locked = true;

            var lockedLayer = JsonUtility.ToJson(recipe.Layers[0]);
            var decay = recipe.Layers[1].DecayMs;
            var pan = recipe.Layers[1].Pan;
            var curve = JsonUtility.ToJson(recipe.Layers[2].AmpCurve);

            for (var i = 0; i < 20; i++) SfxRandomizer.Mutate(recipe, seed * 100u + (uint)i);

            Assert.AreEqual(lockedLayer, JsonUtility.ToJson(recipe.Layers[0]));
            Assert.AreEqual(decay, recipe.Layers[1].DecayMs);
            Assert.AreEqual(pan, recipe.Layers[1].Pan);
            Assert.AreEqual(curve, JsonUtility.ToJson(recipe.Layers[2].AmpCurve));

            AssertIn(new FloatRange(SfxRecipe.MinLengthMs, SfxRecipe.MaxLengthMs), recipe.LengthMs, "length");
            foreach (var layer in recipe.Layers)
            {
                AssertIn(new FloatRange(ParamRanges.PitchMin, ParamRanges.PitchMax), layer.Pitch, "pitch");
                AssertIn(new FloatRange(ParamRanges.CutoffMin, ParamRanges.CutoffMax), layer.Filter.CutoffHz, "cutoff");
                AssertIn(new FloatRange(0f, 1f), layer.Filter.Resonance, "resonance");
                AssertIn(new FloatRange(ParamRanges.LevelMinDb, ParamRanges.LevelMaxDb), layer.LevelDb, "level");
                AssertIn(new FloatRange(-1f, 1f), layer.Pan, "pan");
                AssertIn(new FloatRange(ParamRanges.DecayMinMs, ParamRanges.DecayMaxMs), layer.DecayMs, "decay");
                AssertIn(new FloatRange(0f, ParamRanges.OffsetMaxMs), layer.StartOffsetMs, "offset");
                AssertValidCurve(layer.AmpCurve.Points, layer.Name);
            }
        }

        [Test]
        public void Mutate_PreservesIntervalsBetweenTonalLayers([Values(1u, 2u, 3u)] uint seed)
        {
            var recipe = NewRecipe();
            recipe.Randomizer.Harmony = HarmonyMode.Major;
            SfxRandomizer.Randomize(recipe, LoadTemplates()[SfxCategory.Pickup], seed);
            recipe.Randomizer.VariationAmount = 1f;

            var classesBefore = TonalPitchClasses(recipe);
            SfxRandomizer.Mutate(recipe, seed + 1000u);

            CollectionAssert.AreEqual(classesBefore, TonalPitchClasses(recipe));
        }

        [Test]
        public void Mutate_KeepsPitchCurveValues([Values(1u, 2u, 3u)] uint seed)
        {
            var recipe = NewRecipe();
            SfxRandomizer.Randomize(recipe, LoadTemplates()[SfxCategory.Pickup], seed);
            recipe.Randomizer.VariationAmount = 1f;

            var before = new List<float>();
            foreach (var layer in recipe.Layers)
                foreach (var point in layer.PitchCurve.Points) before.Add(point.Value);

            SfxRandomizer.Mutate(recipe, seed + 50u);

            var after = new List<float>();
            foreach (var layer in recipe.Layers)
                foreach (var point in layer.PitchCurve.Points) after.Add(point.Value);
            CollectionAssert.AreEqual(before, after);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private SfxRecipe NewRecipe()
        {
            var recipe = ScriptableObject.CreateInstance<SfxRecipe>();
            _created.Add(recipe);
            return recipe;
        }

        private SfxRecipe Clone(SfxRecipe source)
        {
            var copy = NewRecipe();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);
            return copy;
        }

        private static List<int> TonalPitchClasses(SfxRecipe recipe)
        {
            var classes = new List<int>();
            var root = recipe.Layers[0].Pitch;
            foreach (var layer in recipe.Layers)
                if (SourceSettings.IsTonal(layer.Source.Type)) classes.Add(PitchClass(layer.Pitch - root));
            return classes;
        }

        private static int PitchClass(float semitones) => ((int)Mathf.Round(semitones) % 12 + 12) % 12;

        private static void AssertIn(FloatRange range, float value, string label)
        {
            var tolerance = Mathf.Max(1e-3f, Mathf.Abs(value) * 1e-5f);
            Assert.That(range.Contains(value, tolerance), $"{label}: {value} outside [{range.Min}, {range.Max}]");
        }

        private static void AssertValidRange(FloatRange range, string label) =>
            Assert.LessOrEqual(range.Min, range.Max, label);

        private static void AssertValidCurve(List<Breakpoint> points, string label)
        {
            Assert.GreaterOrEqual(points.Count, 2, label + " curve points");
            Assert.AreEqual(0f, points[0].Time, label + " curve start");
            Assert.AreEqual(1f, points[points.Count - 1].Time, label + " curve end");

            for (var i = 0; i < points.Count; i++)
            {
                Assert.That(points[i].Value, Is.InRange(0f, 1f), label + " curve value");
                Assert.That(points[i].Tension, Is.InRange(-1f, 1f), label + " curve tension");
                if (i > 0) Assert.Greater(points[i].Time, points[i - 1].Time, label + " curve order");
            }
        }

        private static Dictionary<SfxCategory, CategoryTemplate> LoadTemplates() => ForgeTestRecipes.LoadTemplates();
    }
}
