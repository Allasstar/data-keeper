using System;
using System.Collections.Generic;
using DataKeeper.Forge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Owns everything between a route in the recipe and its marks on the knobs: drop rules,
    // route edits from chips, and refreshing arcs and chips whenever the recipe changes.
    public class ForgeModulation
    {
        private const float DropAmountFraction = 0.25f;
        private const float ChipSensitivity = 0.005f;
        private const float FineFactor = 0.1f;

        private static readonly Color[] SourceColors =
        {
            new(0.36f, 0.62f, 1f),
            new(1f, 0.38f, 0.38f),
            new(0.98f, 0.84f, 0.3f),
            new(0.4f, 0.88f, 0.55f),
            new(0.72f, 0.5f, 1f),
            new(0.3f, 0.86f, 0.9f),
            new(0.92f, 0.5f, 0.82f),
            new(1f, 0.6f, 0.25f),
            new(0.75f, 0.92f, 0.3f),
            new(0.22f, 0.66f, 0.66f),
            new(0.85f, 0.85f, 0.95f),
        };

        // Indexed by the ModSource value, which appends new sources at the end (FSF-D5).
        private static readonly string[] SourceNames =
            { "Size", "Energy", "Tone", "Motion", "LFO 1", "Env 1", "Rnd", "LFO 2", "LFO 3", "Env 2", "Env 3" };

        // Display order for the source bar and the route rows' arrows.
        public static readonly ModSource[] SourceOrder =
        {
            ModSource.Size, ModSource.Energy, ModSource.Tone, ModSource.Motion,
            ModSource.Lfo, ModSource.Lfo2, ModSource.Lfo3,
            ModSource.Envelope, ModSource.Env2, ModSource.Env3,
            ModSource.Random,
        };

        private readonly VisualElement _root;
        private readonly Action<string> _status;
        private readonly Action _routesChanged;
        private readonly List<KnobElement> _knobs = new();

        private SfxRecipe _recipe;
        private SerializedObject _serialized;
        private int _undoGroup;

        public ForgeModulation(VisualElement root, Action<string> status, Action routesChanged)
        {
            _root = root;
            _status = status;
            _routesChanged = routesChanged;
        }

        public static Color SourceColor(ModSource source) => SourceColors[(int)source];

        public static string SourceName(ModSource source) => SourceNames[(int)source];

        public static KnobFormat Format(ModTarget target) => target switch
        {
            ModTarget.Pitch => KnobFormat.Semitones,
            ModTarget.Level or ModTarget.Drive => KnobFormat.Decibels,
            ModTarget.Pan => KnobFormat.Pan,
            ModTarget.Resonance or ModTarget.ReverbMix or ModTarget.DelayMix or ModTarget.TransientAttack
                or ModTarget.LfoDepth => KnobFormat.Percent,
            _ => KnobFormat.Octaves,
        };

        public static bool IsSupported(ModSource source, ModTarget target, RandomMode randomMode) =>
            !ModTargets.IsContinuous(source, randomMode) || ModTargets.IsContinuous(target);

        public static string UnsupportedReason(ModSource source, RandomMode randomMode) =>
            source == ModSource.Random
                ? $"Rnd in {(randomMode == RandomMode.Smooth ? "Smooth" : "Sample & Hold")} mode only modulates Pitch, Cutoff, Level and Pan. Set it to Constant to reach other targets."
                : $"{SourceName(source)} only modulates Pitch, Cutoff, Level and Pan.";

        public static bool IsUnipolar(ModSource source) =>
            source is ModSource.Envelope or ModSource.Env2 or ModSource.Env3;

        public void Bind(SfxRecipe recipe, SerializedObject serialized)
        {
            _recipe = recipe;
            _serialized = serialized;
            Refresh();
        }

        // ── Dropping a source on a knob ─────────────────────────────────────────────

        public bool CanDrop(ModSource source, KnobElement knob, bool allLayers, out string message)
        {
            if (_recipe == null || knob?.ModTarget == null)
            {
                message = "Drop on a highlighted knob to add a route.";
                return false;
            }

            var target = knob.ModTarget.Value;
            var layer = DropLayer(knob, allLayers);
            if (!IsSupported(source, target, _recipe.Random.Mode))
            {
                message = UnsupportedReason(source, _recipe.Random.Mode);
                return false;
            }

            if (Find(source, target, layer) >= 0)
            {
                message = $"{Describe(source, target, layer)} already exists. Drag its chip to change the amount.";
                return false;
            }

            message = ModTargets.IsPerLayer(target) && !allLayers
                ? $"Drop: {Describe(source, target, layer)}.  Alt: all layers."
                : $"Drop: {Describe(source, target, layer)}.";
            return true;
        }

        public void Drop(ModSource source, KnobElement knob, bool allLayers)
        {
            if (!CanDrop(source, knob, allLayers, out var message))
            {
                _status(message);
                return;
            }

            var target = knob.ModTarget.Value;
            var route = new ModRoute(source, target, ModTargets.MaxAmount(target) * DropAmountFraction)
            {
                Layer = DropLayer(knob, allLayers),
            };

            Undo.RecordObject(_recipe, "Add Route");
            _recipe.Routes.Add(route);
            Commit();
            _status($"Added {Describe(source, target, route.Layer)}. Drag its chip to set the amount.");
        }

        private static int DropLayer(KnobElement knob, bool allLayers) =>
            allLayers || !ModTargets.IsPerLayer(knob.ModTarget.Value) ? ModRoute.AllLayers : knob.ModLayer;

        private int Find(ModSource source, ModTarget target, int layer)
        {
            var routes = _recipe.Routes;
            for (var i = 0; i < routes.Count; i++)
            {
                var route = routes[i];
                if (route.Source == source && route.Target == target && route.Layer == layer) return i;
            }

            return -1;
        }

        private static string Describe(ModSource source, ModTarget target, int layer) =>
            ModTargets.IsPerLayer(target)
                ? $"{SourceName(source)} → {target}, {(layer < 0 ? "all layers" : $"layer {layer + 1}")}"
                : $"{SourceName(source)} → {target}";

        // ── Chip edits ──────────────────────────────────────────────────────────────

        public void BeginAmountEdit()
        {
            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
        }

        public void NudgeAmount(int index, float pixels, bool fine)
        {
            var route = _recipe.Routes[index];
            var max = ModTargets.MaxAmount(route.Target);
            SetAmount(index, route.Amount + pixels * ChipSensitivity * 2f * max * (fine ? FineFactor : 1f));
        }

        public void EndAmountEdit() => Undo.CollapseUndoOperations(_undoGroup);

        public void SetAmount(int index, float amount)
        {
            var route = _recipe.Routes[index];
            var max = ModTargets.MaxAmount(route.Target);
            amount = Mathf.Clamp(amount, -max, max);
            RouteProperty(index, nameof(ModRoute.Amount)).floatValue = amount;
            _serialized.ApplyModifiedProperties();
            _status($"{Describe(route.Source, route.Target, route.Layer)}: {KnobElement.FormatAs(Format(route.Target), amount)}");
        }

        public void PopulateChipMenu(int index, int knobLayer, DropdownMenu menu)
        {
            var route = _recipe.Routes[index];
            menu.AppendAction("Enabled", _ => SetEnabled(index, !route.Enabled), Checked(route.Enabled));
            menu.AppendAction("Reset amount", _ => SetAmount(index, 0f));
            if (knobLayer >= 0 && ModTargets.IsPerLayer(route.Target))
            {
                menu.AppendSeparator();
                menu.AppendAction($"Layer {knobLayer + 1} only", _ => SetLayer(index, knobLayer), Checked(route.Layer == knobLayer));
                menu.AppendAction("All layers", _ => SetLayer(index, ModRoute.AllLayers), Checked(route.Layer < 0));
            }

            menu.AppendSeparator();
            menu.AppendAction("Remove", _ => Remove(index));
        }

        private static DropdownMenuAction.Status Checked(bool on) =>
            on ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;

        public string ChipText(int index)
        {
            var route = _recipe.Routes[index];
            var text = $"{Describe(route.Source, route.Target, route.Layer)}: {KnobElement.FormatAs(Format(route.Target), route.Amount)}";
            if (!route.Enabled) return text + " (off)";
            return IsSupported(route.Source, route.Target, _recipe.Random.Mode) ? text : text + " (no effect on this target)";
        }

        private void SetEnabled(int index, bool enabled)
        {
            RouteProperty(index, nameof(ModRoute.Enabled)).boolValue = enabled;
            _serialized.ApplyModifiedProperties();
        }

        private void SetLayer(int index, int layer)
        {
            var route = _recipe.Routes[index];
            if (route.Layer != layer && Find(route.Source, route.Target, layer) >= 0)
            {
                _status($"{Describe(route.Source, route.Target, layer)} already exists.");
                return;
            }

            RouteProperty(index, nameof(ModRoute.Layer)).intValue = layer;
            _serialized.ApplyModifiedProperties();
        }

        private void Remove(int index)
        {
            Undo.RecordObject(_recipe, "Remove Route");
            _recipe.Routes.RemoveAt(index);
            Commit();
        }

        private SerializedProperty RouteProperty(int index, string field) =>
            _serialized.FindProperty(nameof(SfxRecipe.Routes)).GetArrayElementAtIndex(index).FindPropertyRelative(field);

        private void Commit()
        {
            EditorUtility.SetDirty(_recipe);
            _serialized.Update();
            _routesChanged();
            Refresh();
        }

        // ── Arcs and chips ──────────────────────────────────────────────────────────

        public void Refresh()
        {
            _knobs.Clear();
            _root.Query<KnobElement>().ToList(_knobs);
            foreach (var knob in _knobs)
            {
                if (knob.ModTarget.HasValue) RefreshKnob(knob);
            }
        }

        private void RefreshKnob(KnobElement knob)
        {
            var target = knob.ModTarget.Value;
            var chips = knob.ModChips;
            var count = 0;
            knob.BeginModArcs();

            if (_recipe != null)
            {
                var routes = _recipe.Routes;
                for (var i = 0; i < routes.Count; i++)
                {
                    var route = routes[i];
                    if (route.Target != target) continue;
                    if (knob.ModLayer >= 0 && route.Layer >= 0 && route.Layer != knob.ModLayer) continue;

                    var color = SourceColor(route.Source);
                    var active = route.Enabled && IsSupported(route.Source, target, _recipe.Random.Mode);
                    if (active) knob.AddModArc(route.Amount, color, IsUnipolar(route.Source));

                    var chip = count < chips.childCount ? (ModChipElement)chips[count] : AddChip(chips);
                    chip.Set(i, knob.ModLayer, color, active, knob.ModLayer >= 0 && route.Layer < 0);
                    count++;
                }
            }

            while (chips.childCount > count) chips.RemoveAt(chips.childCount - 1);
            knob.EndModArcs();
        }

        private ModChipElement AddChip(VisualElement chips)
        {
            var chip = new ModChipElement(this);
            chips.Add(chip);
            return chip;
        }
    }
}
