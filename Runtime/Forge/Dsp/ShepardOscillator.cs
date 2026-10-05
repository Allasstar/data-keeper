using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    // Octave-spaced sines under a Gaussian window (in octaves) centred on the layer pitch. The
    // whole stack glides; a partial that leaves the window at one edge is replaced by a new one
    // at the other, so a rise or fall never ends.
    public struct ShepardOscillator
    {
        public const float MinWidthOctaves = 0.35f;
        public const float MaxWidthOctaves = 3f;

        // Partials fade out between these phase increments, so nothing reaches 0.45 × sample rate.
        private const float FadeStart = 0.35f;
        private const float FadeEnd = 0.45f;

        // Treating the window as 0 past this exponent keeps far partials out of denormals.
        private const float MaxExponent = 40f;

        // The stack is normalised to the power of one sine, but octave stacks crest about 2 dB
        // higher; this brings a full stack's peak back near 1.
        private const float PeakScale = 0.8f;

        private FixedList64Bytes<float> _phases;
        private int _partials;
        private float _halfSpan;
        private float _shift;
        private float _shiftIncrement;
        private float _inverseTwoSigmaSquared;
        private float _edge;
        private float _inverseWindowPeak;

        public ShepardOscillator(int partials, float widthOctaves, float shiftIncrement, float phase)
        {
            _phases = default;
            for (var k = 0; k < partials; k++) _phases.Add(phase);
            _partials = partials;
            _halfSpan = partials * 0.5f;
            _shift = 0f;
            _shiftIncrement = shiftIncrement;
            _inverseTwoSigmaSquared = 0.5f / (widthOctaves * widthOctaves);
            // The window is lowered by its value at the stack edges, so a partial is exactly
            // silent when it leaves or enters and the hand-over cannot click.
            _edge = Gaussian(_halfSpan, _inverseTwoSigmaSquared);
            _inverseWindowPeak = 1f / (1f - _edge);
        }

        public static float WidthOctaves(float width) => math.lerp(MinWidthOctaves, MaxWidthOctaves, math.saturate(width));

        public float Next(float centreIncrement)
        {
            var lowest = centreIncrement * math.exp2(_shift - _halfSpan);
            var sum = 0f;
            var power = 0f;
            for (var k = 0; k < _partials; k++)
            {
                var window = math.max(0f, Gaussian(k - _halfSpan + _shift, _inverseTwoSigmaSquared) - _edge)
                             * _inverseWindowPeak;
                power += window * window;

                var increment = lowest * (1 << k);
                var phase = _phases[k];
                if (window > 0f && increment < FadeEnd)
                {
                    var fade = math.saturate((FadeEnd - increment) * (1f / (FadeEnd - FadeStart)));
                    sum += window * fade * math.sin(2f * math.PI * phase);
                }

                _phases[k] = math.frac(phase + increment);
            }

            // Each partial keeps its phase when it moves to the neighbouring slot.
            _shift += _shiftIncrement;
            if (_shift >= 1f)
            {
                _shift -= 1f;
                for (var k = _partials - 1; k > 0; k--) _phases[k] = _phases[k - 1];
                _phases[0] = 0f;
            }
            else if (_shift < 0f)
            {
                _shift += 1f;
                for (var k = 0; k < _partials - 1; k++) _phases[k] = _phases[k + 1];
                _phases[_partials - 1] = 0f;
            }

            // Normalising by the window's power, not its sum, keeps the loudness steady while
            // partials hand over, so a narrow window does not throb at the glide rate.
            return sum * PeakScale * math.rsqrt(power);
        }

        private static float Gaussian(float octave, float inverseTwoSigmaSquared)
        {
            var exponent = octave * octave * inverseTwoSigmaSquared;
            return exponent > MaxExponent ? 0f : math.exp(-exponent);
        }
    }
}
