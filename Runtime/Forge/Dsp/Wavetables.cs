using System;
using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    // Built-in banks, each defined as harmonic spectra and rendered once per mip level so a
    // level never holds harmonics above Nyquist for the notes that select it.
    // Layout: bank -> frame -> level -> TableSize samples plus one guard sample for interpolation.
    public static class Wavetables
    {
        public const int TableSize = 1024;
        public const int Stride = TableSize + 1;
        public const int Frames = 8;
        public const int Levels = 10;
        public const int MaxHarmonics = TableSize / 2;
        public const int BankCount = 5;
        public const int BankSize = Frames * Levels * Stride;

        // Internal Classic set for warped Oscillator layers: one frame per Waveform, appended after
        // the public banks so WavetableBank and its stepper stay as they are (FS2-D2).
        public const int ClassicOffset = BankCount * BankSize;
        public const int ClassicWaves = 4;
        public const int TotalSize = ClassicOffset + ClassicWaves * Levels * Stride;

        public static int BankOffset(WavetableBank bank) => (int)bank * BankSize;

        public static int ClassicTable(Waveform waveform, int level) => TableOffset(ClassicOffset, (int)waveform, level);

        public static int TableOffset(int bankOffset, int frame, int level) =>
            bankOffset + (frame * Levels + level) * Stride;

        // Level L holds harmonics below MaxHarmonics >> L; pick the first that fits the note.
        public static int MipLevel(float phaseIncrement)
        {
            var allowed = 0.5f / math.max(phaseIncrement, 1e-7f);
            var level = (int)math.ceil(math.log2(MaxHarmonics / allowed));
            return math.clamp(level, 0, Levels - 1);
        }

        public static void BuildAll(NativeArray<float> destination)
        {
            var re = new double[TableSize];
            var im = new double[TableSize];
            var cos = new double[MaxHarmonics];
            var sin = new double[MaxHarmonics];

            for (var bank = 0; bank < BankCount; bank++)
            {
                for (var frame = 0; frame < Frames; frame++)
                {
                    Array.Clear(cos, 0, cos.Length);
                    Array.Clear(sin, 0, sin.Length);
                    FillSpectrum((WavetableBank)bank, frame / (float)(Frames - 1), frame, cos, sin);
                    WriteLevels(destination, bank * BankSize, frame, cos, sin, re, im, true);
                }
            }

            for (var wave = 0; wave < ClassicWaves; wave++)
            {
                Array.Clear(cos, 0, cos.Length);
                Array.Clear(sin, 0, sin.Length);
                FillClassic((Waveform)wave, cos, sin);
                WriteLevels(destination, ClassicOffset, wave, cos, sin, re, im, false);
            }
        }

        private static void WriteLevels(NativeArray<float> destination, int bankOffset, int frame, double[] cos,
            double[] sin, double[] re, double[] im, bool normalise)
        {
            // Normalise every level by the full-band peak so loudness does not jump when a sweep
            // crosses into a level with fewer harmonics.
            var gain = 1.0;
            for (var level = 0; level < Levels; level++)
            {
                Synthesize(cos, sin, MaxHarmonics >> level, re, im);
                if (level == 0 && normalise) gain = 1.0 / math.max(Peak(re), 1e-9);

                var offset = TableOffset(bankOffset, frame, level);
                for (var i = 0; i < TableSize; i++) destination[offset + i] = (float)(re[i] * gain);
                destination[offset + TableSize] = destination[offset];
            }
        }

        private static void Synthesize(double[] cos, double[] sin, int harmonicLimit, double[] re, double[] im)
        {
            Array.Clear(re, 0, re.Length);
            Array.Clear(im, 0, im.Length);

            var scale = TableSize / 2.0;
            for (var h = 1; h < harmonicLimit; h++)
            {
                re[h] = cos[h] * scale;
                im[h] = -sin[h] * scale;
                re[TableSize - h] = re[h];
                im[TableSize - h] = -im[h];
            }

            Fft.Transform(re, im, true);
        }

        private static double Peak(double[] samples)
        {
            var peak = 0.0;
            foreach (var s in samples) peak = math.max(peak, math.abs(s));
            return peak;
        }

        private static void FillSpectrum(WavetableBank bank, float x, int frame, double[] cos, double[] sin)
        {
            switch (bank)
            {
                case WavetableBank.Basic:
                    // Sine -> triangle -> saw -> square, crossfading spectra between anchors.
                    var segment = math.min(x * 3f, 2.999f);
                    var anchor = (int)segment;
                    var blend = segment - anchor;
                    for (var h = 1; h < MaxHarmonics; h++)
                        sin[h] = math.lerp(BasicAnchor(anchor, h), BasicAnchor(anchor + 1, h), blend);
                    break;

                case WavetableBank.Harmonic:
                    var count = math.exp2(x * 7f);
                    for (var h = 1; h < MaxHarmonics; h++)
                        sin[h] = math.saturate(count - h + 1) / math.pow(h, 0.6);
                    break;

                case WavetableBank.Pulse:
                    var width = math.lerp(0.5, 0.04, x);
                    for (var h = 1; h < MaxHarmonics; h++)
                        cos[h] = 2.0 / (Math.PI * h) * math.sin(Math.PI * h * width);
                    break;

                case WavetableBank.Formant:
                    var center = math.lerp(2.0, 32.0, x);
                    var spread = 1.0 + center * 0.15;
                    for (var h = 1; h < MaxHarmonics; h++)
                    {
                        var distance = (h - center) / spread;
                        sin[h] = 0.3 / math.sqrt(h) + math.exp(-distance * distance);
                    }
                    break;

                default:
                    // Metallic: sparse, uneven harmonic weights that change from frame to frame.
                    sin[1] = 1.0;
                    for (var h = 2; h <= 128; h++)
                    {
                        var weight = math.hash(new uint2((uint)h, (uint)frame + 1u)) / (double)uint.MaxValue;
                        sin[h] = weight * weight * weight / math.sqrt(h);
                    }
                    break;
            }
        }

        // The Fourier series of PolyBlepOscillator's shapes at their own amplitude, not peak
        // normalised, so a warped wave starts out as loud as the unwarped one.
        private static void FillClassic(Waveform waveform, double[] cos, double[] sin)
        {
            switch (waveform)
            {
                case Waveform.Saw:
                    for (var h = 1; h < MaxHarmonics; h++) sin[h] = -2.0 / (Math.PI * h);
                    break;
                case Waveform.Square:
                    for (var h = 1; h < MaxHarmonics; h += 2) sin[h] = 4.0 / (Math.PI * h);
                    break;
                case Waveform.Triangle:
                    for (var h = 1; h < MaxHarmonics; h += 2) cos[h] = 8.0 / (Math.PI * Math.PI * h * h);
                    break;
                default:
                    sin[1] = 1.0;
                    break;
            }
        }

        private static double BasicAnchor(int anchor, int h)
        {
            var odd = (h & 1) == 1;
            switch (anchor)
            {
                case 0:
                    return h == 1 ? 1.0 : 0.0;
                case 1:
                    return odd ? 8.0 / (Math.PI * Math.PI) * ((h / 2) % 2 == 0 ? 1.0 : -1.0) / (h * h) : 0.0;
                case 2:
                    return 2.0 / (Math.PI * h) * (h % 2 == 1 ? 1.0 : -1.0);
                default:
                    return odd ? 4.0 / (Math.PI * h) : 0.0;
            }
        }
    }
}
