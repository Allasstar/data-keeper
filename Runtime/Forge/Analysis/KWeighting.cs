using System;

namespace DataKeeper.Forge.Analysis
{
    public struct Biquad
    {
        public double B0, B1, B2, A1, A2;
        private double _z1, _z2;

        public double Process(double x)
        {
            var y = B0 * x + _z1;
            _z1 = B1 * x - A1 * y + _z2;
            _z2 = B2 * x - A2 * y;
            return y;
        }

        public void Reset()
        {
            _z1 = 0.0;
            _z2 = 0.0;
        }
    }

    // ITU-R BS.1770 K-weighting: a high shelf for head diffraction, then the RLB high-pass.
    // The analog prototypes (from libebur128) give the published 48 kHz coefficients and
    // stay correct at other sample rates.
    public struct KWeighting
    {
        private const double ShelfHz = 1681.974450955533;
        private const double ShelfGainDb = 3.999843853973347;
        private const double ShelfQ = 0.7071752369554196;
        private const double ShelfBandwidth = 0.4996667741545416;
        private const double HighPassHz = 38.13547087602444;
        private const double HighPassQ = 0.5003270373238773;

        public Biquad Shelf;
        public Biquad HighPass;

        public static KWeighting Create(int sampleRate)
        {
            var k = Math.Tan(Math.PI * ShelfHz / sampleRate);
            var vh = Math.Pow(10.0, ShelfGainDb / 20.0);
            var vb = Math.Pow(vh, ShelfBandwidth);
            var a0 = 1.0 + k / ShelfQ + k * k;
            var shelf = new Biquad
            {
                B0 = (vh + vb * k / ShelfQ + k * k) / a0,
                B1 = 2.0 * (k * k - vh) / a0,
                B2 = (vh - vb * k / ShelfQ + k * k) / a0,
                A1 = 2.0 * (k * k - 1.0) / a0,
                A2 = (1.0 - k / ShelfQ + k * k) / a0,
            };

            k = Math.Tan(Math.PI * HighPassHz / sampleRate);
            a0 = 1.0 + k / HighPassQ + k * k;
            var highPass = new Biquad
            {
                B0 = 1.0,
                B1 = -2.0,
                B2 = 1.0,
                A1 = 2.0 * (k * k - 1.0) / a0,
                A2 = (1.0 - k / HighPassQ + k * k) / a0,
            };

            return new KWeighting { Shelf = shelf, HighPass = highPass };
        }

        public double Process(double x) => HighPass.Process(Shelf.Process(x));

        public void Reset()
        {
            Shelf.Reset();
            HighPass.Reset();
        }
    }
}
