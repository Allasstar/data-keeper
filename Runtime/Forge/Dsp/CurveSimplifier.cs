using System.Collections.Generic;
using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    // Ramer-Douglas-Peucker over (time, value): turns a freehand stroke into the few
    // breakpoints that reproduce it within epsilon.
    public static class CurveSimplifier
    {
        public static List<Breakpoint> Simplify(IReadOnlyList<Breakpoint> points, float epsilon)
        {
            var result = new List<Breakpoint>();
            if (points.Count == 0) return result;
            if (points.Count <= 2)
            {
                for (var i = 0; i < points.Count; i++) result.Add(points[i]);
                return result;
            }

            var keep = new bool[points.Count];
            keep[0] = true;
            keep[points.Count - 1] = true;

            // Explicit stack instead of recursion: a long stroke would otherwise go deep.
            var stack = new Stack<int2>();
            stack.Push(new int2(0, points.Count - 1));

            while (stack.Count > 0)
            {
                var span = stack.Pop();
                var farthest = -1;
                var maxDistance = epsilon;

                for (var i = span.x + 1; i < span.y; i++)
                {
                    var distance = DistanceToSegment(points[i], points[span.x], points[span.y]);
                    if (distance <= maxDistance) continue;
                    maxDistance = distance;
                    farthest = i;
                }

                if (farthest < 0) continue;
                keep[farthest] = true;
                stack.Push(new int2(span.x, farthest));
                stack.Push(new int2(farthest, span.y));
            }

            for (var i = 0; i < points.Count; i++)
                if (keep[i]) result.Add(new Breakpoint(points[i].Time, points[i].Value));
            return result;
        }

        private static float DistanceToSegment(Breakpoint p, Breakpoint a, Breakpoint b)
        {
            var point = new float2(p.Time, p.Value);
            var start = new float2(a.Time, a.Value);
            var end = new float2(b.Time, b.Value);
            var segment = end - start;
            var lengthSq = math.lengthsq(segment);
            if (lengthSq <= 0f) return math.distance(point, start);

            var t = math.saturate(math.dot(point - start, segment) / lengthSq);
            return math.distance(point, start + segment * t);
        }
    }
}
