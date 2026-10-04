using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    public struct SamplePlayer
    {
        // Read position in source samples, counted from the playback start.
        public float Position;

        public float Next(NativeArray<float> data, int offset, int length, float step, bool reverse, bool cubic)
        {
            if (Position > length - 1) return 0f;

            var position = reverse ? length - 1 - Position : Position;
            Position += step;
            return ReadAt(data, offset, length, position, cubic);
        }

        // Interpolated read at a fractional index; anything outside the clip reads as silence.
        public static float ReadAt(NativeArray<float> data, int offset, int length, float position, bool cubic)
        {
            var index = (int)math.floor(position);
            var fraction = position - index;

            var x0 = Read(data, offset, length, index);
            var x1 = Read(data, offset, length, index + 1);
            if (!cubic) return math.lerp(x0, x1, fraction);

            var xm1 = Read(data, offset, length, index - 1);
            var x2 = Read(data, offset, length, index + 2);
            return Hermite(xm1, x0, x1, x2, fraction);
        }

        private static float Read(NativeArray<float> data, int offset, int length, int index) =>
            (uint)index < (uint)length ? data[offset + index] : 0f;

        // 4-point Catmull-Rom.
        private static float Hermite(float xm1, float x0, float x1, float x2, float t)
        {
            var c1 = 0.5f * (x1 - xm1);
            var c2 = xm1 - 2.5f * x0 + 2f * x1 - 0.5f * x2;
            var c3 = 0.5f * (x2 - xm1) + 1.5f * (x0 - x1);
            return ((c3 * t + c2) * t + c1) * t + x0;
        }
    }
}
