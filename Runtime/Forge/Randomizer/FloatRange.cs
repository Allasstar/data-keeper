using System;
using Unity.Mathematics;

namespace DataKeeper.Forge
{
    [Serializable]
    public struct FloatRange
    {
        public float Min;
        public float Max;

        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public bool IsEmpty => Max < Min;

        public float Clamp(float value) => math.clamp(value, Min, Max);

        public bool Contains(float value, float tolerance = 1e-3f) =>
            value >= Min - tolerance && value <= Max + tolerance;

        public float Lerp(float t) => math.lerp(Min, Max, t);

        // Frequencies and times are perceived logarithmically, so they are sampled that way.
        public float LerpLog(float t) => Min > 0f && Max > Min ? Min * math.pow(Max / Min, t) : Lerp(t);

        public FloatRange Intersect(FloatRange other) =>
            new(math.max(Min, other.Min), math.min(Max, other.Max));
    }
}
