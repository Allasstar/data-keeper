using System;

namespace DataKeeper.Forge
{
    [Serializable]
    public class Layer
    {
        public string Name = "Layer";
        public bool Enabled = true;
        public bool Mute;
        public bool Solo;
        public bool Locked;
        public LayerParam LockedParams;
        public SourceSettings Source = SourceSettings.Default;
        public UnisonSettings Unison = UnisonSettings.Default;
        public PhaseSettings Phase;
        public WarpSettings Warp;
        public Band Band = Band.Body;

        // Semitones relative to A4 (440 Hz).
        public float Pitch;
        public FilterSettings Filter = FilterSettings.Default;
        public float LevelDb = -6f;
        public float Pan;
        public float StartOffsetMs;

        // Absolute voice length; every curve spans it and the layer is silent afterwards.
        public float DecayMs = 500f;
        public Curve AmpCurve = Curve.DefaultAmp();
        public Curve PitchCurve = Curve.DefaultPitch();
        public Curve CutoffCurve = Curve.DefaultCutoff();
        public Curve PanCurve = Curve.DefaultPan();

        public Curve GetCurve(CurveTarget target) => target switch
        {
            CurveTarget.Pitch => PitchCurve,
            CurveTarget.Cutoff => CutoffCurve,
            CurveTarget.Pan => PanCurve,
            _ => AmpCurve,
        };

        public void SetCurve(CurveTarget target, Curve curve)
        {
            switch (target)
            {
                case CurveTarget.Pitch: PitchCurve = curve; break;
                case CurveTarget.Cutoff: CutoffCurve = curve; break;
                case CurveTarget.Pan: PanCurve = curve; break;
                default: AmpCurve = curve; break;
            }
        }
    }
}
