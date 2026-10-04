using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    // Zavalishin/Simper TPT state variable filter: trapezoidal integrators keep it stable when
    // the cutoff is modulated every sample, unlike the classic Chamberlin form.
    public struct StateVariableFilter
    {
        private const float MaxCutoffRatio = 0.49f;

        private float _ic1;
        private float _ic2;

        public static float CutoffToG(float cutoffHz, float sampleRate)
        {
            var cutoff = math.clamp(cutoffHz, ParamRanges.CutoffMin, sampleRate * MaxCutoffRatio);
            return math.tan(math.PI * cutoff / sampleRate);
        }

        // k = 1/Q; the floor caps Q at 25 so full resonance rings without self-oscillating.
        public static float ResonanceToK(float resonance) => 2f - 1.96f * math.saturate(resonance);

        public float Process(float input, float g, float k, FilterType type)
        {
            var a1 = 1f / (1f + g * (g + k));
            var a2 = g * a1;
            var a3 = g * a2;

            var v3 = input - _ic2;
            var v1 = a1 * _ic1 + a2 * v3;
            var v2 = _ic2 + a2 * _ic1 + a3 * v3;
            _ic1 = 2f * v1 - _ic1;
            _ic2 = 2f * v2 - _ic2;

            switch (type)
            {
                case FilterType.LowPass: return v2;
                case FilterType.HighPass: return input - k * v1 - v2;
                // Scaled by k so the peak gain stays at unity regardless of resonance.
                case FilterType.BandPass: return k * v1;
                case FilterType.Notch: return input - k * v1;
                default: return input;
            }
        }
    }
}
