using System;
using System.Collections.Generic;

namespace DataKeeper.Forge
{
    [Serializable]
    public class Curve
    {
        public List<Breakpoint> Points = new();
        public float Min;
        public float Max = 1f;
        public CurveUnit Unit;
        public bool Locked;

        // Value 0.5 maps to exactly zero offset for the symmetric ranges used by the
        // pitch, cutoff and pan curves, so a flat curve leaves the layer untouched.
        public static Curve Flat(float min, float max, CurveUnit unit) => new()
        {
            Min = min,
            Max = max,
            Unit = unit,
            Points = new List<Breakpoint> { new(0f, 0.5f), new(1f, 0.5f) },
        };

        public static Curve DefaultPitch() => Flat(-24f, 24f, CurveUnit.Semitones);

        public static Curve DefaultCutoff() => Flat(-4f, 4f, CurveUnit.Octaves);

        public static Curve DefaultPan() => Flat(-1f, 1f, CurveUnit.Pan);

        public static Curve Default(CurveTarget target) => target switch
        {
            CurveTarget.Pitch => DefaultPitch(),
            CurveTarget.Cutoff => DefaultCutoff(),
            CurveTarget.Pan => DefaultPan(),
            _ => DefaultAmp(),
        };

        public static Curve DefaultModEnvelope() => new()
        {
            Unit = CurveUnit.Gain,
            Points = new List<Breakpoint> { new(0f, 0f), new(1f, 0f) },
        };

        public static Curve DefaultAmp() => new()
        {
            Unit = CurveUnit.Gain,
            Points = new List<Breakpoint>
            {
                new(0f, 0f),
                new(0.01f, 1f, -0.7f),
                new(1f, 0f),
            },
        };
    }
}
