namespace DataKeeper.Forge
{
    public static class HarmonySets
    {
        private static readonly float[] Unison = { 0f, 12f, -12f };
        private static readonly float[] Fifths = { 0f, 7f, 12f, 19f, -5f };
        private static readonly float[] Major = { 0f, 4f, 7f, 12f, 16f };
        private static readonly float[] Minor = { 0f, 3f, 7f, 12f, 15f };
        private static readonly float[] Dissonant = { 0f, 1f, 6f, 11f, 13f };

        // Nearest pitch, relative to the layer's own note, whose pitch class is in the mode.
        public static float Snap(float semitones, HarmonyMode mode)
        {
            var best = semitones;
            var bestDistance = float.MaxValue;

            foreach (var interval in Intervals(mode))
            {
                var pitchClass = ((interval % 12f) + 12f) % 12f;
                var candidate = System.MathF.Round((semitones - pitchClass) / 12f) * 12f + pitchClass;
                var distance = System.MathF.Abs(candidate - semitones);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        public static float[] Intervals(HarmonyMode mode) => mode switch
        {
            HarmonyMode.Fifths => Fifths,
            HarmonyMode.Major => Major,
            HarmonyMode.Minor => Minor,
            HarmonyMode.Dissonant => Dissonant,
            _ => Unison,
        };
    }
}
