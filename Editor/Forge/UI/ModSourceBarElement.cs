using System;
using DataKeeper.Forge;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Modulation sources as drag chips, on every page, since the macros and the knobs they
    // drive are never on screen together.
    public class ModSourceBarElement : VisualElement
    {
        public const string UssClassName = "forge-mod-bar";
        public const string DraggingClassName = "forge-mod-dragging";

        private const float DragThreshold = 3f;

        private readonly ForgeModulation _modulation;
        private readonly Action<string> _status;
        private readonly Label _ghost;

        private VisualElement _pressedChip;
        private ModSource _source;
        private Vector2 _downPosition;
        private bool _dragging;
        private KnobElement _hovered;

        public VisualElement Ghost => _ghost;

        public event Action<ModSource> Clicked;

        public ModSourceBarElement(ForgeModulation modulation, Action<string> status)
        {
            _modulation = modulation;
            _status = status;
            AddToClassList(UssClassName);

            var title = new Label("MOD");
            title.AddToClassList(UssClassName + "__title");
            Add(title);

            // Chips sit in one box per group, so a narrow window wraps between groups only.
            VisualElement box = null;
            var group = -1;
            foreach (var source in ForgeModulation.SourceOrder)
            {
                if (Group(source) != group)
                {
                    group = Group(source);
                    box = new VisualElement();
                    box.AddToClassList(UssClassName + "__group");
                    Add(box);
                }

                box.Add(Chip(source));
            }

            _ghost = new Label { pickingMode = PickingMode.Ignore };
            _ghost.AddToClassList(UssClassName + "__ghost");
            _ghost.style.display = DisplayStyle.None;
        }

        private static int Group(ModSource source) => source switch
        {
            ModSource.Lfo or ModSource.Lfo2 or ModSource.Lfo3 => 1,
            ModSource.Envelope or ModSource.Env2 or ModSource.Env3 => 2,
            ModSource.Random or ModSource.Random2 or ModSource.Random3 => 3,
            _ => 0,
        };

        private VisualElement Chip(ModSource source)
        {
            var chip = new VisualElement();
            chip.AddToClassList(UssClassName + "__chip");

            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList(UssClassName + "__dot");
            dot.style.backgroundColor = ForgeModulation.SourceColor(source);
            chip.Add(dot);

            var label = new Label(ForgeModulation.SourceName(source)) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(UssClassName + "__name");
            chip.Add(label);

            chip.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt, chip, source));
            chip.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            chip.RegisterCallback<PointerUpEvent>(OnPointerUp);
            chip.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
            return ForgeHints.Set(chip, ForgeHelp.Modulation, "Source bar");
        }

        private void OnPointerDown(PointerDownEvent evt, VisualElement chip, ModSource source)
        {
            if (evt.button != 0) return;

            _pressedChip = chip;
            _source = source;
            _downPosition = evt.position;
            chip.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_pressedChip == null || !_pressedChip.HasPointerCapture(evt.pointerId)) return;

            var position = (Vector2)evt.position;
            if (!_dragging)
            {
                if ((position - _downPosition).sqrMagnitude < DragThreshold * DragThreshold) return;
                BeginDrag();
            }

            var local = _ghost.parent.WorldToLocal(position);
            _ghost.style.left = local.x + 12f;
            _ghost.style.top = local.y + 4f;

            var knob = KnobAt(position);
            var accepted = _modulation.CanDrop(_source, knob, evt.altKey, out var message);
            if (knob != _hovered)
            {
                _hovered?.SetDropState(false, false);
                _hovered = knob;
            }

            _hovered?.SetDropState(true, accepted);
            _ghost.EnableInClassList(UssClassName + "__ghost--refused", knob != null && !accepted);
            _status(message);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_pressedChip == null || !_pressedChip.HasPointerCapture(evt.pointerId)) return;

            var clicked = !_dragging;
            var knob = _dragging ? KnobAt(evt.position) : null;
            _pressedChip.ReleasePointer(evt.pointerId);
            if (knob != null) _modulation.Drop(_source, knob, evt.altKey);
            else if (clicked)
            {
                _status($"Drag {ForgeModulation.SourceName(_source)} onto a knob to add a route.");
                Clicked?.Invoke(_source);
            }

            evt.StopPropagation();
        }

        private void BeginDrag()
        {
            _dragging = true;
            _ghost.text = ForgeModulation.SourceName(_source);
            _ghost.style.borderLeftColor = ForgeModulation.SourceColor(_source);
            _ghost.style.display = DisplayStyle.Flex;
            _ghost.BringToFront();
            _ghost.parent.AddToClassList(DraggingClassName);
        }

        private void EndDrag()
        {
            _pressedChip = null;
            if (!_dragging) return;

            _dragging = false;
            _ghost.style.display = DisplayStyle.None;
            _ghost.parent.RemoveFromClassList(DraggingClassName);
            _hovered?.SetDropState(false, false);
            _hovered = null;
        }

        private KnobElement KnobAt(Vector2 position)
        {
            for (var element = panel.Pick(position); element != null; element = element.parent)
            {
                if (element is KnobElement knob) return knob.ModTarget.HasValue ? knob : null;
            }

            return null;
        }
    }
}
