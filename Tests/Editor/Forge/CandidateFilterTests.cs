using System.Collections.Generic;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Forge.Tests
{
    public class CandidateFilterTests
    {
        private readonly List<int> _selected = new();

        [Test]
        public void Select_KeepsTheRequestedCountBestFirst()
        {
            var candidates = new List<SfxAnalysis>();
            for (var i = 0; i < 24; i++) candidates.Add(Typical(-18f + (i % 5) * 0.5f, 1500f + i * 20f));

            CandidateFilter.Select(candidates, 8, _selected);

            Assert.AreEqual(8, _selected.Count);
            CollectionAssert.AllItemsAreUnique(_selected);
        }

        [Test]
        public void Select_DefectiveCandidatesComeLast()
        {
            var candidates = new List<SfxAnalysis>();
            for (var i = 0; i < 10; i++) candidates.Add(Typical(-18f, 1500f));

            var silent = Typical(-18f, 1500f);
            silent.Peak = 0f;
            silent.Rms = 0f;
            candidates[2] = silent;

            var clipping = Typical(-18f, 1500f);
            clipping.ClippedSamples = 40;
            candidates[5] = clipping;

            var truncated = Typical(-18f, 1500f);
            truncated.EffectiveLengthMs = truncated.LengthMs;
            candidates[7] = truncated;

            var rejected = CandidateFilter.Select(candidates, 10, _selected);

            Assert.AreEqual(3, rejected);
            Assert.AreEqual(new[] { 7, 5, 2 }, _selected.GetRange(7, 3).ToArray());
        }

        [Test]
        public void Select_LoudnessOutlierIsRankedBelowTypicalCandidates()
        {
            var candidates = new List<SfxAnalysis>();
            for (var i = 0; i < 12; i++) candidates.Add(Typical(-18f + (i % 3) * 0.5f, 1500f + i * 30f));
            candidates[4] = Typical(-45f, 1500f);

            var rejected = CandidateFilter.Select(candidates, 12, _selected);

            Assert.AreEqual(1, rejected);
            Assert.AreEqual(4, _selected[11]);
        }

        [Test]
        public void Select_EqualScoresKeepIndexOrder()
        {
            var candidates = new List<SfxAnalysis>();
            for (var i = 0; i < 6; i++) candidates.Add(Typical(-18f, 1500f));

            CandidateFilter.Select(candidates, 4, _selected);

            Assert.AreEqual(new[] { 0, 1, 2, 3 }, _selected.ToArray());
        }

        [Test]
        public void Generator_IsDeterministicAndKeepsTheBest([Values(false, true)] bool mutate)
        {
            var template = ForgeTestRecipes.LoadTemplates()[SfxCategory.Impact];
            var source = ScriptableObject.CreateInstance<SfxRecipe>();
            SfxRandomizer.Randomize(source, template, 3u);

            var first = new List<string>();
            var second = new List<string>();
            using (var generator = new VariationGenerator())
            {
                generator.Generate(source, mutate ? null : template, 42u, 16, 6, first);
                Assert.AreEqual(16, generator.CandidateCount);
            }

            using (var generator = new VariationGenerator())
                generator.Generate(source, mutate ? null : template, 42u, 16, 6, second);

            Assert.AreEqual(6, first.Count);
            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AllItemsAreUnique(first);
            Object.DestroyImmediate(source);
        }

        private static SfxAnalysis Typical(float loudness, float centroid) => new()
        {
            LengthMs = 500f,
            Peak = AudioMath.DbToLinear(loudness + 12f),
            TruePeak = AudioMath.DbToLinear(loudness + 12f),
            Rms = AudioMath.DbToLinear(loudness),
            LoudnessLufs = loudness,
            SpectralCentroidHz = centroid,
            EffectiveLengthMs = 380f,
        };
    }
}
