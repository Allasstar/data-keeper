using System;
using System.Collections.Generic;
using DataKeeper.Forge;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    [UxmlElement]
    public partial class ModRouteElement : VisualElement
    {
        public const string UssClassName = "forge-route";

        private static readonly List<string> LayerChoices = BuildLayerChoices();

        private readonly Toggle _enabled;
        private readonly EnumField _source;
        private readonly EnumField _target;
        private readonly DropdownField _layer;
        private readonly KnobElement _amount;

        private SerializedProperty _route;
        private int _index;

        public event Action<int> RemoveRequested;

        public ModRouteElement()
        {
            AddToClassList(UssClassName);

            var top = new VisualElement();
            top.AddToClassList(UssClassName + "__row");
            _enabled = new Toggle { focusable = false };
            _enabled.AddToClassList(UssClassName + "__enabled");
            _source = new EnumField(ModSource.Size);
            _source.AddToClassList(UssClassName + "__source");
            var arrow = new Label("→");
            arrow.AddToClassList(UssClassName + "__arrow");
            _target = new EnumField(ModTarget.Pitch);
            _target.AddToClassList(UssClassName + "__target");
            top.Add(_enabled);
            top.Add(_source);
            top.Add(arrow);
            top.Add(_target);
            Add(top);

            var bottom = new VisualElement();
            bottom.AddToClassList(UssClassName + "__row");
            _layer = new DropdownField(LayerChoices, 0);
            _layer.AddToClassList(UssClassName + "__layer");
            _layer.RegisterValueChangedCallback(e => SetLayer(LayerChoices.IndexOf(e.newValue) - 1));
            _amount = new KnobElement(null, -1f, 1f, 0f, bipolar: true);
            _amount.AddToClassList(UssClassName + "__amount");
            var remove = new Button(() => RemoveRequested?.Invoke(_index)) { text = "×", focusable = false };
            remove.AddToClassList(UssClassName + "__remove");
            bottom.Add(_layer);
            bottom.Add(_amount);
            bottom.Add(remove);
            Add(bottom);
        }

        public void Bind(SerializedProperty route, int index)
        {
            _route = route;
            _index = index;
            _enabled.BindProperty(Relative(nameof(ModRoute.Enabled)));
            _source.BindProperty(Relative(nameof(ModRoute.Source)));
            _target.BindProperty(Relative(nameof(ModRoute.Target)));
            _amount.BindProperty(Relative(nameof(ModRoute.Amount)));

            this.TrackPropertyValue(route, _ => UpdateState());
            UpdateState();
        }

        private SerializedProperty Relative(string path) => _route.FindPropertyRelative(path);

        private void UpdateState()
        {
            if (_route == null) return;

            var source = (ModSource)Relative(nameof(ModRoute.Source)).intValue;
            var target = (ModTarget)Relative(nameof(ModRoute.Target)).intValue;
            var max = ModTargets.MaxAmount(target);
            _amount.Min = -max;
            _amount.Max = max;
            _amount.Format = Format(target);

            var perLayer = ModTargets.IsPerLayer(target);
            _layer.SetEnabled(perLayer);
            var choice = Relative(nameof(ModRoute.Layer)).intValue + 1;
            _layer.SetValueWithoutNotify(LayerChoices[choice >= 0 && choice < LayerChoices.Count ? choice : 0]);

            // The LFO and envelope only reach targets evaluated at control rate.
            var unsupported = ModTargets.IsContinuous(source) && !ModTargets.IsContinuous(target);
            EnableInClassList(UssClassName + "--unsupported", unsupported);
            tooltip = unsupported ? $"{source} only modulates Pitch, Cutoff, Level and Pan." : string.Empty;
        }

        private void SetLayer(int layer)
        {
            Relative(nameof(ModRoute.Layer)).intValue = layer;
            _route.serializedObject.ApplyModifiedProperties();
        }

        private static KnobFormat Format(ModTarget target) => target switch
        {
            ModTarget.Pitch => KnobFormat.Semitones,
            ModTarget.Level or ModTarget.Drive => KnobFormat.Decibels,
            ModTarget.Pan => KnobFormat.Pan,
            ModTarget.Resonance or ModTarget.ReverbMix or ModTarget.DelayMix or ModTarget.TransientAttack
                or ModTarget.LfoDepth => KnobFormat.Percent,
            _ => KnobFormat.Octaves,
        };

        private static List<string> BuildLayerChoices()
        {
            var choices = new List<string> { "All layers" };
            for (var i = 1; i <= SfxRecipe.MaxLayers; i++) choices.Add($"Layer {i}");
            return choices;
        }
    }
}
