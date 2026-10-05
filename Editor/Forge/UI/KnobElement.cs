using System;
using DataKeeper.Forge;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    public enum KnobScale
    {
        Linear,
        Log,
    }

    public enum KnobFormat
    {
        Number,
        Decibels,
        Hertz,
        Semitones,
        Milliseconds,
        Pan,
        Percent,
        Ratio,
        Octaves,
        Integer,
        Cents,
        Degrees,
    }

    [UxmlElement]
    public partial class KnobElement : BindableElement, INotifyValueChanged<float>
    {
        public const string UssClassName = "forge-knob";

        private const float StartAngle = 135f;
        private const float SweepAngle = 270f;
        private const float DragSensitivity = 0.005f;
        private const float FineFactor = 0.1f;
        private const int MaxModArcs = 8;

        private static readonly CustomStyleProperty<Color> TrackColorProperty = new("--knob-track-color");
        private static readonly CustomStyleProperty<Color> ValueColorProperty = new("--knob-value-color");
        private static readonly CustomStyleProperty<Color> BodyColorProperty = new("--knob-body-color");
        private static readonly CustomStyleProperty<Color> IndicatorColorProperty = new("--knob-indicator-color");

        private readonly Label _title;
        private readonly VisualElement _dial;
        private readonly Label _readout;
        private readonly ModArc[] _modArcs = new ModArc[MaxModArcs];

        private float _value;
        private float _min;
        private float _max = 1f;
        private KnobFormat _format;
        private bool _dragging;
        private bool _locked;
        private float _lastPointerY;
        private float _dragNormalized;
        private ModTarget? _modTarget;
        private VisualElement _modChips;
        private int _modArcCount;
        private int _modArcWrite;
        private bool _modArcsDirty;

        private Color _trackColor = new(0.08f, 0.08f, 0.09f);
        private Color _valueColor = new(1f, 0.57f, 0.19f);
        private Color _bodyColor = new(0.2f, 0.21f, 0.23f);
        private Color _indicatorColor = new(0.92f, 0.92f, 0.94f);

        [UxmlAttribute]
        public string Label
        {
            get => _title.text;
            set
            {
                _title.text = value;
                _title.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute]
        public float Min
        {
            get => _min;
            set { _min = value; Refresh(); }
        }

        [UxmlAttribute]
        public float Max
        {
            get => _max;
            set { _max = value; Refresh(); }
        }

        [UxmlAttribute] public float DefaultValue { get; set; }

        [UxmlAttribute] public KnobScale Scale { get; set; }

        [UxmlAttribute] public bool Bipolar { get; set; }

        [UxmlAttribute] public bool WholeNumbers { get; set; }

        [UxmlAttribute]
        public KnobFormat Format
        {
            get => _format;
            set { _format = value; Refresh(); }
        }

        public bool Lockable { get; set; }

        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                EnableInClassList(UssClassName + "--locked", value);
            }
        }

        public event Action<bool> LockToggled;

        public ModTarget? ModTarget
        {
            get => _modTarget;
            set
            {
                _modTarget = value;
                EnableInClassList(UssClassName + "--mod-target", value.HasValue);
                if (!value.HasValue || _modChips != null) return;

                _modChips = new VisualElement();
                _modChips.AddToClassList(UssClassName + "__mods");
                _dial.Add(_modChips);
            }
        }

        public int ModLayer { get; set; } = ModRoute.AllLayers;

        public VisualElement ModChips => _modChips;

        public float value
        {
            get => _value;
            set
            {
                var clamped = Mathf.Clamp(value, Mathf.Min(_min, _max), Mathf.Max(_min, _max));
                if (WholeNumbers) clamped = Mathf.Round(clamped);
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

        public KnobElement() : this(null, 0f, 1f, 0f)
        {
        }

        public KnobElement(string label, float min, float max, float defaultValue,
            KnobFormat format = KnobFormat.Number, KnobScale scale = KnobScale.Linear, bool bipolar = false)
        {
            AddToClassList(UssClassName);

            _title = new Label();
            _title.AddToClassList(UssClassName + "__title");
            Add(_title);

            _dial = new VisualElement();
            _dial.AddToClassList(UssClassName + "__dial");
            _dial.generateVisualContent += DrawDial;
            Add(_dial);

            _readout = new Label();
            _readout.AddToClassList(UssClassName + "__value");
            Add(_readout);

            Label = label;
            _min = min;
            _max = max;
            DefaultValue = defaultValue;
            _format = format;
            Scale = scale;
            Bipolar = bipolar;
            _value = defaultValue;
            Refresh();

            _dial.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _dial.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _dial.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _dial.RegisterCallback<PointerCaptureOutEvent>(_ => _dragging = false);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            this.AddManipulator(new ContextualMenuManipulator(PopulateContextMenu));
        }

        // Stores the raw value: binding may deliver it before Min/Max are configured, and
        // clamping here would destroy it. Clamping happens on user input and when drawing.
        public void SetValueWithoutNotify(float newValue)
        {
            _value = newValue;
            Refresh();
        }

        public void SetDropState(bool hovered, bool accepted)
        {
            EnableInClassList(UssClassName + "--drop", hovered && accepted);
            EnableInClassList(UssClassName + "--drop-refused", hovered && !accepted);
        }

        // Begin/Add/End compare against the previous set, so refreshing every knob on each
        // recipe change only repaints the ones whose routes actually changed.
        public void BeginModArcs()
        {
            _modArcWrite = 0;
            _modArcsDirty = false;
        }

        public void AddModArc(float amount, Color color, bool unipolar)
        {
            if (_modArcWrite >= MaxModArcs) return;

            var arc = new ModArc(amount, color, unipolar);
            if (_modArcWrite >= _modArcCount || !_modArcs[_modArcWrite].Equals(arc)) _modArcsDirty = true;
            _modArcs[_modArcWrite++] = arc;
        }

        public void EndModArcs()
        {
            if (_modArcWrite != _modArcCount) _modArcsDirty = true;
            _modArcCount = _modArcWrite;
            if (_modArcsDirty) _dial.MarkDirtyRepaint();
        }

        private void PopulateContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (Lockable) evt.menu.AppendAction(_locked ? "Unlock" : "Lock", _ => LockToggled?.Invoke(!_locked));
            evt.menu.AppendAction("Reset", _ => value = DefaultValue);
        }

        private void Refresh()
        {
            _readout.text = FormatValue(_value);
            _dial.MarkDirtyRepaint();
        }

        private float ToNormalized(float v)
        {
            v = Mathf.Clamp(v, Mathf.Min(_min, _max), Mathf.Max(_min, _max));
            if (Scale == KnobScale.Log && _min > 0f && _max > _min)
                return Mathf.Log(v / _min) / Mathf.Log(_max / _min);
            return Mathf.InverseLerp(_min, _max, v);
        }

        private float FromNormalized(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (Scale == KnobScale.Log && _min > 0f && _max > _min)
                return _min * Mathf.Pow(_max / _min, normalized);
            return Mathf.Lerp(_min, _max, normalized);
        }

        // Modulation amounts are octaves on log knobs and the knob's own unit otherwise.
        private float Offset(float v, float amount) =>
            Scale == KnobScale.Log && _min > 0f ? v * Mathf.Pow(2f, amount) : v + amount;

        // Whole-number knobs accumulate the drag: rounding each small step would undo it.
        private void Nudge(float normalizedDelta)
        {
            var from = WholeNumbers ? _dragNormalized : ToNormalized(_value);
            _dragNormalized = Mathf.Clamp01(from + normalizedDelta);
            value = FromNormalized(_dragNormalized);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            if (evt.clickCount == 2)
            {
                value = DefaultValue;
            }
            else
            {
                _dragging = true;
                _lastPointerY = evt.position.y;
                _dragNormalized = ToNormalized(_value);
                _dial.CapturePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging || !_dial.HasPointerCapture(evt.pointerId)) return;

            var delta = _lastPointerY - evt.position.y;
            _lastPointerY = evt.position.y;
            Nudge(delta * DragSensitivity * (evt.shiftKey ? FineFactor : 1f));
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_dial.HasPointerCapture(evt.pointerId)) return;

            _dragging = false;
            _dial.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(TrackColorProperty, out var track)) _trackColor = track;
            if (style.TryGetValue(ValueColorProperty, out var valueColor)) _valueColor = valueColor;
            if (style.TryGetValue(BodyColorProperty, out var body)) _bodyColor = body;
            if (style.TryGetValue(IndicatorColorProperty, out var indicator)) _indicatorColor = indicator;
            _dial.MarkDirtyRepaint();
        }

        private void DrawDial(MeshGenerationContext context)
        {
            var rect = _dial.contentRect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - 2f;
            if (radius <= 4f) return;

            var center = rect.center;
            var painter = context.painter2D;
            var normalized = ToNormalized(_value);

            painter.fillColor = _bodyColor;
            painter.BeginPath();
            painter.Arc(center, radius - 5f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = _trackColor;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(StartAngle), Angle.Degrees(StartAngle + SweepAngle));
            painter.Stroke();

            var origin = Bipolar ? ToNormalized(0f) : 0f;
            var from = StartAngle + SweepAngle * Mathf.Min(origin, normalized);
            var to = StartAngle + SweepAngle * Mathf.Max(origin, normalized);
            if (to - from > 0.5f)
            {
                painter.strokeColor = enabledInHierarchy ? _valueColor : _trackColor;
                painter.BeginPath();
                painter.Arc(center, radius, Angle.Degrees(from), Angle.Degrees(to));
                painter.Stroke();
            }

            DrawModArcs(painter, center, radius - 3.5f, normalized);

            var angle = (StartAngle + SweepAngle * normalized) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            painter.lineWidth = 2f;
            painter.strokeColor = _indicatorColor;
            painter.BeginPath();
            painter.MoveTo(center + direction * (radius * 0.3f));
            painter.LineTo(center + direction * (radius - 6f));
            painter.Stroke();
        }

        private void DrawModArcs(Painter2D painter, Vector2 center, float radius, float normalized)
        {
            if (!enabledInHierarchy) return;

            painter.lineWidth = 2f;
            for (var i = 0; i < _modArcCount; i++)
            {
                var arc = _modArcs[i];
                var high = ToNormalized(Offset(_value, arc.Amount));
                var low = arc.Unipolar ? normalized : ToNormalized(Offset(_value, -arc.Amount));
                var from = StartAngle + SweepAngle * Mathf.Min(low, high);
                var to = StartAngle + SweepAngle * Mathf.Max(low, high);

                painter.strokeColor = arc.Color;
                painter.fillColor = arc.Color;
                if (to - from > 0.5f)
                {
                    painter.BeginPath();
                    painter.Arc(center, radius, Angle.Degrees(from), Angle.Degrees(to));
                    painter.Stroke();
                }

                // Marks the +amount end, so a negative route reads as pushing the other way.
                var angle = (StartAngle + SweepAngle * high) * Mathf.Deg2Rad;
                painter.BeginPath();
                painter.Arc(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, 2f,
                    Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }
        }

        private string FormatValue(float v) => FormatAs(_format, v);

        public static string FormatAs(KnobFormat format, float v) => format switch
        {
            KnobFormat.Decibels => $"{v:0.0} dB",
            KnobFormat.Hertz => v >= 1000f ? $"{v / 1000f:0.00} kHz" : v < 10f ? $"{v:0.00} Hz" : $"{v:0} Hz",
            KnobFormat.Semitones => $"{v:+0.0;-0.0;0.0} st",
            KnobFormat.Milliseconds => v >= 1000f ? $"{v / 1000f:0.00} s" : $"{v:0} ms",
            KnobFormat.Pan => Mathf.Abs(v) < 0.005f ? "C" : v < 0f ? $"L{-v * 100f:0}" : $"R{v * 100f:0}",
            KnobFormat.Percent => $"{v * 100f:0}%",
            KnobFormat.Ratio => $"x{v:0.00}",
            KnobFormat.Octaves => $"{v:+0.00;-0.00;0.00} oct",
            KnobFormat.Integer => $"{v:0}",
            KnobFormat.Cents => $"{v:0} ct",
            KnobFormat.Degrees => $"{v * 360f:0}°",
            _ => $"{v:0.00}",
        };
    }

    internal readonly struct ModArc : IEquatable<ModArc>
    {
        public readonly float Amount;
        public readonly Color Color;
        public readonly bool Unipolar;

        public ModArc(float amount, Color color, bool unipolar)
        {
            Amount = amount;
            Color = color;
            Unipolar = unipolar;
        }

        public bool Equals(ModArc other) => Amount == other.Amount && Color == other.Color && Unipolar == other.Unipolar;
    }
}
