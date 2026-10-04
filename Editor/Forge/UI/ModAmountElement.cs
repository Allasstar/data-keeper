using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // A bipolar horizontal bar: the fill grows from the centre towards ±Max.
    public class ModAmountElement : BindableElement, INotifyValueChanged<float>
    {
        public const string UssClassName = "forge-amount";

        private const float FineFactor = 0.1f;

        private readonly VisualElement _fill;
        private readonly Label _label;

        private float _value;
        private float _max = 1f;
        private KnobFormat _format;
        private bool _dragging;
        private float _lastPointerX;

        public float Max
        {
            get => _max;
            set { _max = Mathf.Max(value, 1e-4f); Refresh(); }
        }

        public KnobFormat Format
        {
            get => _format;
            set { _format = value; Refresh(); }
        }

        public Color Color
        {
            set => _fill.style.backgroundColor = value;
        }

        public float value
        {
            get => _value;
            set
            {
                var clamped = Mathf.Clamp(value, -_max, _max);
                if (clamped == _value) return;

                if (panel == null)
                {
                    SetValueWithoutNotify(clamped);
                    return;
                }

                using var evt = ChangeEvent<float>.GetPooled(_value, clamped);
                evt.target = this;
                SetValueWithoutNotify(clamped);
                SendEvent(evt);
            }
        }

        public ModAmountElement()
        {
            AddToClassList(UssClassName);

            var center = new VisualElement { pickingMode = PickingMode.Ignore };
            center.AddToClassList(UssClassName + "__center");
            Add(center);

            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList(UssClassName + "__fill");
            Add(_fill);

            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.AddToClassList(UssClassName + "__label");
            Add(_label);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => _dragging = false);
        }

        // Stores the raw value, like KnobElement: binding can deliver it before Max is set.
        public void SetValueWithoutNotify(float newValue)
        {
            _value = newValue;
            Refresh();
        }

        private void Refresh()
        {
            var half = Mathf.Clamp(_value / _max, -1f, 1f) * 50f;
            _fill.style.left = Length.Percent(50f + Mathf.Min(half, 0f));
            _fill.style.width = Length.Percent(Mathf.Abs(half));
            _label.text = KnobElement.FormatAs(_format, _value);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            if (evt.clickCount == 2)
            {
                value = 0f;
            }
            else
            {
                _dragging = true;
                _lastPointerX = evt.position.x;
                this.CapturePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging || !this.HasPointerCapture(evt.pointerId)) return;

            var width = Mathf.Max(contentRect.width, 1f);
            var delta = evt.position.x - _lastPointerX;
            _lastPointerX = evt.position.x;
            value = _value + delta / width * 2f * _max * (evt.shiftKey ? FineFactor : 1f);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;

            _dragging = false;
            this.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }
}
