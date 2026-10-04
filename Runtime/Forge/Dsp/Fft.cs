using System;
using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    // In-place iterative radix-2 complex FFT. The double version builds tables; the float
    // version runs inside Burst analysis jobs with a precomputed twiddle table.
    public static class Fft
    {
        public static void BuildTwiddles(NativeArray<float2> twiddles, int size)
        {
            for (var k = 0; k < size / 2; k++)
            {
                var angle = -2.0 * Math.PI * k / size;
                twiddles[k] = new float2((float)Math.Cos(angle), (float)Math.Sin(angle));
            }
        }

        public static void Forward(NativeArray<float> re, NativeArray<float> im, NativeArray<float2> twiddles, int n)
        {
            for (int i = 1, j = 0; i < n; i++)
            {
                var bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i >= j) continue;

                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }

            for (var length = 2; length <= n; length <<= 1)
            {
                var half = length / 2;
                var stride = n / length;
                for (var start = 0; start < n; start += length)
                {
                    for (var k = 0; k < half; k++)
                    {
                        var w = twiddles[k * stride];
                        var a = start + k;
                        var b = a + half;
                        var vRe = re[b] * w.x - im[b] * w.y;
                        var vIm = re[b] * w.y + im[b] * w.x;

                        re[b] = re[a] - vRe;
                        im[b] = im[a] - vIm;
                        re[a] += vRe;
                        im[a] += vIm;
                    }
                }
            }
        }

        public static void Transform(double[] re, double[] im, bool inverse)
        {
            var n = re.Length;

            for (int i = 1, j = 0; i < n; i++)
            {
                var bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i >= j) continue;

                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }

            for (var length = 2; length <= n; length <<= 1)
            {
                var half = length / 2;
                var angle = 2.0 * Math.PI / length * (inverse ? 1.0 : -1.0);
                var stepRe = Math.Cos(angle);
                var stepIm = Math.Sin(angle);

                for (var start = 0; start < n; start += length)
                {
                    var wRe = 1.0;
                    var wIm = 0.0;
                    for (var k = 0; k < half; k++)
                    {
                        var a = start + k;
                        var b = a + half;
                        var vRe = re[b] * wRe - im[b] * wIm;
                        var vIm = re[b] * wIm + im[b] * wRe;

                        re[b] = re[a] - vRe;
                        im[b] = im[a] - vIm;
                        re[a] += vRe;
                        im[a] += vIm;

                        var nextRe = wRe * stepRe - wIm * stepIm;
                        wIm = wRe * stepIm + wIm * stepRe;
                        wRe = nextRe;
                    }
                }
            }

            if (!inverse) return;
            for (var i = 0; i < n; i++)
            {
                re[i] /= n;
                im[i] /= n;
            }
        }
    }
}
