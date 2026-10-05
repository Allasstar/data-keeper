using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    public static class Warp
    {
        public const float MaxSyncOctaves = 4f;

        public static float SyncRatio(float amount) => math.exp2(MaxSyncOctaves * amount);

        // Level-0 offsets of the two frames a warped voice blends: the Wavetable frames around its
        // position, or the Classic table of the oscillator's waveform twice.
        public static void Tables(SourceType source, Waveform waveform, int bankOffset, float position,
            out int tableA, out int tableB, out float blend)
        {
            if (source != SourceType.Wavetable)
            {
                tableA = Wavetables.ClassicTable(waveform, 0);
                tableB = tableA;
                blend = 0f;
                return;
            }

            var framePosition = math.saturate(position) * (Wavetables.Frames - 1);
            var frame = (int)framePosition;
            tableA = Wavetables.TableOffset(bankOffset, frame, 0);
            tableB = Wavetables.TableOffset(bankOffset, math.min(frame + 1, Wavetables.Frames - 1), 0);
            blend = framePosition - frame;
        }
    }

    // Reads band-limited tables at a warped phase instead of PolyBLEP shapes, because warping
    // moves the edges PolyBLEP corrects (FS2-D2).
    public struct WarpOscillator
    {
        public float Phase;

        // The slave runs at ratio × the voice phase and restarts when the voice phase wraps. Its
        // own edges are band-limited by the table level; the restart is a step PolyBLEP smooths.
        public float NextSync(NativeArray<float> tables, int tableA, int tableB, float blend, float phaseIncrement,
            float ratio)
        {
            var dt = math.clamp(phaseIncrement, 0f, PolyBlepOscillator.MaxPhaseIncrement);
            // A slave above SR/4 has no table level left (the top one is empty) and would alias,
            // so high notes sync less instead (FS2-D13). The min guards the level against rounding.
            if (dt * ratio > PolyBlepOscillator.MaxPhaseIncrement) ratio = PolyBlepOscillator.MaxPhaseIncrement / dt;
            var level = Wavetables.MipLevel(math.min(dt * ratio, PolyBlepOscillator.MaxPhaseIncrement));
            var a = tableA + level * Wavetables.Stride;
            var b = tableB + level * Wavetables.Stride;

            var y = Read(tables, a, b, blend, math.frac(Phase * ratio));
            var blep = PolyBlepOscillator.PolyBlep(Phase, dt);
            if (blep != 0f)
            {
                // PolyBlep is the residual of a step of height 2, hence the half.
                var jump = Read(tables, a, b, blend, 0f) - Read(tables, a, b, blend, math.frac(ratio));
                y += 0.5f * jump * blep;
            }

            Phase = math.frac(Phase + dt);
            return y;
        }

        private static float Read(NativeArray<float> tables, int a, int b, float blend, float phase)
        {
            var x = phase * Wavetables.TableSize;
            var index = (int)x;
            var fraction = x - index;
            var sampleA = math.lerp(tables[a + index], tables[a + index + 1], fraction);
            var sampleB = math.lerp(tables[b + index], tables[b + index + 1], fraction);
            return math.lerp(sampleA, sampleB, blend);
        }
    }
}
