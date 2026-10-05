using System;
using DataKeeper.Forge;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // One matrix row: swatch, light, source → target, layer, amount, remove.
    [UxmlElement]
    public partial class ModRouteElement : VisualElement
    {
        public const string UssClassName = "forge-route";

        private readonly VisualElement _swatch;
        private readonly Toggle _enabled;
        private readonly StepperElement _source;
        private readonly StepperElement _target;
        private readonly VisualElement _layerStepper;
        private readonly Button _layer;
        private readonly ModAmountElement _amount;

        private SerializedProperty _route;
        private int _index;

        public event Action<int> RemoveRequested;

        public ModRouteElement()
        {
            AddToClassList(UssClassName);

            _swatch = new VisualElement();
            _swatch.AddToClassList(UssClassName + "__swatch");
            Add(_swatch);

            _enabled = new Toggle { focusable = false };
            _enabled.AddToClassList("forge-power");
            Add(_enabled);

            _source = new StepperElement(ModSource.Size, ForgeModulation.SourceOrder);
            _source.AddToClassList(UssClassName + "__source");
            Add(_source);

            var arrow = new Label("→");
            arrow.AddToClassList(UssClassName + "__arrow");
            Add(arrow);

            _target = new StepperElement(ModTarget.Pitch);
            _target.AddToClassList(UssClassName + "__target");
            Add(_target);

            _layerStepper = new VisualElement();
            _layerStepper.AddToClassList(StepperElement.UssClassName);
            _layerStepper.AddToClassList(UssClassName + "__layer");
            _layerStepper.Add(Arrow("<", -1));
            _layer = new Button(ShowLayerMenu) { focusable = false };
            _layer.AddToClassList(UssClassName + "__layer-name");
            _layerStepper.Add(_layer);
            _layerStepper.Add(Arrow(">", 1));
            Add(_layerStepper);

            _amount = new ModAmountElement();
            _amount.AddToClassList(UssClassName + "__amount");
            Add(_amount);

            var remove = new Button(() => RemoveRequested?.Invoke(_index)) { text = "×", focusable = false };
            remove.AddToClassList(UssClassName + "__remove");
            Add(remove);
        }

        public void Bind(SerializedProperty route, int index)
        {
            _route = route;
            _index = index;
            _enabled.BindProperty(Relative(nameof(ModRoute.Enabled)));
            _source.BindProperty(Relative(nameof(ModRoute.Source)));
            _target.BindProperty(Relative(nameof(ModRoute.Target)));
            _amount.BindProperty(Relative(nameof(ModRoute.Amount)));

            this.TrackPropertyValue(route, _ => Refresh());
            Refresh();
        }

        private SerializedProperty Relative(string path) => _route.FindPropertyRelative(path);

        // Public because the Rnd mode lives outside the route but decides whether it is supported.
        public void Refresh()
        {
            if (_route == null) return;

            var source = (ModSource)Relative(nameof(ModRoute.Source)).intValue;
            var target = (ModTarget)Relative(nameof(ModRoute.Target)).intValue;
            var color = ForgeModulation.SourceColor(source);
            _swatch.style.backgroundColor = color;
            _amount.Color = color;
            _amount.Max = ModTargets.MaxAmount(target);
            _amount.Format = ForgeModulation.Format(target);

            var perLayer = ModTargets.IsPerLayer(target);
            _layerStepper.SetEnabled(perLayer);
            _layer.text = perLayer ? LayerName(Relative(nameof(ModRoute.Layer)).intValue) : "Global";

            var enabled = Relative(nameof(ModRoute.Enabled)).boolValue;
            var randomMode = (RandomMode)_route.serializedObject
                .FindProperty($"{nameof(SfxRecipe.Random)}.{nameof(RandomSettings.Mode)}").intValue;
            var supported = ForgeModulation.IsSupported(source, target, randomMode);
            EnableInClassList(UssClassName + "--off", !enabled);
            EnableInClassList(UssClassName + "--unsupported", !supported);
            tooltip = supported ? string.Empty : ForgeModulation.UnsupportedReason(source, randomMode);
        }

        private Button Arrow(string text, int direction)
        {
            var button = new Button(() => StepLayer(direction)) { text = text, focusable = false };
            button.AddToClassList(StepperElement.UssClassName + "__arrow");
            return button;
        }

        // Wraps through All layers, then Layer 1..MaxLayers.
        private void StepLayer(int direction)
        {
            var count = SfxRecipe.MaxLayers + 1;
            var index = Relative(nameof(ModRoute.Layer)).intValue + 1;
            SetLayer((index + direction + count) % count - 1);
        }

        private void ShowLayerMenu()
        {
            var current = Relative(nameof(ModRoute.Layer)).intValue;
            var menu = new GenericMenu();
            for (var layer = ModRoute.AllLayers; layer < SfxRecipe.MaxLayers; layer++)
            {
                var choice = layer;
                menu.AddItem(new GUIContent(LayerName(layer)), layer == current, () => SetLayer(choice));
            }

            menu.DropDown(_layer.worldBound);
        }

        private void SetLayer(int layer)
        {
            Relative(nameof(ModRoute.Layer)).intValue = layer;
            _route.serializedObject.ApplyModifiedProperties();
        }

        private static string LayerName(int layer) => layer < 0 ? "All layers" : $"Layer {layer + 1}";
    }
}
