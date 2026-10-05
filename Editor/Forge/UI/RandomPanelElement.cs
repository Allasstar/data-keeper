using DataKeeper.Forge;
using DataKeeper.Forge.Render;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // The Rnd source's Mod page panel. The picture is layer 1's moving signal over the whole
    // sound; Constant has no signal over time, so it shows a line of text instead.
    public class RandomPanelElement : ParamBoxElement
    {
        private const string PanelClassName = LfoPanelElement.PanelClassName;

        private readonly StepperElement _mode;
        private readonly KnobElement _rate;
        private readonly FxGraphElement _graph;
        private readonly Label _constant;

        private uint _layerSeed;
        private float _lengthSeconds;

        public RandomPanelElement(Color color) : base("RND")
        {
            AddToClassList(PanelClassName);
            var topic = ForgeHelp.Modulation;

            _graph = new FxGraphElement { Bipolar = true };
            _graph.AddToClassList(PanelClassName + "__graph");
            _graph.SetTraceColor(color);
            Add(ForgeHints.Set(_graph, topic, "RND"));

            _constant = new Label("Constant: every route gets one fixed value per layer, picked by the Seed. " +
                                  "Change the Seed for another take.");
            _constant.AddToClassList(PanelClassName + "__text");
            Add(ForgeHints.Set(_constant, topic, "RND"));

            var steppers = new VisualElement();
            steppers.AddToClassList(PanelClassName + "__steppers");
            _mode = new StepperElement(RandomMode.Constant);
            _mode.Changed += _ => Redraw();
            steppers.Add(ForgeHints.Set(_mode, topic, "Rnd mode"));
            Add(steppers);

            _rate = new KnobElement("Rate", RandomSettings.MinRateHz, RandomSettings.MaxRateHz, 4f, KnobFormat.Hertz, KnobScale.Log);
            _rate.RegisterValueChangedCallback(_ => Redraw());
            Add(ForgeHints.Set(_rate, topic, "Rnd rate"));
        }

        public void Bind(SerializedProperty random)
        {
            _mode.BindProperty(random.FindPropertyRelative(nameof(RandomSettings.Mode)));
            _rate.BindProperty(random.FindPropertyRelative(nameof(RandomSettings.RateHz)));
        }

        // Layer 1's seed, as SfxRenderer derives it.
        public void Show(SfxRecipe recipe)
        {
            _layerSeed = math.hash(new uint2(recipe.Seed, 0u));
            _lengthSeconds = recipe.LengthMs / 1000f;
            Draw(recipe.Random.Mode, recipe.Random.RateHz);
        }

        private void Redraw() => Draw((RandomMode)_mode.value, _rate.value);

        private void Draw(RandomMode mode, float rateHz)
        {
            var constant = mode == RandomMode.Constant;
            _graph.style.display = constant ? DisplayStyle.None : DisplayStyle.Flex;
            _constant.style.display = constant ? DisplayStyle.Flex : DisplayStyle.None;
            _rate.style.display = constant ? DisplayStyle.None : DisplayStyle.Flex;
            if (constant) return;

            var steps = _lengthSeconds * Mathf.Clamp(rateHz, RandomSettings.MinRateHz, RandomSettings.MaxRateHz);
            var trace = _graph.Trace(FxGraphElement.Capacity);
            for (var i = 0; i < trace.Length; i++)
                trace[i] = ModMatrix.RandomSignal(mode, _layerSeed, (float)i / (trace.Length - 1) * steps);
            _graph.MarkDirtyRepaint();
        }
    }
}
