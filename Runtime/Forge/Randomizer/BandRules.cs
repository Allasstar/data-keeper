using DataKeeper.Forge.Dsp;

namespace DataKeeper.Forge
{
    public static class BandRules
    {
        public static FloatRange Hz(Band band) => band switch
        {
            Band.Sub => new FloatRange(20f, 120f),
            Band.Body => new FloatRange(120f, 2000f),
            Band.Click => new FloatRange(2000f, 6000f),
            _ => new FloatRange(6000f, 20000f),
        };

        public static FloatRange Semitones(Band band)
        {
            var hz = Hz(band);
            var semitones = new FloatRange(
                AudioMath.RatioToSemitones(hz.Min / AudioMath.ReferenceHz),
                AudioMath.RatioToSemitones(hz.Max / AudioMath.ReferenceHz));
            return semitones.Intersect(new FloatRange(ParamRanges.PitchMin, ParamRanges.PitchMax));
        }

        // A template range that misses the band entirely is kept as authored rather than
        // collapsed to nothing; the band is a guide for masking, not a hard error.
        public static FloatRange FitPitch(FloatRange pitch, Band band) => Fit(pitch, Semitones(band));

        public static FloatRange FitCutoff(FloatRange cutoff, Band band) => Fit(cutoff, Hz(band));

        // Moves by whole octaves so harmony intervals survive; clamps only if the range is
        // narrower than an octave.
        public static float FoldOctaves(float semitones, FloatRange range)
        {
            while (semitones > range.Max) semitones -= 12f;
            while (semitones < range.Min) semitones += 12f;
            return range.Clamp(semitones);
        }

        private static FloatRange Fit(FloatRange range, FloatRange band)
        {
            var fitted = range.Intersect(band);
            return fitted.IsEmpty ? range : fitted;
        }
    }
}
