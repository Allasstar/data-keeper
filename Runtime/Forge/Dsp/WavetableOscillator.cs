using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    public struct WavetableOscillator
    {
        public float Phase;

        public float Next(NativeArray<float> tables, int bankOffset, float position, float phaseIncrement)
        {
            var level = Wavetables.MipLevel(phaseIncrement);
            var framePosition = math.saturate(position) * (Wavetables.Frames - 1);
            var frame = (int)framePosition;
            var nextFrame = math.min(frame + 1, Wavetables.Frames - 1);

            var a = Read(tables, Wavetables.TableOffset(bankOffset, frame, level));
            var b = Read(tables, Wavetables.TableOffset(bankOffset, nextFrame, level));

            Phase = math.frac(Phase + phaseIncrement);
            return math.lerp(a, b, framePosition - frame);
        }

        private float Read(NativeArray<float> tables, int offset)
        {
            var x = Phase * Wavetables.TableSize;
            var index = (int)x;
            return math.lerp(tables[offset + index], tables[offset + index + 1], x - index);
        }
    }
}
