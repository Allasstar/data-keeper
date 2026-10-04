using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // One route on a knob. Lives in the dial's bottom gap and is reused across refreshes, so a
    // drag keeps its pointer capture while the recipe changes underneath it.
    public class ModChipElement : VisualElement
    {
        public const string UssClassName = "forge-mod-chip";

        private readonly ForgeModulation _modulation;
        private int _route;
        private int _knobLayer;
        private bool _dragging;
        private float _lastPointerY;

        public ModChipElement(ForgeModulation modulation)
        {
            _modulation = modulation;
            AddToClassList(UssClassName);
            ForgeHints.Set(this, ForgeHelp.Modulation, "Chips");

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
            RegisterCallback<PointerEnterEvent>(_ => tooltip = _modulation.ChipText(_route));
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                _modulation.PopulateChipMenu(_route, _knobLayer, evt.menu);
                // Keeps the knob's own Lock/Reset items out of a chip's menu.
                evt.StopPropagation();
            }));
        }

        public void Set(int route, int knobLayer, Color color, bool active, bool allLayers)
        {
            _route = route;
            _knobLayer = knobLayer;
            style.backgroundColor = allLayers ? Color.clear : color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
            style.borderLeftColor = color;
            style.borderRightColor = color;
            EnableInClassList(UssClassName + "--off", !active);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            if (evt.clickCount == 2)
            {
                _modulation.SetAmount(_route, 0f);
            }
            else
            {
                _dragging = true;
                _lastPointerY = evt.position.y;
                _modulation.BeginAmountEdit();
                this.CapturePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging || !this.HasPointerCapture(evt.pointerId)) return;

            var delta = _lastPointerY - evt.position.y;
            _lastPointerY = evt.position.y;
            if (delta != 0f) _modulation.NudgeAmount(_route, delta, evt.shiftKey);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;

            this.ReleasePointer(evt.pointerId);
            EndDrag();
            evt.StopPropagation();
        }

        private void EndDrag()
        {
            if (!_dragging) return;

            _dragging = false;
            _modulation.EndAmountEdit();
        }
    }
}
