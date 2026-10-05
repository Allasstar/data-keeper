using DataKeeper.Forge;
using DataKeeper.Forge.Render;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // An Rnd source's Mod page panel: its controls in a bar on top, the picture below. The
    // picture is layer 1's moving signal over the whole sound; Constant has no signal over
    // time, so it shows a line of text instead.
    public class RandomPanelElement : ParamBoxElement
    {
        private const string PanelClassName = LfoPanelElement.PanelClassName;

        private readonly StepperElement _mode;
        private readonly KnobElement _rate;
        private readonly UnsignedIntegerField _seed;
        private readonly FxGraphElement _graph;
        private readonly Label _constant;

        private readonly ModSource _source;

        private uint _recipeLayerSeed;
        private float _lengthSeconds;

        public RandomPanelElement(string title, Color color, ModSource source) : base(title)
        {
            _source = source;
            AddToClassList(PanelClassName);
            AddToClassList(UssClassName + "--stacked");
            var topic = ForgeHelp.Modulation;

            var bar = new VisualElement();
            bar.AddToClassList(PanelClassName + "__bar");
            bar.AddToClassList(PanelClassName + "__bar--rnd");
            Add(bar);

            _mode = new StepperElement(RandomMode.Constant);
            _mode.AddToClassList(PanelClassName + "__mode");
            _mode.Changed += _ => Redraw();
            bar.Add(ForgeHints.Set(_mode, topic, "Rnd mode"));

            _rate = new KnobElement("Rate", RandomSettings.MinRateHz, RandomSettings.MaxRateHz, 4f, KnobFormat.Hertz, KnobScale.Log);
            _rate.AddToClassList("forge-knob--inline");
            _rate.RegisterValueChangedCallback(_ => Redraw());
            bar.Add(ForgeHints.Set(_rate, topic, "Rnd rate"));

            var spacer = new VisualElement();
            spacer.AddToClassList(PanelClassName + "__spacer");
            bar.Add(spacer);

            _seed = new UnsignedIntegerField("Seed");
            _seed.AddToClassList("forge-seed");
            _seed.AddToClassList(PanelClassName + "__seed");
            _seed.RegisterValueChangedCallback(_ => Redraw());
            bar.Add(ForgeHints.Set(_seed, topic, "Rnd seed"));

            var roll = new Button(Roll) { text = "Roll", focusable = false };
            roll.AddToClassList("forge-button");
            roll.AddToClassList("forge-button--pill");
            bar.Add(ForgeHints.Set(roll, topic, "Rnd seed"));

            _graph = new FxGraphElement { Bipolar = true };
            _graph.AddToClassList(PanelClassName + "__graph");
            _graph.SetTraceColor(color);
            Add(ForgeHints.Set(_graph, topic, "RND"));

            _constant = new Label("Constant: every route gets one fixed value per layer, picked by the Seed. " +
                                  "Roll the Seed for another take.");
            _constant.AddToClassList(PanelClassName + "__text");
            Add(ForgeHints.Set(_constant, topic, "RND"));
        }

        public void Bind(SerializedProperty random)
        {
            _mode.BindProperty(random.FindPropertyRelative(nameof(RandomSettings.Mode)));
            _rate.BindProperty(random.FindPropertyRelative(nameof(RandomSettings.RateHz)));
            _seed.BindProperty(random.FindPropertyRelative(nameof(RandomSettings.Seed)));
        }

        // Layer 1's seed for this source, as SfxRenderer derives it.
        public void Show(SfxRecipe recipe)
        {
            _recipeLayerSeed = math.hash(new uint2(recipe.Seed, 0u));
            _lengthSeconds = recipe.LengthMs / 1000f;
            var random = recipe.RandomSettingsOf(_source);
            Draw(random.Mode, random.RateHz, random.Seed);
        }

        // Never 0: that value means "follow the render seed", which would make a roll a no-op.
        private void Roll() => _seed.value = (uint)UnityEngine.Random.Range(1, int.MaxValue);

        private void Redraw() => Draw((RandomMode)_mode.value, _rate.value, _seed.value);

        private void Draw(RandomMode mode, float rateHz, uint sourceSeed)
        {
            var constant = mode == RandomMode.Constant;
            _graph.style.display = constant ? DisplayStyle.None : DisplayStyle.Flex;
            _constant.style.display = constant ? DisplayStyle.Flex : DisplayStyle.None;
            _rate.style.display = constant ? DisplayStyle.None : DisplayStyle.Flex;
            if (constant) return;

            var layerSeed = ModMatrix.RandomSeed(_recipeLayerSeed, _source, sourceSeed);
            var steps = _lengthSeconds * Mathf.Clamp(rateHz, RandomSettings.MinRateHz, RandomSettings.MaxRateHz);
            var trace = _graph.Trace(FxGraphElement.Capacity);
            for (var i = 0; i < trace.Length; i++)
                trace[i] = ModMatrix.RandomSignal(mode, layerSeed, (float)i / (trace.Length - 1) * steps);
            _graph.MarkDirtyRepaint();
        }
    }
}
