using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    public static class CurveEvaluator
    {
        // Tension -1..1 maps to an exponent of 1/4..4.
        public static float Bend(float x, float tension) => math.pow(x, math.exp2(tension * 2f));

        public static float Evaluate(NativeArray<Breakpoint> points, int start, int count, float t)
        {
            if (count <= 0) return 0f;

            var first = points[start];
            if (count == 1 || t <= first.Time) return first.Value;

            var lastIndex = start + count - 1;
            var last = points[lastIndex];
            if (t >= last.Time) return last.Value;

            for (var i = start; i < lastIndex; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                if (t >= b.Time) continue;

                var span = b.Time - a.Time;
                var local = span > 0f ? (t - a.Time) / span : 1f;
                return math.lerp(a.Value, b.Value, Bend(local, a.Tension));
            }

            return last.Value;
        }

        // Managed twin of the job version for editor drawing; must stay numerically identical.
        public static float Evaluate(IReadOnlyList<Breakpoint> points, float t)
        {
            var count = points.Count;
            if (count == 0) return 0f;

            var first = points[0];
            if (count == 1 || t <= first.Time) return first.Value;

            var last = points[count - 1];
            if (t >= last.Time) return last.Value;

            for (var i = 0; i < count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                if (t >= b.Time) continue;

                var span = b.Time - a.Time;
                var local = span > 0f ? (t - a.Time) / span : 1f;
                return math.lerp(a.Value, b.Value, Bend(local, a.Tension));
            }

            return last.Value;
        }
    }
}
