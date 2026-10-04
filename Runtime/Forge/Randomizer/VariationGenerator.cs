using System;
using System.Collections.Generic;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Render;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Forge
{
    // Rule 10: renders a batch of candidates, analyses each and keeps the best ones.
    // Same source recipe, template and base seed always give the same variations.
    public sealed class VariationGenerator : IDisposable
    {
        private readonly SfxRenderer _renderer = new();
        private readonly SfxAnalyzer _analyzer = new();
        private readonly SfxRecipe _scratch;
        private readonly List<string> _candidates = new();
        private readonly List<SfxAnalysis> _analyses = new();
        private readonly List<int> _selected = new();

        public int CandidateCount => _candidates.Count;
        public int Rejected { get; private set; }

        public VariationGenerator()
        {
            _scratch = ScriptableObject.CreateInstance<SfxRecipe>();
            _scratch.hideFlags = HideFlags.HideAndDontSave;
        }

        // A null template mutates the source instead of randomizing it from scratch.
        public void Generate(SfxRecipe source, CategoryTemplate template, uint baseSeed, int candidates, int keep,
            List<string> results)
        {
            var sourceJson = JsonUtility.ToJson(source);
            candidates = math.clamp(candidates, keep, math.max(keep, CandidateFilter.MaxCandidates));

            _candidates.Clear();
            _analyses.Clear();
            for (var i = 0; i < candidates; i++)
            {
                JsonUtility.FromJsonOverwrite(sourceJson, _scratch);
                var seed = math.max(1u, math.hash(new uint2(baseSeed, (uint)i)));

                if (template != null)
                {
                    SfxRandomizer.Randomize(_scratch, template, seed);
                }
                else
                {
                    SfxRandomizer.Mutate(_scratch, seed);
                    // New noise too, so mutations never share the exact same texture.
                    _scratch.Seed = seed;
                }

                _renderer.Render(_scratch);
                _analyses.Add(_analyzer.Analyze(_renderer.Output, SfxRenderer.Channels, _renderer.SampleRate));
                _candidates.Add(JsonUtility.ToJson(_scratch));
            }

            Rejected = CandidateFilter.Select(_analyses, keep, _selected);

            results.Clear();
            foreach (var index in _selected) results.Add(_candidates[index]);
        }

        public void ClearSampleCache() => _renderer.ClearSampleCache();

        public void Dispose()
        {
            _renderer.Dispose();
            _analyzer.Dispose();
            if (_scratch != null) Object.DestroyImmediate(_scratch);
        }
    }
}
