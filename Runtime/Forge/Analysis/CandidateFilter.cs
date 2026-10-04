using System.Collections.Generic;
using Unity.Mathematics;

namespace DataKeeper.Forge.Analysis
{
    // Ranks rendered candidates: defects (silence, clipping, DC, a tail cut off by the buffer
    // end) cost the most, then distance from the batch's typical sound, measured with robust
    // z-scores so a few wild candidates cannot drag the reference toward themselves.
    public static class CandidateFilter
    {
        public const int DefaultCandidates = 24;
        public const int MaxCandidates = 64;

        private const float RejectScore = 1000f;
        private const float OutlierZ = 3f;
        private const float OutlierPenalty = 4f;
        private const float TypicalityWeight = 0.1f;
        private const float ClippingPenalty = 5f;
        private const float DcPenalty = 3f;
        private const float TruncatedPenalty = 2f;
        private const float MostlySilentPenalty = 1f;
        private const float SpikePenalty = 1f;

        private const float SilentPeakDb = -50f;
        private const float MaxDcOffset = 0.02f;
        private const float TruncatedRatio = 0.98f;
        private const float MostlySilentRatio = 0.2f;
        private const float MaxCrestDb = 30f;

        private const int FeatureCount = 4;
        private static readonly float[] s_FeatureFloor = { 1f, 0.25f, 0.25f, 1f };

        // Fills selected with the best `keep` indices, best first, and returns how many
        // candidates were rejected as defective or outliers.
        public static int Select(IReadOnlyList<SfxAnalysis> candidates, int keep, List<int> selected)
        {
            var count = candidates.Count;
            var scores = new float[count];
            var features = new float[count * FeatureCount];
            var valid = new List<float>(count);
            var rejected = 0;

            for (var i = 0; i < count; i++)
            {
                var analysis = candidates[i];
                scores[i] = DefectScore(analysis);
                Features(analysis, features, i * FeatureCount);
            }

            var median = new float[FeatureCount];
            var spread = new float[FeatureCount];
            for (var f = 0; f < FeatureCount; f++)
            {
                valid.Clear();
                for (var i = 0; i < count; i++)
                    if (scores[i] < RejectScore) valid.Add(features[i * FeatureCount + f]);

                median[f] = Median(valid);
                for (var i = 0; i < valid.Count; i++) valid[i] = math.abs(valid[i] - median[f]);
                // 1.4826 scales the median absolute deviation to a standard deviation for normal data.
                spread[f] = math.max(Median(valid) * 1.4826f, s_FeatureFloor[f]);
            }

            for (var i = 0; i < count; i++)
            {
                if (scores[i] >= RejectScore)
                {
                    rejected++;
                    continue;
                }

                var defective = scores[i] > 0f;
                var distance = 0f;
                var outlier = false;
                for (var f = 0; f < FeatureCount; f++)
                {
                    var z = (features[i * FeatureCount + f] - median[f]) / spread[f];
                    outlier |= math.abs(z) > OutlierZ;
                    distance += z * z;
                }

                scores[i] += distance / FeatureCount * TypicalityWeight;
                if (outlier) scores[i] += OutlierPenalty;
                if (defective || outlier) rejected++;
            }

            selected.Clear();
            for (var i = 0; i < count; i++) selected.Add(i);
            // Index breaks ties so equal scores keep a stable, deterministic order.
            selected.Sort((a, b) => scores[a] != scores[b] ? scores[a].CompareTo(scores[b]) : a.CompareTo(b));
            if (selected.Count > keep) selected.RemoveRange(keep, selected.Count - keep);
            return rejected;
        }

        public static float DefectScore(SfxAnalysis analysis)
        {
            if (analysis.IsSilent || analysis.PeakDb < SilentPeakDb || !math.isfinite(analysis.Rms))
                return RejectScore;

            var score = 0f;
            if (analysis.IsClipping) score += ClippingPenalty;
            if (analysis.DcOffset > MaxDcOffset) score += DcPenalty;
            if (analysis.EffectiveLengthMs >= analysis.LengthMs * TruncatedRatio) score += TruncatedPenalty;
            if (analysis.EffectiveLengthMs < analysis.LengthMs * MostlySilentRatio) score += MostlySilentPenalty;
            if (analysis.CrestFactorDb > MaxCrestDb) score += SpikePenalty;
            return score;
        }

        private static void Features(SfxAnalysis analysis, float[] features, int offset)
        {
            features[offset] = analysis.LoudnessLufs;
            features[offset + 1] = math.log2(math.max(analysis.SpectralCentroidHz, 20f));
            features[offset + 2] = math.log2(math.max(analysis.EffectiveLengthMs, 1f));
            features[offset + 3] = analysis.CrestFactorDb;
        }

        private static float Median(List<float> values)
        {
            if (values.Count == 0) return 0f;
            values.Sort();
            var mid = values.Count / 2;
            return values.Count % 2 == 1 ? values[mid] : 0.5f * (values[mid - 1] + values[mid]);
        }
    }
}
