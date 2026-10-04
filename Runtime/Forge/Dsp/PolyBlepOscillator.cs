using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    public struct PolyBlepOscillator
    {
        // Above this the two correction regions of a square/triangle period overlap.
        public const float MaxPhaseIncrement = 0.25f;

        public float Phase;

        public float Next(Waveform waveform, float phaseIncrement)
        {
            var dt = math.clamp(phaseIncrement, 0f, MaxPhaseIncrement);
            var t = Phase;

            float y;
            switch (waveform)
            {
                case Waveform.Saw:
                    y = 2f * t - 1f - PolyBlep(t, dt);
                    break;
                case Waveform.Square:
                    y = (t < 0.5f ? 1f : -1f) + PolyBlep(t, dt) - PolyBlep(Wrap(t + 0.5f), dt);
                    break;
                case Waveform.Triangle:
                    y = 4f * math.abs(t - 0.5f) - 1f
                        - 8f * dt * PolyBlamp(t, dt)
                        + 8f * dt * PolyBlamp(Wrap(t + 0.5f), dt);
                    break;
                default:
                    y = math.sin(2f * math.PI * t);
                    break;
            }

            Phase = Wrap(t + dt);
            return y;
        }

        // Residual of a rising step of height 2 at phase 0.
        public static float PolyBlep(float t, float dt)
        {
            if (t < dt)
            {
                var x = t / dt;
                return -(1f - x) * (1f - x);
            }

            if (t > 1f - dt)
            {
                var x = (t - 1f) / dt;
                return (x + 1f) * (x + 1f);
            }

            return 0f;
        }

        // Residual of a unit slope change (per sample) at phase 0: (1 - |x|)^3 / 6.
        public static float PolyBlamp(float t, float dt)
        {
            float x;
            if (t < dt) x = t / dt;
            else if (t > 1f - dt) x = (1f - t) / dt;
            else return 0f;

            var r = 1f - x;
            return r * r * r * (1f / 6f);
        }

        private static float Wrap(float t) => t >= 1f ? t - 1f : t;
    }
}
