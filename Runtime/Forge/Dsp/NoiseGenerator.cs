using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    public struct NoiseGenerator
    {
        private const float PinkGain = 0.11f;
        private const float BrownGain = 3.5f;

        private Random _random;
        private float _b0, _b1, _b2, _b3, _b4, _b5, _b6;
        private float _brown;

        public NoiseGenerator(uint seed)
        {
            _random = new Random(seed == 0 ? 1u : seed);
            _b0 = _b1 = _b2 = _b3 = _b4 = _b5 = _b6 = 0f;
            _brown = 0f;
        }

        public float Next(NoiseColor color)
        {
            var white = _random.NextFloat(-1f, 1f);

            switch (color)
            {
                case NoiseColor.Pink:
                    // Paul Kellet's refined pink filter.
                    _b0 = 0.99886f * _b0 + white * 0.0555179f;
                    _b1 = 0.99332f * _b1 + white * 0.0750759f;
                    _b2 = 0.96900f * _b2 + white * 0.1538520f;
                    _b3 = 0.86650f * _b3 + white * 0.3104856f;
                    _b4 = 0.55000f * _b4 + white * 0.5329522f;
                    _b5 = -0.7616f * _b5 - white * 0.0168980f;
                    var pink = _b0 + _b1 + _b2 + _b3 + _b4 + _b5 + _b6 + white * 0.5362f;
                    _b6 = white * 0.115926f;
                    return pink * PinkGain;

                case NoiseColor.Brown:
                    // Leaky integrator: a pure one would random-walk out of range on long sounds.
                    _brown = (_brown + 0.02f * white) / 1.02f;
                    return _brown * BrownGain;

                default:
                    return white;
            }
        }
    }
}
