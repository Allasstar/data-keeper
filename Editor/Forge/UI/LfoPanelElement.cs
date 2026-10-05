using DataKeeper.Forge;
using DataKeeper.Forge.Render;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // One LFO's Mod page panel. The picture is drawn from the controls while they are dragged,
    // and from the recipe when it changes underneath them (undo, presets).
    public class LfoPanelElement : ParamBoxElement
    {
        public const string PanelClassName = "forge-mod-panel";

        private const float GraphCycles = 2f;

        private readonly StepperElement _shape;
        private readonly StepperElement _mode;
        private readonly KnobElement _rate;
        private readonly KnobElement _phase;
        private readonly FxGraphElement _graph;

        public LfoPanelElement(string title, Color color, ModTarget? rateTarget) : base(title)
        {
            AddToClassList(PanelClassName);
            var topic = ForgeHelp.Modulation;

            _graph = new FxGraphElement { Bipolar = true };
            _graph.AddToClassList(PanelClassName + "__graph");
            _graph.SetTraceColor(color);
            Add(ForgeHints.Set(_graph, topic, "LFO"));

            var steppers = new VisualElement();
            steppers.AddToClassList(PanelClassName + "__steppers");
            _shape = new StepperElement(Waveform.Sine);
            _shape.Changed += _ => Redraw();
            steppers.Add(ForgeHints.Set(_shape, topic, "Shape"));
            _mode = new StepperElement(LfoMode.Retrigger);
            steppers.Add(ForgeHints.Set(_mode, topic, "Mode"));
            Add(steppers);

            _rate = new KnobElement("Rate", LfoSettings.MinRateHz, LfoSettings.MaxRateHz, 4f, KnobFormat.Hertz, KnobScale.Log)
            {
                ModTarget = rateTarget,
            };
            Add(ForgeHints.Set(_rate, topic, "Rate"));

            _phase = new KnobElement("Phase", 0f, 1f, 0f, KnobFormat.Degrees);
            _phase.RegisterValueChangedCallback(_ => Redraw());
            Add(ForgeHints.Set(_phase, topic, "Phase"));
        }

        public void Bind(SerializedProperty lfo)
        {
            _shape.BindProperty(lfo.FindPropertyRelative(nameof(LfoSettings.Shape)));
            _mode.BindProperty(lfo.FindPropertyRelative(nameof(LfoSettings.Mode)));
            _rate.BindProperty(lfo.FindPropertyRelative(nameof(LfoSettings.RateHz)));
            _phase.BindProperty(lfo.FindPropertyRelative(nameof(LfoSettings.Phase)));
        }

        public void Show(LfoSettings settings) => Draw(settings.Shape, settings.Phase);

        private void Redraw() => Draw((Waveform)_shape.value, _phase.value);

        // The shape from phase 0, with the marker where Phase starts the cycle. The last point
        // stops short of a whole cycle, so a saw doesn't end on a drop at the right edge.
        private void Draw(Waveform shape, float phase)
        {
            var trace = _graph.Trace(FxGraphElement.Capacity);
            for (var i = 0; i < trace.Length; i++)
                trace[i] = ModMatrix.Lfo(shape, (float)i / trace.Length * GraphCycles);

            _graph.MarkerX = Mathf.Clamp01(phase) / GraphCycles;
            _graph.MarkDirtyRepaint();
        }
    }
}
