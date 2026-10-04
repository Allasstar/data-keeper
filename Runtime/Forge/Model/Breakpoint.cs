using System;

namespace DataKeeper.Forge
{
    // Tension bends the segment that starts at this point; the last point's tension is unused.
    [Serializable]
    public struct Breakpoint
    {
        public float Time;
        public float Value;
        public float Tension;

        public Breakpoint(float time, float value, float tension = 0f)
        {
            Time = time;
            Value = value;
            Tension = tension;
        }
    }
}
